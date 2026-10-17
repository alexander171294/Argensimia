using UnityEngine;

namespace Argensimia
{
    // Giro continuo (rotor del helicóptero presidencial, etc.).
    public class Spin : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        [Tooltip("Grados por segundo")] public float speed = 120f;

        void Update() => transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
    }
}
