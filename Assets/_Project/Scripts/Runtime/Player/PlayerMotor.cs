using System;
using UnityEngine;
using ZvezdnoePero.Input;
using ZvezdnoePero.UI;

namespace ZvezdnoePero.Player
{
    /// <summary>
    /// Ходьба и полёт на метле. Направление движения берётся относительно камеры,
    /// как в прототипе: джойстик вверх — персонаж идёт «от камеры».
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Ходьба")]
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float jumpHeight = 1.2f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float turnSpeed = 720f;

        [Header("Метла")]
        [SerializeField] float broomSpeed = 15f;
        [SerializeField] float broomVerticalSpeed = 7f;
        [SerializeField] float broomAcceleration = 4f;
        [SerializeField] float maxFlightHeight = 160f;
        [SerializeField] GameObject broomVisual;

        [Header("Ссылки")]
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform cameraTransform;
        [SerializeField] Transform visual;

        [Header("Падение с острова")]
        [SerializeField] float respawnBelowY = -80f;

        public bool OnBroom { get; private set; }
        public float Speed01 { get; private set; }
        public event Action<bool> BroomChanged;

        CharacterController controller;
        Vector3 velocity;
        Vector3 lastSafePosition;
        float bobTime;

        public void Configure(PlayerInputReader reader, Transform cam, Transform body, GameObject broom)
        {
            input = reader; cameraTransform = cam; visual = body; broomVisual = broom;
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            lastSafePosition = transform.position;
            if (broomVisual != null) broomVisual.SetActive(false);
        }

        void Update()
        {
            if (input == null) return;
            float dt = Time.deltaTime;

            if (input.BroomPressed) SetBroom(!OnBroom);

            Vector3 wish = CameraRelative(input.Move);
            if (OnBroom) Fly(wish, dt); else Walk(wish, dt);

            if (wish.sqrMagnitude > 0.001f)
            {
                var target = Quaternion.LookRotation(wish, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * dt);
            }

            AnimateVisual(dt);

            if (transform.position.y < respawnBelowY) Respawn();
        }

        Vector3 CameraRelative(Vector2 move)
        {
            if (move.sqrMagnitude < 0.0001f) return Vector3.zero;
            Vector3 fwd = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            return Vector3.ClampMagnitude(fwd * move.y + right * move.x, 1f);
        }

        void Walk(Vector3 wish, float dt)
        {
            bool grounded = controller.isGrounded;
            if (grounded && velocity.y < 0f) velocity.y = -2f;
            if (grounded && input.JumpPressed) velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            velocity.x = wish.x * walkSpeed;
            velocity.z = wish.z * walkSpeed;
            velocity.y += gravity * dt;

            controller.Move(velocity * dt);
            Speed01 = wish.magnitude;

            if (controller.isGrounded) lastSafePosition = transform.position;
        }

        void Fly(Vector3 wish, float dt)
        {
            Vector3 target = wish * broomSpeed;
            float vertical = (input.Up ? 1f : 0f) - (input.Down ? 1f : 0f);
            target.y = vertical * broomVerticalSpeed;
            if (transform.position.y > maxFlightHeight && target.y > 0f) target.y = 0f;

            velocity = Vector3.Lerp(velocity, target, 1f - Mathf.Exp(-broomAcceleration * dt));
            controller.Move(velocity * dt);
            Speed01 = new Vector2(velocity.x, velocity.z).magnitude / broomSpeed;

            // Приземлились — слезаем с метлы сами.
            if (controller.isGrounded && vertical < 0f) SetBroom(false);
        }

        public void SetBroom(bool on)
        {
            if (OnBroom == on) return;
            OnBroom = on;
            if (on)
            {
                velocity = new Vector3(velocity.x, 0f, velocity.z);
                controller.Move(Vector3.up * 1.2f);
            }
            if (broomVisual != null) broomVisual.SetActive(on);
            BroomChanged?.Invoke(on);
        }

        void AnimateVisual(float dt)
        {
            if (visual == null) return;
            bobTime += dt * (OnBroom ? 2f : 9f * Mathf.Max(Speed01, 0.05f));
            float bob = OnBroom ? Mathf.Sin(bobTime) * 0.12f : Mathf.Abs(Mathf.Sin(bobTime)) * 0.08f * Speed01;
            float lean = OnBroom ? Speed01 * 12f : 0f;
            visual.localPosition = new Vector3(0f, bob, 0f);
            visual.localRotation = Quaternion.Euler(lean, 0f, 0f);
        }

        void Respawn()
        {
            controller.enabled = false;
            transform.position = lastSafePosition + Vector3.up * 2f;
            controller.enabled = true;
            velocity = Vector3.zero;
            SetBroom(false);
            Hud.Toast("Ты упал с острова. Метла поможет летать между островами (кнопка «Метла»)");
        }
    }
}
