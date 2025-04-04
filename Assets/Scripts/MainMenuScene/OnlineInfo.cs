using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OnlineInfo : MonoBehaviour
{
    public TextMeshProUGUI onlineInfoText;

    // Start is called before the first frame update
    void Start()
    {
        List<int> allUserStatus = KHeartbeat.Instance.RecvHeartbeat();
        if (allUserStatus == null) {
            allUserStatus = new List<int>(new int[(int)KHeartbeat.UserStatus.COUNT]);
        }
        SetText(allUserStatus);
    }

    // Update is called once per frame
    void Update()
    {

        List<int> allUserStatus = KHeartbeat.Instance.RecvHeartbeat();
        if (allUserStatus != null) {
            SetText(allUserStatus);
        }
    }

    private void SetText(List<int> allUserStatus)
    {
        int sum = 0;
        foreach (int count in allUserStatus) {
            sum += count;
        }
        string newText = "";
        newText += string.Format("在线人数：{0}\n", sum.ToString());
        newText += string.Format("空闲中：{0}\n", allUserStatus[(int)KHeartbeat.UserStatus.IDLE].ToString());
        newText += string.Format("匹配中：{0}\n", allUserStatus[(int)KHeartbeat.UserStatus.PVP_MATCHING].ToString());
        newText += string.Format("游戏中：{0}", (allUserStatus[(int)KHeartbeat.UserStatus.PVP_GAMING] + allUserStatus[(int)KHeartbeat.UserStatus.PVE_GAMING]).ToString());
        onlineInfoText.text = newText;
    }
}
