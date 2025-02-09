using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KanbanView : MonoBehaviour
{
    public GameObject kanban;

    private long lastUpdateTs = 0;

    private int kanbanImgNameIndex = 0;

    private static string[] kanbanImgNameList = { "kanban.kanade.hisaishi.png", "kanban.kumiko.oumae.png",
        "kanban.mizore.yoroizuka.png", "kanban.nozomi.kasaki.png",
        "kanban.reina.kousaka.png", "kanban.ririka.kenzaki.png" };

    // Start is called before the first frame update
    void Start()
    {
        Image image = kanban.GetComponent<Image>();
        image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
        image.sprite = null;
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
            float alpha = image.sprite == null ? 0f : Mathf.Lerp(0f, 1f, (duration - elapsed) / duration);
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
            elapsed = KTime.CurrentMill() - startTs;
            yield return null;
        }
        elapsed = 0f;
        Sprite oldSprite = image.sprite;
        KResources.Instance.Load<Sprite>(image, @"Image/texture/kanban/" + kanbanImgNameList[kanbanImgNameIndex]);
        kanbanImgNameIndex = (kanbanImgNameIndex + 1) % kanbanImgNameList.Length;
        while (image.sprite == oldSprite) {
            yield return null;
        }
        startTs = KTime.CurrentMill();
        while (elapsed < duration) {
            image.color = new Color(image.color.r, image.color.g, image.color.b, Mathf.Lerp(0f, 1f, elapsed / duration));
            elapsed = KTime.CurrentMill() - startTs;
            yield return null;
        }
        image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
    }
}
