using UnityEngine;
using ZvezdnoePero.UI;

namespace ZvezdnoePero.World
{
    /// <summary>Остров-район. Когда игрок заходит в его радиус, сверху показывается название.</summary>
    public class IslandZone : MonoBehaviour
    {
        [SerializeField] string displayName = "Остров";
        [SerializeField, TextArea] string subtitle = "";
        [SerializeField] float radius = 100f;

        static IslandZone current;
        static Transform player;

        public string DisplayName => displayName;
        public float Radius => radius;

        public void Configure(string title, string sub, float r)
        {
            displayName = title; subtitle = sub; radius = r;
        }

        void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }

            Vector3 d = player.position - transform.position;
            d.y = 0f;
            bool inside = d.sqrMagnitude < radius * radius;
            if (inside && current != this)
            {
                current = this;
                Hud.ShowZone(displayName, subtitle);
            }
            else if (!inside && current == this)
            {
                current = null;
                Hud.ShowZone("Небо над островами", "");
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
