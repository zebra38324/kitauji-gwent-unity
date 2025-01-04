using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager
{
    public RoomManager()
    {
    }

    public void StartPVE()
    {
        PlayerPrefs.SetString(PlayerPrefsKey.PLAY_SCENE_SELF_NAME.ToString(), KConfig.Instance.playerName);
        PlayerPrefs.SetString(PlayerPrefsKey.PLAY_SCENE_ENEMY_NAME.ToString(), "北宇治B编");
        PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_SELF_GROUP.ToString(), (int)CardGroup.KumikoSecondYear);
        PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_ENEMY_GROUP.ToString(), (int)CardGroup.KumikoSecondYear);
        PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_IS_HOST.ToString(), 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("PlayScene");
    }
}
