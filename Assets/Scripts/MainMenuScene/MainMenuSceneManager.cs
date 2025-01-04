using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuSceneManager : MonoBehaviour
{
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
        RoomManager roomManager = new RoomManager();
        roomManager.StartPVE();
    }

    public void SwtichToDeckConfigScene()
    {
        SceneManager.LoadScene("DeckConfigScene");
    }
}
