using UnityEngine;
using ZvezdnoePero.Input;
using ZvezdnoePero.UI;

namespace ZvezdnoePero.World
{
    /// <summary>
    /// Дверь здания. Рядом с ней появляется подсказка, кнопка «Действие» — войти.
    /// Интерьеры переносим на этапе «Механики», пока показываем, куда игрок вошёл.
    /// </summary>
    public class BuildingEntrance : MonoBehaviour
    {
        [SerializeField] string buildingName = "Здание";
        [SerializeField, TextArea] string whatInside = "";
        [SerializeField] float useRadius = 3.5f;

        static BuildingEntrance focused;
        Transform player;
        PlayerInputReader input;

        public void Configure(string title, string inside)
        {
            buildingName = title; whatInside = inside;
        }

        void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p == null) return;
                player = p.transform;
                input = p.GetComponent<PlayerInputReader>();
            }

            bool near = (player.position - transform.position).sqrMagnitude < useRadius * useRadius;
            if (near && focused != this)
            {
                focused = this;
                Hud.ShowHint($"Войти: {buildingName}");
            }
            else if (!near && focused == this)
            {
                focused = null;
                Hud.ShowHint(null);
            }

            if (near && focused == this && input != null && input.ActionPressed)
                Hud.Toast($"{buildingName}. {whatInside}");
        }
    }
}
