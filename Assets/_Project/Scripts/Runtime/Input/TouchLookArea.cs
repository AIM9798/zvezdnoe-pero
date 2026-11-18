using UnityEngine;
using UnityEngine.EventSystems;

namespace ZvezdnoePero.Input
{
    /// <summary>Правая часть экрана: свайп пальцем вращает камеру.</summary>
    public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        Vector2 accumulated;
        int pointerId = int.MinValue;

        /// <summary>Сдвиг пальца в пикселях с прошлого кадра. Обнуляется при чтении.</summary>
        public Vector2 ConsumeDelta()
        {
            var d = accumulated;
            accumulated = Vector2.zero;
            return d;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId == int.MinValue) pointerId = e.pointerId;
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == pointerId) accumulated += e.delta;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == pointerId) pointerId = int.MinValue;
        }
    }
}
