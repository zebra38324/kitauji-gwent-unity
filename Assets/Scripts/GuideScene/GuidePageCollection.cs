using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidePageCollection : MonoBehaviour
{
    public List<GameObject> pageList;

    public int curPage { get; private set; }

    public int totalPage {
        get {
            return pageList.Count;
        }
        private set {

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        curPage = 1;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetActive(bool active)
    {
        if (active == gameObject.activeSelf) {
            return;
        }
        foreach (var page in pageList) {
            page.SetActive(false);
        }
        gameObject.SetActive(active);
        curPage = 1;
        GetCurPage().SetActive(active);
    }

    public void NextPage()
    {
        if (curPage >= totalPage) {
            return;
        }
        GetCurPage().SetActive(false);
        curPage += 1;
        GetCurPage().SetActive(true);
    }

    public void PrevPage()
    {
        if (curPage <= 1) {
            return;
        }
        GetCurPage().SetActive(false);
        curPage -= 1;
        GetCurPage().SetActive(true);
    }

    private GameObject GetCurPage()
    {
        return pageList[curPage - 1];
    }
}
