using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WeatherCardAreaView : MonoBehaviour
{
    public GameObject woodArea;

    public GameObject brassArea;

    public GameObject percussionArea;

    private WeatherAreaModel weatherAreaModel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    // model变化时，尝试更新ui
    public void UpdateModel(WeatherAreaModel model)
    {
        if (weatherAreaModel == model) {
            return;
        }
        weatherAreaModel = model;
        woodArea.GetComponent<RowAreaView>().UpdateModel(weatherAreaModel.wood);
        brassArea.GetComponent<RowAreaView>().UpdateModel(weatherAreaModel.brass);
        percussionArea.GetComponent<RowAreaView>().UpdateModel(weatherAreaModel.percussion);
    }
}
