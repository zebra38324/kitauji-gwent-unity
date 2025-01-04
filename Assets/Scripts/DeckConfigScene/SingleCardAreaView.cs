using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleCardAreaView : MonoBehaviour
{
    public GameObject curCard { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddCard(GameObject card)
    {
        if (curCard == card) {
            return;
        }
        UpdateUI(card);
    }

    public void RemoveCard()
    {
        if (curCard == null) {
            return;
        }
        if (curCard.transform.parent == gameObject.transform) {
            curCard.transform.SetParent(null);
        }
        curCard = null;
    }

    private void UpdateUI(GameObject card)
    {
        if (curCard != null) {
            RemoveCard();
        }
        curCard = card;
        curCard.transform.SetParent(gameObject.transform);
        float originCardWidth = curCard.GetComponent<RectTransform>().rect.width;
        float gap = 15f; // 左右gap
        float realCardWidth = gameObject.GetComponent<RectTransform>().rect.width - gap;
        float scale = realCardWidth / originCardWidth;
        curCard.transform.localScale = Vector3.one * scale;
        curCard.transform.localPosition = new Vector3(-realCardWidth / 2, 0, 0);
    }
}
