using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardButtonScript : MonoBehaviour
{
    public GameObject discardArea;
    public bool isSelf;
    private bool isShowing = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowDiscardArea()
    {
        if (isShowing) {
            // 避免连续点两边的弃牌区按钮
            discardArea.GetComponent<DiscardArea>().CloseArea();
        }
        isShowing = true;
        DiscardCardManager manager = SelfDiscardCardManager.Instance;
        if (!isSelf) {
            manager = EnemyDiscardCardManager.Instance;
        }
        discardArea.GetComponent<DiscardArea>().ShowArea(manager.GetCardList(), false);
    }
}
