using System;
using UnityEngine.SceneManagement;

namespace UnityEngine.AddressableAssets
{
    public static partial class AddressableManager
    {
        public static void Initialize(Action onSucceeded, Action onFailed = null)
        {
            Clear();

            var operation = Addressables.InitializeAsync();
            operation.Completed += handle => OnInitializeCompleted(handle, onSucceeded, onFailed);
        }

        public static void LoadLocations(object key, Action<object> onSucceeded, Action<object> onFailed = null)
        {
            if (key == null)
                return;

            var operation = Addressables.LoadResourceLocationsAsync(key);
            operation.Completed += handle => OnLoadLocationsCompleted(handle, key, onSucceeded, onFailed);
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

        public static void LoadAsset<T>(AssetReferenceT<T> assetReference, Action<string, T> onSucceeded,
            Action<string> onFailed = null) where T : Object
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                onFailed?.Invoke(string.Empty);
                return;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (!_assets.ContainsKey(key))
            {
                var operation = assetReference.LoadAssetAsync<T>();
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

        public static void LoadScene(string key, Action<Scene> onSucceeded, Action<string> onFailed = null,
            LoadSceneMode loadMode = LoadSceneMode.Single, bool activeOnLoad = true, int priority = 100)
        {
            key = GuardKey(key);

            if (_scenes.ContainsKey(key))
            {
                onSucceeded?.Invoke(_scenes[key].Scene);
                return;
            }

            var operation = Addressables.LoadSceneAsync(key, loadMode, activeOnLoad, priority);
            operation.Completed += handle => OnLoadSceneCompleted(handle, key, onSucceeded, onFailed);
        }

        public static void LoadScene(AssetReference assetReference, Action<Scene> onSucceeded, Action<string> onFailed = null,
            LoadSceneMode loadMode = LoadSceneMode.Single, bool activeOnLoad = true, int priority = 100)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                onFailed?.Invoke(string.Empty);
                return;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (_assets.ContainsKey(key))
            {
                onSucceeded?.Invoke(_scenes[key].Scene);
                return;
            }

            var operation = assetReference.LoadSceneAsync(loadMode, activeOnLoad, priority);
            operation.Completed += handle => OnLoadSceneCompleted(handle, key, onSucceeded, onFailed);
        }

        public static void UnloadScene(string key, Action<string> onSucceeded = null, Action<string> onFailed = null,
            bool autoReleaseHandle = true)
        {
            key = GuardKey(key);

            if (!_scenes.TryGetValue(key, out var scene))
            {
                onFailed?.Invoke(key);
                return;
            }

            _scenes.Remove(key);

            var operation = Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
            operation.Completed += handle => OnUnloadSceneCompleted(handle, key, onSucceeded, onFailed);
        }

        public static void UnloadScene(AssetReference assetReference, Action<string> onSucceeded = null, Action<string> onFailed = null)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                onFailed?.Invoke(string.Empty);
                return;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (!_scenes.ContainsKey(key))
            {
                onFailed?.Invoke(key);
                return;
            }

            _scenes.Remove(key);

            var operation = assetReference.UnLoadScene();
            operation.Completed += handle => OnUnloadSceneCompleted(handle, key, onSucceeded, onFailed);
        }

        public static void Instantiate(string key, Action<string, GameObject> onSucceeded, Action<string> onFailed = null,
            Transform parent = null, bool inWorldSpace = false, bool trackHandle = true)
        {
            key = GuardKey(key);

            var operation = Addressables.InstantiateAsync(key, parent, inWorldSpace, trackHandle);
            operation.Completed += handle => OnInstantiateCompleted(handle, key, onSucceeded, onFailed);
        }

        public static void Instantiate(AssetReference assetReference, Action<string, GameObject> onSucceeded,
            Action<string> onFailed = null, Transform parent = null, bool inWorldSpace = false)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                onFailed?.Invoke(string.Empty);
            }

            var key = assetReference.RuntimeKey.ToString();
            var operation = assetReference.InstantiateAsync(parent, inWorldSpace);
            operation.Completed += handle => OnInstantiateCompleted(handle, key, onSucceeded, onFailed);
        }
    }
}