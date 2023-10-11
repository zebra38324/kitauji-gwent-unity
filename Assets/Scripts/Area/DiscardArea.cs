using UnityEngine;

// 打出的牌所在区域，一个RowArea为一排，包括这一排的分数、指挥牌、普通牌
public class DiscardArea : MonoBehaviour
{
    public GameObject discardArea;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CloseDiscardArea()
    {
        discardArea.SetActive(false);
    }
}
