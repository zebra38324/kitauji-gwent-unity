using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChoosePVEArea : MonoBehaviour
{
    private static string TAG = "ChoosePVEArea";

    public TMP_Dropdown aiLevelSelect;

    private static int aiLevel = 0; // 默认AI等级

    // Start is called before the first frame update
    void Start()
    {
        aiLevelSelect.GetComponent<TMP_Dropdown>().value = aiLevel;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnClickK1()
    {
        RoomManager roomManager = new RoomManager();
        PlaySceneAI.AIType type = (PlaySceneAI.AIType)(aiLevel * 3 + 0);
        roomManager.StartPVE(type);
    }

    public void OnClickK2()
    {
        RoomManager roomManager = new RoomManager();
        PlaySceneAI.AIType type = (PlaySceneAI.AIType)(aiLevel * 3 + 1);
        roomManager.StartPVE(type);
    }

    public void OnClickK3()
    {
        RoomManager roomManager = new RoomManager();
        PlaySceneAI.AIType type = (PlaySceneAI.AIType)(aiLevel * 3 + 2);
        roomManager.StartPVE(type);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
    }

    public void AILevelChange()
    {
        aiLevel = aiLevelSelect.GetComponent<TMP_Dropdown>().value;
        KLog.I(TAG, "AILevelChange: value = " + aiLevel);
    }
}
