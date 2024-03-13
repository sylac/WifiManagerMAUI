using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Plugin.MauiWifiManager.Abstractions;
using System.Runtime.Versioning;
using ActivityContext = Android.App.Application;
using Context = Android.Content.Context;

namespace Plugin.MauiWifiManager
{
    /// <summary>
    /// Interface for WiFiNetworkService
    /// </summary>
    /// 
    public partial class WifiNetworkService : IWifiNetworkService
    {
        private static ConnectivityManager? _connectivityManager;
        private static bool _requested;
        private static NetworkData _networkData = new();

        private readonly NetworkCallback _callback;
        private readonly NetworkRequest _request;
        private readonly WifiManager _wifiManager;
        public WifiNetworkService()
        {
            _wifiManager = (WifiManager?)(ActivityContext.Context?.GetSystemService(Context.WifiService))
                ?? throw new InvalidOperationException("WifiManager is not available. Ensure the service is correctly initialized.");

            _request = new NetworkRequest.Builder()
                .AddTransportType(transportType: TransportType.Wifi)
                ?.Build()
                ?? throw new InvalidOperationException("  ");
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
            if (OperatingSystem.IsAndroidVersionAtLeast(30)) // Android 11 and above
            {
                //var wifiConnectedTcs = new TaskCompletionSource<bool>();
                //ActivityContext.Context.RegisterReceiver(new WifiStateChangeReceiver((sender, wifiInfo) =>
                //{
                //    if (wifiInfo?.SSID == ssid)
                //    {
                //        wifiConnectedTcs.SetResult(true);
                //    }
                //}), new IntentFilter(WifiManager.NetworkStateChangedAction));

                //await wifiConnectedTcs.Task;
                await AddWifi(ssid, password);
            }
            else if (OperatingSystem.IsAndroidVersionAtLeast(29)) // Android 10
            {
                RequestNetwork(ssid, password);
            }
            else // Android 10 and below
            {
                if (!_wifiManager!.IsWifiEnabled)
                {
                    _wifiManager.SetWifiEnabled(true);
                }

                if (_wifiManager.ConnectionInfo?.SSID != string.Format("\"{0}\"", ssid))
                {
                    var wifiConfig = new WifiConfiguration
                    {
                        Ssid = string.Format("\"{0}\"", ssid),
                        PreSharedKey = string.Format("\"{0}\"", password)
                    };

                    int netId = _wifiManager.AddNetwork(wifiConfig);
                    _wifiManager.Disconnect();
                    _wifiManager.EnableNetwork(netId, true);
                    _wifiManager.Reconnect();
                    _networkData.Ssid = ssid;

                }
                else
                {
                    Console.WriteLine("Cannot find valid SSID");
                }
            }

            return _networkData;
        }

