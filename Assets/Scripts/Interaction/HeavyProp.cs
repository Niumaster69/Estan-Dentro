using UnityEngine;

namespace EstanDentro.Interaction
{
    /// <summary>
    /// Hace que un prop NO se mueva al chocar con el player. Default: totalmente inamovible
    /// (Rigidbody.isKinematic = true). Es el approach clasico para muebles decorativos en horror.
    ///
    /// Si en algun mueble especifico SI quieres que se pueda mover (ej. una caja para empujar),
    /// destilda 'Lock In Place' y ajusta masa/drag.
    ///
    /// Uso: seleccionar todos los muebles del salon (Ctrl+Click) y Add Component → Heavy Prop.
    /// </summary>
    public class HeavyProp : MonoBehaviour
    {
        [Header("Comportamiento")]
        [SerializeField, Tooltip("Si true (recomendado): el mueble es totalmente inamovible. El player choca y se detiene, el mueble no se mueve. Default ON.")]
        private bool lockInPlace = true;

        [Header("Si Lock In Place = false (mueble que SI se puede empujar)")]
        [SerializeField, Tooltip("Masa en kg. Mas alto = mas dificil de empujar.")]
        private float mass = 30f;
        [SerializeField, Tooltip("Drag lineal. Mas alto = se detiene mas rapido.")]
        private float linearDrag = 8f;
        [SerializeField, Tooltip("Drag angular. Mas alto = deja de girar mas rapido.")]
        private float angularDrag = 10f;
        [SerializeField, Tooltip("Velocidad maxima en m/s. Aunque el CharacterController empuje fuerte, no excede esto.")]
        private float maxLinearVelocity = 0.3f;
        [SerializeField, Tooltip("Congelar rotacion en X y Z (no se vuelca al chocar).")]
        private bool freezeRotationXZ = true;

        [Header("Setup")]
        [SerializeField, Tooltip("Si el GO no tiene Rigidbody, se agrega uno.")]
        private bool addRigidbodyIfMissing = true;

        [Header("Audio (solo aplica si Lock In Place = false)")]
        [SerializeField, Tooltip("Sonido de raspe al chocar / mover el prop. Se reproduce con cooldown para no spamear.")]
        private AudioClip scrapeClip;
        [SerializeField, Range(0f, 1f)] private float scrapeVolume = 0.6f;
        [SerializeField, Tooltip("Cooldown minimo entre raspes (s) para evitar spam si el player se queda apoyado.")]
        private float scrapeCooldown = 0.4f;
        [SerializeField, Tooltip("Magnitud minima del impulso para gatillar el raspe.")]
        private float scrapeMinImpulse = 0.05f;

        private float lastScrapeTime = -999f;
        private AudioSource scrapeSrc;

        private void Awake()
        {
            ApplyToRigidbody();
            if (scrapeClip != null && !lockInPlace)
            {
                scrapeSrc = gameObject.AddComponent<AudioSource>();
                scrapeSrc.clip = scrapeClip;
                scrapeSrc.playOnAwake = false;
                scrapeSrc.spatialBlend = 1f;
                scrapeSrc.maxDistance = 12f;
                scrapeSrc.rolloffMode = AudioRolloffMode.Linear;
                scrapeSrc.volume = scrapeVolume;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (lockInPlace || scrapeSrc == null || scrapeClip == null) return;
            if (Time.time - lastScrapeTime < scrapeCooldown) return;
            if (collision.relativeVelocity.magnitude < scrapeMinImpulse) return;
            lastScrapeTime = Time.time;
            scrapeSrc.PlayOneShot(scrapeClip, scrapeVolume);
        }

        private void ApplyToRigidbody()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                if (!addRigidbodyIfMissing) return;
                rb = gameObject.AddComponent<Rigidbody>();
            }

            if (lockInPlace)
            {
                // INAMOVIBLE: kinematic + sin gravedad. El player choca, no traspasa, no empuja.
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.None; // irrelevante en kinematic
                return;
            }

            // EMPUJABLE pero pesado
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = mass;
            rb.linearDamping = linearDrag;
            rb.angularDamping = angularDrag;
            rb.maxLinearVelocity = maxLinearVelocity;
            rb.maxAngularVelocity = 0.8f;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            RigidbodyConstraints c = RigidbodyConstraints.None;
            if (freezeRotationXZ) c |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.constraints = c;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            ApplyToRigidbody();
        }
    }
}
