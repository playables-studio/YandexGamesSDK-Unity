using System;
using PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Types;

namespace PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Authentication
{
    public interface IAuthenticationModule
    {
        /// <summary>
        /// Current user profile data
        /// </summary>
        UserProfile CurrentUser { get; }

        /// <summary>
        /// Whether the user is currently authorized
        /// </summary>
        bool IsAuthorized { get; }

        /// <summary>
        /// Whether the user has granted permission to access their personal profile data
        /// </summary>
        bool HasPersonalProfileDataPermission { get; }

        /// <summary>
        /// Event triggered when user authorizes in background during polling
        /// </summary>
        event Action AuthorizedInBackground;

        /// <summary>
        /// Authenticates the user with Yandex Games
        /// </summary>
        /// <param name="requireSignin">Whether to force the sign-in dialog</param>
        /// <param name="callback">Callback with result and error message if any</param>
        void AuthenticateUser(bool requireSignin, Action<bool, string> callback = null);

        /// <summary>
        /// Starts polling for authorization status in background
        /// </summary>
        /// <param name="repeatDelay">Delay between polling attempts</param>
        /// <param name="successCallback">Called when authorization succeeds</param>
        /// <param name="errorCallback">Called if polling fails</param>
        void StartAuthorizationPolling(TimeSpan repeatDelay, Action successCallback = null, Action<string> errorCallback = null);

        /// <summary>
        /// Requests permission to access user's personal profile data
        /// </summary>
        /// <param name="callback">Callback with result, updated profile data, and error message if any</param>
        void RequestProfilePermission(Action<bool, UserProfile, string> callback);

        /// <summary>
        /// Gets the current user profile data
        /// </summary>
        /// <returns>Current user profile or null if not authenticated</returns>
        UserProfile GetUserProfile();
    }
}