using UnityEngine;
using UnityEngine.EventSystems;

namespace EditorSpeedSplits.GUIManager
{
    internal class EditorSplitsUIInputBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Plugin.central?.cam == null)
                return;

            Plugin.central.cam.OverrideOutsideGameView(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Plugin.central?.cam == null)
                return;

            Plugin.central.cam.OverrideOutsideGameView(false);
        }
    }
}
