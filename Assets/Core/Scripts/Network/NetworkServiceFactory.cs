using Core;

namespace Server
{
    public static class NetworkServiceFactory
    {
        public static INetworkService Create(NetworkSettings settings)
        {
            if (settings == null)
            {
                return new OfflineNetworkService();
            }

            switch (settings.backendType)
            {
                case NetworkBackendType.Nakama:
                {
                    var nakamaConnection = new NakamaConnection
                    {
                        SettingsAddress = settings.nakamaSettingsAsset,
                    };

                    return new NakamaNetworkService(nakamaConnection);
                }

                case NetworkBackendType.None:
                default:
                    return new OfflineNetworkService();
            }
        }
    }
}
