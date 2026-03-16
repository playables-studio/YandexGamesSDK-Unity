using System;

namespace PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Modules.Feedback
{
    public interface IFeedbackModule
    {
        void CanReview(Action<bool, bool, string> callback = null);
        void RequestReview(Action<bool, bool, string> callback = null);
    }
}
