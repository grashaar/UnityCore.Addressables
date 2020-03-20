using System;
using System.Collections;
using UnityEngine.SceneManagement;

namespace UnityEngine.AddressableAssets
{
    public static partial class AddressableManager
    {
        public static IEnumerator InitializeCoroutine(Action onSucceeded = null, Action onFailed = null)
        {
            Clear();

            var operation = Addressables.InitializeAsync();
            yield return operation;

            OnInitializeCompleted(operation, onSucceeded, onFailed);
        }

        public static IEnumerator LoadLocationsCoroutine(object key, Action<object> onSucceeded = null, Action<object> onFailed = null)
        {
            if (key != null)
            {
                var operation = Addressables.LoadResourceLocationsAsync(key);
                yield return operation;

                OnLoadLocationsCompleted(operation, key, onSucceeded, onFailed);
            }
        }

        public static IEnumerator LoadAssetCoroutine<T>(string key, Action<string, T> onSucceeded = null, Action<string> onFailed = null) where T : Object
        {
            key = GuardKey(key);

            if (!_assets.ContainsKey(key))
            {
                var operation = Addressables.LoadAssetAsync<T>(key);
                yield return operation;

                OnLoadAssetCompleted(operation, key, onSucceeded, onFailed);
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

        public static IEnumerator LoadAssetCoroutine<T>(AssetReferenceT<T> assetReference, Action<string, T> onSucceeded = null, Action<string> onFailed = null) where T : Object
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                onFailed?.Invoke(string.Empty);
            }
            else
            {
                var key = assetReference.RuntimeKey.ToString();

                if (!_assets.ContainsKey(key))
                {
                    var operation = assetReference.LoadAssetAsync<T>();
                    yield return operation;

                    OnLoadAssetCompleted(operation, key, onSucceeded, onFailed);
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
        }

        public static IEnumerator LoadSceneCoroutine(string key, LoadSceneMode loadMode = LoadSceneMode.Single,
            bool activeOnLoad = true, int priority = 100, Action<Scene> onSucceeded = null, Action<string> onFailed = null)
        {
            key = GuardKey(key);

            if (_scenes.ContainsKey(key))
            {
                onSucceeded?.Invoke(_scenes[key].Scene);
            }
            else
            {
                var operation = Addressables.LoadSceneAsync(key, loadMode, activeOnLoad, priority);
                yield return operation;

                OnLoadSceneCompleted(operation, key, onSucceeded, onFailed);
            }
        }

        public static IEnumerator LoadSceneCoroutine(AssetReference assetReference, LoadSceneMode loadMode = LoadSceneMode.Single,
            bool activeOnLoad = true, int priority = 100, Action<Scene> onSucceeded = null, Action<string> onFailed = null)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                onFailed?.Invoke(string.Empty);
            }
            else
            {
                var key = assetReference.RuntimeKey.ToString();

                if (_assets.ContainsKey(key))
                {
                    onSucceeded?.Invoke(_scenes[key].Scene);
                }
                else
                {
                    var operation = assetReference.LoadSceneAsync(loadMode, activeOnLoad, priority);
                    yield return operation;

                    OnLoadSceneCompleted(operation, key, onSucceeded, onFailed);
                }
            }
        }

        public static IEnumerator UnloadSceneCoroutine(string key, bool autoReleaseHandle = true, Action<string> onSucceeded = null, Action<string> onFailed = null)
        {
            key = GuardKey(key);

            if (!_scenes.TryGetValue(key, out var scene))
            {
                onFailed?.Invoke(key);
            }
            else
            {
                _scenes.Remove(key);

                var operation = Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
                yield return operation;

                OnUnloadSceneCompleted(operation, key, onSucceeded, onFailed);
            }
        }

        public static IEnumerator UnloadSceneCoroutine(AssetReference assetReference, Action<string> onSucceeded = null,
            Action<string> onFailed = null)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                onFailed?.Invoke(string.Empty);
            }
            else
            {
                var key = assetReference.RuntimeKey.ToString();

                if (!_scenes.ContainsKey(key))
                {
                    onFailed?.Invoke(key);
                }
                else
                {
                    _scenes.Remove(key);

                    var operation = assetReference.UnLoadScene();
                    yield return operation;

                    OnUnloadSceneCompleted(operation, key, onSucceeded, onFailed);
                }
            }
        }

        public static IEnumerator InstantiateCoroutine(string key, Transform parent = null, bool inWorldSpace = false,
            bool trackHandle = true, Action<string, GameObject> onSucceeded = null, Action<string> onFailed = null)
        {
            key = GuardKey(key);

            if (_instances.ContainsKey(key))
            {
                onSucceeded?.Invoke(key, _instances[key]);
            }
            else
            {
                var operation = Addressables.InstantiateAsync(key, parent, inWorldSpace, trackHandle);
                yield return operation;

                OnInstantiateCompleted(operation, key, onSucceeded, onFailed);
            }
        }

        public static IEnumerator InstantiateCoroutine(AssetReference assetReference,
            Transform parent = null, bool inWorldSpace = false, Action<string, GameObject> onSucceeded = null,
            Action<string> onFailed = null)
        {
            if (assetReference == null)
            {
                Debug.LogException(new System.ArgumentNullException(nameof(assetReference)));
                onFailed?.Invoke(string.Empty);
            }
            else
            {
                var key = assetReference.RuntimeKey.ToString();

                if (_instances.ContainsKey(key))
                {
                    onSucceeded?.Invoke(key, _instances[key]);
                }
                else
                {
                    var operation = assetReference.InstantiateAsync(parent, inWorldSpace);
                    yield return operation;

                    OnInstantiateCompleted(operation, key, onSucceeded, onFailed);
                }
            }
        }
    }
}