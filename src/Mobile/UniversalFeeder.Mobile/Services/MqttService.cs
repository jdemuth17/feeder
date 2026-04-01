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
        public event EventHandler<(string feederId, string payload)>? LogMessageReceived;
        public event EventHandler<(string feederId, string payload)>? ScheduleListReceived;

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
                .WithTlsOptions(o =>
                {
                    o.UseTls();
                    o.WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12);
                    o.WithCertificateValidationHandler(_ => true);
                })
                .WithClientId($"mobile-{Guid.NewGuid():N}"[..23])
                .WithCleanSession()
                .Build();

            try
            {
                await _client.ConnectAsync(options);
                ConnectionChanged?.Invoke(this, true);
                // register global message handler
                _client.ApplicationMessageReceivedAsync += e =>
                {
                    try
                    {
                        var topic = e.ApplicationMessage.Topic;
                        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload ?? Array.Empty<byte>());
                        // match log topic pattern: feeders/{id}/logs
                        if (!string.IsNullOrEmpty(topic) && topic.Contains("/logs"))
                        {
                            // extract feeder id between prefix and /logs
                            var prefix = "feeders/";
                            var idx = topic.IndexOf(prefix);
                            if (idx >= 0)
                            {
                                var start = idx + prefix.Length;
                                var end = topic.IndexOf("/logs", start);
                                if (end > start)
                                {
                                    var feederId = topic.Substring(start, end - start);
                                    LogMessageReceived?.Invoke(this, (feederId, payload));
                                }
                            }
                        }
                        // match schedule topic pattern: feeders/{id}/schedule
                        if (!string.IsNullOrEmpty(topic) && topic.Contains("/schedule"))
                        {
                            var prefix = "feeders/";
                            var idx = topic.IndexOf(prefix);
                            if (idx >= 0)
                            {
                                var start = idx + prefix.Length;
                                var end = topic.IndexOf("/schedule", start);
                                if (end > start)
                                {
                                    var feederId = topic.Substring(start, end - start);
                                    ScheduleListReceived?.Invoke(this, (feederId, payload));
                                }
                            }
                        }
                    }
                    catch { }
                    return Task.CompletedTask;
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT Connect Error: {ex}");
                ConnectionChanged?.Invoke(this, false);
                throw; // Let ViewModel display the error
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

        public async Task<bool> SendScheduleAsync(string feederId, object schedule)
        {
            if (_client is not { IsConnected: true }) return false;

            var topic = MqttCommands.GetScheduleTopic(feederId);
            var payload = JsonSerializer.Serialize(new
            {
                action = MqttCommands.ActionSetSchedule,
                schedule = schedule
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

        public async Task<List<Models.FeedingScheduleEntry>> RequestScheduleAsync(string feederId, int timeoutMs = 5000)
        {
            if (_client is not { IsConnected: true }) return new List<Models.FeedingScheduleEntry>();

            var scheduleTopic = MqttCommands.GetScheduleTopic(feederId);
            var tcs = new TaskCompletionSource<List<Models.FeedingScheduleEntry>>();

            // Subscribe to schedule topic to receive the response
            await _client.SubscribeAsync(scheduleTopic, MqttQualityOfServiceLevel.AtLeastOnce);

            EventHandler<(string feederId, string payload)>? handler = null;
            handler = (_, args) =>
            {
                if (args.feederId != feederId) return;
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(args.payload);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("action", out var actionEl) &&
                        actionEl.GetString() == MqttCommands.ActionScheduleList &&
                        root.TryGetProperty("schedule", out var schedEl) &&
                        schedEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var entries = new List<Models.FeedingScheduleEntry>();
                        foreach (var item in schedEl.EnumerateArray())
                        {
                            var timeStr = item.TryGetProperty("time", out var t) ? t.GetString() ?? "00:00" : "00:00";
                            var durationMs = item.TryGetProperty("duration_ms", out var d) ? d.GetInt32() : 5000;
                            var enabled = item.TryGetProperty("enabled", out var e) ? e.GetBoolean() : true;
                            if (!TimeSpan.TryParseExact(timeStr, @"hh\:mm", null, out var ts))
                                ts = TimeSpan.Zero;
                            entries.Add(new Models.FeedingScheduleEntry { Time = ts, DurationSeconds = durationMs / 1000.0, Enabled = enabled });
                        }
                        tcs.TrySetResult(entries);
                    }
                }
                catch { }
            };

            ScheduleListReceived += handler;

            // Send get_schedule request to command topic
            var cmdTopic = MqttCommands.GetCommandTopic(feederId);
            var payload = System.Text.Json.JsonSerializer.Serialize(new { action = MqttCommands.ActionGetSchedule });
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(cmdTopic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            await _client.PublishAsync(message);

            var timeout = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(tcs.Task, timeout);

            ScheduleListReceived -= handler;

            return completed == tcs.Task ? await tcs.Task : new List<Models.FeedingScheduleEntry>();
        }

        public async Task<bool> SubscribeToLogsAsync(string feederId)
        {
            if (_client is not { IsConnected: true }) return false;

            var topic = MqttCommands.GetLogTopic(feederId);
            try
            {
                await _client.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT Subscribe Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RequestLogsAsync(string feederId, long sinceUnix = 0)
        {
            if (_client is not { IsConnected: true }) return false;

            var topic = MqttCommands.GetCommandTopic(feederId);
            object payloadObj = sinceUnix > 0 ? new { action = "request_logs", since = sinceUnix } : new { action = "request_logs" };
            var payload = JsonSerializer.Serialize(payloadObj);

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
