using MQTTnet;
using System.Text;
using System.Text.Json;

namespace UniversalFeeder.Server.Services
{
    public class MqttFeederClient : IFeederClient
    {
        private readonly IMqttClient _mqttClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttFeederClient> _logger;

        public MqttFeederClient(IServiceProvider serviceProvider, ILogger<MqttFeederClient> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();
        }

        private async Task<MqttClientOptions> GetMqttOptionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            string host = await settings.GetSettingAsync("MqttHost", config["Mqtt:Host"] ?? "localhost");
            string user = await settings.GetSettingAsync("MqttUsername", config["Mqtt:Username"] ?? "");
            string pass = await settings.GetSettingAsync("MqttPassword", config["Mqtt:Password"] ?? "");

            return new MqttClientOptionsBuilder()
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
                    var options = await GetMqttOptionsAsync();
                    await _mqttClient.ConnectAsync(options);
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
