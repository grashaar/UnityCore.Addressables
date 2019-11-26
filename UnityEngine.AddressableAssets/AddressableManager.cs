using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.AddressableAssets
{
    using ResourceManagement.AsyncOperations;

    public static class AddressableManager
    {
        private static readonly Dictionary<string, Object> _assets = new Dictionary<string, Object>();

        private static readonly List<string> _keys = new List<string>();

        private static readonly string[] _filters = new[] { "\n", "\r" };

        public static bool isReady { get; set; }

        public static bool ContainsAsset(string key)
            => _assets.ContainsKey(key) && _assets[key] != null;

        public static bool ContainsKey(string key)
            => _keys.Contains(key);

        public static IEnumerator Initialize()
        {
            var operation = Addressables.InitializeAsync();
            yield return operation;

            var keys = operation.Result.Keys.ToArray();

            for (var i = 0; i < keys.Length; i += 2)
            {
                _keys.Add(keys[i].ToString());
            }
        }

        public static IEnumerator Load<T>(string key) where T : Object
        {
            for (var i = 0; i < _filters.Length; i++)
            {
                key = key.Replace(_filters[i], string.Empty);
            }

            if (!_assets.ContainsKey(key))
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                yield return handle;

                if (handle.Result is T result)
                {
                    _assets.Add(key, result);
                }
                else
                {
                    Debug.LogError($"Cannot load any asset of type {typeof(T)} by key={key}.");
                }
            }
        }

        public static IEnumerator Load<T>(string key, Action<string, T> callback) where T : Object
        {
            if (!_assets.ContainsKey(key))
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                yield return handle;

                if (handle.Result is T result)
                {
                    _assets.Add(key, result);
                    callback?.Invoke(key, result);
                }
                else
                {
                    Debug.LogError($"Cannot load any asset of type {typeof(T)} by key={key}.");
                }
            }
            else if (_assets[key] is T result)
            {
                callback?.Invoke(key, result);
            }
            else
            {
                Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
            }
        }

        public static void LoadAsync<T>(string key, Action<string, T> callback) where T : Object
        {
            if (!_assets.ContainsKey(key))
            {
                var operation = Addressables.LoadAssetAsync<T>(key);
                operation.Completed += handle => OnCompleted(handle, key, callback);
                return;
            }

            if (_assets[key] is T result)
            {
                callback?.Invoke(key, result);
            }
            else
            {
                Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
            }
        }

        private static void OnCompleted<T>(AsyncOperationHandle<T> handle, string key, Action<string, T> callback) where T : Object
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
                return;

            _assets.Add(key, handle.Result);
            callback?.Invoke(key, handle.Result);
        }

        public static T Get<T>(string key) where T : Object
        {
            if (!_assets.ContainsKey(key))
            {
                Debug.LogWarning($"Cannot find any asset by key={key}.");
                return default;
            }

            var asset = _assets[key];

            if (asset is T assetT)
                return assetT;

            Debug.LogWarning($"The asset with key={key} is not an instance of {typeof(T)}.");
            return default;
        }

        public static void Unload(string key)
        {
            if (!_assets.TryGetValue(key, out var asset))
                return;

            _assets.Remove(key);
            Addressables.Release(asset);
        }

        public static IEnumerator<GameObject> Instantiate(string key)
        {
            for (var i = 0; i < _filters.Length; i++)
            {
                key = key.Replace(_filters[i], string.Empty);
            }

            var handle = Addressables.InstantiateAsync(key);

            while (!handle.IsDone)
                yield return null;

            yield return handle.Result;
        }
    }
}