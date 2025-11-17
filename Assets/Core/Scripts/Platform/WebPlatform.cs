using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core
{
    public class WebPlatform : IPlatform
    {
        private readonly IPlatform platform;
        private readonly AssetReference assetReference;

        private WebPlatformSettings webPlatformSettings;

        public WebPlatform(AssetReference assetReference)
        {
            this.assetReference = assetReference;
        }

        public IEnumerator Initialize(object applicationData)
        {
            if (platform != null)
            {
                yield return platform.Initialize(applicationData);
            }

            var handle = Addressables.LoadAssetAsync<WebPlatformSettings>(assetReference);
            yield return new WaitUntil(() => handle.IsDone);
            webPlatformSettings = handle.Result;
            Debug.Log($"Device Platform {webPlatformSettings.devicePlatform} initialized");
        }

        public IApplicationLifecycle InputHandler()
        {
            var inputHandler = new WebInput((WebInputSettings)webPlatformSettings.inputSettings);
            inputHandler.Initialize();
            return inputHandler;
        }

        public void Tick() { }

        public void Dispose() { }

        public void OnApplicationQuit() { }
    }
}
