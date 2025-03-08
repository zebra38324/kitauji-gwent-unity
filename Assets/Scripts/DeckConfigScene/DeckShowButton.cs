using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DeckShowButton : MonoBehaviour
{
    public GameObject checkMark;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowCheckMark(bool show)
    {
        checkMark.SetActive(show);
    }
}
