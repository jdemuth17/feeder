using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using UniversalFeeder.Shared;

namespace UniversalFeeder.Mobile.Services
{
    public class MqttService : IDisposable
    {
        private IMqttClient? _client;
        private readonly MqttFactory _factory = new();

        private const string Host = "0827f2b3c2a54b1c8a0d539d4f5e3990.s1.eu.hivemq.cloud";
        private const int Port = 8883;
        private const string Username = "Jdemuth17_IOT";
        private const string Password = "Pdazzle17_IOT!";

        public bool IsConnected => _client?.IsConnected ?? false;
        public event EventHandler<bool>? ConnectionChanged;

        public async Task ConnectAsync()
        {
            if (_client is { IsConnected: true }) return;

            _client = _factory.CreateMqttClient();
            _client.DisconnectedAsync += async e =>
            {
                ConnectionChanged?.Invoke(this, false);
                await Task.Delay(5000);
                try
                {
                    await _client.ReconnectAsync();
                    ConnectionChanged?.Invoke(this, true);
                }
                catch
                {
                    // Will retry on next disconnect event
                }
            };

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(Host, Port)
                .WithCredentials(Username, Password)
                .WithTlsOptions(o => o.UseTls())
                .WithClientId($"mobile-{Guid.NewGuid():N}".Substring(0, 23))
                .WithCleanSession()
                .Build();

            try
            {
                await _client.ConnectAsync(options);
                ConnectionChanged?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT Connect Error: {ex.Message}");
                ConnectionChanged?.Invoke(this, false);
            }
        }

        public async Task DisconnectAsync()
        {
            if (_client is { IsConnected: true })
            {
                await _client.DisconnectAsync();
                ConnectionChanged?.Invoke(this, false);
            }
        }

        public async Task<bool> SendFeedCommandAsync(string feederId, int durationMs = 5000)
        {
            if (_client is not { IsConnected: true }) return false;

            var topic = MqttCommands.GetCommandTopic(feederId);
            var payload = JsonSerializer.Serialize(new
            {
                action = MqttCommands.ActionFeed,
                ms = durationMs
            });

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(Encoding.UTF8.GetBytes(payload))
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _client.PublishAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT Publish Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendChimeCommandAsync(string feederId, float volume = 1.0f)
        {
            if (_client is not { IsConnected: true }) return false;

            var topic = MqttCommands.GetCommandTopic(feederId);
            var payload = JsonSerializer.Serialize(new
            {
                action = MqttCommands.ActionChime,
                vol = volume
            });

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(Encoding.UTF8.GetBytes(payload))
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _client.PublishAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT Publish Error: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
