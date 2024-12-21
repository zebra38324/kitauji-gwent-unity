using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameFinishAreaView : MonoBehaviour
{

    public GameObject gameResultTable;

    public GameObject gameResultTableCellPrefab;

    public GameObject gameFinishAreaResultText;

    public PlayStateTracker tracker { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        UpdateUI();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ExitButton()
    {
        PlaySceneManager.Instance.Reset();
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateUI()
    {
        // 绘制表格
        GameObject cellEmpty = GameObject.Instantiate(gameResultTableCellPrefab, gameResultTable.transform);
        GameObject cellSelfName = GameObject.Instantiate(gameResultTableCellPrefab, gameResultTable.transform);
        cellSelfName.GetComponent<TextMeshProUGUI>().text = tracker.selfName;
        GameObject cellEnemyName = GameObject.Instantiate(gameResultTableCellPrefab, gameResultTable.transform);
        cellEnemyName.GetComponent<TextMeshProUGUI>().text = tracker.enemyName;

        for (int i = 0; i <= tracker.curSet; i++) {
            GameObject cellSetTitle = GameObject.Instantiate(gameResultTableCellPrefab, gameResultTable.transform);
            cellSetTitle.GetComponent<TextMeshProUGUI>().text = string.Format("第{0}局", (i + 1).ToString());
            GameObject cellSelfScore = GameObject.Instantiate(gameResultTableCellPrefab, gameResultTable.transform);
            cellSelfScore.GetComponent<TextMeshProUGUI>().text = tracker.setRecordList[i].selfScore.ToString();
            GameObject cellEnemyScore = GameObject.Instantiate(gameResultTableCellPrefab, gameResultTable.transform);
            cellEnemyScore.GetComponent<TextMeshProUGUI>().text = tracker.setRecordList[i].enemyScore.ToString();
            // 添加颜色
            if (tracker.setRecordList[i].result == 1) {
                cellSelfScore.GetComponent<TextMeshProUGUI>().color = new Color(0, 0.8f, 0, 1); // green
                cellEnemyScore.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0, 0, 1); // red
            } else if (tracker.setRecordList[i].result == -1) {
                cellSelfScore.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0, 0, 1); // red
                cellEnemyScore.GetComponent<TextMeshProUGUI>().color = new Color(0, 0.8f, 0, 1); // green
            } else {
                cellSelfScore.GetComponent<TextMeshProUGUI>().color = new Color(0, 0.8f, 0, 1); // green
                cellEnemyScore.GetComponent<TextMeshProUGUI>().color = new Color(0, 0.8f, 0, 1); // green
            }
            // 先手添加下划线
            if (tracker.setRecordList[i].selfFirst) {
                cellSelfScore.GetComponent<TextMeshProUGUI>().text = string.Format("<u>{0}</u>", cellSelfScore.GetComponent<TextMeshProUGUI>().text);
            } else {
                cellEnemyScore.GetComponent<TextMeshProUGUI>().text = string.Format("<u>{0}</u>", cellEnemyScore.GetComponent<TextMeshProUGUI>().text);
            }
        }
        GameObject cellResultTitle = GameObject.Instantiate(gameResultTableCellPrefab, gameResultTable.transform);
        cellResultTitle.GetComponent<TextMeshProUGUI>().text = "大比分";
        GameObject cellSelfResult = GameObject.Instantiate(gameResultTableCellPrefab, gameResultTable.transform);
        cellSelfResult.GetComponent<TextMeshProUGUI>().text = string.Format("<b>{0}</b>", tracker.selfSetScore.ToString());
        GameObject cellEnemyResult = GameObject.Instantiate(gameResultTableCellPrefab, gameResultTable.transform);
        cellEnemyResult.GetComponent<TextMeshProUGUI>().text = string.Format("<b>{0}</b>", tracker.enemySetScore.ToString());
        // 添加颜色
        if (tracker.isSelfWinner) {
            cellSelfResult.GetComponent<TextMeshProUGUI>().color = new Color(0, 0.8f, 0, 1); // green
            cellEnemyResult.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0, 0, 1); // red
        } else {
            cellSelfResult.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0, 0, 1); // red
            cellEnemyResult.GetComponent<TextMeshProUGUI>().color = new Color(0, 0.8f, 0, 1); // green
        }

        gameFinishAreaResultText.GetComponent<TextMeshProUGUI>().text = string.Format("{0} 获得胜利！", tracker.isSelfWinner ? tracker.selfName : tracker.enemyName);
    }
}
