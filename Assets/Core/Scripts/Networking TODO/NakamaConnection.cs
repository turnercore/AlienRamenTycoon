using System;
using System.Collections;
using System.Linq;
using Core;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

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

        //I am not sure how to locate this with labels or dynamically, something to look up,
        // for now need to copy the string and paste here
        private readonly string _settingsAddress = "Assets/Game/Settings/NakamaSettings.asset";

        public void Dispose()
        {
            // Terminate connection
            CloseConnection();
            // Release server data
            settingsHandle.Release();
        }

        private void CloseConnection()
        {
            this.Status = NakamaConnectionStatus.Disconnected;
        }

        public IEnumerator Initialize()
        {
            // Load settings data addressable, if not found error
            AsyncOperationHandle<NakamaSettings> handle =
                Addressables.LoadAssetAsync<NakamaSettings>(_settingsAddress);
            settingsHandle = handle;
            yield return new WaitUntil(() => handle.IsDone);
            settings = handle.Result;

            TryToConnect();
            // Clear error codes
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
