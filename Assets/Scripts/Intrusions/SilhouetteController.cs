using UnityEngine;

namespace EstanDentro.Intrusions
{
    /// <summary>
    /// Controla la visibilidad de una silueta periferica basada en el angulo
    /// entre el forward de la camara y la direccion hacia la silueta.
    /// Regla del GDD:
    ///   angulo &lt; innerAngleDeg  -> opacidad 0   (si la miras de frente, desaparece)
    ///   entre inner y outer      -> opacidad gradual
    ///   angulo &gt; outerAngleDeg  -> opacidad maxima
    /// El shader (Shader Graph HDRP) solo necesita una property "_Opacity" (float).
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class SilhouetteController : MonoBehaviour
    {
        [Header("Camara")]
        [Tooltip("Si queda vacia, usa Camera.main.")]
        public Camera targetCamera;

        [Header("Curva de opacidad por angulo")]
        [Range(0f, 90f)] public float innerAngleDeg = 15f;
        [Range(0f, 180f)] public float outerAngleDeg = 45f;
        [Range(0f, 1f)] public float maxOpacity = 1f;
        [Tooltip("Velocidad de interpolacion de la opacidad (mayor = mas brusco).")]
        public float fadeSpeed = 6f;

        [Header("Adherida al jugador (estilo Lisa de P.T.)")]
        [Tooltip("Si esta activo, la silueta se mantiene a lockDistance del jugador, al angulo lockAngleDeg desde camera.forward.")]
        public bool lockToView = false;
        [Tooltip("Distancia a la que orbita la silueta alrededor del jugador.")]
        public float lockDistance = 3f;
        [Tooltip("Angulo fijo respecto a camera.forward (siempre en la periferia).")]
        [Range(0f, 180f)] public float lockAngleDeg = 60f;
        [Tooltip("Si esta activo, la silueta gira lentamente alrededor del jugador.")]
        public bool orbit = false;
        public float orbitSpeedDegPerSec = 8f;

        [Header("Pulso de tension (opcional)")]
        [Tooltip("Suma una pequena variacion de opacidad para que la silueta respire.")]
        public bool pulse = true;
        public float pulseAmplitude = 0.08f;
        public float pulseFrequency = 0.7f;

        static readonly int OpacityId = Shader.PropertyToID("_Opacity");

        Renderer rend;
        MaterialPropertyBlock mpb;
        float currentOpacity;
        float orbitAngle;

        void Awake()
        {
            rend = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
            if (targetCamera == null) targetCamera = Camera.main;
            orbitAngle = lockAngleDeg;
        }

        void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            if (lockToView) UpdateLockedPosition();

            float target = ComputeTargetOpacity();
            if (pulse)
            {
                target += Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) * pulseAmplitude;
                target = Mathf.Clamp01(target);
            }

            currentOpacity = Mathf.Lerp(currentOpacity, target, Time.deltaTime * fadeSpeed);
            ApplyOpacity(currentOpacity);
        }

        float ComputeTargetOpacity()
        {
            Vector3 toSil = transform.position - targetCamera.transform.position;
            if (toSil.sqrMagnitude < 0.0001f) return 0f;

            float angle = Vector3.Angle(targetCamera.transform.forward, toSil);

            if (angle <= innerAngleDeg) return 0f;
            if (angle >= outerAngleDeg) return maxOpacity;

            float t = Mathf.InverseLerp(innerAngleDeg, outerAngleDeg, angle);
            // curva suave: smoothstep para que el fade no sea lineal
            t = t * t * (3f - 2f * t);
            return t * maxOpacity;
        }

        void UpdateLockedPosition()
        {
            Transform cam = targetCamera.transform;
            if (orbit) orbitAngle += orbitSpeedDegPerSec * Time.deltaTime;

            // Rota el forward de la camara alrededor de su eje up un angulo fijo.
            Quaternion rot = Quaternion.AngleAxis(orbitAngle, cam.up);
            Vector3 dir = rot * cam.forward;
            transform.position = cam.position + dir.normalized * lockDistance;
            transform.rotation = Quaternion.LookRotation(cam.position - transform.position, Vector3.up);
        }

        void ApplyOpacity(float value)
        {
            rend.GetPropertyBlock(mpb);
            mpb.SetFloat(OpacityId, value);
            rend.SetPropertyBlock(mpb);
        }

        void OnDrawGizmosSelected()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            Vector3 origin = cam.transform.position;
            Vector3 fwd = cam.transform.forward;

            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            DrawCone(origin, fwd, innerAngleDeg, 5f);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            DrawCone(origin, fwd, outerAngleDeg, 5f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, transform.position);
        }

        void DrawCone(Vector3 origin, Vector3 dir, float halfAngleDeg, float length)
        {
            int segments = 24;
            Vector3 axis = dir.normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Cross(axis, up).normalized;
            up = Vector3.Cross(right, axis).normalized;

            float r = Mathf.Tan(halfAngleDeg * Mathf.Deg2Rad) * length;
            Vector3 tip = origin;
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 p = tip + axis * length + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
                if (i > 0) Gizmos.DrawLine(prev, p);
                if (i % 6 == 0) Gizmos.DrawLine(tip, p);
                prev = p;
            }
        }
    }
}
