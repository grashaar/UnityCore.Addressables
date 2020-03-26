using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace UnityEngine.AddressableAssets
{
    using ResourceManagement.ResourceProviders;
    using ResourceManagement.ResourceLocations;

    public static partial class AddressableManager
    {
        private static readonly List<IResourceLocation> _noLocation;
        private static readonly Dictionary<string, List<IResourceLocation>> _locations;
        private static readonly Dictionary<string, Object> _assets;
        private static readonly Dictionary<string, SceneInstance> _scenes;
        private static readonly List<object> _keys;
        private static readonly string[] _filters;

        public static IReadOnlyList<object> Keys
            => _keys;

        static AddressableManager()
        {
            _noLocation = new List<IResourceLocation>();
            _locations = new Dictionary<string, List<IResourceLocation>>();
            _assets = new Dictionary<string, Object>();
            _scenes = new Dictionary<string, SceneInstance>();
            _keys = new List<object>();
            _filters = new[] { "\n", "\r" };
        }

        public static bool ContainsAsset(string key)
            => _assets.ContainsKey(key) && _assets[key];

        public static bool ContainsKey(object key)
            => _keys.Contains(key);

        public static void Initialize()
        {
            Clear();

            var operation = Addressables.InitializeAsync();
            operation.Completed += handle => OnInitializeCompleted(handle);
        }

        public static void LoadLocations(object key)
        {
            if (key == null)
                return;

            var operation = Addressables.LoadResourceLocationsAsync(key);
            operation.Completed += handle => OnLoadLocationsCompleted(handle, key);
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

        public static void LoadAsset<T>(AssetReferenceT<T> assetReference) where T : Object
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                return;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (!_assets.ContainsKey(key))
            {
                var operation = assetReference.LoadAssetAsync<T>();
                operation.Completed += handle => OnLoadAssetCompleted(handle, key);
                return;
            }

            if (!(_assets[key] is T))
            {
                Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
            }
        }

        public static void LoadScene(string key, LoadSceneMode loadMode, bool activeOnLoad = true, int priority = 100)
        {
            key = GuardKey(key);

            if (_scenes.ContainsKey(key))
                return;

            var operation = Addressables.LoadSceneAsync(key, loadMode, activeOnLoad, priority);
            operation.Completed += handle => OnLoadSceneCompleted(handle, key);
        }

        public static void LoadScene(AssetReference assetReference, LoadSceneMode loadMode, bool activeOnLoad = true,
            int priority = 100)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                return;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (_scenes.ContainsKey(key))
                return;

            var operation = assetReference.LoadSceneAsync(loadMode, activeOnLoad, priority);
            operation.Completed += handle => OnLoadSceneCompleted(handle, key);
        }

        public static void UnloadScene(string key, bool autoReleaseHandle = true)
        {
            key = GuardKey(key);

            if (!_scenes.TryGetValue(key, out var scene))
                return;

            _scenes.Remove(key);
            Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
        }

        public static void UnloadScene(AssetReference assetReference)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                return;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (!_scenes.ContainsKey(key))
                return;

            _scenes.Remove(key);
            assetReference.UnLoadScene();
        }

        public static void Instantiate(string key, Transform parent = null, bool inWorldSpace = false, bool trackHandle = true)
        {
            key = GuardKey(key);

            var operation = Addressables.InstantiateAsync(key, parent, inWorldSpace, trackHandle);
            operation.Completed += handle => OnInstantiateCompleted(handle, key);
        }

        public static void Instantiate(AssetReference assetReference, Transform parent = null, bool inWorldSpace = false)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                return;
            }

            var key = assetReference.RuntimeKey.ToString();
            var operation = assetReference.InstantiateAsync(parent, inWorldSpace);
            operation.Completed += handle => OnInstantiateCompleted(handle, key);
        }

        public static IReadOnlyList<IResourceLocation> GetLocations(string key)
        {
            key = GuardKey(key);

            if (!_locations.TryGetValue(key, out var list))
                return _noLocation;

            return list;
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

        public static void UnloadAsset(AssetReference assetReference)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                return;
            }

            var key = assetReference.RuntimeKey.ToString();

            if (!_assets.ContainsKey(key))
                return;

            _assets.Remove(key);
            assetReference.ReleaseAsset();
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

        private static void Clear()
        {
            _keys.Clear();
            _locations.Clear();
            _assets.Clear();
        }
    }
}