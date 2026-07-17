using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableMedicine : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 originalPosition;
    private System.Action<DraggableMedicine, PointerEventData> onEndDragCallback;
    private bool isLocked = false;
    private int medicineIndex = -1; // 0 to 7

    public void Init(Canvas parentCanvas, Vector2 spawnPos, int idx, System.Action<DraggableMedicine, PointerEventData> endDragCallback)
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = parentCanvas;
        originalPosition = spawnPos;
        medicineIndex = idx;
        onEndDragCallback = endDragCallback;
        isLocked = false;
    }

    public int GetIndex() => medicineIndex;
    public Vector2 GetOriginalPosition() => originalPosition;

    public void SetLocked(bool lockState)
    {
        isLocked = lockState;
    }

    public void ReturnToOriginalPosition()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
            // Tray size: Height 100px (compact on tray)
            rectTransform.sizeDelta = new Vector2(100f * 0.7589f, 100f); 
        }
    }

    public void SnapTo(Vector2 targetPos)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = targetPos;
            // Snapped box compartment size: Height 160px (enlarged inside box, fitting the compartment)
            rectTransform.sizeDelta = new Vector2(160f * 0.7589f, 160f); 
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        transform.SetAsLastSibling();
        // Dragging size: Height 110px
        rectTransform.sizeDelta = new Vector2(110f * 0.7589f, 110f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        if (rectTransform != null && canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        if (onEndDragCallback != null)
        {
            onEndDragCallback(this, eventData);
        }
    }
}
