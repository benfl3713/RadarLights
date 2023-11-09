using System.Net.NetworkInformation;
using System.Text.Json;
using JorgeSerrano.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using RadarLights.Services.HomeAssistant;

namespace RadarLights.Services;

public class HomeAssistantService : BackgroundService
{
    private readonly AppConfig.MqttConfig _mqttConfig;
    private readonly ILogger<HomeAssistantService> _logger;
    private readonly MqttPublisher _publisher;
    private RadarSettings _radarSettings;

    public HomeAssistantService(ILogger<HomeAssistantService> logger, AppConfig config, MqttPublisher publisher)
    {
        _mqttConfig = config.Mqtt;
        _logger = logger;
        _publisher = publisher;
        _radarSettings = RadarSettings.Load();
        RadarSettings.SettingsUpdated += async (sender, _) =>
        {
            _radarSettings = (RadarSettings)sender!;
            await SendDeviceState();
        };
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SetupDeviceConfig();
        await SetupListeners();
        await SendDeviceState();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            try
            {
                await SetupDeviceConfig();
                await SendDeviceState();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to update home assistant");
            }
        }
    }

    private async Task SendDeviceState()
    {
        await _publisher.SendState($"{_mqttConfig.TopicPrefix}/switch/{_mqttConfig.UniqueId}/enabled/state", _radarSettings.Enabled ? "ON" : "OFF");
    }

    private async Task SetupDeviceConfig()
    {
        var config = new HAEntityConfig
        {
            Name = "Enabled",
            StateTopic = $"{_mqttConfig.TopicPrefix}/switch/{_mqttConfig.UniqueId}/enabled/state",
            CommandTopic = $"{_mqttConfig.TopicPrefix}/switch/{_mqttConfig.UniqueId}/enabled/set",
            UniqueId = _mqttConfig.UniqueId + "_enabled",
            Icon = "mdi:airplane",
            Device = new HADeviceConfig
            {
                Name = _mqttConfig.DeviceName,
                Manufacturer = "benfl3713",
                Connections = new List<string[]>
                {
                    new [] {"mac", GetDefaultMacAddress()}
                }
            }
        };

        Console.WriteLine($"Sending config: {JsonSerializer.Serialize(config, new JsonSerializerOptions{PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()})}");

        await _publisher.SendConfiguration(config, $"{_mqttConfig.TopicPrefix}/switch/{_mqttConfig.UniqueId}/enabled/config");
    }

    private async Task SetupListeners()
    {
        var client = await MqttPublisher.GetMqttClient(_mqttConfig.Server);
        client.ApplicationMessageReceivedAsync += args =>
        {
            Console.WriteLine(args.ApplicationMessage.ConvertPayloadToString());
            if (args.ApplicationMessage.Topic == $"{_mqttConfig.TopicPrefix}/switch/{_mqttConfig.UniqueId}/enabled/set")
            {
                _radarSettings.Enabled = args.ApplicationMessage.ConvertPayloadToString() == "ON";
                _radarSettings.Save();
            }

            return Task.CompletedTask;
        };

        await client.SubscribeAsync(new MqttClientSubscribeOptions
        {
            TopicFilters = new List<MqttTopicFilter>
            {
                new MqttTopicFilter
                {
                    Topic = $"{_mqttConfig.TopicPrefix}/switch/{_mqttConfig.UniqueId}/enabled/set"
                }
            }
        });
    }

    public string GetDefaultMacAddress()
    {
        Dictionary<string, long> macAddresses = new Dictionary<string, long>();
        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus == OperationalStatus.Up)
                macAddresses[nic.GetPhysicalAddress().ToString()] = nic.GetIPStatistics().BytesSent + nic.GetIPStatistics().BytesReceived;
        }

        long maxValue = 0;
        string mac = "";
        foreach (KeyValuePair<string, long> pair in macAddresses)
        {
            if (pair.Value > maxValue)
            {
                mac = pair.Key;
                maxValue = pair.Value;
            }
        }

        return mac;
    }
}
