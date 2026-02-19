using MQTTnet;
using Polly;
using Polly.Retry;
using System.Text;
using System.Text.Json;
using UniversalFeeder.Shared;

namespace UniversalFeeder.Server.Services
{
    public class MqttFeederClient : IFeederClient
    {
        private readonly IMqttClient _mqttClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttFeederClient> _logger;
        private readonly AsyncRetryPolicy<bool> _retryPolicy;

        public MqttFeederClient(IServiceProvider serviceProvider, ILogger<MqttFeederClient> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            _retryPolicy = Policy<bool>
                .Handle<Exception>()
                .OrResult(success => !success)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (result, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("MQTT publication failed. Retry {Count} after {Time}s. Error: {Error}", 
                            retryCount, timeSpan.TotalSeconds, result.Exception?.Message ?? "Action returned false");
                    });
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
            return await _retryPolicy.ExecuteAsync(async () =>
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
                    _logger.LogError(ex, "Transient error publishing MQTT command to {Topic}", topic);
                    throw; // Let Polly handle it
                }
            });
        }

        public async Task<bool> TriggerFeedAsync(string identifier, int durationMs)
        {
            string topic = MqttCommands.GetCommandTopic(identifier);
            return await PublishCommandAsync(topic, new Dictionary<string, object>
            {
                [MqttCommands.KeyAction] = MqttCommands.ActionFeed,
                [MqttCommands.KeyDurationMs] = durationMs
            });
        }

        public async Task<bool> TriggerChimeAsync(string identifier, float volume)
        {
            string topic = MqttCommands.GetCommandTopic(identifier);
            return await PublishCommandAsync(topic, new Dictionary<string, object>
            {
                [MqttCommands.KeyAction] = MqttCommands.ActionChime,
                [MqttCommands.KeyVolume] = volume
            });
        }
    }
}
