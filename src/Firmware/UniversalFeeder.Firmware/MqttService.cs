using System;
using System.Text;
using System.Threading;
using UniversalFeeder.Shared;
#if NANOFRAMEWORK
using nanoFramework.M2Mqtt;
using nanoFramework.M2Mqtt.Messages;
#endif

namespace UniversalFeeder.Firmware
{
    public class MqttService : IDisposable
    {
        private readonly IFeedingSequenceService _feedingSequence;
        private readonly IBuzzerService _buzzerService;
#if NANOFRAMEWORK
        private MqttClient _client;
#endif
        private string _clientId;
        private string _host;
        private string _username;
        private string _password;
        private Timer _reconnectTimer;
        private int _reconnectDelayMs = 5000;
        private const int MaxReconnectDelayMs = 60000;

        public MqttService(IFeedingSequenceService feedingSequence, IBuzzerService buzzerService)
        {
            _feedingSequence = feedingSequence;
            _buzzerService = buzzerService;
        }

        public void Start(string host, string username, string password, string clientId)
        {
            _clientId = clientId;
            _host = host;
            _username = username;
            _password = password;
#if NANOFRAMEWORK
            try
            {
                _client = new MqttClient(host, 8883, true, null, null, MqttSslProtocols.TLSv1_2);
                _client.MqttMsgPublishReceived += OnMessageReceived;
                _client.ConnectionClosed += OnConnectionClosed;
                _client.Connect(_clientId, username, password);

                if (_client.IsConnected)
                {
                    _reconnectDelayMs = 5000;
                    SubscribeToCommands();
                    Console.WriteLine($"Connected to MQTT! Subscribed to {MqttCommands.GetCommandTopic(_clientId)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT Start Error: {ex.Message}. Will retry in {_reconnectDelayMs / 1000}s...");
                ScheduleReconnect();
            }
#endif
        }

#if NANOFRAMEWORK
        private void SubscribeToCommands()
        {
            string topic = MqttCommands.GetCommandTopic(_clientId);
            _client.Subscribe(new string[] { topic }, new MqttQoSLevel[] { MqttQoSLevel.AtLeastOnce });
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            Console.WriteLine($"MQTT connection lost. Reconnecting in {_reconnectDelayMs / 1000}s...");
            ScheduleReconnect();
        }

        private void ScheduleReconnect()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = new Timer(ReconnectCallback, null, _reconnectDelayMs, Timeout.Infinite);
        }

        private void ReconnectCallback(object state)
        {
            try
            {
                Console.WriteLine("MQTT: Attempting reconnection...");
                if (_client != null && !_client.IsConnected)
                {
                    _client.Connect(_clientId, _username, _password);
                }

                if (_client != null && _client.IsConnected)
                {
                    _reconnectDelayMs = 5000;
                    SubscribeToCommands();
                    Console.WriteLine("MQTT Reconnected!");
                }
                else
                {
                    IncreaseBackoff();
                    ScheduleReconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT Reconnect Error: {ex.Message}. Retrying in {_reconnectDelayMs / 1000}s...");
                IncreaseBackoff();
                ScheduleReconnect();
            }
        }

        private void IncreaseBackoff()
        {
            _reconnectDelayMs = (_reconnectDelayMs * 2 > MaxReconnectDelayMs) ? MaxReconnectDelayMs : _reconnectDelayMs * 2;
        }
#endif

#if NANOFRAMEWORK
        private void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                string message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
                Console.WriteLine($"MQTT Command Received: {message}");

                // Improved robust parsing for nanoFramework (no full JSON parser)
                var action = ExtractString(message, $"\"{MqttCommands.KeyAction}\"");

                if (action == MqttCommands.ActionFeed)
                {
                    int ms = ExtractInt(message, $"\"{MqttCommands.KeyDurationMs}\"");
                    _feedingSequence.Execute(ms > 0 ? ms : 5000);
                }
                else if (action == MqttCommands.ActionChime)
                {
                    float vol = ExtractFloat(message, $"\"{MqttCommands.KeyVolume}\"");
                    _buzzerService.Play(vol > 0 ? vol : 1.0f, 1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT Message Error: {ex.Message}");
            }
        }

        private string ExtractString(string json, string key)
        {
            int keyIdx = json.IndexOf(key);
            if (keyIdx == -1) return null;

            int valStart = json.IndexOf(":", keyIdx) + 1;
            int quoteStart = json.IndexOf("\"", valStart);
            if (quoteStart == -1) return null;

            int quoteEnd = json.IndexOf("\"", quoteStart + 1);
            if (quoteEnd == -1) return null;

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }

        private int ExtractInt(string json, string key)
        {
            int keyIdx = json.IndexOf(key);
            if (keyIdx == -1) return 0;

            int valStart = json.IndexOf(":", keyIdx) + 1;
            int commaIdx = json.IndexOf(",", valStart);
            int braceIdx = json.IndexOf("}", valStart);
            int end = (commaIdx != -1 && commaIdx < braceIdx) ? commaIdx : braceIdx;

            if (end == -1) return 0;

            string val = json.Substring(valStart, end - valStart).Trim();
            return int.TryParse(val, out int result) ? result : 0;
        }

        private float ExtractFloat(string json, string key)
        {
            int keyIdx = json.IndexOf(key);
            if (keyIdx == -1) return 0;

            int valStart = json.IndexOf(":", keyIdx) + 1;
            int commaIdx = json.IndexOf(",", valStart);
            int braceIdx = json.IndexOf("}", valStart);
            int end = (commaIdx != -1 && commaIdx < braceIdx) ? commaIdx : braceIdx;

            if (end == -1) return 0;

            string val = json.Substring(valStart, end - valStart).Trim();
            return float.TryParse(val, out float result) ? result : 0;
        }
#endif

        public void Dispose()
        {
            _reconnectTimer?.Dispose();
#if NANOFRAMEWORK
            if (_client != null && _client.IsConnected)
            {
                _client.Disconnect();
            }
#endif
        }
    }
}
