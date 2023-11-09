namespace RadarLights.Services.HomeAssistant;

public class HADeviceConfig
{
    public required string Name { get; set; }
    public required string Manufacturer { get; set; }
    public required List<string[]> Connections { get; set; }
}
