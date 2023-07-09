﻿using CoreLocation;
using Foundation;
using NetworkExtension;
using Plugin.MauiWifiManager.Abstractions;
using System;
using System.Threading.Tasks;
using SystemConfiguration;
using UIKit;

namespace Plugin.MauiWifiManager
{
    /// <summary>
    /// Interface for WiFiNetworkService
    /// </summary>
    public class WifiNetworkService : IWifiNetworkService
    {
        public WifiNetworkService()
        {
        }
        public async Task<NetworkData> ConnectWifi(string ssid, string password)
        {
            NetworkData networkData = new NetworkData();
            NEHotspotConfigurationManager.SharedManager.RemoveConfiguration(ssid);
            var config = new NEHotspotConfiguration(ssid, password, isWep: false);
            config.JoinOnce = false;
            var tcs = new TaskCompletionSource<NSError>();
            NEHotspotConfigurationManager.SharedManager.ApplyConfiguration(config, err => tcs.TrySetResult(err));
            var error = await tcs.Task;

            if (error != null)
            {
                if (error?.LocalizedDescription == "already associated.")
                {
                    
                }
                else
                {
                    Console.WriteLine("Not Connected");
                }
            }
            return networkData;
        }

        /// <summary>
        /// Disconnect Wi-Fi
        /// </summary>
        public void DisconnectWifi(string ssid)
        {
            NEHotspotConfigurationManager.SharedManager.RemoveConfiguration(ssid);
        }

        /// <summary>
        /// Get Wi-Fi Network Info
        /// </summary>
        public Task<NetworkData> GetNetworkInfo()
        {
            NetworkData networkData = new NetworkData();
            var manager = new CLLocationManager();
            if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
                manager.RequestWhenInUseAuthorization();

            if (CLLocationManager.Status is CLAuthorizationStatus.Authorized ||
                    CLLocationManager.Status is CLAuthorizationStatus.AuthorizedAlways ||
                    CLLocationManager.Status is CLAuthorizationStatus.AuthorizedWhenInUse)
            {
                if (CaptiveNetwork.TryGetSupportedInterfaces(out string[] supportedInterfaces) == StatusCode.OK)
                {
                    foreach (var item in supportedInterfaces)
                    {
                        if (CaptiveNetwork.TryCopyCurrentNetworkInfo(item, out NSDictionary? info) == StatusCode.OK)
                        {
                            networkData.Ssid = info?[CaptiveNetwork.NetworkInfoKeySSID].ToString();                            
                        }
                    }
                }
            }
            return Task.FromResult(networkData);
        }

        /// <summary>
        /// OpenWifiSetting
        /// For iOS 8 and 9, we can navigate automatically to the settings
        /// App-Pre0fs:root=WIFI is forbidden by the app store guidelines
        /// </summary>
        public Task<bool> OpenWifiSetting()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {                
                NSUrl url = new NSUrl(UIApplication.OpenSettingsUrlString);
                return Task.FromResult(UIApplication.SharedApplication.OpenUrl(url));
            }
            return Task.FromResult(false);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {

        }
    }
}