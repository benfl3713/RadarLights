using System.Text.Json;
using System.Text.Json.Serialization;
using JorgeSerrano.Json;
using MQTTnet;
using MQTTnet.Client;

namespace RadarLights.Services.HomeAssistant;

public class MqttPublisher
{
    private readonly AppConfig.MqttConfig _mqttConfig;

    public MqttPublisher(AppConfig config)
    {
        _mqttConfig = config.Mqtt;
    }
    
    public async Task SendConfiguration(HAEntityConfig config, string topic)
    {
        using IMqttClient mqttClient = await GetMqttClient(_mqttConfig.Server);

        var jsonPayload = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy(),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(jsonPayload)
            .Build();

        await mqttClient.PublishAsync(applicationMessage);

        await mqttClient.DisconnectAsync();

        Console.WriteLine("MQTT application message is published.");
    }

    public async Task SendState(string topic, string state)
    {
        using IMqttClient mqttClient = await GetMqttClient(_mqttConfig.Server);

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(state)
            .Build();

        await mqttClient.PublishAsync(applicationMessage);

        await mqttClient.DisconnectAsync();
    }
    
    public static async Task<IMqttClient> GetMqttClient(string server)
    {
        var mqttFactory = new MqttFactory();

        var mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(server)
            .Build();

        await mqttClient.ConnectAsync(mqttClientOptions);
        return mqttClient;
    }
}
