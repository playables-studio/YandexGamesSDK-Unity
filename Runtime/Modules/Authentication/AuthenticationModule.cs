using AOT;
using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Abstractions;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Networking;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Types;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Logging;

namespace PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Authentication
{
    public class AuthenticationModule : YGModuleBase, IAuthenticationModule
    {
        public UserProfile CurrentUser { get; private set; }
        public bool IsAuthorized => GetPlayerAccountIsAuthorized();

        public bool HasPersonalProfileDataPermission => GetPlayerAccountHasPersonalProfileDataPermission();

        public event Action AuthorizedInBackground;

        #region Static Callbacks

        private static Action<bool, string> s_authCallback;
        private static Action<bool, UserProfile, string> s_profilePermissionCallback;
        private static Action s_pollingSuccessCallback;
        private static Action s_pollingErrorCallback;
        private static AuthenticationModule s_instance;

        private static bool GetPlayerAccountIsAuthorized()
        {
            return AuthenticationApi_GetPlayerAccountIsAuthorized();
        }

        private static bool GetPlayerAccountHasPersonalProfileDataPermission()
        {
            return AuthenticationApi_GetPlayerAccountHasPersonalProfileDataPermission();
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleAuthenticationResponse(string jsonResponse)
        {
            try
            {
                YGLogger.Debug($"HandleAuthenticationResponse. jsonResponse: {jsonResponse}");
                var response = JsonConvert.DeserializeObject<JSResponse<UserProfile>>(jsonResponse);
                if (response.status && response.data != null)
                {
                    s_instance.CurrentUser = response.data;
                    YGLogger.Debug($"Authentication successful. User: {s_instance.CurrentUser.name}, ID: {s_instance.CurrentUser.id}");
                    s_authCallback?.Invoke(true, null);
                }
                else
                {
                    s_authCallback?.Invoke(false, response.error);
                }
            }
            catch (Exception ex)
            {
                YGLogger.Error($"Authentication failed: {ex.Message}");
                s_authCallback?.Invoke(false, ex.Message);
            }
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleProfilePermissionResponse(string jsonResponse)
        {
            try
            {
                YGLogger.Debug($"HandleProfilePermissionResponse. jsonResponse: {jsonResponse}");

                var response = JsonConvert.DeserializeObject<JSResponse<UserProfile>>(jsonResponse);
                if (response.status && response.data != null)
                {
                    s_instance.CurrentUser = response.data;
                    YGLogger.Debug($"Profile permission granted for user: {s_instance.CurrentUser.name}");
                    s_profilePermissionCallback?.Invoke(true, s_instance.CurrentUser, null);
                }
                else
                {
                    s_profilePermissionCallback?.Invoke(false, null, response.error);
                }
            }
            catch (Exception ex)
            {
                YGLogger.Error($"Profile permission request failed: {ex.Message}");
                s_profilePermissionCallback?.Invoke(false, null, ex.Message);
            }
        }

        [MonoPInvokeCallback(typeof(Action))]
        private static void HandlePollingSuccessCallback()
        {
            YGLogger.Debug("Authorization polling succeeded");
            s_instance.AuthorizedInBackground?.Invoke();
            s_pollingSuccessCallback?.Invoke();
        }

        [MonoPInvokeCallback(typeof(Action))]
        private static void HandlePollingErrorCallback()
        {
            YGLogger.Error("Authorization polling failed");
            s_pollingErrorCallback?.Invoke();
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleErrorResponse(string error)
        {
            YGLogger.Error($"Operation failed: {error}");
            s_authCallback?.Invoke(false, error);
            s_profilePermissionCallback?.Invoke(false, null, error);
        }

        #endregion

        #region DllImports

        [DllImport("__Internal")]
        private static extern bool AuthenticationApi_GetPlayerAccountIsAuthorized();

        [DllImport("__Internal")]
        private static extern bool AuthenticationApi_GetPlayerAccountHasPersonalProfileDataPermission();

        [DllImport("__Internal")]
        private static extern void AuthenticationApi_AuthenticateUser(bool requireSignin,
            Action<string> successCallback, Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern void AuthenticationApi_StartAuthorizationPolling(int repeatDelay,
            Action successCallback, Action errorCallback);

        [DllImport("__Internal")]
        private static extern void AuthenticationApi_RequestProfilePermission(
            Action<string> successCallback, Action<string> errorCallback);

        #endregion

        public override void Initialize()
        {
            s_instance = this;
            YGLogger.Debug("Authentication Module initialized");
        }

        public void AuthenticateUser(bool requireSignin, Action<bool, string> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGamesSDK.IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, "SDK is not initialized");
                return;
            }

            s_authCallback = callback;
            AuthenticationApi_AuthenticateUser(requireSignin, HandleAuthenticationResponse, HandleErrorResponse);
#else
            YGLogger.Debug("Authentication is only available in WebGL builds.");
            
            CurrentUser = new UserProfile
            {
                id = "mock-user-id",
                name = "Mock User",
                isAuthorized = true,
                avatarUrlSmall = "",
                avatarUrlMedium = "",
                avatarUrlLarge = ""
            };
            
            callback?.Invoke(false, "Authentication is only available in WebGL builds.");
#endif
        }

        public void StartAuthorizationPolling(int repeatDelay, Action successCallback = null,
            Action errorCallback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGamesSDK.IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                errorCallback?.Invoke();
                return;
            }

            s_pollingSuccessCallback = successCallback;
            s_pollingErrorCallback = errorCallback;
            AuthenticationApi_StartAuthorizationPolling(repeatDelay, HandlePollingSuccessCallback,
                HandlePollingErrorCallback);
#else
            YGLogger.Debug("Authorization polling is only available in WebGL builds.");
            errorCallback?.Invoke();
#endif
        }

        public void RequestProfilePermission(Action<bool, UserProfile, string> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGamesSDK.IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, null, "SDK is not initialized");
                return;
            }

            if (!IsAuthorized)
            {
                YGLogger.Error("User must be authenticated before requesting permissions.");
                callback?.Invoke(false, null, "User not authenticated");
                return;
            }

            s_profilePermissionCallback = callback;
            AuthenticationApi_RequestProfilePermission(HandleProfilePermissionResponse, HandleErrorResponse);
#else
            YGLogger.Debug("Profile permissions are only available in WebGL builds.");
            callback?.Invoke(false, null, "Profile permissions are only available in WebGL builds.");
#endif
        }

        public UserProfile GetUserProfile() => CurrentUser;

        private void OnDestroy()
        {
            s_authCallback = null;
            s_profilePermissionCallback = null;
            s_pollingSuccessCallback = null;
            s_pollingErrorCallback = null;
            s_instance = null;
        }
    }
}
