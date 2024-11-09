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

    // 边框
    public GameObject frame;

    private CardPower cardPower;

    private bool hasInit = false;

    void Awake()
    {

    }

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
        if (hasInit) {
            return;
        }
        hasInit = true;
        // Image
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
            powerNum.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);
        } else {
            powerBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/power/power-normal");
        }
        powerNum.GetComponent<TextMeshProUGUI>().text = cardInfo.originPower.ToString();
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
        
        // frame
        if (frame != null) {
            frame.SetActive(false);
        }
    }

    public void SetCardInfo(CardInfo info)
    {
        cardInfo = new CardInfo(info);
        cardPower = new CardPower(cardInfo);
        Init();
    }

    public CardInfo GetCardInfo()
    {
        return new CardInfo(cardInfo);
    }

    public int GetCurrentPower()
    {
        return cardPower.GetCurrentPower();
    }

    // 添加buff并指定添加buff的数量
    public void AddBuff(CardBuffType buffType, int num)
    {
        cardPower.AddBuff(buffType, num);
        UpdateDisplayPower();
    }

    // 移除buff并指定移除buff的数量，为0时表示移除所有
    public void RemoveBuff(CardBuffType buffType, int num = 0)
    {
        cardPower.RemoveBuff(buffType, num);
        UpdateDisplayPower();
    }

    // 设置buff为指定数量
    public void SetBuff(CardBuffType buffType, int num)
    {
        cardPower.RemoveBuff(buffType);
        cardPower.AddBuff(buffType, num);
        UpdateDisplayPower();
    }

    public bool IsDead()
    {
        return cardPower.IsDead();
    }

    // 消除除天气外的debuff
    public void RemoveNormalDebuff()
    {
        cardPower.RemoveNormalDebuff();
        UpdateDisplayPower();
    }

    public void RemoveAllBuff()
    {
        cardPower.RemoveAllBuff();
        UpdateDisplayPower();
    }

    private void UpdateDisplayPower()
    {
        if (cardInfo.cardType == CardType.Hero) {
            return;
        }
        int result = GetCurrentPower();
        powerNum.GetComponent<TextMeshProUGUI>().text = result.ToString();
        UpdatePowerNumColor(result);
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
            case CardAbility.Morale:
                return "morale";
            case CardAbility.Medic:
                return "medic";
            case CardAbility.Horn:
                return "horn";
            default: // None
                return "none";
        }
    }

    public void SetFrameVisible(bool flag)
    {
        if (frame != null) {
            frame.SetActive(flag);
        }
    }
}
