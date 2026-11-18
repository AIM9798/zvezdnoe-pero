using UnityEngine;
using ZvezdnoePero.Input;
using ZvezdnoePero.Player;

namespace ZvezdnoePero.CameraRig
{
    /// <summary>
    /// Камера за спиной сверху, как в прототипе. Свайп справа — поворот и наклон.
    /// На метле камера отъезжает дальше. Не проходит сквозь стены.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] float walkDistance = 7.5f;
        [SerializeField] float broomDistance = 11f;
        [SerializeField] float height = 1.4f;
        [SerializeField] float pitch = 22f;
        [SerializeField] float minPitch = 5f;
        [SerializeField] float maxPitch = 70f;
        [SerializeField] float smooth = 10f;
        [SerializeField] LayerMask collisionMask = ~0;

        float yaw;
        float distance;

        public void Configure(Transform follow, PlayerInputReader reader, PlayerMotor playerMotor)
        {
            target = follow; input = reader; motor = playerMotor;
        }

        void Start()
        {
            if (target != null) yaw = target.eulerAngles.y;
            distance = walkDistance;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;

            if (input != null)
            {
                yaw += input.Look.x;
                pitch = Mathf.Clamp(pitch - input.Look.y, minPitch, maxPitch);
            }

            float wanted = motor != null && motor.OnBroom ? broomDistance : walkDistance;
            distance = Mathf.Lerp(distance, wanted, 1f - Mathf.Exp(-3f * dt));

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * height;
            Vector3 dir = rot * Vector3.back;
            float d = distance;
            if (Physics.SphereCast(pivot, 0.3f, dir, out var hit, distance, collisionMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target))
            {
                d = Mathf.Max(1.2f, hit.distance);
            }

            Vector3 want = pivot + dir * d;
            transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-smooth * dt));
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }
    }
}
