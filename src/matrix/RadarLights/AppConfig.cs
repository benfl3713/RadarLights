using System.Reflection;

namespace RadarLights;

public record AppConfig
{
    public string PiAwareServer { get; init; } = "http://raspberrypi:8080";
    public int RowLength { get; init; } = 128;
    public int ColumnLength { get; init; } = 128;
    public double HomeLatitude { get; init; }
    public double HomeLongitude { get; init; }
    public string RadarSpinnerColour { get; init; } = "0, 150, 0";
    public MqttConfig Mqtt { get; } = new MqttConfig();

    public void Log()
    {
        // log each property
        foreach (PropertyInfo propertyInfo in GetType().GetProperties())
        {
            Serilog.Log.Logger.Information("Config: {Property} = {Value}", propertyInfo.Name, propertyInfo.GetValue(this, null));
        }
    }

    public record MqttConfig
    {
        public bool Enabled { get; init; } = false;
        public string Server { get; init; }
        public string TopicPrefix { get; init; } = "homeassistant";
        public string DeviceName { get; init; } = "Plane Radar";
        public string UniqueId { get; init; } = "led_plane_radar";
    }
}