        /// <summary>
        /// Disconnect Wi-Fi
        /// From Android Q (Android 10) you can't enable/disable wifi programmatically anymore. 
        /// So, use Settings Panel to toggle wifi connectivity
        /// </summary>
        public void DisconnectWifi(string? ssid)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                var panelIntent = new Intent(Settings.Panel.ActionWifi);
                ActivityContext.Context.StartActivity(panelIntent);
            }
            else
            {
                _wifiManager?.SetWifiEnabled(false); // Disable wifi
                _wifiManager?.SetWifiEnabled(true); // Enable wifi
            }
        }

        /// <summary>
        /// Get Wi-Fi Network Info
        /// </summary>
        public async Task<NetworkData> GetNetworkInfo()
        {
            int apiLevel = (int)Build.VERSION.SdkInt;
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                ConnectivityManager? connectivityManager = ActivityContext.Context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
                NetworkInfo? activeNetworkInfo = connectivityManager?.ActiveNetworkInfo;

                if (connectivityManager is null
                    || activeNetworkInfo is null)
                {
                    Console.WriteLine("Failed to get data");
                    return _networkData;
                }

                var networkCallback = new NetworkCallback((int)NetworkCallbackFlags.IncludeLocationInfo);
                connectivityManager.RequestNetwork(_request, networkCallback);
            }
            else
            {
                if (_wifiManager.IsWifiEnabled)
                {
                    _networkData.Ssid = _wifiManager.ConnectionInfo?.SSID?.Trim(new char[] { '"', '\"' });
                    _networkData.IpAddress = _wifiManager.DhcpInfo?.IpAddress ?? 0;
                    _networkData.GatewayAddress = _wifiManager.DhcpInfo?.Gateway.ToString();
                    _networkData.NativeObject = _wifiManager;
                }
                else
                {
                    Console.WriteLine("WI-Fi turned off");
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
            Intent panelIntent;
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                panelIntent = new Intent(Settings.Panel.ActionWifi);
            else
                panelIntent = new Intent(Settings.ActionWifiSettings);

            panelIntent.SetFlags(ActivityFlags.NewTask);
            ActivityContext.Context.StartActivity(panelIntent);

            return Task.FromResult(true);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose() { }

        [SupportedOSPlatform("android30.0")]
        private async Task AddWifi(string ssid, string psk)
        {
            await Task.Run(async () =>
            {
                var suggestions = new List<WifiNetworkSuggestion>
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

                var status = _wifiManager.AddNetworkSuggestions(suggestions);

                var taskCompletionSource = new TaskCompletionSource();
                var intentFilter = new IntentFilter(WifiManager.ActionWifiNetworkSuggestionPostConnection);
                ActivityContext.Context.RegisterReceiver(new WifiSuggestionReceiver((sender, _) =>
                {
                    taskCompletionSource.SetResult();
                }), intentFilter);

                await taskCompletionSource.Task;

                //await OpenWifiSetting();
                //var bundle = new Bundle();
                //bundle.PutParcelableArrayList("android.provider.extra.WIFI_NETWORK_LIST", suggestions);
                //var intent = new Intent(Settings.ActionWifiAddNetworks);
                //intent.PutExtras(bundle);
                //intent.SetFlags(ActivityFlags.NewTask);
                //ActivityContext.Context.StartActivity(intent);
            });
        }

        [SupportedOSPlatform("android29.0")]
        [ObsoletedOSPlatform("android30.0")]
        public void RequestNetwork(string ssid, string password)
        {
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
            _connectivityManager = ActivityContext.Context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
            if (_requested)
            {
                _connectivityManager?.UnregisterNetworkCallback(_callback);
            }
            _connectivityManager?.RequestNetwork(request, _callback);
            _requested = true;
        }

        private static void UnregisterNetworkCallback(NetworkCallback networkCallback)
        {
            if (networkCallback != null)
            {
                _connectivityManager = ActivityContext.Context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
                _connectivityManager?.UnregisterNetworkCallback(networkCallback);
            }
        }

        /// <summary>
        /// Scan Wi-Fi Networks
        /// </summary>
        public Task<List<NetworkData>> ScanWifiNetworks()
        {
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

            return Task.FromResult(wifiNetworks);
        }

        private class NetworkCallback : ConnectivityManager.NetworkCallback
        {
            public Action<Network>? NetworkAvailable { get; set; }
            public Action? NetworkUnavailable { get; set; }

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

        private class WifiStateChangeReceiver : BroadcastReceiver
        {
            private EventHandler<WifiInfo?>? _networkSSIDChanged;

            public WifiStateChangeReceiver(EventHandler<WifiInfo?>? networkChangedHandler)
            {
                _networkSSIDChanged = networkChangedHandler;
            }

            public override void OnReceive(Context? context, Intent? intent)
            {
                if (intent?.Action == WifiManager.NetworkStateChangedAction)
                {
                    WifiManager? wifiManager = (WifiManager?)ActivityContext.Context.GetSystemService(Context.WifiService);
                    if (wifiManager?.ConnectionInfo != null)
                        _networkSSIDChanged?.Invoke(this, wifiManager.ConnectionInfo);
                }
            }
        }

        private class WifiSuggestionReceiver : BroadcastReceiver
        {
            private EventHandler? _networkSSIDChanged;

            public WifiSuggestionReceiver(EventHandler? networkChangedHandler)
            {
                _networkSSIDChanged = networkChangedHandler;
            }

            public override void OnReceive(Context? context, Intent? intent)
            {
                if (intent?.Action == WifiManager.ActionWifiNetworkSuggestionPostConnection)
                {
                    _networkSSIDChanged?.Invoke(this, new EventArgs());
                }
            }
        }
    }
}
