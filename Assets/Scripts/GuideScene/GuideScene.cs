using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GuideScene : MonoBehaviour
{
    private string TAG = "GuideScene";

    public List<GameObject> buttonList;

    public GuideShowArea guideShowArea;

    // Start is called before the first frame update
    void Start()
    {
        buttonList[(int)GuideShowArea.SelectType.Overview].GetComponent<GuideSelectButton>().ShowCheckMark(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickSelectButton(int value)
    {
        KLog.I(TAG, "OnClickSelectButton: value = " + value);
        GuideShowArea.SelectType type = (GuideShowArea.SelectType)value;
        ResetButtonList();
        buttonList[value].GetComponent<GuideSelectButton>().ShowCheckMark(true);
        guideShowArea.selectType = type;
    }

    public void Exit()
    {
        KLog.I(TAG, "Exit");
        SceneManager.LoadScene("MainMenuScene");
    }

    private void ResetButtonList()
    {
        for (int i = 0; i < buttonList.Count; i++) {
            buttonList[i].GetComponent<GuideSelectButton>().ShowCheckMark(false);
        }
    }
}
