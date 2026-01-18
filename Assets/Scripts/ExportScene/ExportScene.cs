using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

public class ExportScene : MonoBehaviour
{
    public GameObject cardPrefab;
    public int width = 410;
    public int height = 775;

    private Camera cam;

    private void Start()
    {
        Init();
        StartCoroutine(ExportAll());
    }

    public void Init()
    {
        // 1. 准备环境
        cam = new GameObject("TempCam").AddComponent<Camera>();
        gameObject.GetComponent<Canvas>().worldCamera = cam;
        
        // 设置相机参数（根据你的卡牌大小调整位置和正交尺寸）
        cam.orthographic = true;
        cam.orthographicSize = 5; // 调整这个值以适配卡牌大小
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); // 设置背景透明
    }

    public IEnumerator ExportAll()
    {
        yield return new WaitForSeconds(1);
        yield return Export(CardGroup.KumikoFirstYear);
        yield return Export(CardGroup.KumikoSecondYear);
        yield return Export(CardGroup.KumikoThirdYear);
        yield return Export(CardGroup.Neutral);
    }

    public IEnumerator Export(CardGroup cardGroup)
    {
        CardGenerator cardGenerator = new CardGenerator(true);
        List<CardModel> allCardModelList = cardGenerator.GetGroupCardList(cardGroup);
        string groupName = cardGroup switch {
            CardGroup.KumikoFirstYear => "K1",
            CardGroup.KumikoSecondYear => "K2",
            CardGroup.KumikoThirdYear => "K3",
            CardGroup.Neutral => "N",
            _ => throw new ArgumentOutOfRangeException()
        };
        foreach (CardModel cardModel in allCardModelList) {
            yield return GenBytes(cardModel);
            string filename = GetPath(groupName, cardModel);
            File.WriteAllBytes(filename, bytes);
            Debug.Log($"{filename} saved.");
        }
    }

    public string GetPath(string groupName, CardModel cardModel)
    {
        int count = 1;
        string filename = Application.dataPath + $"/ExportCardImg/{groupName}/{groupName}_{cardModel.cardInfo.chineseName}.png";
        string directoryPath = Path.GetDirectoryName(filename);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) {
            Directory.CreateDirectory(directoryPath);
        }
        while (File.Exists(filename)) {
            filename = Application.dataPath + $"/ExportCardImg/{groupName}/{groupName}_{cardModel.cardInfo.chineseName}_{count}.png";
            count++;
        }
        return filename;
    }
    
    private byte[] bytes;

    public IEnumerator GenBytes(CardModel cardModel)
    {
        GameObject card = Instantiate(cardPrefab, transform);
        card.transform.localPosition = new Vector3(-width / 2f, 0, 0);
        card.GetComponent<CardDisplay>().SetCardModel(cardModel);
        yield return new WaitForSeconds(1f);
        
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        cam.Render();
        // 3. 读取像素
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.ARGB32, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // 4. 保存文件
        bytes = screenShot.EncodeToPNG();

        // 5. 清理
        Object.DestroyImmediate(card);
        RenderTexture.active = null;
    }
}
