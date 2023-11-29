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
            "EXS" => new Color(200, 0, 0), // Jet2
            "SWR" => new Color(150, 0, 0), // Swiss
            "KLM" => new Color(0, 100, 100), // KLM
            "EZY" or "EJU" or "EZS" => new Color(255, 25, 0), // easyJet
            "RYR" or "RUK" => new Color(0, 0, 200), // Ryanair
            "AFR" => new Color(0, 0, 255), // Air france
            "EIN" or "EUK" => new Color(0, 200, 0), // Aer Lingus
            "TOM" or "TUI" => new Color(0, 60, 120), // TUI
            "UAE" => new Color(110, 130, 0), // Emirates
            "ETD" => new Color(200, 200, 0), // Etihad
            "BRU" or "BEL" => new Color(0, 20, 150), // Brussels
            "TAR" => new Color(200, 10, 0), //Tunisair
            "THY" => new Color(150, 0, 0), //Turkish
            "BAW" or "SHT" => new Color(0, 50, 180),
            "UAL" => new Color(0, 20, 180), // United
            "ACA" => new Color(200, 10, 0), //Air Canada
            "VIR" => new Color(230, 0, 0), //Virgin
            "LOG" => new Color(30, 10, 230), //loganair
            _ => new Color(200, 200, 200)
        };

        // if (colour.Equals(new Color(200, 200, 200)))
        //     Console.WriteLine($"Unknown Airline Code: {airline}");
        
        return colour;
    }
}
