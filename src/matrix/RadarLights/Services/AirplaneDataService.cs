using System.Text.Json;
using System.Text.Json.Serialization;
using Geo;
using Geolocation;
using Color = RpiLedMatrix.Color;
using Coordinate = Geolocation.Coordinate;

namespace RadarLights.Services;

public class AirplaneDataService : BackgroundService
{
    private readonly ILogger<AirplaneDataService> _logger;
    private readonly HttpClient _client;
    private readonly AirplaneDataCache _cache;
    private readonly OverheadAlertService _overheadAlertService;

    private static readonly Coordinate EGBB = new Coordinate(52.4547149, -1.7411949);
    private static readonly Coordinate EGCC = new Coordinate(53.354003, -2.274456);
    private static readonly Coordinate EGLL = new Coordinate(51.476997, -0.462795);
    private Envelope _envelope = null!;
    private double _totalLon;
    private double _totalLat;
    private readonly AppConfig _config;
    private RadarSettings _radarSettings;
    private JsonElement _geoJson;
    private List<(double lon, double lat)>? _countryPoints;

    public AirplaneDataService(ILogger<AirplaneDataService> logger, HttpClient client, AppConfig config, AirplaneDataCache cache, OverheadAlertService overheadAlertService)
    {
        _config = config;
        _logger = logger;
        _client = client;
        _cache = cache;
        _overheadAlertService = overheadAlertService;
        _radarSettings = RadarSettings.Load();
        RadarSettings.SettingsUpdated += async (sender, _) =>
        {
            _radarSettings = (RadarSettings)sender!;
            Setup(_radarSettings.Range);
            await UpdateAirplaneData();
        };
        
        
        Setup(_radarSettings.Range);
    }

    private void Setup(int range)
    {
        var boundaries = new CoordinateBoundaries(new Coordinate(_config.HomeLatitude, _config.HomeLongitude), range, DistanceUnit.Miles);
        _envelope = new Envelope(boundaries.MinLatitude, boundaries.MinLongitude, boundaries.MaxLatitude, boundaries.MaxLongitude);

        _totalLon = GeoCalculator.GetDistance(new Coordinate(_envelope.MinLat, _envelope.MinLon), new Coordinate(_envelope.MinLat, _envelope.MaxLon));
        _totalLat = GeoCalculator.GetDistance(new Coordinate(_envelope.MinLat, _envelope.MinLon), new Coordinate(_envelope.MaxLat, _envelope.MinLon));
        SetupCountryBorder();
        LoadHistory();
    }

    private async void SetupCountryBorder()
    {
        if (_countryPoints == null)
        {
            // var geoJsonRes = await _client.GetAsync("https://geodata.ucdavis.edu/gadm/gadm4.1/json/gadm41_GBR_1.json");
            // _geoJson = await geoJsonRes.Content.ReadFromJsonAsync<JsonElement>();

            var jsonString = await File.ReadAllTextAsync("./LocationJson/gadm41_GBR_1.json");
            _geoJson = JsonSerializer.Deserialize<JsonElement>(jsonString);

            List<double> points = new List<double>();

            foreach (JsonElement element in _geoJson.GetProperty("features").EnumerateArray())
            {
                TraverseGeoJson(ref points, element.GetProperty("geometry").GetProperty("coordinates"));
            }

            // combine points in sets of 2
            _countryPoints = points.Select((p, i) => new { p, i }).GroupBy(p => p.i / 2).Select(p => ( p.First().p, p.Last().p )).ToList();
        }

        var fitInPoints = _countryPoints.Where(p => _envelope.Contains(new Geo.Coordinate(p.lat, p.lon))).ToList();

        var matrixPoints = fitInPoints.Select(p => CalculateMatrixPosition(p.lat, p.lon)).Distinct().ToList();
        
        _cache.Data.FixedPixels = matrixPoints.Select(p => (p.mLon, p.mLat, new Color(26, 130, 45) / 2)).ToList();
    }

