using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;
using System.IO;

// 资源加载统一接口
public class KResources : MonoBehaviour
{
    private static string TAG = "KResources";

    public static KResources Instance;

    private Dictionary<string, Sprite> imageCache;

    private static string cdnUrl = @"https://kitauji-gwent-1336421851.cos.ap-shanghai.myqcloud.com/kitauji-gwent-unity-res/v1.0.0/";

    void Start()
    {
        imageCache = new Dictionary<string, Sprite>();
        Instance = this;
        StartCoroutine(PreloadRes());
    }

    public static T Load<T>(string filename) where T : Object
    {
        return Resources.Load<T>(filename);
    }

    public void Load<T>(Object target, string filename) where T : Object
    {
        T localRes = TryLoadLocal<T>(filename);
        if (target is Image image) {
            if (localRes != null) {
                image.sprite = localRes as Sprite;
            } else if (imageCache.ContainsKey(filename)) {
                image.sprite = imageCache[filename];
            } else {
                StartCoroutine(DownloadImageFromCdn(image, filename));
            }
        } else if (target is AudioSource audioSource) {
            if (localRes != null) {
                audioSource.clip = localRes as AudioClip;
            }
            // TODO: 在线下载
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

    private IEnumerator DownloadImageFromCdn(Image image, string filename)
    {
        byte[] imageBytes = null;
        yield return DownloadResFromCdn(filename, (readBytes) => {
            if (readBytes != null) {
                imageBytes = (byte[])readBytes.Clone();
            }
        });
        if (imageBytes == null) {
            KLog.W(TAG, "DownloadImageFromCdn: " + filename + " fail");
            yield break;
        }
        
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false); // 创建一个临时的 Texture2D 对象
        texture.LoadImage(imageBytes); // 加载字节数据到纹理
        // 一些影响画质的设置
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 1;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        imageCache[filename] = sprite;
        if (image != null) {
            image.sprite = sprite;
        }
    }

    private IEnumerator DownloadResFromCdn(string filename, System.Action<byte[]> downloadComplete)
    {
        string url = cdnUrl + filename;
        byte[] downloadBytes = null;
        int maxRetryTimes = 3;
        for (int i = 0; i < maxRetryTimes; i++) {
            yield return TryDownload(url, (readBytes) => {
                downloadBytes = readBytes;
            });
        }
        if (downloadBytes == null) {
            KLog.W(TAG, "DownloadResFromCdn: " + filename + " fail");
        }
        downloadComplete(downloadBytes);
        yield break;
    }

    private IEnumerator TryDownload(string url, System.Action<byte[]> downloadComplete)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();
        try {
            if (request.result == UnityWebRequest.Result.Success) {
                byte[] readBytes = request.downloadHandler.data;
                downloadComplete(readBytes);
                yield break;
            } else {
                KLog.W(TAG, "TryDownload: " + url + " fail");
            }
        } catch (System.Exception e) {
            KLog.W(TAG, "TryDownload: " + url + " fail, Exception: " + e.Message);
        }
        downloadComplete(null);
    }

    private IEnumerator PreloadRes()
    {
        TextAsset preloadResAsset = Load<TextAsset>(@"PreloadRes");
        JObject preloadResJson = JObject.Parse(preloadResAsset.text);
        yield return TryRecursionPreload(preloadResJson, "");
    }

    // json格式：{"folder1":[{"folder2": ["file1"]}, "file2"]}
    private IEnumerator TryRecursionPreload(JObject preloadResJson, string prefix)
    {
        foreach (var property in preloadResJson.Properties()) {
            if (property.Name != "PreloadRes") {
                prefix = prefix == "" ? property.Name : $"{prefix}/{property.Name}";
            }
            // 所有的JObject，value都是数组。数组元素可能是JObject或string
            if (property.Value is JArray array) {
                foreach (var item in array) {
                    if (item is JObject obj) {
                        StartCoroutine(TryRecursionPreload(obj, prefix));
                    } else {
                        string filename = $"{prefix}/{item}";
                        KLog.I(TAG, "preload " + filename);
                        StartCoroutine(DownloadImageFromCdn(null, filename));
                    }
                }
            }
        }
        yield break;
    }
}
