﻿using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Plugin.MauiWifiManager.Abstractions;
using static Android.Provider.Settings;
using Context = Android.Content.Context;

namespace Plugin.MauiWifiManager
{
    /// <summary>
    /// Interface for WiFiNetworkService
    /// </summary>
    /// 
    public partial class WifiNetworkService : IWifiNetworkService
    {
        private static NetworkData? _networkData;
        private static Context _context;
        private static NetworkCallback _callback;
        private static ConnectivityManager? _connectivityManager;
        private static bool _requested;
        private static NetworkRequest? _request;
        private static WifiManager? _wifiManager;
        public WifiNetworkService()
        {
            _context = Android.App.Application.Context;
            _networkData = new NetworkData();
            _wifiManager = (WifiManager)(_context.GetSystemService(Context.WifiService));
            _request = new NetworkRequest.Builder().AddTransportType(transportType: TransportType.Wifi).Build();
            _callback = new NetworkCallback
            {
                NetworkAvailable = network =>
                {

                },
                NetworkUnavailable = () =>
                {

                }
            };
        }

        /// <summary>
        /// Connect Wi-Fi
        /// </summary>
        public async Task<NetworkData> ConnectWifi(string ssid, string password)
        {
            _wifiManager = (WifiManager)(_context.GetSystemService(Context.WifiService));
            if (Build.VERSION.SdkInt <= BuildVersionCodes.P)
            {
                if (!_wifiManager.IsWifiEnabled)
                {
                    _wifiManager.SetWifiEnabled(true);
                }
                string wifiSsid = _wifiManager.ConnectionInfo.SSID.ToString();
                if (wifiSsid != string.Format("\"{0}\"", ssid))
                {
                    WifiConfiguration wifiConfig = new WifiConfiguration();
                    wifiConfig.Ssid = string.Format("\"{0}\"", ssid);
                    wifiConfig.PreSharedKey = string.Format("\"{0}\"", password);
                    int netId = _wifiManager.AddNetwork(wifiConfig);
                    _wifiManager.Disconnect();
                    _wifiManager.EnableNetwork(netId, true);
                    _wifiManager.Reconnect();
                    _networkData.Ssid = wifiConfig.Ssid;

                }
                else
                {
                    Console.WriteLine("Cannot find valid SSID");
                }
            }
            else if (Build.VERSION.SdkInt == BuildVersionCodes.Q)
            {
                RequestNetwork(ssid, password);
            }
            else
                await AddWifi(ssid, password);
            return _networkData;
        }

