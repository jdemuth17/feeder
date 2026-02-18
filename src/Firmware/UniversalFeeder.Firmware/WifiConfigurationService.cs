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
            // but we can force it or wait for IP
#endif
        }
    }
}
