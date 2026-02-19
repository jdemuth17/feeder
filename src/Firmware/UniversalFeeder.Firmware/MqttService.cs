using System;
using System.Text;
using UniversalFeeder.Shared;
#if NANOFRAMEWORK
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
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

        public MqttService(IFeedingSequenceService feedingSequence, IBuzzerService buzzerService)
        {
            _feedingSequence = feedingSequence;
            _buzzerService = buzzerService;
        }

        public void Start(string host, string username, string password, string clientId)
        {
            _clientId = clientId;
#if NANOFRAMEWORK
            try
            {
                _client = new MqttClient(host, 8883, true, null, null, MqttSslProtocols.TLSv1_2);
                _client.MqttMsgPublishReceived += OnMessageReceived;
                _client.Connect(_clientId, username, password);

                if (_client.IsConnected)
                {
                    string topic = MqttCommands.GetCommandTopic(_clientId);
                    _client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                    Console.WriteLine($"Connected to MQTT! Subscribed to {topic}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT Start Error: {ex.Message}");
            }
#endif
        }

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
#if NANOFRAMEWORK
            if (_client != null && _client.IsConnected)
            {
                _client.Disconnect();
            }
#endif
        }
    }
}
