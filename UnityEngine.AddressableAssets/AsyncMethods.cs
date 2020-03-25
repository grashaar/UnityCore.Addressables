﻿using UnityEngine.SceneManagement;
using UniRx.Async;

namespace UnityEngine.AddressableAssets
{
    using ResourceManagement.AsyncOperations;
    using ResourceManagement.ResourceProviders;
    using ResourceLocators;

    public readonly struct AsyncResult<T>
    {
        public readonly bool Succeeded;
        public readonly T Result;

        public AsyncResult(bool succeeded, T result)
        {
            this.Succeeded = succeeded;
            this.Result = result;
        }

        public static implicit operator AsyncResult<T>(AsyncOperationHandle<T> handle)
            => new AsyncResult<T>(handle.Status == AsyncOperationStatus.Succeeded, handle.Result);

        public static implicit operator T(in AsyncResult<T> result)
            => result.Result;
    }

    public static partial class AddressableManager
    {
        public static async UniTask<AsyncResult<IResourceLocator>> InitializeAsync()
        {
            Clear();

            var operation = Addressables.InitializeAsync();
            await operation.Task;

            OnInitializeCompleted(operation);
            return operation;
        }

        public static async UniTask<AsyncResult<object>> LoadLocationsAsync(object key)
        {
            if (key == null)
                return new AsyncResult<object>(false, key);

            var operation = Addressables.LoadResourceLocationsAsync(key);
            await operation.Task;

            OnLoadLocationsCompleted(operation, key);
            return new AsyncResult<object>(operation.Status == AsyncOperationStatus.Succeeded, key);
        }

        public static async UniTask<AsyncResult<T>> LoadAssetAsync<T>(string key) where T : Object
        {
            key = GuardKey(key);

            if (!_assets.ContainsKey(key))
            {
                var operation = Addressables.LoadAssetAsync<T>(key);
                await operation.Task;

                OnLoadAssetCompleted(operation, key);
                return operation;
            }

            if (_assets[key] is T asset)
                return new AsyncResult<T>(true, asset);

            Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
            return default;
        }

        public static async UniTask<AsyncResult<T>> LoadAssetAsync<T>(AssetReferenceT<T> assetReference) where T : Object
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                return default;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (!_assets.ContainsKey(key))
            {
                var operation = assetReference.LoadAssetAsync<T>();
                await operation.Task;

                OnLoadAssetCompleted(operation, key);
                return operation;
            }

            if (_assets[key] is T asset)
                return new AsyncResult<T>(true, asset);

            Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
            return default;
        }

        public static async UniTask<AsyncResult<SceneInstance>> LoadSceneAsync(string key,
            LoadSceneMode loadMode, bool activeOnLoad = true, int priority = 100)
        {
            key = GuardKey(key);

            if (_scenes.ContainsKey(key))
                return new AsyncResult<SceneInstance>(true, _scenes[key]);

            var operation = Addressables.LoadSceneAsync(key, loadMode, activeOnLoad, priority);
            await operation.Task;

            OnLoadSceneCompleted(operation, key);
            return operation;
        }

        public static async UniTask<AsyncResult<SceneInstance>> LoadSceneAsync(AssetReference assetReference,
            LoadSceneMode loadMode, bool activeOnLoad = true, int priority = 100)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                return default;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (_assets.ContainsKey(key))
                return new AsyncResult<SceneInstance>(true, _scenes[key]);

            var operation = assetReference.LoadSceneAsync(loadMode, activeOnLoad, priority);
            await operation.Task;

            OnLoadSceneCompleted(operation, key);
            return operation;
        }

        public static async UniTask<AsyncResult<SceneInstance>> UnloadSceneAsync(string key, bool autoReleaseHandle = true)
        {
            key = GuardKey(key);

            if (!_scenes.TryGetValue(key, out var scene))
                return default;

            _scenes.Remove(key);

            var operation = Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
            await operation.Task;

            return operation;
        }

        public static async UniTask<AsyncResult<SceneInstance>> UnloadSceneAsync(AssetReference assetReference)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                return default;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (!_scenes.ContainsKey(key))
                return default;

            _scenes.Remove(key);

            var operation = assetReference.UnLoadScene();
            await operation.Task;

            return operation;
        }

        public static async UniTask<AsyncResult<GameObject>> InstantiateAsync(string key,
            Transform parent = null, bool inWorldSpace = false, bool trackHandle = true)
        {
            key = GuardKey(key);

            if (_instances.ContainsKey(key))
                return new AsyncResult<GameObject>(true, _instances[key]);

            var operation = Addressables.InstantiateAsync(key, parent, inWorldSpace, trackHandle);
            await operation.Task;

            OnInstantiateCompleted(operation, key);
            return operation;
        }

        public static async UniTask<AsyncResult<GameObject>> InstantiateAsync(AssetReference assetReference,
            Transform parent = null, bool inWorldSpace = false)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                return default;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (_instances.ContainsKey(key))
                return new AsyncResult<GameObject>(true, _instances[key]);

            var operation = assetReference.InstantiateAsync(parent, inWorldSpace);
            await operation.Task;

            OnInstantiateCompleted(operation, key);
            return operation;
        }
    }
}