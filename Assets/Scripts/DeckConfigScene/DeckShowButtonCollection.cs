using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DeckShowButtonCollection : MonoBehaviour
{
    private static string TAG = "DeckShowButtonCollection";

    public List<GameObject> buttonList;

    public List<GameObject> deckCardAreaViewList;

    private List<bool> showTypeList;

    void Awake()
    {
        showTypeList = Enumerable.Repeat(true, (int)DeckCardAreaView.ShowType.Count).ToList();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick(int value)
    {
        KLog.I(TAG, "OnClick: value = " + value + ", " + showTypeList[value] + " -> " + !showTypeList[value]);
        DeckCardAreaView.ShowType showType = (DeckCardAreaView.ShowType)value;
        // All选中，点击All，全部清空
        // All未选中，点击All，全部打开
        // 其他按钮，选择后反转，并同步更新All状态
        showTypeList[value] = !showTypeList[value];
        if (showType == DeckCardAreaView.ShowType.All) {
            for (int i = (int)DeckCardAreaView.ShowType.All; i < (int)DeckCardAreaView.ShowType.Count; i++) {
                showTypeList[i] = showTypeList[(int)DeckCardAreaView.ShowType.All];
                buttonList[i].GetComponent<DeckShowButton>().ShowCheckMark(showTypeList[i]);
            }
        } else {
            bool allEnable = true;
            for (int i = (int)DeckCardAreaView.ShowType.All + 1; i < (int)DeckCardAreaView.ShowType.Count; i++) {
                allEnable = allEnable && showTypeList[i];
                buttonList[i].GetComponent<DeckShowButton>().ShowCheckMark(showTypeList[i]);
            }
            showTypeList[(int)DeckCardAreaView.ShowType.All] = allEnable;
            buttonList[(int)DeckCardAreaView.ShowType.All].GetComponent<DeckShowButton>().ShowCheckMark(allEnable);
        }
        foreach (GameObject deckCardAreaView in deckCardAreaViewList) {
            deckCardAreaView.GetComponent<DeckCardAreaView>().UpdateShowType(showTypeList);
        }
    }
}
