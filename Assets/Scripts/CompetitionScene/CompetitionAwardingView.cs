using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompetitionAwardingView : MonoBehaviour
{
    private string TAG = "CompetitionAwardingView";

    public AwardingResult awardingResult;

    public Button continueButton;

    public Button restartButton;

    public Button restartCurrentButton;

    public Image review;

    private CompetitionContextModel context;

    private long lastUpdateReviewTs = 0;

    private bool enableReview = false;

    private int reviewImgNameIndex = 0;

    private static string[] reviewImgNameList = { "Kyoto_A_1.jpg", "Kyoto_A_2.jpg", "Kyoto_A_3.jpg", "Kyoto_A_4.jpg", "Kyoto_A_5.jpg" };

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (enableReview && KTime.CurrentMill() - lastUpdateReviewTs > 10000) {
            lastUpdateReviewTs = KTime.CurrentMill();
            StartCoroutine(UpdateReview());
        }
    }

    public void Show(CompetitionContextModel context)
    {
        KLog.I(TAG, "Show");
        // reset
        enableReview = false;
        lastUpdateReviewTs = 0;
        review.color = new Color(review.color.r, review.color.g, review.color.b, 0f);
        review.sprite = null;
        continueButton.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
        restartCurrentButton.gameObject.SetActive(false);

        this.context = context;
        gameObject.SetActive(true);
        context.FinishCurrentLevel();
        awardingResult.Show(context);
        AudioManager.Instance.PauseBGM();
        AudioManager.Instance.PlaySFX(AudioManager.SFXType.Awarding);
        StartCoroutine(UpdateUI());
    }

    public void Hide()
    {
        KLog.I(TAG, "Hide");
        gameObject.SetActive(false);
        KConfig.Instance.SaveCompetitionContext(context.GetRecord());
        AudioManager.Instance.StopSFX();
        AudioManager.Instance.ResumeBGM();
    }

    private IEnumerator UpdateUI()
    {
        // 0-3s，保留
        // 2s开始展示剧照
        yield return new WaitForSeconds(2);
        enableReview = true;
        yield return new WaitForSeconds(1.5f);

        // 3-4s，awardingResult移动
        yield return awardingResult.Move();

        // 4s后，按钮显示
        var playerTeam = context.teamDict[context.playerName];
        continueButton.gameObject.SetActive(playerTeam.prize == CompetitionBase.Prize.GoldPromote);
        restartButton.gameObject.SetActive(true);
        restartCurrentButton.gameObject.SetActive(true);
    }

    private IEnumerator UpdateReview()
    {
        Image image = review;
        float elapsed = 0f;
        // 淡入淡出效果
        float duration = 1500f;
        long startTs = KTime.CurrentMill();
        while (elapsed < duration) {
            float alpha = image.sprite == null ? 0f : Mathf.Lerp(0f, 1f, (duration - elapsed) / duration);
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
            elapsed = KTime.CurrentMill() - startTs;
            yield return null;
        }
        elapsed = 0f;
        Sprite oldSprite = image.sprite;
        // TODO: 预加载
        KResources.Instance.Load<Sprite>(image, @"Image/Competition/Review/" + reviewImgNameList[reviewImgNameIndex]);
        reviewImgNameIndex = (reviewImgNameIndex + 1) % reviewImgNameList.Length;
        while (image.sprite == oldSprite) {
            yield return null;
        }
        startTs = KTime.CurrentMill();
        while (elapsed < duration) {
            image.color = new Color(image.color.r, image.color.g, image.color.b, Mathf.Lerp(0f, 1f, elapsed / duration));
            elapsed = KTime.CurrentMill() - startTs;
            yield return null;
        }
        image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
    }
}
