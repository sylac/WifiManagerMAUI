using Plugin.MauiWifiManager.Abstractions;

namespace Plugin.MauiWifiManager
{
    /// <summary>
    /// Interface for WiFiNetworkService
    /// </summary>
    /// 
    public partial class WifiNetworkService : IWifiNetworkService
    {
#if !ANDROID && !IOS && !WINDOWS && !MACCATALYST
        public Task<NetworkData> ConnectWifi(string ssid, string password) => throw new NotImplementedException();
        public void DisconnectWifi(string? ssid) => throw new NotImplementedException();
        public Task<NetworkData> GetNetworkInfo() => throw new NotImplementedException();
        public Task<bool> OpenWifiSetting() => throw new NotImplementedException();
        public Task<List<NetworkData>> ScanWifiNetworks() => throw new NotImplementedException();
        public void Dispose() { }
#endif
    }
}
