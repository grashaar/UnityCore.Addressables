using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UniRx.Async;

namespace UnityEngine.AddressableAssets
{
    using ResourceManagement.AsyncOperations;
    using ResourceManagement.ResourceProviders;
    using ResourceLocators;

    public static class AddressableManager
    {
        private static readonly Dictionary<string, Object> _assets
            = new Dictionary<string, Object>();

        private static readonly Dictionary<string, SceneInstance> _scenes
            = new Dictionary<string, SceneInstance>();

        private static readonly List<string> _keys = new List<string>();

        private static readonly string[] _filters = new[] { "\n", "\r" };

        public static bool isReady { get; set; }

        public static bool ContainsAsset(string key)
            => _assets.ContainsKey(key) && _assets[key];

        public static bool ContainsKey(string key)
            => _keys.Contains(key);

        public static IEnumerator InitializeCoroutine()
        {
            var operation = Addressables.InitializeAsync();
            yield return operation;

            OnInitializeCompleted(operation);
        }

        public static IEnumerator InitializeCoroutine(Action onSucceeded, Action onFailed = null)
        {
            var operation = Addressables.InitializeAsync();
            yield return operation;

            OnInitializeCompleted(operation, onSucceeded, onFailed);
        }

        public static void Initialize()
        {
            var operation = Addressables.InitializeAsync();
            operation.Completed += handle => OnInitializeCompleted(handle);
        }

        public static void Initialize(Action onSucceeded, Action onFailed = null)
        {
            var operation = Addressables.InitializeAsync();
            operation.Completed += handle => OnInitializeCompleted(handle, onSucceeded, onFailed);
        }

        private static void OnInitializeCompleted(AsyncOperationHandle<IResourceLocator> handle, Action onSucceeded = null, Action onFailed = null)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var keys = handle.Result.Keys.ToArray();

                for (var i = 0; i < keys.Length; i += 2)
                {
                    _keys.Add(keys[i].ToString());
                }

