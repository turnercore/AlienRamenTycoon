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
        public string ServerAddress;

        private string ValidateWebAddress(string address)
        {
            return address;
        }
    }
}
