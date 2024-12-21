using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeatherCardAreaView : MonoBehaviour
{
    public GameObject woodArea;

    public GameObject brassArea;

    public GameObject percussionArea;

    private WeatherCardAreaModel weatherCardAreaModel_;
    public WeatherCardAreaModel weatherCardAreaModel {
        get {
            return weatherCardAreaModel_;
        }
        set {
            weatherCardAreaModel_ = value;
            woodArea.GetComponent<RowAreaView>().rowAreaModel = weatherCardAreaModel_.woodArea;
            brassArea.GetComponent<RowAreaView>().rowAreaModel = weatherCardAreaModel_.brassArea;
            percussionArea.GetComponent<RowAreaView>().rowAreaModel = weatherCardAreaModel_.percussionArea;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateUI()
    {
        woodArea.GetComponent<RowAreaView>().UpdateUI();
        brassArea.GetComponent<RowAreaView>().UpdateUI();
        percussionArea.GetComponent<RowAreaView>().UpdateUI();
    }
}
