using System;
#if NANOFRAMEWORK
using System.Net.NetworkInformation;
using nanoFramework.Networking;
#endif

namespace UniversalFeeder.Firmware
{
    public static class WifiConfigurationService
    {
        public static bool HasCredentials()
        {
#if NANOFRAMEWORK
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    var config = Wireless80211Configuration.GetAllWireless80211Configurations();
                    if (config.Length > 0 && !string.IsNullOrEmpty(config[0].Ssid))
                    {
                        return true;
                    }
                }
            }
#endif
            return false;
        }

        public static void SaveCredentials(string ssid, string password)
        {
#if NANOFRAMEWORK
            var config = new Wireless80211Configuration(0)
            {
                Ssid = ssid,
                Password = password,
                Options = Wireless80211Configuration.ConfigurationOptions.AutoConnect
            };
            config.SaveConfiguration();
            Console.WriteLine("Wi-Fi Credentials Saved to NVS");
#endif
        }

        public static void Connect()
        {
#if NANOFRAMEWORK
            Console.WriteLine("Connecting to Wi-Fi...");
            // nanoFramework handles connection automatically if AutoConnect is set
#endif
        }

        public static string WaitForIp(int timeoutSeconds = 30)
        {
#if NANOFRAMEWORK
            DateTime end = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < end)
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        if (ni.IPv4Address != "0.0.0.0" && !string.IsNullOrEmpty(ni.IPv4Address))
                        {
                            return ni.IPv4Address;
                        }
                    }
                }
                Thread.Sleep(1000);
            }
#endif
            return null;
        }
    }
}
