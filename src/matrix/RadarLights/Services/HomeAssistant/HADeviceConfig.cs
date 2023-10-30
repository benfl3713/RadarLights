namespace RadarLights.Services.HomeAssistant;

public class HADeviceConfig
{
    public string Name { get; set; }
    public string Manufacturer { get; set; }
    public List<string[]> Connections { get; set; }
}
