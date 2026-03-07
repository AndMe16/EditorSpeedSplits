using UnityEngine;
using UnityEngine.EventSystems;

namespace EditorSpeedSplits.GUIManager
{
    internal class EditorSplitsUIDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        internal RectTransform Target { get; set; }

        private Vector2 dragStart;
        private Vector2 targetStart;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Target == null)
                return;

            RectTransform parentRect = Target.parent as RectTransform;
            if (parentRect == null)
                return;

            NormalizeAnchorsForRuntimeTransform(Target, parentRect);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out dragStart))
                return;

            targetStart = Target.anchoredPosition;
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

            Vector2 delta = currentLocal - dragStart;
            Target.anchoredPosition = targetStart + delta;
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
