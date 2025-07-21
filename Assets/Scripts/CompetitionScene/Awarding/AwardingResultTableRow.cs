using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AwardingResultTableRow : MonoBehaviour
{
    public TextMeshProUGUI num;

    public TextMeshProUGUI schoolName;

    public TextMeshProUGUI prize;

    public TextMeshProUGUI promote;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateText(string num_, string schoolName_, string prize_, string promote_)
    {
        num.text = num_;
        schoolName.text = schoolName_;
        prize.text = prize_;
        promote.text = promote_;
    }

    public void HidePromote()
    {
        promote.gameObject.SetActive(false);
    }
}
