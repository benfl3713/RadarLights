namespace RadarLights.Services.HomeAssistant;

public class HAEntityConfig
{
    public string? DeviceClass { get; set; }
    public HADeviceConfig? Device { get; set; }
    public string Name { get; set; }
    public string? Icon { get; set; }
    public string? StateTopic { get; set; }
    public string? StateClass { get; set; }

    public string? CommandTopic { get; set; }
    public string? UniqueId { get; set; }
    public string? UnitOfMeasurement { get; set; }
    public string? ValueTemplate { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
}
