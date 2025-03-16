using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideCardAbility : MonoBehaviour
{
    public GameObject descCollection;

    public GameObject descPrefab;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init()
    {
        foreach (CardAbility cardAbility in Enum.GetValues(typeof(CardAbility))) {
            if (cardAbility == CardAbility.None) {
                continue;
            }
            GameObject desc = Instantiate(descPrefab, descCollection.transform);
            KResources.Instance.Load<Sprite>(desc.GetComponent<GuideCardAbilityCell>().image, CardDisplay.GetAbilityImagePath(cardAbility));
            desc.GetComponent<GuideCardAbilityCell>().desc.text = CardText.cardAbilityText[(int)cardAbility];
        }
    }
}
