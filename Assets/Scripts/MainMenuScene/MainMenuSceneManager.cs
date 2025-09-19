using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuSceneManager : MonoBehaviour
{
    private static string TAG = "MainMenuSceneManager";

    public GameObject matchingArea;

    public GameObject toastView;

    public GameObject choosePVEArea;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayPVE()
    {
        choosePVEArea.SetActive(true);
    }

    public void SwtichToDeckConfigScene()
    {
        KLog.I(TAG, "onClick SwtichToDeckConfigScene");
        SceneManager.LoadScene("DeckConfigScene");
    }

    public void SwtichToGuideScene()
    {
        KLog.I(TAG, "onClick SwtichToGuideScene");
        LoadingScene.Load("GuideScene");
    }

    public void SwtichToCompetitionScene()
    {
        KLog.I(TAG, "onClick SwtichToCompetitionScene");
        SceneManager.LoadScene("CompetitionScene");
    }
}
