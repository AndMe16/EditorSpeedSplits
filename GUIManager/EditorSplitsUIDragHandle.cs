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
    }
}
