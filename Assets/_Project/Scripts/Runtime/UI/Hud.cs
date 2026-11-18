using UnityEngine;
using UnityEngine.UI;

namespace ZvezdnoePero.UI
{
    /// <summary>Подписи поверх экрана: название острова, подсказка у двери, всплывающие сообщения.</summary>
    public class Hud : MonoBehaviour
    {
        [SerializeField] Text zoneTitle;
        [SerializeField] Text zoneSubtitle;
        [SerializeField] Text hint;
        [SerializeField] Text toast;
        [SerializeField] float toastSeconds = 4f;

        static Hud instance;
        float toastUntil;
        float zoneUntil;

        public void Configure(Text title, Text sub, Text hintText, Text toastText)
        {
            zoneTitle = title; zoneSubtitle = sub; hint = hintText; toast = toastText;
        }

        void Awake()
        {
            instance = this;
            SetText(hint, null);
            SetText(toast, null);
        }

        void Update()
        {
            if (toast != null && toast.enabled && Time.time > toastUntil) SetText(toast, null);
            if (zoneTitle != null && Time.time > zoneUntil)
            {
                float a = Mathf.MoveTowards(zoneTitle.color.a, 0.55f, Time.deltaTime);
                zoneTitle.color = new Color(zoneTitle.color.r, zoneTitle.color.g, zoneTitle.color.b, a);
            }
        }

        public static void ShowZone(string title, string subtitle)
        {
            if (instance == null) return;
            SetText(instance.zoneTitle, title);
            SetText(instance.zoneSubtitle, subtitle);
            if (instance.zoneTitle != null)
            {
                var c = instance.zoneTitle.color;
                instance.zoneTitle.color = new Color(c.r, c.g, c.b, 1f);
            }
            instance.zoneUntil = Time.time + 3f;
        }

        public static void ShowHint(string text)
        {
            if (instance != null) SetText(instance.hint, text);
        }

        public static void Toast(string text)
        {
            if (instance == null) { Debug.Log(text); return; }
            SetText(instance.toast, text);
            instance.toastUntil = Time.time + instance.toastSeconds;
        }

        static void SetText(Text t, string value)
        {
            if (t == null) return;
            t.enabled = !string.IsNullOrEmpty(value);
            t.text = value ?? "";
        }
    }
}
