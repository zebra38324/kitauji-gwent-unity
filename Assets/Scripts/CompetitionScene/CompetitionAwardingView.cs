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

    private CompetitionContextModel context;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Show(CompetitionContextModel context)
    {
        KLog.I(TAG, "Show");
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
        yield return new WaitForSeconds(3);

        // 3-4s，awardingResult移动
        float elapsed = 0f;
        float duration = 1000f;
        long startTs = KTime.CurrentMill();
        while (elapsed < duration) {
            float newElapsed = KTime.CurrentMill() - startTs;
            float diffX = Mathf.Lerp(0f, 1f, (newElapsed - elapsed) / duration) * 500f; // target
            var targetPos = awardingResult.transform.localPosition;
            targetPos.x += diffX;
            awardingResult.transform.localPosition = targetPos;
            elapsed = newElapsed;
            yield return null;
        }

        // 4s后，按钮显示
        var playerTeam = context.teamDict[context.playerName];
        continueButton.gameObject.SetActive(playerTeam.prize == CompetitionBase.Prize.GoldPromote);
        restartButton.gameObject.SetActive(true);
        restartCurrentButton.gameObject.SetActive(true);

        // 同时开始展示剧照
        // TODO
        yield break;
    }
}
