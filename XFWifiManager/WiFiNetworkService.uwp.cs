﻿using Plugin.MauiWifiManager.Abstractions;
using System;
using System.Threading.Tasks;

namespace Plugin.MauiWifiManager
{
    /// <summary>
    /// Interface for WiFiNetworkService
    /// </summary>
    public class WiFiNetworkService : IWifiNetworkService
    {
        public Task<NetworkDataModel> ConnectWifi(string ssid, string password)
        {
            throw new NotImplementedException();
        }

        public void DisconnectWifi(string ssid)
        {
            throw new NotImplementedException();
        }

        public Task<NetworkDataModel> GetNetworkInfo()
        {
            throw new NotImplementedException();
        }

        public Task<bool> OpenWifiSetting()
        {
            throw new NotImplementedException();
        }
    }
}