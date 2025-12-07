using Core;
using UnityEngine;

namespace Server
{
    [CreateAssetMenu(
        fileName = "NakamaSettings",
        menuName = "Server/Settings/NakamaServerSettings"
    )]
    public class NakamaSettings : ScriptableObject
    {
        public string Scheme; // "http" / "https"
        public string Host; // "127.0.0.1" or your domain
        public int Port; // 7350 by default
        public string ServerKey; // "defaultkey" unless you changed it
        public int TimeoutSeconds; // optional
        public int SocketConnectTimeoutSeconds; // optional
        public bool UseMainThreadSocket; // optional
    }
}
