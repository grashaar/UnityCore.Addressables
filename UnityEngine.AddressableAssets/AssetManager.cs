using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.AddressableAssets
{
    public static class AssetManager
    {
        private static readonly Dictionary<string, UnityEngine.Object> _assets = new Dictionary<string, UnityEngine.Object>();

        private static readonly List<string> _keys = new List<string>();

        private static readonly string[] _filters = new string[] { "\n", "\r" };

        public static bool isReady { get; set; }

        public static bool ContainsAsset(string address)
            => _assets.ContainsKey(address) && _assets[address] != null;

        public static bool ContainsAddress(string address)
            => _keys.Contains(address);

        public static IEnumerator Initialize()
        {
            var initializer = Addressables.InitializeAsync();
            yield return initializer;

            var keys = initializer.Result.Keys.ToArray();

            for (var i = 0; i < keys.Length; i += 2)
            {
                _keys.Add(keys[i].ToString());
            }
        }

        public static IEnumerator Load<T>(string address) where T : UnityEngine.Object
        {
            for (var i = 0; i < _filters.Length; i++)
            {
                address = address.Replace(_filters[i], string.Empty);
            }

            if (!_assets.ContainsKey(address))
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                yield return handle;

                if (handle.Result == null)
                {
                    Debug.LogError($"Cannot load any asset by address={address}");
                }
                else
                {
                    _assets.Add(address, handle.Result);
                }
            }
        }

        public static T Get<T>(string address) where T : UnityEngine.Object
        {
            if (!_assets.ContainsKey(address))
            {
                Debug.LogWarning($"Cannot find any asset by address={address}");
                return default;
            }

            var asset = _assets[address];

            if (asset is T assetT)
                return assetT;

            Debug.LogWarning($"The asset with address={address} is not an instance of {typeof(T)}");
            return default;
        }

        public static IEnumerator<GameObject> Instantiate(string address)
        {
            for (var i = 0; i < _filters.Length; i++)
            {
                address = address.Replace(_filters[i], string.Empty);
            }

            var handle = Addressables.InstantiateAsync(address);

            while (!handle.IsDone)
                yield return null;

            yield return handle.Result;
        }
    }
}