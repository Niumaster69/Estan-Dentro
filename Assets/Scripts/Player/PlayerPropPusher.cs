using UnityEngine;

namespace EstanDentro.Player
{
    /// <summary>
    /// Limita la fuerza con la que el CharacterController del Player empuja Rigidbodies dinamicos
    /// (sillas, mesas, escritorios, props). Por default Unity empuja Rigidbodies con fuerza
    /// "infinita" desde el CC, sin respetar masa — esto provoca que objetos con HeavyProp se
    /// muevan demasiado facil.
    ///
    /// Este script aplica un impulso CONTROLADO inversamente proporcional a la masa del objeto:
    ///   - Mesas pesadas (mass=35-80): apenas se desplazan unos cm
    ///   - Sillas medianas (mass=15-35): se mueven un poco
    ///   - Objetos sueltos (mass<10): se empujan facil
    ///
    /// Uso: Add Component al GameObject del Player (el que tiene CharacterController + PlayerController).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerPropPusher : MonoBehaviour
    {
        [Header("Empuje")]
        [SerializeField, Tooltip("Fuerza base del impulso. Se divide entre la masa del objeto. Subir = empuja mas; bajar = el player es mas 'liviano'.")]
        private float pushPower = 0.4f;
        [SerializeField, Tooltip("Masa minima a usar en la division (evita push infinito si un Rigidbody tiene mass=0).")]
        private float minMassForPush = 1f;
        [SerializeField, Tooltip("Si la masa supera este valor, se ignora el push (objeto inamovible para el player).")]
        private float maxPushableMass = 200f;
        [SerializeField, Tooltip("Si la velocidad horizontal del player es menor que este umbral, no empuja (evita micro-temblores al estar quieto contra un mueble).")]
        private float minSpeedToPush = 0.4f;

        [Header("Filtros")]
        [SerializeField, Tooltip("Si true, ignora props que tengan Rigidbody.isKinematic=true (puertas con LockedDoor, decoraciones).")]
        private bool ignoreKinematic = true;
        [SerializeField, Tooltip("Si true, no empuja hacia abajo (al pisar el suelo no aplica fuerza vertical).")]
        private bool ignoreGroundedHits = true;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private CharacterController cc;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rb = hit.collider.attachedRigidbody;
            if (rb == null) return;
            if (ignoreKinematic && rb.isKinematic) return;
            if (rb.mass > maxPushableMass) return;
            if (ignoreGroundedHits && hit.moveDirection.y < -0.3f) return;

            // Velocidad horizontal del player
            float horizSpeed = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
            if (horizSpeed < minSpeedToPush) return;

            // Empuje horizontal en la direccion de movimiento del player
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            if (pushDir.sqrMagnitude < 0.001f) return;
            pushDir.Normalize();

            float effectiveMass = Mathf.Max(rb.mass, minMassForPush);
            float impulseMag = pushPower * horizSpeed / effectiveMass;

            rb.AddForceAtPosition(pushDir * impulseMag, hit.point, ForceMode.Impulse);

            if (debugLog)
                Debug.Log($"[PropPusher] '{hit.collider.name}' (mass={rb.mass:F1}) push={impulseMag:F2}");
        }
    }
}