        /// <summary>
        /// Disconnect Wi-Fi
        /// From Android Q (Android 10) you can't enable/disable wifi programmatically anymore. 
        /// So, use Settings Panel to toggle wifi connectivity
        /// </summary>
        public void DisconnectWifi(string? ssid)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                Intent panelIntent = new Intent(Panel.ActionWifi);
                _context.StartActivity(panelIntent);
            }
            else
            {
                _wifiManager = (WifiManager)(_context.GetSystemService(Context.WifiService));
                _wifiManager.SetWifiEnabled(false); // Disable wifi
                _wifiManager.SetWifiEnabled(true); // Enable wifi
            }
        }

        /// <summary>
        /// Get Wi-Fi Network Info
        /// </summary>
        public async Task<NetworkData> GetNetworkInfo()
        {
            int apiLevel = (int)Build.VERSION.SdkInt;
            if (apiLevel < 31)
            {
                _wifiManager = (WifiManager)(_context.GetSystemService(Context.WifiService));
                if (_wifiManager.IsWifiEnabled)
                {
                    _networkData.Ssid = _wifiManager.ConnectionInfo.SSID.Trim(new char[] { '"', '\"' });
                    _networkData.IpAddress = _wifiManager.DhcpInfo.IpAddress;
                    _networkData.GatewayAddress = _wifiManager.DhcpInfo.Gateway.ToString();
                    _networkData.NativeObject = _wifiManager;
                }
                else
                {
                    Console.WriteLine("WI-Fi turned off");
                }
            }
            else
            {
                ConnectivityManager connectivityManager = _context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
                NetworkInfo activeNetworkInfo = connectivityManager.ActiveNetworkInfo;
                if (activeNetworkInfo != null)
                {
                    NetworkCallbackFlags flagIncludeLocationInfo = NetworkCallbackFlags.IncludeLocationInfo;
                    NetworkCallback networkCallback = new NetworkCallback((int)flagIncludeLocationInfo);
                    connectivityManager.RequestNetwork(_request, networkCallback);
                }
                else
                {
                    Console.WriteLine("Failed to get data");
                }
            }
            await Task.Delay(1000);
            return _networkData;
        }

        /// <summary>
        /// Open Wi-Fi Setting
        /// </summary>
        public Task<bool> OpenWifiSetting()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            Intent panelIntent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                panelIntent = new Intent(Panel.ActionWifi);
            else
                panelIntent = new Intent(ActionWifiSettings);
            _context.StartActivity(panelIntent);
            taskCompletionSource.TrySetResult(true);
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {

        }

        private async Task AddWifi(string ssid, string psk)
        {
            await Task.Run(async () =>
            {
                var suggestions = new List<IParcelable>
                        {
                           new WifiNetworkSuggestion.Builder()
                            .SetSsid(ssid)
                            .SetWpa2Passphrase(psk)
                            .SetIsAppInteractionRequired(true)
                            .SetIsUserInteractionRequired(true)
                            .SetIsEnhancedOpen(false)
                            .SetIsHiddenSsid(false)
                            .Build()
                    };

                await OpenWifiSetting();
                var bundle = new Bundle();
                bundle.PutParcelableArrayList("android.provider.extra.WIFI_NETWORK_LIST", suggestions);
                var intent = new Intent("android.settings.WIFI_ADD_NETWORKS");
                intent.PutExtras(bundle);
                _context.StartActivity(intent);

            });
        }

        public void RequestNetwork(string ssid, string password)
        {
            _wifiManager = (WifiManager)(_context.GetSystemService(Context.WifiService));
            if (!_wifiManager.IsWifiEnabled)
            {
                Console.WriteLine("Wi-Fi is turned off");
            }

            var specifier = new WifiNetworkSpecifier.Builder()
               .SetSsid(ssid)
               .SetWpa2Passphrase(password)
               .Build();

            var request = new NetworkRequest.Builder()?
                .AddTransportType(TransportType.Wifi)?
                .SetNetworkSpecifier(specifier)?
                .Build();

            UnregisterNetworkCallback(_callback);
            _connectivityManager = _context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
            if (_requested)
            {
                _connectivityManager?.UnregisterNetworkCallback(_callback);
            }
            _connectivityManager?.RequestNetwork(request, _callback);
            _requested = true;
        }

        private void UnregisterNetworkCallback(NetworkCallback networkCallback)
        {
            if (networkCallback != null)
            {
                try
                {
                    _connectivityManager = _context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
                    _connectivityManager.UnregisterNetworkCallback(networkCallback);

                }
                catch
                {
                    networkCallback = null;
                }
            }
        }

        /// <summary>
        /// Scan Wi-Fi Networks
        /// </summary>
        public async Task<List<NetworkData>> ScanWifiNetworks()
        {
            _wifiManager = (WifiManager)(_context.GetSystemService(Context.WifiService));
            List<NetworkData> wifiNetworks = new List<NetworkData>();
            if (_wifiManager.IsWifiEnabled)
            {
                _wifiManager?.StartScan();
                var scanResults = _wifiManager?.ScanResults;
                foreach (var result in scanResults)
                {
                    wifiNetworks.Add(new NetworkData()
                    {
                        Bssid = result.Bssid,
                        Ssid = result.Ssid,
                        NativeObject = result
                    });
                }
            }
            else
            {
                Console.WriteLine("WI-Fi turned off");
            }
            return wifiNetworks;
        }

        private class NetworkCallback : ConnectivityManager.NetworkCallback
        {
            public Action<Network> NetworkAvailable { get; set; }
            public Action NetworkUnavailable { get; set; }

            public NetworkCallback(int flags)
            {
            }
            public NetworkCallback()
            {
            }
            public override void OnAvailable(Network network)
            {
                base.OnAvailable(network);
                NetworkAvailable?.Invoke(network);
            }

            public override void OnUnavailable()
            {
                base.OnUnavailable();
                NetworkUnavailable?.Invoke();
            }
            public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities)
            {
                base.OnCapabilitiesChanged(network, networkCapabilities);
                WifiInfo wifiInfo = (WifiInfo)networkCapabilities.TransportInfo;

                if (wifiInfo != null)
                {
                    if (wifiInfo.SupplicantState == SupplicantState.Completed)
                    {
                        _networkData.StausId = 1;
                        _networkData.Ssid = wifiInfo?.SSID?.Trim(new char[] { '"', '\"' });
                        _networkData.Bssid = wifiInfo?.BSSID;
                        _networkData.IpAddress = wifiInfo.IpAddress;
                        _networkData.NativeObject = wifiInfo;
                        _networkData.SignalStrength = wifiInfo.Rssi;
                    }
                }
            }
        }

        [Flags]
        public enum NetworkCallbackFlags
        {
            //
            // Summary:
            //     To be added.
            [IntDefinition(null, JniField = "")]
            None = 0x0,
            //
            // Summary:
            //     To be added.
            [IntDefinition("Android.Net.ConnectivityManager.NetworkCallback.FlagIncludeLocationInfo", JniField = "android/net/ConnectivityManager$NetworkCallback.FLAG_INCLUDE_LOCATION_INFO")]
            IncludeLocationInfo = 0x1
        }

        private class WifiScanReceiver : BroadcastReceiver
        {
            public List<ScanResult> ScanResults { get; private set; }
            public WifiScanReceiver(WifiNetworkService wifiScanner)
            {
                ScanResults = new List<ScanResult>();
            }
            public override void OnReceive(Context context, Intent intent)
            {
            }
        }
    }
}
