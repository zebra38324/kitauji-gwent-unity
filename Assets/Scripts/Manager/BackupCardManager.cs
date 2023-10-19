using System.Collections.Generic;
using System.Diagnostics;

// 卡牌管理器，保存场上的备用卡牌池信息
public class BackupCardManager
{
    private static readonly BackupCardManager instance = new BackupCardManager();
    private List<CardInfo> cardInfoList = new List<CardInfo>();
    public delegate void BackupCardNumChangeNotifyHandler(int num);
    public event BackupCardNumChangeNotifyHandler BackupCardNumChangeNotify;

    static BackupCardManager() {}

    public static BackupCardManager Instance
    {
        get
        {
            return instance;
        }
    }

    public void SetCardInfoList(List<CardInfo> infoList)
    {
        foreach (CardInfo info in infoList) {
            cardInfoList.Add(info);
        }
        UpdateBackupCardNum();
    }

    public List<CardInfo> GetCardInfos(int num)
    {
        List<CardInfo> result = new List<CardInfo>();
        for (int i = 0; i < num; i++) {
            System.Random ran = new System.Random();
            CardInfo info = cardInfoList[ran.Next(0, cardInfoList.Count)];
            result.Add(info);
            cardInfoList.Remove(info);
        }
        UpdateBackupCardNum();
        return result;
    }

    public List<CardInfo> GetCardInfosWithMusterType(string musterType)
    {
        List<CardInfo> result = new List<CardInfo>();
        foreach (CardInfo info in cardInfoList) {
            if (info.musterType == musterType) {
                result.Add(info);
            }
        }
        foreach (CardInfo info in result) {
            cardInfoList.Remove(info);
        }
        UpdateBackupCardNum();
        return result;
    }

    private void UpdateBackupCardNum()
    {
        BackupCardNumChangeNotify(cardInfoList.Count);
    }
}
