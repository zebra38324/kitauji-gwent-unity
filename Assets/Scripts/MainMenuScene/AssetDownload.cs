using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetDownload : MonoBehaviour
{
    private static string TAG = "AssetDownload";

    public TextMeshProUGUI progressText;

    private static string[] assetGroupNames = { "image_k1", "image_k2", "image_k3", "image_neutral" };

    private static bool hasShowed = false; // 一次访问只显示一次

    private Dictionary<string, string> progressRecord = new Dictionary<string, string>();

    // Start is called before the first frame update
    void Start()
    {
        if (!hasShowed) {
            foreach (string assetGroupName in assetGroupNames) {
                StartCoroutine(DownloadAsset(assetGroupName));
            }
            hasShowed = true;
        } else {
            gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateProgressText();
    }

    private IEnumerator DownloadAsset(string assetGroupName)
    {
        var downloadHandle = Addressables.DownloadDependenciesAsync(assetGroupName);
        while (!downloadHandle.IsDone) {
            progressRecord[assetGroupName] = string.Format("下载进度 {0:F2}%", downloadHandle.PercentComplete * 100);
            yield return null;
        }
        if (downloadHandle.Status == AsyncOperationStatus.Succeeded) {
            progressRecord[assetGroupName] = "下载完成";
            KLog.I(TAG, "DownloadAsset: " + assetGroupName + " success");
        } else {
            progressRecord[assetGroupName] = "下载失败";
            KLog.E(TAG, "DownloadAsset: " + assetGroupName + " fail");
        }
    }

    private void UpdateProgressText()
    {
        string newProgressText = "建议等待下载完成后，再进行游戏\n";
        bool allSuccess = true;
        foreach (KeyValuePair<string, string> entry in progressRecord) {
            string assetName = "";
            if (entry.Key == "image_k1") {
                assetName = "久一年牌组";
            } else if (entry.Key == "image_k2") {
                assetName = "久二年牌组";
            } else if (entry.Key == "image_k3") {
                assetName = "久三年牌组";
            } else if (entry.Key == "image_neutral") {
                assetName = "中立牌组";
            }
            newProgressText += assetName + ": " + entry.Value + "\n";
            allSuccess = allSuccess && entry.Value == "下载完成";
        }
        progressText.text = newProgressText;
        if (allSuccess) {
            StartCoroutine(HideTip());
        }
    }

    // 下载完成后，隐藏提示
    private IEnumerator HideTip()
    {
        yield return new WaitForSeconds(5);
        KLog.I(TAG, "HideTip");
        gameObject.SetActive(false);
    }
}
