using Plugin.MauiWifiManager.Abstractions;

namespace Plugin.MauiWifiManager
{
    public interface IWifiNetworkService : IDisposable
    {
        /// <summary>
        /// Connect Wi-Fi
        /// </summary>
        Task<NetworkData> ConnectWifi(string ssid, string password);

        /// <summary>
        /// Get Wi-Fi Network Info
        /// </summary>
        Task<NetworkData> GetNetworkInfo();

        /// <summary>
        /// Disconnect Wi-Fi
        /// From Android Q (Android 10) you can't enable/disable wifi programmatically anymore. 
        /// So, use Settings Panel to toggle wifi connectivity
        /// </summary>
        /// 
        void DisconnectWifi(string ssid);

        /// <summary>
        /// Open Wi-Fi Setting
        /// </summary>
        Task<bool> OpenWifiSetting();

        /// <summary>
        /// Scan Wi-Fi Networks
        /// </summary>
        Task<List<NetworkData>> ScanWifiNetworks();
    }
}
