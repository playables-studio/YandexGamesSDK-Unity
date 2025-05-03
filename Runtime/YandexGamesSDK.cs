using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Logging;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Abstractions;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Advertisement;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Authentication;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Leaderboard;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.LocalStorage;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Storage;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Networking;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Types;
using UnityEngine;
using Newtonsoft.Json;
using AOT;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Purchases;

namespace PlayablesStudio.Plugins.YandexGamesSDK.Runtime
{
    [DefaultExecutionOrder(-100)]
    public sealed class YandexGamesSDK : MonoBehaviour
    {
        private static YandexGamesSDK _instance;
        private static bool _isInitialized = false;

        /// <summary>
        /// Enable to log SDK callbacks in the console.
        /// </summary>
        public static bool VerboseLogging { get; set; } = false;

        /// <summary>
        /// SDK is initialized automatically on load.
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return YandexGamesPlugin_IsInitialized() == 1;
#else
                return _isInitialized;
#endif
            }
            private set { _isInitialized = value; }
        }

        /// <summary>
        /// Use to check whether you're running on Yandex platform
        /// </summary>
        public static bool IsRunningOnYandex
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return YandexGamesPlugin_IsRunningOnYandex() == 1;
#else
                return false;
#endif
            }
        }

        public static YandexGamesSDK Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<YandexGamesSDK>();

                    if (_instance == null)
                    {
                        GameObject sdkObject = new GameObject(nameof(YandexGamesSDK));
                        DontDestroyOnLoad(sdkObject);
                        _instance = sdkObject.AddComponent<YandexGamesSDK>();
                    }
                }

                return _instance;
            }
        }

        #region Static Callbacks

        private static Action<bool, string, string> s_serverTimeCallback;
        private static Action<bool, YGEnvironmentData, string> s_environmentCallback;
        private static Action<bool, string> s_gameplayReadyCallback;
        private static Action<bool, string> s_gameplayStartCallback;
        private static Action<bool, string> s_gameplayStopCallback;
        private static Action<bool, InitData, string> s_initializeCallback;

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleInitializeResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<YGJSResponse<InitData>>(jsonResponse);
            s_initializeCallback?.Invoke(response.status, response.data, response.error);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleServerTimeResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<YGJSResponse<string>>(jsonResponse);
            s_serverTimeCallback?.Invoke(response.status, response.data, response.error);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleEnvironmentResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<YGJSResponse<YGEnvironmentData>>(jsonResponse);
            s_environmentCallback?.Invoke(response.status, response.data, response.error);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleGameplayReadyResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<YGJSResponse<object>>(jsonResponse);
            s_gameplayReadyCallback?.Invoke(response.status, response.error);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleGameplayStartResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<YGJSResponse<object>>(jsonResponse);
            s_gameplayStartCallback?.Invoke(response.status, response.error);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleGameplayStopResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<YGJSResponse<object>>(jsonResponse);
            s_gameplayStopCallback?.Invoke(response.status, response.error);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleErrorResponse(string errorJson)
        {
            var response = JsonConvert.DeserializeObject<YGJSResponse<object>>(errorJson);

            s_serverTimeCallback?.Invoke(false, null, response.error);
            s_environmentCallback?.Invoke(false, null, response.error);
            s_gameplayReadyCallback?.Invoke(false, response.error);
            s_gameplayStartCallback?.Invoke(false, response.error);
            s_gameplayStopCallback?.Invoke(false, response.error);
        }

        #endregion

        #region DllImport Methods

        [DllImport("__Internal")]
        private static extern void YandexGamesPlugin_Initialize(Action<string> successCallback,
            Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern void YandexGamesPlugin_GetServerTime(Action<string> successCallback,
            Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern void YandexGamesPlugin_GetEnvironment(Action<string> successCallback,
            Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern void YandexGamesPlugin_SetGameplayReady(Action<string> successCallback,
            Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern void YandexGamesPlugin_SetGameplayStart(Action<string> successCallback,
            Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern void YandexGamesPlugin_SetGameplayStop(Action<string> successCallback,
            Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern int YandexGamesPlugin_IsInitialized();

        [DllImport("__Internal")]
        private static extern int YandexGamesPlugin_IsRunningOnYandex();

        [DllImport("__Internal")]
        private static extern int YandexGamesPlugin_GetDeviceType();

        #endregion

        public IAuthenticationModule Authentication { get; private set; }
        public ICloudStorageModule CloudStorage { get; private set; }
        public ILocalStorageModule LocalStorage { get; private set; }
        public ILeaderboardModule Leaderboard { get; private set; }
        public IAdvertisementModule Advertisement { get; private set; }
        public IPurchaseModule Purchases { get; private set; }

        private readonly Dictionary<Type, YGModuleBase> _modules = new Dictionary<Type, YGModuleBase>();
        private Action _onInitializeCallback;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

#if !UNITY_WEBGL || UNITY_EDITOR
                InitializeModules();
                IsInitialized = true;
#endif
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize the SDK. Must be called before using any Yandex functionality.
        /// </summary>
        /// <returns>Coroutine that waits for initialization to complete</returns>
        public IEnumerator Initialize()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            bool isComplete = false;
            string error = null;

            s_initializeCallback = (success, data, errorMsg) =>
            {
                isComplete = true;
                if (!success) error = errorMsg;
            };

            YandexGamesPlugin_Initialize(HandleInitializeResponse, HandleErrorResponse);

            while (!isComplete)
                yield return null;
          
            InitializeModules();

            if (error != null)
                Debug.LogError($"Failed to initialize Yandex Games SDK: {error}");
#else
            YGLogger.Debug("SDK initialization skipped in editor.");
            InitializeModules();
            IsInitialized = true;
            yield break;
#endif
        }

        public void GetServerTime(Action<bool, string, string> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, null, "SDK is not initialized");
                return;
            }

            s_serverTimeCallback = callback;
            YandexGamesPlugin_GetServerTime(HandleServerTimeResponse, HandleErrorResponse);
#else
            YGLogger.Debug("GetServerTime is only available in WebGL builds.");
            callback?.Invoke(false, null, "GetServerTime is only available in WebGL builds.");
#endif
        }

        public void GetEnvironment(Action<bool, YGEnvironmentData, string> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, null, "SDK is not initialized");
                return;
            }

            s_environmentCallback = callback;
            YandexGamesPlugin_GetEnvironment(HandleEnvironmentResponse, HandleErrorResponse);
#else
            YGLogger.Debug("GetEnvironment is only available in WebGL builds.");
            callback?.Invoke(false, null, "GetEnvironment is only available in WebGL builds.");
#endif
        }

        public void SetGameplayReady(Action<bool, string> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, "SDK is not initialized");
                return;
            }

            s_gameplayReadyCallback = callback;
            YandexGamesPlugin_SetGameplayReady(HandleGameplayReadyResponse, HandleErrorResponse);
