using System;
using System.Threading;
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
            var config = Wireless80211Configuration.GetAllWireless80211Configurations();
            Wireless80211Configuration currentConfig;
            if (config.Length > 0)
            {
                currentConfig = config[0];
            }
            else
            {
                currentConfig = new Wireless80211Configuration(0);
            }

            currentConfig.Ssid = ssid;
            currentConfig.Password = password;
            currentConfig.Options = Wireless80211Configuration.ConfigurationOptions.AutoConnect | Wireless80211Configuration.ConfigurationOptions.Enable;
            currentConfig.SaveConfiguration();
            Console.WriteLine($"Wi-Fi Credentials Saved for SSID: {ssid}");
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
