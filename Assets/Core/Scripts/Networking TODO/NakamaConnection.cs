using System;
using System.Collections;
using System.Linq;
using Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Server
{
    enum NakamaConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Other,
    }

    class NakamaConnection
    {
        public NakamaConnectionStatus Status;
        private NakamaSettings settings;
        private AsyncOperationHandle<NakamaSettings> settingsHandle;
        public string[] errorCodes;

        // Default Server Settings if not set by bootstrapper
        private readonly string _settingsAddress = "Assets/Game/Settings/NakamaSettings.asset";

        // A way for something like the entrypoint to override the server settings for main build
        public AssetReferenceT<NakamaSettings> SettingsAddress;

        public void Dispose()
        {
            // Terminate connection
            CloseConnection();
            // Release server data
            settingsHandle.Release();
            // Clear error codes here
        }

        private void CloseConnection()
        {
            this.Status = NakamaConnectionStatus.Disconnected;
        }

        public IEnumerator Initialize()
        {
            // Add error handling here
            if (SettingsAddress == null)
            {
                // Load settings data addressable with default address, if not found
                AsyncOperationHandle<NakamaSettings> handle =
                    Addressables.LoadAssetAsync<NakamaSettings>(_settingsAddress);
                settingsHandle = handle;
            }
            else
            {
                AsyncOperationHandle<NakamaSettings> handle =
                    Addressables.LoadAssetAsync<NakamaSettings>(SettingsAddress);
                settingsHandle = handle;
            }
            yield return new WaitUntil(() => settingsHandle.IsDone);
            settings = settingsHandle.Result;

            TryToConnect();
        }

        public async void TryToConnect()
        {
            Status = NakamaConnectionStatus.Connecting;
            if (false)
            {
                Status = NakamaConnectionStatus.Connected;
            }
            else
            {
                Status = NakamaConnectionStatus.Error;
                errorCodes.Append<string>("generic connection error");
                LogOutput.Display("Error connecting to server.");
            }
            return;
        }
    }
}
