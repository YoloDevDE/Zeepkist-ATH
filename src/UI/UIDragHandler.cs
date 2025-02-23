using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 offset;
    private RectTransform parentRectTransform;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRectTransform = rectTransform.parent as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out offset);
        offset = rectTransform.anchoredPosition - offset;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentRectTransform == null || rectTransform == null)
        {
            return;
        }

        Vector2 pointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out pointerPosition))
        {
            Vector2 targetPosition = pointerPosition + offset;
            rectTransform.anchoredPosition = ClampToParentBounds(targetPosition);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"Dragged to: {rectTransform.anchoredPosition}");
    }

    private Vector2 ClampToParentBounds(Vector2 position)
    {
        Vector2 objectSize = rectTransform.rect.size * rectTransform.lossyScale;
        Vector2 parentSize = parentRectTransform.rect.size;

        float minX = -parentSize.x / 2 + objectSize.x / 2;
        float maxX = parentSize.x / 2 - objectSize.x / 2;
        float minY = -parentSize.y / 2 + objectSize.y / 2;
        float maxY = parentSize.y / 2 - objectSize.y / 2;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}