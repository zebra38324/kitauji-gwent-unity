using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NewSetButtonScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NewSet()
    {
        //List<int> selfInfoIdList = new List<int> { 2001, 2002, 2003 };
        //List<int> enemyInfoIdList = new List<int> { 2001, 2002, 2003 };
        List<int> selfInfoIdList = Enumerable.Range(2001, 50).ToList();
        List<int> enemyInfoIdList = Enumerable.Range(2001, 50).ToList();
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.StartGame, selfInfoIdList, enemyInfoIdList);
    }
}