    private void TraverseGeoJson(ref List<double> points, JsonElement jsonElement)
    {
        if (jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement element in jsonElement.EnumerateArray())
            {
                TraverseGeoJson(ref points, element);
            }
        }
        else if (jsonElement.ValueKind == JsonValueKind.Number)
        {
            points.Add((jsonElement.GetDouble()));
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SetupRefreshHistory(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_radarSettings.Enabled)
                    await UpdateAirplaneData();
            }
            catch (JsonException)
            {
                // ignore as it happens when the json file is updating
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Occured when Updating Airplane Data: {ErrorMessage}", ex.Message);
            }
            await Task.Delay(1500, stoppingToken);
        }
    }

    private async void SetupRefreshHistory(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            _logger.LogInformation("Refreshing History");
            try
            {
                LoadHistory();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to refresh history");
            }
        }
    }

    public async Task UpdateAirplaneData()
    {
        HttpResponseMessage result = await _client.GetAsync(new Uri(new Uri(_config.PiAwareServer), "data/aircraft.json"));
        AircraftResponse? data = await result.Content.ReadFromJsonAsync<AircraftResponse>();

        var aircraft = data?.aircraft.Where(p =>
        {
            if (!p.Lat.HasValue || !p.Lon.HasValue)
                return false;

            return _envelope.Contains(new Geo.Coordinate(p.Lat.Value, p.Lon.Value));
        }).ToList() ?? new List<AircraftResponse.Aircraft>();

        _overheadAlertService.Process(aircraft);

        List<MatrixAircraft> matrixAircraft = new List<MatrixAircraft>();

        foreach (AircraftResponse.Aircraft aircraft1 in aircraft)
        {
            var ma = GetMatrixAircraft(aircraft1);
            matrixAircraft.Add(ma);
            UpdateHistory(ma);
        }

        _logger.LogDebug("Found {PlaneCount} Planes", matrixAircraft.Count);
        _cache.Data.Aircraft = matrixAircraft;
        _cache.Data.ExtraPixels = GetExtraPixels();
    }

    private void UpdateHistory(MatrixAircraft ma)
    {
        if (string.IsNullOrEmpty(ma.Flight))
            return;
        
        if (_cache.Data.History.ContainsKey(ma.Flight))
        {
            if (_cache.Data.History[ma.Flight].Last().X != ma.X && _cache.Data.History[ma.Flight].Last().Y != ma.Y)
                _cache.Data.History[ma.Flight].Add(ma);
        }
        else
        {
            _cache.Data.History.Add(ma.Flight, new List<MatrixAircraft> { ma });
        }
    }

    public async void LoadHistory()
    {
        List<MatrixAircraft> matrixHistoryAircraft = new List<MatrixAircraft>();

        for (int i = 0; i < 120; i++)
        {
            try
            {
                HttpResponseMessage result = await _client.GetAsync(new Uri(new Uri(_config.PiAwareServer), $"data/history_{i}.json"));
                AircraftResponse? data = await result.Content.ReadFromJsonAsync<AircraftResponse>();

                var aircraft = data?.aircraft.Where(p =>
                {
                    if (!p.Lat.HasValue || !p.Lon.HasValue || string.IsNullOrEmpty(p.Flight))
                        return false;

                    return _envelope.Contains(new Geo.Coordinate(p.Lat.Value, p.Lon.Value));
                }).ToList() ?? new List<AircraftResponse.Aircraft>();

                _overheadAlertService.Process(aircraft);

                foreach (AircraftResponse.Aircraft aircraft1 in aircraft)
                {
                    matrixHistoryAircraft.Add(GetMatrixAircraft(aircraft1));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to load history {HistoryNumber}: {ErrorMessage}", i, e.Message);
            }
        }

        Dictionary<string, List<MatrixAircraft>> history = matrixHistoryAircraft.GroupBy(p => p.Flight)
            .ToDictionary(a => a.Key!, a => a.Distinct(new MatrixAircraftCoordinateComparer()).ToList());
        _cache.Data.History = history;
    }

    public MatrixAircraft GetMatrixAircraft(AircraftResponse.Aircraft aircraft)
    {
        (int mLon, int mLat) = CalculateMatrixPosition(aircraft.Lat!.Value, aircraft.Lon!.Value);

        return new MatrixAircraft
        {
            X = mLon,
            Y = mLat,
            Altitude = aircraft.AltBaro ?? aircraft.AltGeom ?? 0,
            Track = aircraft.Track,
            Flight = aircraft.Flight
        };
    }

    public List<(int x, int y, Color color)> GetExtraPixels()
    {
        var pixels = new List<(int x, int y, Color color)>();
        AddAirport(pixels, EGBB, new Color(250, 250, 250));
        AddAirport(pixels, EGCC, new Color(250, 250, 250), true);
        AddAirport(pixels, EGLL, new Color(250, 250, 250), true);

        return pixels;
    }

    private void AddAirport(List<(int x, int y, Color color)> pixels, Coordinate airport, Color color, bool horizonal = false)
    {
        var loc = CalculateMatrixPosition(airport.Latitude, airport.Longitude);
        pixels.Add((loc.mLon, loc.mLat, color));
        if (horizonal)
        {
            pixels.Add((loc.mLon + 1, loc.mLat, color));
            pixels.Add((loc.mLon - 1, loc.mLat, color));
        }
        else
        {
            pixels.Add((loc.mLon, loc.mLat + 1, color));
            pixels.Add((loc.mLon, loc.mLat - 1, color));
        }
    }

    private (int mLon, int mLat) CalculateMatrixPosition(double lat, double lon)
    {
        var longLength = GeoCalculator.GetDistance(new Coordinate(_envelope.MinLat, lon), new Coordinate(_envelope.MinLat, _envelope.MinLon));

        var latLength = GeoCalculator.GetDistance(new Coordinate(lat, _envelope.MinLon), new Coordinate(_envelope.MinLat, _envelope.MinLon));


        var mLon = (int)Math.Round(Math.Abs(longLength) / Math.Abs(_totalLon) * (_config.ColumnLength - 1));
        var mLat = _config.RowLength - 1 - (int)Math.Round(Math.Abs(latLength) / Math.Abs(_totalLat) * (_config.RowLength - 1));
        return (mLon, mLat);
    }

    public class AircraftResponse
    {
        public required List<Aircraft> aircraft { get; set; }

        public class Aircraft
        {
            public double? Lat { get; set; }
            public double? Lon { get; set; }
            [JsonPropertyName("alt_baro")]
            public int? AltBaro { get; set; }

            [JsonPropertyName("alt_geom")] 
            public int? AltGeom { get; set; }
            public double? Track { get; set; }
            public string? Flight { get; set; }
        }
    }
}
