using System;

namespace PlayablesStudio.Plugins.YandexGamesSDK.Runtime.Types
{
    [Serializable]
    public class UserProfile
    {
        public string id;
        public string name;
        public string avatarUrlSmall;
        public string avatarUrlMedium;
        public string avatarUrlLarge;
        public bool isAuthorized;
        public string signature;
    }
}
