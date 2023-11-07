using Color = RpiLedMatrix.Color;

namespace RadarLights.Services;

public class AirplaneColourService
{
    private RadarSettings _radarSettings;

    public AirplaneColourService()
    {
        _radarSettings = RadarSettings.Load();
        RadarSettings.SettingsUpdated += (sender, _) => _radarSettings = (RadarSettings)sender!;
    }
    
    public Color GetColour(MatrixAircraft aircraft)
    {
        return _radarSettings.ColourMode switch
        {
            RadarSettings.ColourModes.Altitude => GetAltitudeColour(aircraft),
            RadarSettings.ColourModes.Airline => GetAirlineColour(aircraft),
            _ => throw new ArgumentOutOfRangeException(nameof(_radarSettings.ColourMode))
        };
    }

    private Color GetAltitudeColour(MatrixAircraft aircraft)
    {
        return aircraft.Altitude switch
        {
            < 3000 => new Color(150, 0, 0),
            < 6000 => new Color(130, 130, 0),
            < 10_000 => new Color(0, 200, 0),
            < 20_000 => new Color(0, 120, 180),
            < 30_000 => new Color(0, 0, 200),
            _ => new Color(120, 0, 170)
        };
    }

    private Color GetAirlineColour(MatrixAircraft aircraft)
    {
        if (string.IsNullOrEmpty(aircraft.Flight))
            return new Color(200, 200, 200);

        string airline = aircraft.Flight.Substring(0, 3);

        var colour =  airline switch
        {
            "EXS" or "SWR" => new Color(200, 0, 0),
            "KLM" => new Color(0, 100, 100),
            "EZY" => new Color(255, 25, 0),
            "RYR" or "AFR" => new Color(0, 0, 255),
            "EIN" => new Color(0, 200, 0),
            "TOM" or "TUI" => new Color(0, 60, 120),
            "UAE" => new Color(110, 130, 0),
            _ => new Color(200, 200, 200)
        };

        // if (colour.Equals(new Color(200, 200, 200)))
        //     Console.WriteLine($"Unknown Airline Code: {airline}");
        
        return colour;
    }
}
