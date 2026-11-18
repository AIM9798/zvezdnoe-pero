using UnityEngine;
using UnityEngine.EventSystems;

namespace ZvezdnoePero.Input
{
    /// <summary>
    /// Экранный джойстик. Палец ставится в любое место зоны, ручка следует за ним.
    /// Value — направление от -1 до 1 по каждой оси.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform background;
        [SerializeField] RectTransform handle;
        [SerializeField] float radius = 110f;
        [SerializeField, Range(0f, 0.5f)] float deadZone = 0.12f;

        public Vector2 Value { get; private set; }

        RectTransform area;
        Vector2 restPosition;

        public void Configure(RectTransform bg, RectTransform knob, float r)
        {
            background = bg; handle = knob; radius = r;
        }

        void Awake()
        {
            area = (RectTransform)transform;
            if (background != null) restPosition = background.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (background != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out var local))
            {
                background.anchoredPosition = local;
            }
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (background == null || handle == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, e.pressEventCamera, out var local))
                return;
            var v = Vector2.ClampMagnitude(local, radius);
            handle.anchoredPosition = v;
            var n = v / radius;
            Value = n.magnitude < deadZone ? Vector2.zero : n;
        }

        public void OnPointerUp(PointerEventData e)
        {
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (background != null) background.anchoredPosition = restPosition;
        }
    }
}
