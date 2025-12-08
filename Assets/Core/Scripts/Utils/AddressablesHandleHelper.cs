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
        private struct HandleRegistration
        {
            public AsyncOperationHandle Handle;
            public Action Unsubscribe;
        }

        private readonly List<HandleRegistration> registrations = new();

        /// <summary>
        /// Loads an addressable asset, stores the handle internally and returns it for the caller to await/use.
        /// </summary>
        public AsyncOperationHandle<T> LoadAssetAsync<T>(
            AssetReferenceT<T> assetReference,
            Action<AsyncOperationHandle<T>> onCompleted = null
        )
            where T : UnityEngine.Object
        {
            var handle = assetReference.LoadAssetAsync<T>();
            TrackHandle(handle, onCompleted);
            return handle;
        }

        /// <summary>
        /// Loads an addressable asset using an address/key string to support non-AssetReference lookups.
        /// </summary>
        public AsyncOperationHandle<T> LoadAssetAsync<T>(
            string address,
            Action<AsyncOperationHandle<T>> onCompleted = null
        )
            where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            TrackHandle(handle, onCompleted);
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
        public AsyncOperationHandle<T> RegisterHandle<T>(
            AsyncOperationHandle<T> handle,
            Action<AsyncOperationHandle<T>> onCompleted = null
        )
        {
            TrackHandle(handle, onCompleted);
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

            ReleaseRegistration(handle);
        }

        /// <summary>
        /// Releases all tracked handles. Call this when the owning state is disposed.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var registration in registrations)
            {
                registration.Unsubscribe?.Invoke();
                if (registration.Handle.IsValid())
                {
                    Addressables.Release(registration.Handle);
                }
            }

            registrations.Clear();
        }

        public void Dispose()
        {
            ReleaseAll();
        }

        private void TrackHandle<T>(
            AsyncOperationHandle<T> handle,
            Action<AsyncOperationHandle<T>> onCompleted
        )
        {
            Action unsubscribe = null;
            if (onCompleted != null)
            {
                void Wrapped(AsyncOperationHandle<T> op) => onCompleted(op);
                handle.Completed += Wrapped;
                unsubscribe = () => handle.Completed -= Wrapped;
            }

            registrations.Add(
                new HandleRegistration
                {
                    Handle = handle,
                    Unsubscribe = unsubscribe,
                }
            );
        }

        private void ReleaseRegistration(AsyncOperationHandle handle)
        {
            for (int i = 0; i < registrations.Count; i++)
            {
                if (!registrations[i].Handle.Equals(handle))
                {
                    continue;
                }

                registrations[i].Unsubscribe?.Invoke();
                if (registrations[i].Handle.IsValid())
                {
                    Addressables.Release(registrations[i].Handle);
                }

                registrations.RemoveAt(i);
                break;
            }
        }
    }
}
