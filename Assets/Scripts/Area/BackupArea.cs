using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BackupArea : MonoBehaviour
{
    public GameObject backupCardNum;
    public bool isSelf;

    // Start is called before the first frame update
    void Start()
    {
        if (isSelf) {
            BackupCardManager.Instance.BackupCardNumChangeNotify += new BackupCardManager.BackupCardNumChangeNotifyHandler(BackupCardNumUpdate);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BackupCardNumUpdate(int num)
    {
        backupCardNum.GetComponent<TextMeshProUGUI>().text = num.ToString();
    }
}
