using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChoosePVEArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool isPointerInside = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            if (!isPointerInside) {
                gameObject.SetActive(false);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
    }

    public void OnClickK1()
    {
        RoomManager roomManager = new RoomManager();
        roomManager.StartPVE(PlaySceneAI.AIType.L1K1);
    }

    public void OnClickK2()
    {
        RoomManager roomManager = new RoomManager();
        roomManager.StartPVE(PlaySceneAI.AIType.L1K2);
    }
}
