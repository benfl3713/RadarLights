using BdfFontParser;
using RpiLedMatrix;
using Color = RpiLedMatrix.Color;

namespace RadarLights.Services;

public class OverheadAlertService
{
    private readonly ILogger<OverheadAlertService> _logger;
    private readonly PlaneRenderService _planeRenderService;
    private readonly ILedMatrix _matrix;
    private readonly List<AirplaneDataService.AircraftResponse.Aircraft> _nearbyAircraft = new List<AirplaneDataService.AircraftResponse.Aircraft>();
    private RadarSettings _radarSettings;
    
    public OverheadAlertService(ILogger<OverheadAlertService> logger, PlaneRenderService planeRenderService, ILedMatrix matrix)
    {
        _logger = logger;
        _planeRenderService = planeRenderService;
        _matrix = matrix;
        _radarSettings = RadarSettings.Load();
        RadarSettings.SettingsUpdated += (sender, _) => _radarSettings = (RadarSettings)sender!;
    }

    public void Process(List<AirplaneDataService.AircraftResponse.Aircraft> aircrafts)
    {
        if (!_radarSettings.Alerts)
            return;
        
        foreach (AirplaneDataService.AircraftResponse.Aircraft aircraft in aircrafts)
        {
            ProcessAircraft(aircraft);
        }
    }
    
    private void ProcessAircraft(AirplaneDataService.AircraftResponse.Aircraft aircraft)
    {
        if (string.IsNullOrEmpty(aircraft.Flight?.Trim()))
            return;

        if (IsOverhead(aircraft, out double kmAway))
        {
            if (AddUpdateNearbyAircraft(aircraft))
            {
                _logger.LogInformation("New Aircraft Detected: {Aircraft} {KmAway}km away at {Feet} ft", aircraft, Convert.ToInt32(kmAway),
                    aircraft.AltBaro);
                
                SendAlert(aircraft, Convert.ToInt32(kmAway));
            }
        }
        else if (_nearbyAircraft.Any(a => a.Flight == aircraft.Flight))
        {
            _nearbyAircraft.RemoveAt(_nearbyAircraft.FindIndex(a => a.Flight == aircraft.Flight));
            _logger.LogInformation("Aircraft Gone out of range: {aircraft}", aircraft);
        }
    }

    private void SendAlert(AirplaneDataService.AircraftResponse.Aircraft aircraft, int distance)
    {
        _logger.LogInformation("Aircraft Overhead: {Flight}", aircraft.Flight);
        _planeRenderService.Pause();
        Thread.Sleep(200);
        _matrix.Clear();
        _matrix.DrawCircle(63, 63, 60, new Color(200, 0, 0));
        _matrix.DrawText(new BdfFont("./Fonts/8x13.bdf"), 30, 63, new Color(200, 0, 0), aircraft.Flight ?? string.Empty);
        _matrix.DrawText(new BdfFont("./Fonts/8x13.bdf"), 15, 63 + 15, new Color(200, 0, 0), "Overhead !!!");
        _matrix.DrawText(new BdfFont("./Fonts/8x13.bdf"), 15, 63 + 30, new Color(200, 0, 0), $"at FL{aircraft.AltBaro / 100:000}");
        _matrix.Update();
        Thread.Sleep(10000);
        _planeRenderService.Unpause();
    }

    private bool AddUpdateNearbyAircraft(AirplaneDataService.AircraftResponse.Aircraft aircraft)
    {
        if (_nearbyAircraft.Any(a => a.Flight == aircraft.Flight))
        {
            _nearbyAircraft[_nearbyAircraft.FindIndex(a => a.Flight == aircraft.Flight)] = aircraft;
            return false;
        }

        _nearbyAircraft.Add(aircraft);
        return true;
    }

    private bool IsOverhead(AirplaneDataService.AircraftResponse.Aircraft aircraft, out double kmAway)
    {
        if (!aircraft.Lat.HasValue || !aircraft.Lon.HasValue)
        {
            kmAway = 0;
            return false;
        }
        
        double homeLat = 52.340012;
        double homeLon = -1.584784;
        int radiusOfEarthKm = 6371;
        double dLat = ToRadians(aircraft.Lat.Value - homeLat);
        double dLon = ToRadians(aircraft.Lon.Value - homeLon);
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(homeLat)) * Math.Cos(ToRadians(aircraft.Lat.Value)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        kmAway = radiusOfEarthKm * c; // Distance in km
        return kmAway < IsOverheadRangeKm;
    }

    private const int IsOverheadRangeKm = 5;
    
    private double ToRadians(double deg)
    {
        return deg * (Math.PI / 180);
    }
}
