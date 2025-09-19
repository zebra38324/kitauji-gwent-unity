using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using Cysharp.Threading.Tasks;

public class RoomManager
{
    public RoomManager()
    {
    }

    public void StartPVE(PlaySceneAI.AIType aiType)
    {
        GameConfig.Instance.Reset();
        var gameConfig = GameConfig.Instance;
        gameConfig.selfName = KConfig.Instance.playerName;
        gameConfig.enemyName = "北宇治B编";
        gameConfig.selfGroup = KConfig.Instance.deckCardGroup;
        gameConfig.isHost = true;
        gameConfig.pveAIType = aiType;
        gameConfig.fromScene = "MainMenuScene";
        SceneManager.LoadScene("PlayScene");
    }
}
