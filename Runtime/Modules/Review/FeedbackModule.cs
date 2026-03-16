using AOT;
using Newtonsoft.Json;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Logging;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Abstractions;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Networking;
using System;
using System.Runtime.InteropServices;

namespace PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Feedback
{
    public class FeedbackModule : YGModuleBase, IFeedbackModule
    {
        #region Static Callbacks

        private static Action<bool, bool, string> s_canReviewCallback;
        private static Action<bool, bool, string> s_requestReviewCallback;
        private static FeedbackModule s_instance;

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleCanReviewResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<JSResponse<bool>>(jsonResponse);
            if (response.status)
            {
                s_canReviewCallback?.Invoke(true, response.data, null);
            }
            else
            {
                s_canReviewCallback?.Invoke(false, default, response.error);
            }
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleCanReviewError(string errorJson)
        {
            var response = JsonConvert.DeserializeObject<JSResponse<bool>>(errorJson);
            s_canReviewCallback?.Invoke(false, default, response.error);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleRequestReviewResponse(string jsonResponse)
        {
            var response = JsonConvert.DeserializeObject<JSResponse<bool>>(jsonResponse);
            if (response.status)
            {
                s_requestReviewCallback?.Invoke(true, response.data, null);
            }
            else
            {
                s_requestReviewCallback?.Invoke(false, default, response.error);
            }
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void HandleRequestReviewError(string errorJson)
        {
            var response = JsonConvert.DeserializeObject<JSResponse<bool>>(errorJson);
            s_requestReviewCallback?.Invoke(false, default, response.error);
        }

        [DllImport("__Internal")]
        private static extern void FeedbackApi_CanReview(Action<string> successCallback, Action<string> errorCallback);

        [DllImport("__Internal")]
        private static extern void FeedbackApi_RequestReview(Action<string> successCallback, Action<string> errorCallback);

        #endregion

        public override void Initialize()
        {
            s_instance = this;
            YGLogger.Debug("Review Module initialized");
        }

        public void CanReview(Action<bool, bool, string> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGamesSDK.IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, default, "SDK is not initialized");
                return;
            }

            s_canReviewCallback = callback;
            FeedbackApi_CanReview(HandleCanReviewResponse, HandleCanReviewError);
#else
            YGLogger.Debug("Feedback is only available in WebGL builds.");
            callback?.Invoke(false, default, "Feedback is only available in WebGL builds.");
#endif
        }

        public void RequestReview(Action<bool, bool, string> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGamesSDK.IsInitialized)
            {
                YGLogger.Error("SDK is not initialized. Call Initialize first.");
                callback?.Invoke(false, default, "SDK is not initialized");
                return;
            }

            s_requestReviewCallback = callback;
            FeedbackApi_RequestReview(HandleRequestReviewResponse, HandleRequestReviewError);
#else
            YGLogger.Debug("Feedback is only available in WebGL builds.");
            callback?.Invoke(false, default, "Feedback is only available in WebGL builds.");
#endif
        }

        private void OnDestroy()
        {
            s_canReviewCallback = null;
            s_requestReviewCallback = null;
            s_instance = null;
        }
    }
}
