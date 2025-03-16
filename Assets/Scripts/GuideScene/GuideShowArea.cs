using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GuideShowArea : MonoBehaviour
{
    private string TAG = "GuideShowArea";

    public TextMeshProUGUI pageNum;

    public List<GuidePageCollection> pageCollectionList;

    public enum SelectType
    {
        Overview = 0,
        Flow,
        PlayScene,
        Card,
        CardGroup,
    }

    private SelectType selectType_ = SelectType.Overview;

    public SelectType selectType {
        get {
            return selectType_;
        }
        set {
            if (selectType_ == value) {
                return;
            }
            pageCollectionList[(int)selectType_].SetActive(false);
            selectType_ = value;
            pageCollectionList[(int)selectType_].SetActive(true);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        pageCollectionList[(int)SelectType.Overview].SetActive(true);
        UpdatePageNum();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePageNum();
    }

    public void OnClickPageLeft()
    {
        KLog.I(TAG, "OnClickPageLeft");
        GuidePageCollection cur = pageCollectionList[(int)selectType];
        cur.PrevPage();
    }

    public void OnClickPageRight()
    {
        KLog.I(TAG, "OnClickPageRight");
        GuidePageCollection cur = pageCollectionList[(int)selectType];
        cur.NextPage();
    }

    private void UpdatePageNum()
    {
        GuidePageCollection cur = pageCollectionList[(int)selectType];
        pageNum.text = string.Format("{0}/{1}", cur.curPage.ToString(), cur.totalPage.ToString());
    }
}
