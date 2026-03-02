using System;
using System.Threading;
#if NANOFRAMEWORK
using System.Net.NetworkInformation;
using nanoFramework.Networking;
#endif

namespace UniversalFeeder.Firmware
{
    public class Program
    {
        private static BleProvisioningService _bleService;
        private static MqttService _mqttService;
        private static IFeedingSequenceService _feedingSequence;

        public static void Main()
        {
            Console.WriteLine("Universal Auto-Feeder Firmware Starting...");

            // 1. Check for Wi-Fi Credentials
            if (!WifiConfigurationService.HasCredentials())
            {
                StartProvisioningMode();
            }
            else
            {
                StartNormalMode();
            }
            
            Thread.Sleep(Timeout.Infinite);
        }

        private static void StartProvisioningMode()
        {
            Console.WriteLine("Entering Provisioning Mode (BLE)...");
            _bleService = new BleProvisioningService();
            _bleService.OnCredentialsReceived += (s, e) =>
            {
                Console.WriteLine("Credentials received. Attempting connection...");
                WifiConfigurationService.SaveCredentials(_bleService.Ssid, _bleService.Password);
                
                string ip = WifiConfigurationService.WaitForIp();
                if (ip != null)
                {
                    Console.WriteLine($"Connected! IP: {ip}");
                    _bleService.UpdateIpAddress(ip);
                    
                    // Give mobile app time to read IP
                    Thread.Sleep(5000);
                    
                    Console.WriteLine("Provisioning complete. Rebooting...");
#if NANOFRAMEWORK
                    nanoFramework.Runtime.Native.Power.RebootDevice();
#endif
                }
                else
                {
                    Console.WriteLine("Connection failed. Remaining in provisioning mode.");
                }
            };
            _bleService.Start("Feeder-Setup");
        }

        private static void StartNormalMode()
        {
            Console.WriteLine("Starting Normal Mode (Wi-Fi)...");
            
            // Connect to Wi-Fi
            WifiConfigurationService.Connect();

            // Setup Services
            var motor = new MotorService();
            var buzzer = new BuzzerService();
            _feedingSequence = new FeedingSequenceService(motor, buzzer);
            
            _mqttService = new MqttService(_feedingSequence, buzzer);
            
            // HiveMQ Cloud credentials
            string mqttHost = "0827f2b3c2a54b1c8a0d539d4f5e3990.s1.eu.hivemq.cloud";
            string mqttUser = "Jdemuth17_IOT";
            string mqttPass = "Pdazzle17_IOT!";
            string clientId = GetUniqueId();

            _mqttService.Start(mqttHost, mqttUser, mqttPass, clientId);
        }

        private static string GetUniqueId()
        {
#if NANOFRAMEWORK
            var ni = NetworkInterface.GetAllNetworkInterfaces()[0];
            var mac = ni.PhysicalAddress;
            return mac[0].ToString("X2") + mac[1].ToString("X2") + mac[2].ToString("X2") + mac[3].ToString("X2") + mac[4].ToString("X2") + mac[5].ToString("X2");
#else
            return "DevFeeder01";
#endif
        }
    }
}
