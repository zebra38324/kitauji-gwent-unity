using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

// 切换到远程加载的scene使用
public class LoadingScene : MonoBehaviour
{
    public Slider progressBar;

    public TextMeshProUGUI progressText;

    private static string nextScene;

    private static string scenePrefix = @"Assets/Scenes/";

    void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    public static void Load(string sceneName)
    {
        SceneManager.LoadScene("LoadingScene");
        nextScene = sceneName;
    }

    private IEnumerator LoadSceneAsync()
    {
        string scenePath = scenePrefix + nextScene;
        AsyncOperationHandle handle = Addressables.LoadSceneAsync(scenePath);
        while (!handle.IsDone) {
            progressBar.value = handle.PercentComplete;
            progressText.text = $"加载进度: {handle.PercentComplete * 100:F2}%";
            yield return null;
        }
    }
}