#else
            YGLogger.Debug("SetGameplayReady is only available in WebGL builds.");
            callback?.Invoke(false, "SetGameplayReady is only available in WebGL builds.");
#endif
        }

        public void SetGameplayStart(Action<bool, string> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, "SDK is not initialized");
                return;
            }

            YandexGamesPlugin_SetGameplayStart(HandleGameplayStartResponse, HandleErrorResponse);
#else
            YGLogger.Debug("SetGameplayStart is only available in WebGL builds.");
            callback?.Invoke(false, "SetGameplayStart is only available in WebGL builds.");
#endif
        }

        public void SetGameplayStop(Action<bool, string> callback = null)
        {
#if UNITY_WEBGL&& !UNITY_EDITOR
            if (!IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, "SDK is not initialized");
                return;
            }

            YandexGamesPlugin_SetGameplayStop(HandleGameplayStopResponse, HandleErrorResponse);
#else
            YGLogger.Debug("SetGameplayStop is only available in WebGL builds.");
            callback?.Invoke(false, "SetGameplayStop is only available in WebGL builds.");
#endif
        }

        private void InitializeModules()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            LocalStorage = LoadAndInitializeModule<PlayerPrefsLocalStorageModule>();
            Authentication = LoadAndInitializeModule<AuthenticationModule>();
            Advertisement = LoadAndInitializeModule<AdvertisementModule>();
            Leaderboard = LoadAndInitializeModule<LeaderboardModule>();
            CloudStorage = LoadAndInitializeModule<CloudStorageModule>();
            Purchases = LoadAndInitializeModule<PurchaseModule>();
#elif UNITY_EDITOR
            CloudStorage = LoadAndInitializeModule<MockCloudStorageModule>();

            YGLogger.Debug("Modules initialized with mock data settings in editor.");
#endif
        }

        public TModule GetModule<TModule>() where TModule : YGModuleBase
        {
            var moduleType = typeof(TModule);

            if (_modules.TryGetValue(moduleType, out var module))
            {
                return (TModule)module;
            }

            var registeredModule = GetComponent<TModule>()
                                   ?? gameObject.AddComponent<TModule>();
            _modules[moduleType] = registeredModule;
            registeredModule.Initialize();

            return registeredModule;
        }

        private TModule LoadAndInitializeModule<TModule>() where TModule : YGModuleBase
        {
            var moduleType = typeof(TModule);

            var module = GetComponent<TModule>() ?? gameObject.AddComponent<TModule>();
            _modules[moduleType] = module;

            module.Initialize();

            return module;
        }
        
        [JsonObject]
        private class InitData
        {
            [JsonProperty("initialized")] public bool initialized;
        }

        private void OnDestroy()
        {
            s_serverTimeCallback = null;
            s_environmentCallback = null;
            s_gameplayReadyCallback = null;
            s_gameplayStartCallback = null;
            s_gameplayStopCallback = null;
            s_initializeCallback = null;
        }

        /// <summary>
        /// Gets the type of device the game is running on.
        /// </summary>
        /// <returns>The YGDeviceType enum representing the device type.</returns>
        public YGDeviceType GetDeviceType()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                return YGDeviceType.Desktop; // Default or throw error
            }
            return (YGDeviceType)YandexGamesPlugin_GetDeviceType();
#else
            YGLogger.Debug("GetDeviceType is only available in WebGL builds. Returning Desktop.");
            return YGDeviceType.Desktop; // Default for editor
#endif
        }
    }
    
    [JsonObject]
    public class YGJSResponse<T>
    {
        [JsonProperty("status")] public bool status;

        [JsonProperty("data")] public T data;

        [JsonProperty("error")] public string error;
    }
}