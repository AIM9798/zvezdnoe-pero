using UnityEngine;
using UnityEngine.InputSystem;

namespace ZvezdnoePero.Input
{
    /// <summary>
    /// Собирает ввод с экрана телефона и с клавиатуры (для проверки в редакторе):
    /// WASD — ходьба, мышь с правой кнопкой — камера, Space — прыжок/вверх,
    /// Ctrl — вниз, F — метла, E — действие.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] VirtualJoystick joystick;
        [SerializeField] TouchLookArea lookArea;
        [SerializeField] HoldButton jumpButton;
        [SerializeField] HoldButton downButton;
        [SerializeField] HoldButton broomButton;
        [SerializeField] HoldButton actionButton;
        [SerializeField] float touchLookSensitivity = 0.25f;
        [SerializeField] float mouseLookSensitivity = 0.15f;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Up { get; private set; }
        public bool Down { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool BroomPressed { get; private set; }
        public bool ActionPressed { get; private set; }

        bool jumpQueued, broomQueued, actionQueued;

        public void Configure(VirtualJoystick stick, TouchLookArea look, HoldButton jump, HoldButton down, HoldButton broom, HoldButton action)
        {
            joystick = stick; lookArea = look; jumpButton = jump; downButton = down; broomButton = broom; actionButton = action;
        }

        void OnEnable()
        {
            if (jumpButton != null) jumpButton.Pressed += QueueJump;
            if (broomButton != null) broomButton.Pressed += QueueBroom;
            if (actionButton != null) actionButton.Pressed += QueueAction;
        }

        void OnDisable()
        {
            if (jumpButton != null) jumpButton.Pressed -= QueueJump;
            if (broomButton != null) broomButton.Pressed -= QueueBroom;
            if (actionButton != null) actionButton.Pressed -= QueueAction;
        }

        void QueueJump() => jumpQueued = true;
        void QueueBroom() => broomQueued = true;
        void QueueAction() => actionQueued = true;

        void Update()
        {
            var move = joystick != null ? joystick.Value : Vector2.zero;
            var look = lookArea != null ? lookArea.ConsumeDelta() * touchLookSensitivity : Vector2.zero;
            bool up = jumpButton != null && jumpButton.Held;
            bool down = downButton != null && downButton.Held;

            var kb = Keyboard.current;
            if (kb != null)
            {
                var k = new Vector2(
                    (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0),
                    (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
                if (k != Vector2.zero) move = k.normalized;
                up |= kb.spaceKey.isPressed;
                down |= kb.leftCtrlKey.isPressed;
                jumpQueued |= kb.spaceKey.wasPressedThisFrame;
                broomQueued |= kb.fKey.wasPressedThisFrame;
                actionQueued |= kb.eKey.wasPressedThisFrame;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
                look += mouse.delta.ReadValue() * mouseLookSensitivity;

            Move = move;
            Look = look;
            Up = up;
            Down = down;
            JumpPressed = jumpQueued;
            BroomPressed = broomQueued;
            ActionPressed = actionQueued;
            jumpQueued = broomQueued = actionQueued = false;
        }
    }
}
