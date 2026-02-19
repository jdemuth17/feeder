using System;
using System.Text;
#if NANOFRAMEWORK
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Security.Cryptography.X509Certificates;
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
            // HiveMQ Cloud usually requires port 8883 and TLS
            _client = new MqttClient(host, 8883, true, null, null, MqttSslProtocols.TLSv1_2);
            
            _client.MqttMsgPublishReceived += OnMessageReceived;
            
            _client.Connect(_clientId, username, password);

            if (_client.IsConnected)
            {
                string topic = $"feeders/{_clientId}/commands";
                _client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                Console.WriteLine($"Connected to MQTT! Subscribed to {topic}");
            }
#endif
        }

#if NANOFRAMEWORK
        private void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
            Console.WriteLine($"MQTT Command Received: {message}");

            // Basic parsing (Real app would use a JSON parser)
            if (message.Contains(""action":"feed""))
            {
                int ms = ExtractInt(message, ""ms":");
                _feedingSequence.Execute(ms > 0 ? ms : 5000);
            }
            else if (message.Contains(""action":"chime""))
            {
                float vol = ExtractFloat(message, ""vol":");
                _buzzerService.Play(vol > 0 ? vol : 1.0f, 1000);
            }
        }

        private int ExtractInt(string json, string key)
        {
            try {
                int start = json.IndexOf(key) + key.Length;
                int end = json.IndexOf(",", start);
                if (end == -1) end = json.IndexOf("}", start);
                string val = json.Substring(start, end - start).Trim();
                return int.Parse(val);
            } catch { return 0; }
        }

        private float ExtractFloat(string json, string key)
        {
            try {
                int start = json.IndexOf(key) + key.Length;
                int end = json.IndexOf(",", start);
                if (end == -1) end = json.IndexOf("}", start);
                string val = json.Substring(start, end - start).Trim();
                return float.Parse(val);
            } catch { return 0; }
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
