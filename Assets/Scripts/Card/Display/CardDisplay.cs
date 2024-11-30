using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardDisplay : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    private static string TAG = "CardDisplay";

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

    private CardModel cardModel;

    // ui交互
    private static int hoverUpDistance = 10; // 悬停时卡片上移距离

    private bool isHovering = false; // 鼠标是否在ui内

    private bool isCardInfoShowing = false; // 当前card info正处于展示状态

    void Awake()
    {

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        JudgeShowCardInfo();
    }

    public void SetCardModel(CardModel model)
    {
        cardModel = model;
        Init();
    }

    // 每次操作完，更新ui
    public void UpdateUI()
    {
        UpdateDisplayPower();
        SetFrameVisible(cardModel.selectType == CardSelectType.WithstandAttack);
    }

    private void Init()
    {
        // Image
        originImage.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/origin-image/KumikoSecondYear/" + cardModel.cardInfo.imageName);

        // Dialog
        dialogBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/dialog/dialog");
        cardName.GetComponent<TextMeshProUGUI>().text = cardModel.cardInfo.englishName;
        quote.GetComponent<TextMeshProUGUI>().text = cardModel.cardInfo.quote;
        chineseName.GetComponent<TextMeshProUGUI>().text = cardModel.cardInfo.chineseName;

        // Belt
        belt.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/belt/" + GetBeltName());

        // Power
        if (cardModel.cardInfo.cardType == CardType.Hero) {
            powerBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/power/power-hero");
            powerNum.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);
        } else {
            powerBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/power/power-normal");
        }
        powerNum.GetComponent<TextMeshProUGUI>().text = cardModel.cardInfo.originPower.ToString();
        powerType.GetComponent<Image>().color = new Color(0, 0, 0, 0); // TODO: 未考虑非角色牌

        // Badge
        badgeBackground.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/badge/badge");
        badgeType.GetComponent<Image>().sprite = Resources.Load<Sprite>(@"Image/texture/badge/type/" + GetBadgeTypeName());

        // Ability
        if (cardModel.cardInfo.ability != CardAbility.None) {
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

    private void UpdateDisplayPower()
    {
        if (cardModel.cardInfo.cardType == CardType.Hero) {
            return;
        }
        int result = cardModel.currentPower;
        powerNum.GetComponent<TextMeshProUGUI>().text = result.ToString();
        UpdatePowerNumColor(result);
    }

    private void UpdatePowerNumColor(int newPower)
    {
        if (newPower > cardModel.cardInfo.originPower) {
            powerNum.GetComponent<TextMeshProUGUI>().color = new Color(0, 0.8f, 0, 1);
        } else if (newPower == cardModel.cardInfo.originPower) {
            powerNum.GetComponent<TextMeshProUGUI>().color = new Color(0, 0, 0, 1);
        } else {
            powerNum.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0, 0, 1);
        }
    }

    static string[] beltNames = { "belt-red", "belt-blue", "belt-green" };
    private string GetBeltName()
    {
        switch (cardModel.cardInfo.group) {
            case CardGroup.KumikoFirstYearS1:
            case CardGroup.KumikoFirstYearS2:
                return beltNames[(cardModel.cardInfo.grade - 1 + 3) % 3];
            default:
                return beltNames[(cardModel.cardInfo.grade - 2 + 3) % 3];
        }
    }

    private string GetBadgeTypeName()
    {
        switch (cardModel.cardInfo.badgeType) {
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
        switch (cardModel.cardInfo.ability) {
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

    private void SetFrameVisible(bool flag)
    {
        if (frame != null) {
            frame.SetActive(flag);
        }
    }

    // ========================= ui交互逻辑 ===========================
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        // hide如果也放在update里，可能造成下一卡片已经展示info，然后info区域被本卡牌关掉
        JudgeHideCardInfo();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        KLog.I(TAG, "on click " + cardModel.cardInfo.chineseName);
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.ChooseCard, cardModel);
    }

    public void UpdatePosition(Vector3 position)
    {
        transform.localPosition = position;
        if (isCardInfoShowing && HoverNeedUp()) {
            // 手牌区、正在展示info时更新了ui，需要保留卡牌上移状态
            transform.Translate(0, hoverUpDistance, 0);
        }
    }

    // 判断悬停时是否需要卡牌上移
    private bool HoverNeedUp()
    {
        return cardModel.cardLocation == CardLocation.HandArea;
    }

    // 判断是否需要显示info
    private void JudgeShowCardInfo()
    {
        if (!isHovering || isCardInfoShowing) {
            return;
        }
        // 将鼠标屏幕坐标转换为RectTransform的局部坐标
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(gameObject.GetComponent<RectTransform>(), Input.mousePosition, null, out localMousePos);
        Rect cardRect = gameObject.GetComponent<RectTransform>().rect;
        Rect excludeRect = new Rect(cardRect.position.x,
                cardRect.position.y,
                cardRect.size.x,
                cardRect.size.y / 10);
        if (excludeRect.Contains(localMousePos)) {
            return; // 避免卡牌上下鬼畜，靠下侧width*(height/10)区域不进行响应
        }
        if (HoverNeedUp()) {
            transform.Translate(0, hoverUpDistance, 0); // 卡片上移
        }
        isCardInfoShowing = true;
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.ShowCardInfo, cardModel.cardInfo);
    }

    // 判断是否需要隐藏info
    private void JudgeHideCardInfo()
    {
        if (!isCardInfoShowing) {
            return;
        }
        if (HoverNeedUp()) {
            transform.Translate(0, -hoverUpDistance, 0); // 卡片下移恢复
        }
        isCardInfoShowing = false;
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.HideCardInfo);
    }
}
