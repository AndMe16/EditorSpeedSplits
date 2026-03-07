using UnityEngine;
using UnityEngine.EventSystems;

namespace EditorSpeedSplits.GUIManager
{
    internal class EditorSplitsUIResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        internal RectTransform Target { get; set; }
        internal float MinWidth { get; set; } = 280f;
        internal float MinHeight { get; set; } = 110f;

        private Vector2 targetBottomLeft;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Target == null)
                return;

            RectTransform parentRect = Target.parent as RectTransform;
            if (parentRect == null)
                return;

            NormalizeAnchorsForRuntimeTransform(Target, parentRect);
            targetBottomLeft = GetBottomLeftLocal(Target, parentRect);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null)
                return;

            RectTransform parentRect = Target.parent as RectTransform;
            if (parentRect == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 currentLocal))
                return;

            float newWidth = Mathf.Max(MinWidth, currentLocal.x - targetBottomLeft.x);
            float newHeight = Mathf.Max(MinHeight, currentLocal.y - targetBottomLeft.y);

            Target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            Target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
        }

        private static void NormalizeAnchorsForRuntimeTransform(RectTransform target, RectTransform parent)
        {
            Vector2 bottomLeft = GetBottomLeftLocal(target, parent);
            Vector2 currentSize = target.rect.size;
            Vector2 parentBottomLeft = parent.rect.min;

            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.zero;
            target.pivot = Vector2.zero;
            target.anchoredPosition = bottomLeft - parentBottomLeft;
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentSize.x);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentSize.y);
        }

        private static Vector2 GetBottomLeftLocal(RectTransform target, RectTransform parent)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            return parent.InverseTransformPoint(corners[0]);
        }
    }
}
