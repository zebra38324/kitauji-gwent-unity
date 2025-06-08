using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using Newtonsoft.Json.Linq;
using System.IO;

// 资源加载统一接口
public class KResources : MonoBehaviour
{
    private static string TAG = "KResources";

    public static KResources Instance;

    private static string addressablesPrefix = @"Assets/RemoteRes/";

    void Start()
    {
        Instance = this;
    }

    public static T Load<T>(string filename) where T : Object
    {
        return Resources.Load<T>(filename);
    }

    public void Load<T>(Object target, string filename) where T : Object
    {
        // TODO: 这块代码太丑陋了
        T localRes = TryLoadLocal<T>(filename);
        if (target is Image image) {
            if (localRes != null) {
                image.sprite = localRes as Sprite;
            } else {
                StartCoroutine(TryLoadAddressables<Sprite>(filename, (sprite) => {
                    if (sprite != null) {
                        if (image != null) {
                            image.sprite = sprite;
                        }
                    } else {
                        KLog.W(TAG, "Load: " + filename + " fail");
                    }
                }));
            }
        }
    }

    public T LoadLocal<T>(string filename) where T : Object
    {
        return Resources.Load<T>(filename);
    }

    private T TryLoadLocal<T>(string filename) where T : Object
    {
        string directory = Path.GetDirectoryName(filename);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        filename = Path.Combine(directory, fileNameWithoutExtension);
        return Resources.Load<T>(filename);
    }

    private IEnumerator TryLoadAddressables<T>(string filename, System.Action<T> loadComplete) where T : Object
    {
        var loadHandle = Addressables.LoadAssetAsync<T>(addressablesPrefix + filename);
        yield return loadHandle;
        loadComplete(loadHandle.Result);
    }
}
