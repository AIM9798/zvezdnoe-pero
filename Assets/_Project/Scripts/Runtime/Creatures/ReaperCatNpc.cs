using UnityEngine;
using ZvezdnoePero.Input;
using ZvezdnoePero.UI;

namespace ZvezdnoePero.Creatures
{
    /// <summary>
    /// Кот-Хранитель Мрак. Модель без скелета, поэтому анимация процедурная, как в прототипе:
    /// парит и дышит, вперевалку ходит между точками, здоровается с игроком,
    /// иногда крутится или «читает». Кнопка «Действие» рядом — поговорить.
    /// </summary>
    public class ReaperCatNpc : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Vector3[] wanderPoints;
        [SerializeField] float walkSpeed = 1.6f;
        [SerializeField] float greetRadius = 6f;
        [SerializeField] float talkRadius = 3.5f;

        static readonly string[] Lines =
        {
            "Мрр. Собери все карточки Академии, и я открою тебе секрет.",
            "Чернила в Зельеварне сегодня особенно пахучие.",
            "Летать над Туманными Рубежами ночью опасно. Но интересно.",
            "Я сторожу эту башню уже триста лет. Ну, или с утра.",
        };

        enum State { Idle, Walk, Greet, Spin, Read }

        State state = State.Idle;
        float stateTime;
        float stateLength = 2f;
        int pointIndex;
        Vector3 baseScale;
        Transform player;
        PlayerInputReader input;
        bool greeted;
        int lineIndex;

        public void Configure(Transform model, Vector3[] points)
        {
            body = model; wanderPoints = points;
        }

        void Start()
        {
            if (body == null) body = transform;
            baseScale = body.localScale;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;
            stateTime += dt;

            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) { player = p.transform; input = p.GetComponent<PlayerInputReader>(); }
            }

            float dist = player != null ? Vector3.Distance(player.position, transform.position) : float.MaxValue;
            if (dist < greetRadius && !greeted && state != State.Greet)
            {
                greeted = true;
                Enter(State.Greet, 1.6f);
                Hud.ShowHint("Поговорить: Кот-Хранитель Мрак");
            }
            else if (dist > greetRadius * 1.5f && greeted)
            {
                greeted = false;
                Hud.ShowHint(null);
            }

            if (dist < talkRadius && input != null && input.ActionPressed)
            {
                Hud.Toast("Мрак: " + Lines[lineIndex]);
                lineIndex = (lineIndex + 1) % Lines.Length;
                Enter(State.Greet, 1.2f);
            }

            switch (state)
            {
                case State.Idle:
                    if (stateTime > stateLength) PickNext();
                    break;
                case State.Walk:
                    WalkTowardsPoint(dt);
                    break;
                case State.Greet:
                    if (player != null) Face(player.position, dt * 6f);
                    if (stateTime > stateLength) Enter(State.Idle, 1.5f);
                    break;
                case State.Spin:
                    transform.Rotate(0f, 360f * dt / stateLength, 0f);
                    if (stateTime > stateLength) Enter(State.Idle, 1f);
                    break;
                case State.Read:
                    if (stateTime > stateLength) Enter(State.Idle, 1f);
                    break;
            }

            Animate(t);
        }

        void PickNext()
        {
            float r = Random.value;
            if (r < 0.15f) Enter(State.Spin, 1.2f);
            else if (r < 0.3f) Enter(State.Read, 3f);
            else if (wanderPoints != null && wanderPoints.Length > 0)
            {
                pointIndex = (pointIndex + 1 + Random.Range(0, wanderPoints.Length - 1)) % wanderPoints.Length;
                Enter(State.Walk, 20f);
            }
            else Enter(State.Idle, 2f);
        }

        void Enter(State s, float length)
        {
            state = s;
            stateTime = 0f;
            stateLength = length;
        }

        void WalkTowardsPoint(float dt)
        {
            Vector3 target = wanderPoints[pointIndex];
            target.y = transform.position.y;
            Vector3 to = target - transform.position;
            if (to.magnitude < 0.3f || stateTime > stateLength) { Enter(State.Idle, Random.Range(1.5f, 4f)); return; }
            Face(target, dt * 4f);
            transform.position += to.normalized * (walkSpeed * dt);
        }

        void Face(Vector3 point, float k)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Mathf.Clamp01(k));
        }

        void Animate(float t)
        {
            float hover = 0.15f + Mathf.Sin(t * 2f) * 0.08f;
            float breathe = 1f + Mathf.Sin(t * 3f) * 0.025f;
            float waddle = 0f, hop = 0f, tilt = 0f;

            if (state == State.Walk) waddle = Mathf.Sin(t * 9f) * 8f;
            if (state == State.Greet) hop = Mathf.Abs(Mathf.Sin(stateTime * 8f)) * 0.25f;
            if (state == State.Read) tilt = 10f + Mathf.Sin(t * 1.5f) * 3f;

            body.localPosition = new Vector3(0f, hover + hop, 0f);
            body.localRotation = Quaternion.Euler(tilt, 0f, waddle);
            body.localScale = new Vector3(baseScale.x * breathe, baseScale.y / breathe, baseScale.z * breathe);
        }
    }
}