                onSucceeded?.Invoke();
            }
            else if (handle.Status == AsyncOperationStatus.Failed)
            {
                onFailed?.Invoke();
            }
        }

        public static AsyncOperationHandle<IResourceLocator> InitializeAsync()
        {
            var operation = Addressables.InitializeAsync();
            operation.Completed += handle => OnInitializeCompleted(handle);

            return operation;
        }

        public static IEnumerator LoadAssetCoroutine<T>(string key) where T : Object
        {
            key = GuardKey(key);

            if (!_assets.ContainsKey(key))
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                yield return handle;

                OnLoadAssetCompleted(handle, key);
            }
            else if (!(_assets[key] is T))
            {
                Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
            }
        }

        public static IEnumerator LoadAssetCoroutine<T>(string key, Action<string, T> onSucceeded, Action<string> onFailed = null) where T : Object
        {
            key = GuardKey(key);

            if (!_assets.ContainsKey(key))
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                yield return handle;

                OnLoadAssetCompleted(handle, key, onSucceeded, onFailed);
            }
            else if (_assets[key] is T asset)
            {
                onSucceeded?.Invoke(key, asset);
            }
            else
            {
                Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
                onFailed?.Invoke(key);
            }
        }

        public static void LoadAsset<T>(string key) where T : Object
        {
            key = GuardKey(key);

            if (!_assets.ContainsKey(key))
            {
                var operation = Addressables.LoadAssetAsync<T>(key);
                operation.Completed += handle => OnLoadAssetCompleted(handle, key);
                return;
            }

            if (!(_assets[key] is T))
            {
                Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
            }
        }

        public static void LoadAsset<T>(string key, Action<string, T> onSucceeded, Action<string> onFailed = null) where T : Object
        {
            key = GuardKey(key);

            if (!_assets.ContainsKey(key))
            {
                var operation = Addressables.LoadAssetAsync<T>(key);
                operation.Completed += handle => OnLoadAssetCompleted(handle, key, onSucceeded, onFailed);
                return;
            }

            if (_assets[key] is T asset)
            {
                onSucceeded?.Invoke(key, asset);
            }
            else
            {
                Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
                onFailed?.Invoke(key);
            }
        }

        private static void OnLoadAssetCompleted<T>(AsyncOperationHandle<T> handle, string key, Action<string, T> onSucceeded = null, Action<string> onFailed = null) where T : Object
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                onFailed?.Invoke(key);
                return;
            }

            if (!handle.Result)
            {
                Debug.LogError($"Cannot load any asset of type {typeof(T)} by key={key}.");
                onFailed?.Invoke(key);
                return;
            }

            if (_assets.ContainsKey(key))
            {
                if (!(_assets[key] is T))
                {
                    Debug.LogError($"An asset of type={_assets[key].GetType()} has been already registered with key={key}.");
                    onFailed?.Invoke(key);
                    return;
                }
            }
            else
            {
                _assets.Add(key, handle.Result);
            }

            onSucceeded?.Invoke(key, handle.Result);
        }

        public static async UniTask<T> LoadAssetAsync<T>(string key) where T : Object
        {
            key = GuardKey(key);

            if (!_assets.ContainsKey(key))
            {
                var operation = Addressables.LoadAssetAsync<T>(key);
                operation.Completed += handle => OnLoadAssetCompleted(handle, key);

                await operation.Task;
                return operation.Result;
            }

            if (!(_assets[key] is T asset))
            {
                Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
                return default;
            }

            return asset;
        }

        public static T GetAsset<T>(string key) where T : Object
        {
            key = GuardKey(key);

            if (!_assets.ContainsKey(key))
            {
                Debug.LogWarning($"Cannot find any asset by key={key}.");
                return default;
            }

            if (_assets[key] is T asset)
                return asset;

            Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
            return default;
        }

        public static void UnloadAsset(string key)
        {
            key = GuardKey(key);

            if (!_assets.TryGetValue(key, out var asset))
                return;

            _assets.Remove(key);
            Addressables.Release(asset);
        }

        public static IEnumerator LoadSceneCoroutine(string key, LoadSceneMode loadMode, bool activeOnLoad = true, int priority = 100, Action<Scene> onSucceeded = null, Action<string> onFailed = null)
        {
            key = GuardKey(key);

            if (!_scenes.ContainsKey(key))
            {
                var operation = Addressables.LoadSceneAsync(key, loadMode, activeOnLoad, priority);
                yield return operation;

                OnLoadSceneCompleted(operation, key, onSucceeded, onFailed);
            }
            else
            {
                onSucceeded?.Invoke(_scenes[key].Scene);
            }
        }

        public static void LoadScene(string key, LoadSceneMode loadMode,
            bool activeOnLoad = true, int priority = 100, Action<Scene> onSucceeded = null, Action<string> onFailed = null)
        {
            key = GuardKey(key);

            if (!_scenes.ContainsKey(key))
            {
                var operation = Addressables.LoadSceneAsync(key, loadMode, activeOnLoad, priority);
                operation.Completed += handle => OnLoadSceneCompleted(handle, key, onSucceeded, onFailed);
                return;
            }

            onSucceeded?.Invoke(_scenes[key].Scene);
        }

        private static void OnLoadSceneCompleted(AsyncOperationHandle<SceneInstance> handle, string key, Action<Scene> onSucceeded = null, Action<string> onFailed = null)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _scenes.Add(key, handle.Result);
                onSucceeded?.Invoke(handle.Result.Scene);
            }
            else if (handle.Status == AsyncOperationStatus.Failed)
            {
                onFailed?.Invoke(key);
            }
        }

        public static async UniTask<SceneInstance> LoadSceneAsync(string key,
            LoadSceneMode loadMode, bool activeOnLoad = true, int priority = 100)
        {
            key = GuardKey(key);

            if (!_scenes.ContainsKey(key))
            {
                var operation = Addressables.LoadSceneAsync(key, loadMode, activeOnLoad, priority);
                operation.Completed += handle => OnLoadSceneCompleted(handle, key);

                await operation.Task;
                return operation.Result;
            }

            return _scenes[key];
        }

        public static void UnloadScene(string key, bool autoReleaseHandle = true)
        {
            key = GuardKey(key);

            if (!_scenes.TryGetValue(key, out var scene))
                return;

            _scenes.Remove(key);
            Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
        }

        public static IEnumerator UnloadSceneCoroutine(string key, bool autoReleaseHandle = true,
            Action<string> onSucceeded = null, Action<string> onFailed = null)
        {
            key = GuardKey(key);

            if (_scenes.TryGetValue(key, out var scene))
            {
                _scenes.Remove(key);

                var operation = Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
                yield return operation;

                OnUnloadSceneCompleted(operation, key, onSucceeded, onFailed);
            }
            else
            {
                onFailed?.Invoke(key);
            }
        }

        public static void UnloadScene(string key, bool autoReleaseHandle = true,
            Action<string> onSucceeded = null, Action<string> onFailed = null)
        {
            key = GuardKey(key);

            if (_scenes.TryGetValue(key, out var scene))
            {
                _scenes.Remove(key);

                var operation = Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
                operation.Completed += handle => OnUnloadSceneCompleted(handle, key, onSucceeded, onFailed);
                return;
            }

            onFailed?.Invoke(key);
        }

        private static void OnUnloadSceneCompleted(AsyncOperationHandle<SceneInstance> handle, string key, Action<string> onSucceeded, Action<string> onFailed)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onSucceeded?.Invoke(key);
            }
            else if (handle.Status == AsyncOperationStatus.Failed)
            {
                onFailed?.Invoke(key);
            }
        }

        public static async UniTask<SceneInstance> UnloadSceneAsync(string key, bool autoReleaseHandle = true)
        {
            key = GuardKey(key);

            if (_scenes.TryGetValue(key, out var scene))
            {
                _scenes.Remove(key);

                var operation = Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
                await operation.Task;
                return operation.Result;
            }

            return default;
        }

        public static IEnumerator InstantiateCoroutine(string key)
        {
            key = GuardKey(key);

            var operation = Addressables.InstantiateAsync(key);
            yield return operation;
        }

        public static IEnumerator InstantiateCoroutine(string key, Action<GameObject> callback)
        {
            key = GuardKey(key);

            var operation = Addressables.InstantiateAsync(key);
            yield return operation;

            callback?.Invoke(operation.Result);
        }

        public static void Instantiate(string key,
            Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            key = GuardKey(key);

            Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace, trackHandle);
        }

        public static void Instantiate(string key, Action<GameObject> callback,
            Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            key = GuardKey(key);

            var operation = Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace, trackHandle);
            operation.Completed += handle => callback?.Invoke(handle.Result);
        }

        public static async UniTask<GameObject> InstantiateAsync(string key,
            Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            key = GuardKey(key);

            var operation = Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace, trackHandle);
            await operation.Task;
            return operation.Result;
        }

        private static string GuardKey(string key)
        {
            var guardedKey = key ?? string.Empty;

            for (var i = 0; i < _filters.Length; i++)
            {
                guardedKey = guardedKey.Replace(_filters[i], string.Empty);
            }

            return guardedKey;
        }
    }
}