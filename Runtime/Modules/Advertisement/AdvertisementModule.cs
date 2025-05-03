using AOT;
using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Dashboard;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Abstractions;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Networking;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Logging;
using UnityEngine;
using UnityEngine.Events;

namespace PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Advertisement
{
    [Serializable]
    public enum YGAdResponse
    {
        AdOpened,
        AdClosed,
        AdRewarded,
        BannerShown,
        BannerHidden
    }

    public class AdvertisementModule : YGModuleBase, IAdvertisementModule
    {
        private YGPauseSettings _pauseSettings => YandexGamesSDKConfig.Instance.pauseSettings;

        public UnityEvent OnAdOpened = new UnityEvent();
        public UnityEvent OnAdClosed = new UnityEvent();
        public UnityEvent OnAdRewarded = new UnityEvent();

        private bool originalAudioPause;
        private float originalTimeScale;
        private bool originalCursorVisible;
        private CursorLockMode originalCursorLockMode;

        #region Static Callbacks

        private static Action<bool, YGAdResponse, string> s_interstitialCallback;
        private static Action<bool, YGAdResponse, string> s_rewardedCallback;
        private static Action<bool, YGAdResponse, string> s_bannerCallback;
        private static AdvertisementModule s_instance;

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleInterstitialResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<JSResponse<AdResponseData>>(jsonResponse);
            if (response.status && response.data != null)
            {
                if (Enum.TryParse<YGAdResponse>(response.data.response, out var adResponse))
                {
                    if (adResponse == YGAdResponse.AdOpened)
                        s_instance.HandleAdOpened();
                    else if (adResponse == YGAdResponse.AdClosed)
                        s_instance.HandleAdClosed();

                    s_interstitialCallback?.Invoke(true, adResponse, null);
                }
                else
                {
                    s_interstitialCallback?.Invoke(true, default, null);
                }
            }
            else
            {
                s_interstitialCallback?.Invoke(false, default, response.error);
            }
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleRewardedResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<JSResponse<AdResponseData>>(jsonResponse);
            if (response.status && response.data != null)
            {
                if (Enum.TryParse<YGAdResponse>(response.data.response, out var adResponse))
                {
                    if (adResponse == YGAdResponse.AdOpened)
                        s_instance.HandleAdOpened();
                    else if (adResponse == YGAdResponse.AdClosed)
                        s_instance.HandleAdClosed();
                    else if (adResponse == YGAdResponse.AdRewarded)
                        s_instance.OnAdRewarded?.Invoke();

                    s_rewardedCallback?.Invoke(true, adResponse, null);
                }
                else
                {
                    s_rewardedCallback?.Invoke(true, default, null);
                }
            }
            else
            {
                s_rewardedCallback?.Invoke(false, default, response.error);
            }
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleBannerResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<JSResponse<AdResponseData>>(jsonResponse);
            if (response.status && response.data != null)
            {
                if (Enum.TryParse<YGAdResponse>(response.data.response, out var adResponse))
                {
                    s_bannerCallback?.Invoke(true, adResponse, null);
                }
                else
                {
                    s_bannerCallback?.Invoke(true, default, null);
                }
            }
            else
            {
                s_bannerCallback?.Invoke(false, default, response.error);
            }
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleErrorResponse(string errorJson)
        {
            var response = JsonConvert.DeserializeObject<JSResponse<AdResponseData>>(errorJson);
            s_interstitialCallback?.Invoke(false, default, response.error);
            s_rewardedCallback?.Invoke(false, default, response.error);
            s_bannerCallback?.Invoke(false, default, response.error);
        }

        #endregion

        #region DllImports

        [DllImport("__Internal")]
        private static extern void AdvertisementApi_ShowInterstitialAd(Action<string> successCallback,
            Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern void AdvertisementApi_ShowRewardedAd(Action<string> successCallback,
            Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern void AdvertisementApi_ShowBannerAd(string position, Action<string> successCallback,
            Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern void AdvertisementApi_HideBannerAd(Action<string> successCallback,
            Action<string> errorCallback);

        #endregion

        public override void Initialize()
        {
            s_instance = this;
            YGLogger.Debug("Advertisement Module initialized");
        }

        public void ShowInterstitialAd(Action<bool, YGAdResponse, string> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGamesSDK.IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, default, "SDK is not initialized");
                return;
            }
            
            s_interstitialCallback = callback;
            AdvertisementApi_ShowInterstitialAd(HandleInterstitialResponse, HandleErrorResponse);
#else
            YGLogger.Debug("Interstitial ads are only available in WebGL builds.");
            callback?.Invoke(false, default, "Interstitial ads are only available in WebGL builds.");
#endif
        }

        public void ShowRewardedAd(Action<bool, YGAdResponse, string> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGamesSDK.IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, default, "SDK is not initialized");
                return;
            }
            
            s_rewardedCallback = callback;
            AdvertisementApi_ShowRewardedAd(HandleRewardedResponse, HandleErrorResponse);
#else
            YGLogger.Debug("Rewarded ads are only available in WebGL builds.");
            callback?.Invoke(false, default, "Rewarded ads are only available in WebGL builds.");
#endif
        }

        public void ShowBannerAd(string position = "bottom", Action<bool, YGAdResponse, string> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGamesSDK.IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, default, "SDK is not initialized");
                return;
            }
            
            s_bannerCallback = callback;
            AdvertisementApi_ShowBannerAd(position, HandleBannerResponse, HandleErrorResponse);
#else
            YGLogger.Debug("Banner ads are only available in WebGL builds.");
            callback?.Invoke(false, default, "Banner ads are only available in WebGL builds.");
#endif
        }

        public virtual void HideBannerAd(Action<bool, YGAdResponse, string> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGamesSDK.IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, default, "SDK is not initialized");
                return;
            }
            
            s_bannerCallback = callback;
            AdvertisementApi_HideBannerAd(HandleBannerResponse, HandleErrorResponse);
#else
            YGLogger.Debug("Banner ads are only available in WebGL builds.");
            callback?.Invoke(false, default, "Banner ads are only available in WebGL builds.");
#endif
        }

        protected void HandleAdOpened()
        {
            originalAudioPause = AudioListener.pause;
            originalTimeScale = Time.timeScale;
            originalCursorVisible = Cursor.visible;
            originalCursorLockMode = Cursor.lockState;

            if (_pauseSettings.pauseAudio)
                AudioListener.pause = true;
            if (_pauseSettings.pauseTime)
                Time.timeScale = 0f;
            if (_pauseSettings.hideCursor)
            {
                Cursor.visible = false;
                Cursor.lockState = _pauseSettings.cursorLockMode;
            }

            OnAdOpened?.Invoke();
        }

        protected void HandleAdClosed()
        {
            AudioListener.pause = originalAudioPause;
            Time.timeScale = originalTimeScale;
            Cursor.visible = originalCursorVisible;
            Cursor.lockState = originalCursorLockMode;

            OnAdClosed?.Invoke();
        }

        private void OnDestroy()
        {
            s_interstitialCallback = null;
            s_rewardedCallback = null;
            s_bannerCallback = null;
            s_instance = null;
        }
    }

    [Serializable]
    public class AdResponseData
    {
        public string response;
        public bool wasShown;
    }
}