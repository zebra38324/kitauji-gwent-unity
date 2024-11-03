using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardRowController : MonoBehaviour
{
    private CardRowView cardRowView;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Init()
    {
        cardRowView = gameObject.GetComponent<CardRowView>();
    }

    public void AddCard(GameObject newCard)
    {
        cardRowView.AddCard(newCard);
    }

    public void Remove(int cardId)
    {
        cardRowView.RemoveCard(cardId);
    }
}
