using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KanbanView : MonoBehaviour
{
    public GameObject kanban;

    private long lastUpdateTs = 0;

    private Sprite[] kanbanImgList = null;

    private int kanbanImgIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        kanbanImgList = Resources.LoadAll<Sprite>(@"Image/texture/kanban");
        lastUpdateTs = KTime.CurrentMill();
        kanban.GetComponent<Image>().sprite = kanbanImgList[kanbanImgIndex];
        kanbanImgIndex = (kanbanImgIndex + 1) % kanbanImgList.Length;
    }

    // Update is called once per frame
    void Update()
    {
        if (KTime.CurrentMill() - lastUpdateTs > 15000) {
            // 15s更新一次
            lastUpdateTs = KTime.CurrentMill();
            StartCoroutine(UpdateKanbanImg());
        }
    }

    private IEnumerator UpdateKanbanImg()
    {
        Image image = kanban.GetComponent<Image>();
        float elapsed = 0f;
        // 2s时间的淡入淡出效果
        float duration = 1000f;
        long startTs = KTime.CurrentMill();
        while (elapsed < duration) {
            image.color = new Color(image.color.r, image.color.g, image.color.b, Mathf.Lerp(0f, 1f, (duration - elapsed) / duration));
            elapsed = KTime.CurrentMill() - startTs;
            yield return null;
        }
        elapsed = 0f;
        startTs = KTime.CurrentMill();
        image.sprite = kanbanImgList[kanbanImgIndex];
        kanbanImgIndex = (kanbanImgIndex + 1) % kanbanImgList.Length;
        while (elapsed < duration) {
            image.color = new Color(image.color.r, image.color.g, image.color.b, Mathf.Lerp(0f, 1f, elapsed / duration));
            elapsed = KTime.CurrentMill() - startTs;
            yield return null;
        }
        image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
    }
}
