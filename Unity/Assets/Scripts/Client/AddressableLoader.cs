using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace SlyPirates.Client
{
    public static class AddressableLoader
    {
        private static readonly Dictionary<Scene, GameObject> StagingRoots = new();

        static AddressableLoader() => SceneManager.sceneUnloaded += RemoveStagingRoot;

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

            if (!cancellationToken.IsCancellationRequested)
            {
                Release(asset);
            }

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

        public static async Task<GameObject> InstantiateStaged(
            object key,
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

            if (cancellationToken.IsCancellationRequested)
            {
                return null!;
            }

            GameObject instance = await Instantiate(key, GetStagingRoot(owner), cancellationToken);

            if (instance == null)
            {
                return null!;
            }

            if (cancellationToken.IsCancellationRequested || owner == null)
            {
                ReleaseInstance(instance);
                return null!;
            }

            return instance;
        }

        private static Transform GetStagingRoot(UnityEngine.Object owner)
        {
            Scene scene = owner is Component component
                ? component.gameObject.scene
                : SceneManager.GetActiveScene();

            if (StagingRoots.TryGetValue(scene, out GameObject stagingRoot) && stagingRoot != null)
            {
                return stagingRoot.transform;
            }

            stagingRoot = new GameObject("AddressableViewStaging");
            stagingRoot.SetActive(false);
            if (scene.IsValid() && scene.isLoaded && stagingRoot.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(stagingRoot, scene);
            }

            StagingRoots[scene] = stagingRoot;
            return stagingRoot.transform;
        }

        private static void RemoveStagingRoot(Scene scene) => StagingRoots.Remove(scene);

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
