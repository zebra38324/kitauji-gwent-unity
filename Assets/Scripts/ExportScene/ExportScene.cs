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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) {
            Export();
        }
    }

    public void Export()
    {
        // 1. 准备环境
        GameObject instance = Instantiate(cardPrefab, transform);
        instance.transform.localPosition = new Vector3(-width / 2f, 0, 0);
        RenderTexture rt = new RenderTexture(width, height, 24);
        Camera cam = new GameObject("TempCam").AddComponent<Camera>();
        gameObject.GetComponent<Canvas>().worldCamera = cam;
        
        // 设置相机参数（根据你的卡牌大小调整位置和正交尺寸）
        cam.targetTexture = rt;
        cam.orthographic = true;
        cam.orthographicSize = 5; // 调整这个值以适配卡牌大小
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); // 设置背景透明

        // 2. 渲染
        cam.Render();

        // 3. 读取像素
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.ARGB32, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // 4. 保存文件
        byte[] bytes = screenShot.EncodeToPNG();
        string filename = Application.dataPath + "/CardExport.png";
        File.WriteAllBytes(filename, bytes);

        // 5. 清理
        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(cam.gameObject);
        RenderTexture.active = null;
        Debug.Log("导出成功: " + filename);
    }
}
