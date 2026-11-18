using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZvezdnoePero.Input
{
    /// <summary>Кнопка, которую можно удерживать (вверх/вниз на метле) или просто нажать.</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }
        public event Action Pressed;

        public void OnPointerDown(PointerEventData e)
        {
            Held = true;
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData e) => Held = false;
        public void OnPointerExit(PointerEventData e) => Held = false;
    }
}
