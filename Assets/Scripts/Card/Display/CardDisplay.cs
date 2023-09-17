using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [HideInInspector]
    public CardInfo cardInfo;

    public GameObject originImage;

    // Dialog
    public GameObject dialogBackground;
    public GameObject cardName;
    public GameObject quote;

    // Belt
    public GameObject belt;

    // Power
    public GameObject powerBackground;
    public GameObject powerNum;
    public GameObject powerType;

    // Badge
    public GameObject badgeBackground;
    public GameObject badgeType;

    // Ability
    public GameObject abilityBackground;
    public GameObject ability;

    void Awake()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        ShowCard();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ShowCard()
    {
        originImage.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/origin-image/KumikoSecondYear/" + cardInfo.imageName);

        // Dialog
        dialogBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/dialog/dialog");
        cardName.GetComponent<TextMeshProUGUI>().text = cardInfo.englishName;
        quote.GetComponent<TextMeshProUGUI>().text = cardInfo.quote;

        // Belt
        belt.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/belt/" + GetBeltName());

        // Power
        if (cardInfo.cardType == CardType.Hero) {
            powerBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/power/power-hero");
            powerNum.GetComponent<TextMeshProUGUI>().text = cardInfo.originPower.ToString();
            powerNum.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);
        } else {
            powerBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/power/power-normal");
            powerNum.GetComponent<TextMeshProUGUI>().text = cardInfo.originPower.ToString();
        }
        powerType.GetComponent<Image>().color = new Color(0, 0, 0, 0); // TODO: 未考虑非角色牌

        // Badge
        badgeBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/badge/badge");
        badgeType.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/badge/type/" + GetBadgeTypeName());

        // Ability
        if (cardInfo.ability != CardAbility.None)
        {
            abilityBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/ability/ability-background");
            ability.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/ability/" + GetAbilityName());
        } else {
            abilityBackground.SetActive(false);
            ability.SetActive(false);
        }
        
    }

    static string[] beltNames = { "belt-red", "belt-blue", "belt-green" };
    private string GetBeltName()
    {
        switch (cardInfo.group)
        {
            case CardGroup.KumikoFirstYearS1:
            case CardGroup.KumikoFirstYearS2:
                return beltNames[(cardInfo.grade - 1 + 3) % 3];
            default:
                return beltNames[(cardInfo.grade - 2 + 3) % 3];
        }
    }

    private string GetBadgeTypeName()
    {
        switch (cardInfo.badgeType)
        {
            case CardBadgeType.Wood:
                return "wood";
            case CardBadgeType.Brass:
                return "brass";
            default:
                return "perc";
        }
    }

    private string GetAbilityName()
    {
        switch (cardInfo.ability)
        {
            case CardAbility.Attack:
                return "attack";
            case CardAbility.Spy:
                return "spy";
            default: // None
                return "none";
        }
    }
}
