using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using RadarLights;
using RadarLights.Services;

namespace RadarLightsTests;

public class AirplaneDataServiceTests
{
    [Test]
    public async Task Test()
    {
        AirplaneDataService service = new AirplaneDataService(new Logger<AirplaneDataService>(new LoggerFactory()), new HttpClient(), new AppConfig(), new AirplaneDataCache(), new Mock<OverheadAlertService>().Object);
        await service.UpdateAirplaneData();
    }
    //
    // [Test]
    // public void Test2()
    // {
    //     AirplaneDataService service = new AirplaneDataService(new Logger<AirplaneDataService>(new LoggerFactory()), new HttpClient(), new OptionsWrapper<AppConfig>(new AppConfig()), new AirplaneDataCache());
    //     var result = service.GetMatrixAircraft(new AirplaneDataService.AircraftResponse.Aircraft
    //     {
    //         Lat = 52.576690,
    //         Lon = -1.763302
    //     });
    // }

    [Test]
    public void Test1()
    {
        SetupCountryBorder();
    }

    private async void SetupCountryBorder()
    {
        var geoJsonRes = await new HttpClient().GetAsync("https://geodata.ucdavis.edu/gadm/gadm4.1/json/gadm41_GBR_1.json");
        var _geoJson = await geoJsonRes.Content.ReadFromJsonAsync<JsonElement>();

        List<double> points = new List<double>();

        
        
        foreach (JsonElement element in _geoJson.GetProperty("features").EnumerateArray())
        {
            TraverseGeoJson(ref points, element.GetProperty("geometry").GetProperty("coordinates"));
        }

        // combine points in sets of 2
        var _countryPoints = points.Select((p, i) => new { p, i }).GroupBy(p => p.i / 2).Select(p => (p.First().p, p.Last().p)).ToList();
        //var fitInPoints = _countryPoints.Where(p => _envelope.Contains(new Geo.Coordinate(p.lat, p.lon))).ToList();
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
}
