using System.Text.Json;

namespace RadarLights;

public class RadarSettings
{
    public bool Enabled { get; set; } = true;
    public int Range { get; set; } = 100;
    public ColourModes ColourMode { get; set; } = ColourModes.Altitude;
    public AirplaneIcons AirplaneIcon { get; set; } = AirplaneIcons.Dot;
    public bool Alerts { get; set; }
    public bool Trails { get; set; }
    public bool ShowAltitudeKey { get; set; } = true;
    public bool ShowClock { get; set; }

    public static RadarSettings Load()
    {
        // load from config.json
        if (File.Exists("config.json"))
        {
            return JsonSerializer.Deserialize<RadarSettings>(File.ReadAllText("config.json"))!;
        }
        
        // create a new config file
        var settings = new RadarSettings();
        try
        {
            File.WriteAllText("config.json", JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return settings;
    }
    
    public void Save()
    {
        SettingsUpdated?.Invoke(this, EventArgs.Empty);
        File.WriteAllText("config.json", JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static event EventHandler? SettingsUpdated; 

    public enum ColourModes
    {
        Altitude,
        Airline
    }

    public enum AirplaneIcons
    {
        Dot,
        Diamond,
        Arrow
    }
}
