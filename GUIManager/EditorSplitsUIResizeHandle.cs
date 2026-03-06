using UnityEngine;
using UnityEngine.EventSystems;

namespace EditorSpeedSplits.GUIManager
{
    internal class EditorSplitsUIResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        internal RectTransform Target { get; set; }

        private RectTransform parentRect;

        private const float MinWidth = 280f;
        private const float MinHeight = 110f;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Target == null)
                return;

            parentRect = Target.parent as RectTransform;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null || parentRect == null)
                return;

            Vector2 delta = eventData.delta;

            Vector2 normalizedDelta = new Vector2(
                delta.x / parentRect.rect.width,
                delta.y / parentRect.rect.height
            );

            Vector2 newAnchorMax = Target.anchorMax + normalizedDelta;

            float minAnchorWidth = MinWidth / parentRect.rect.width;
            float minAnchorHeight = MinHeight / parentRect.rect.height;

            newAnchorMax.x = Mathf.Max(newAnchorMax.x, Target.anchorMin.x + minAnchorWidth);
            newAnchorMax.y = Mathf.Max(newAnchorMax.y, Target.anchorMin.y + minAnchorHeight);

            Target.anchorMax = newAnchorMax;
        }
    }
}