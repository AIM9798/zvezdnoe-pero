using UnityEngine;

namespace ZvezdnoePero.World
{
    /// <summary>Лёгкое покачивание и вращение: камни под островами, огоньки, кристаллы.</summary>
    public class FloatingMotion : MonoBehaviour
    {
        [SerializeField] float bobAmplitude = 0.4f;
        [SerializeField] float bobSpeed = 0.6f;
        [SerializeField] float spinSpeed = 0f;

        Vector3 start;
        float phase;

        public void Configure(float amplitude, float speed, float spin)
        {
            bobAmplitude = amplitude; bobSpeed = speed; spinSpeed = spin;
        }

        void Start()
        {
            start = transform.localPosition;
            phase = Random.value * 10f;
        }

        void Update()
        {
            float t = Time.time * bobSpeed + phase;
            transform.localPosition = start + Vector3.up * (Mathf.Sin(t) * bobAmplitude);
            if (spinSpeed != 0f) transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
