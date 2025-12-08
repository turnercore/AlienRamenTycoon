using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core
{
    /// <summary>
    /// Utility that tracks Addressables handles so that callers can easily release them when the owner is disposed.
    /// </summary>
    public sealed class AddressablesHandleHelper : IDisposable
    {
        private readonly List<AsyncOperationHandle> handles = new();

        /// <summary>
        /// Loads an addressable asset, stores the handle internally and returns it for the caller to await/use.
        /// </summary>
        public AsyncOperationHandle<T> LoadAssetAsync<T>(AssetReferenceT<T> assetReference)
            where T : UnityEngine.Object
        {
            var handle = assetReference.LoadAssetAsync<T>();
            RegisterHandle(handle);
            return handle;
        }

        /// <summary>
        /// Loads an addressable asset using an address/key string to support non-AssetReference lookups.
        /// </summary>
        public AsyncOperationHandle<T> LoadAssetAsync<T>(string address)
            where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            RegisterHandle(handle);
            return handle;
        }

        /// <summary>
        /// Loads an asset and blocks until completion, returning the asset instance.
        /// </summary>
        public T LoadAssetAndWait<T>(AssetReferenceT<T> assetReference)
            where T : UnityEngine.Object
        {
            var handle = LoadAssetAsync(assetReference);
            return handle.WaitForCompletion();
        }

        /// <summary>
        /// Loads an asset by address and blocks until completion, returning the asset instance.
        /// </summary>
        public T LoadAssetAndWait<T>(string address)
            where T : UnityEngine.Object
        {
            var handle = LoadAssetAsync<T>(address);
            return handle.WaitForCompletion();
        }

        /// <summary>
        /// Registers an externally created handle so the helper will release it later.
        /// </summary>
        public AsyncOperationHandle<T> RegisterHandle<T>(AsyncOperationHandle<T> handle)
        {
            handles.Add(handle);
            return handle;
        }

        /// <summary>
        /// Releases a single tracked handle early if needed.
        /// </summary>
        public void ReleaseHandle(AsyncOperationHandle handle)
        {
            if (!handle.IsValid())
            {
                return;
            }

            Addressables.Release(handle);
            handles.Remove(handle);
        }

        /// <summary>
        /// Releases all tracked handles. Call this when the owning state is disposed.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var handle in handles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            handles.Clear();
        }

        public void Dispose()
        {
            ReleaseAll();
        }
    }
}
