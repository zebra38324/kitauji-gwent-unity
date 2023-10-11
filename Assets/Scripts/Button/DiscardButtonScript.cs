using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardButtonScript : MonoBehaviour
{
    public GameObject discardArea;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowDiscardArea()
    {
        discardArea.SetActive(true);
    }

    public void CloseDiscardArea()
    {
        discardArea.SetActive(false);
    }
}
