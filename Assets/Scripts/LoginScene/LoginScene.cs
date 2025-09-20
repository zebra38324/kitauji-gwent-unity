using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginScene : MonoBehaviour
{
    private static string TAG = "LoginScene";
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnClickTourist()
    {
        KLog.I(TAG, "OnClickTourist");
        SceneManager.LoadScene("MainMenuScene");
    }

    public void OnClickDelete()
    {
        KLog.I(TAG, "OnClickDelete");
        PlayerPrefs.DeleteAll();
    }
}
