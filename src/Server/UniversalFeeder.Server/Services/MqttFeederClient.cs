using MQTTnet;
using System.Text;
using System.Text.Json;

namespace UniversalFeeder.Server.Services
{
    public class MqttFeederClient : IFeederClient
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttOptions;
        private readonly ILogger<MqttFeederClient> _logger;

        public MqttFeederClient(IConfiguration config, ILogger<MqttFeederClient> logger)
        {
            _logger = logger;
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            // These would normally come from appsettings.json or User Secrets
            string host = config["Mqtt:Host"] ?? "localhost";
            string user = config["Mqtt:Username"] ?? "";
            string pass = config["Mqtt:Password"] ?? "";

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(host)
                .WithCredentials(user, pass)
                .WithTlsOptions(o => o.WithTargetHost(host))
                .Build();
        }

        private async Task<bool> PublishCommandAsync(string topic, object command)
        {
            try
            {
                if (!_mqttClient.IsConnected)
                {
                    await _mqttClient.ConnectAsync(_mqttOptions);
                }

                var payload = JsonSerializer.Serialize(command);
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish MQTT command to {Topic}", topic);
                return false;
            }
        }

        public async Task<bool> TriggerFeedAsync(string identifier, int durationMs)
        {
            // For MQTT, 'identifier' is the FeederId/Topic suffix
            string topic = $"feeders/{identifier}/commands";
            return await PublishCommandAsync(topic, new { action = "feed", ms = durationMs });
        }

        public async Task<bool> TriggerChimeAsync(string identifier, float volume)
        {
            string topic = $"feeders/{identifier}/commands";
            return await PublishCommandAsync(topic, new { action = "chime", vol = volume });
        }
    }
}
