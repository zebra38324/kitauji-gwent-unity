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
        var playerTeam = context.teamDict[context.playerName];
        continueButton.gameObject.SetActive(playerTeam.prize == CompetitionBase.Prize.GoldPromote);
        AudioManager.Instance.PauseBGM();
        AudioManager.Instance.PlaySFX(AudioManager.SFXType.Awarding);
    }

    public void Hide()
    {
        KLog.I(TAG, "Hide");
        gameObject.SetActive(false);
        KConfig.Instance.SaveCompetitionContext(context.GetRecord());
        AudioManager.Instance.StopSFX();
        AudioManager.Instance.ResumeBGM();
    }
}
