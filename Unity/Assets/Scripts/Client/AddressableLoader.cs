using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SlyPirates.Client
{
    public static class AddressableLoader
    {
        private static async Task<T> Load<T>(object key, CancellationToken cancellationToken)
            where T : UnityEngine.Object
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null!;
            }

            try
            {
                return await Addressables.LoadAssetAsync<T>(key).ToAwaitable(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null!;
            }
        }

        public static async Task<T> Load<T>(
            object key,
            UnityEngine.Object owner,
            CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            if (owner == null)
            {
                return null!;
            }

            if (!cancellationToken.CanBeCanceled && owner is MonoBehaviour behaviour)
            {
                cancellationToken = behaviour.destroyCancellationToken;
            }

            T asset = await Load<T>(key, cancellationToken);
            if (asset == null)
            {
                return null!;
            }

            if (!cancellationToken.IsCancellationRequested && owner != null)
            {
                return asset;
            }

            Release(asset);
            return null!;
        }

        private static async Task<GameObject> Instantiate(object key, Transform parent, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null!;
            }

            try
            {
                return await Addressables.InstantiateAsync(key, parent).ToAwaitable(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null!;
            }
        }

        public static async Task<GameObject> Instantiate(
            object key,
            Transform parent,
            UnityEngine.Object owner,
            CancellationToken cancellationToken = default)
        {
            if (owner == null)
            {
                return null!;
            }

            if (!cancellationToken.CanBeCanceled && owner is MonoBehaviour behaviour)
            {
                cancellationToken = behaviour.destroyCancellationToken;
            }

            GameObject instance = await Instantiate(key, parent, cancellationToken);
            if (instance == null)
            {
                return null!;
            }

            if (!cancellationToken.IsCancellationRequested && owner != null)
            {
                return instance;
            }

            ReleaseInstance(instance);
            return null!;
        }

        public static void Release<T>(T asset) where T : UnityEngine.Object
        {
            if (asset != null)
            {
                Addressables.Release(asset);
            }
        }

        public static void ReleaseInstance(GameObject instance)
        {
            if (instance != null)
            {
                Addressables.ReleaseInstance(instance);
            }
        }
    }
}
