using Core;
using Nakama;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Server
{
    [CreateAssetMenu(
        fileName = "NetworkSettings",
        menuName = "Project/Network Settings",
        order = 100
    )]
    public class NetworkSettings : ScriptableObject
    {
        public NetworkBackendType backendType = NetworkBackendType.Nakama;

        // Nakama-specific config lives here, not in EntryPoint.
        public AssetReferenceT<NakamaSettings> nakamaSettingsAsset;

        // Later:
        // public PhotonSettings photonSettings;
    }

    public enum NetworkBackendType
    {
        None,
        Nakama,
        // Photon,
        // Custom,
    }
}
