using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [HideInInspector]
    private CardInfo cardInfo;

    public GameObject originImage;

    // Dialog
    public GameObject dialogBackground;
    public GameObject cardName;
    public GameObject quote;
    public GameObject chineseName;

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

    private CardPowerBuff cardPowerBuff;

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

    private void ShowCard()
    {
        originImage.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/origin-image/KumikoSecondYear/" + cardInfo.imageName);

        // Dialog
        dialogBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/dialog/dialog");
        cardName.GetComponent<TextMeshProUGUI>().text = cardInfo.englishName;
        quote.GetComponent<TextMeshProUGUI>().text = cardInfo.quote;
        chineseName.GetComponent<TextMeshProUGUI>().text = cardInfo.chineseName;

        // Belt
        belt.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/belt/" + GetBeltName());

        // Power
        if (cardInfo.cardType == CardType.Hero) {
            powerBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/power/power-hero");
            powerNum.GetComponent<TextMeshProUGUI>().text = cardPowerBuff.basePower.ToString();
            powerNum.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);
        } else {
            powerBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/power/power-normal");
            powerNum.GetComponent<TextMeshProUGUI>().text = cardPowerBuff.basePower.ToString();
        }
        UpdateDisplayPower();
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

    public void SetCardInfo(CardInfo info)
    {
        cardInfo = info;
        InitCardPowerBuff();
    }

    public CardInfo GetCardInfo()
    {
        return cardInfo;
    }

    public int GetCurrentPower()
    {
        return (cardPowerBuff.basePower + cardPowerBuff.add + cardPowerBuff.minus) * cardPowerBuff.times;
    }

    // 添加点数buff
    public void SetBuffAddMinus(int diff)
    {
        if (diff > 0) {
            cardPowerBuff.add += diff;
        } else {
            cardPowerBuff.minus += diff;
        }
        UpdateDisplayPower();
    }

    public void SetBuffTimes(int times)
    {
        if (times < 1) {
            return;
        }
        cardPowerBuff.times = times;
        UpdateDisplayPower();
    }

    // 消除除天气外的debuff
    public void ClearNormalDebuff()
    {
        if (cardPowerBuff.basePower < cardInfo.originPower) {
            cardPowerBuff.basePower = cardInfo.originPower;
        }
        cardPowerBuff.minus = 0;
        UpdateDisplayPower();
    }

    private void InitCardPowerBuff()
    {
        cardPowerBuff.basePower = cardInfo.originPower;
        cardPowerBuff.add = 0;
        cardPowerBuff.minus = 0;
        cardPowerBuff.times = 1;
    }

    private void UpdateDisplayPower()
    {
        int result = GetCurrentPower();
        powerNum.GetComponent<TextMeshProUGUI>().text = result.ToString();
        if (cardInfo.cardType == CardType.Normal) {
            UpdatePowerNumColor(result);
        }
    }

    private void UpdatePowerNumColor(int newPower)
    {
        if (newPower > cardInfo.originPower) {
            powerNum.GetComponent<TextMeshProUGUI>().color = new Color(0, 0.8f, 0, 1);
        } else if (newPower == cardInfo.originPower) {
            powerNum.GetComponent<TextMeshProUGUI>().color = new Color(0, 0, 0, 1);
        } else {
            powerNum.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0, 0, 1);
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
            case CardAbility.Tunning:
                return "tunning";
            case CardAbility.Bond:
                return "bond";
            case CardAbility.ScorchWood:
                return "umbrella";
            case CardAbility.Muster:
                return "muster";
            default: // None
                return "none";
        }
    }
}
