using System.Collections.Generic;
using UnityEngine;
using EstanDentro.Interaction;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// El candado 3D que se ve en el mundo. Mirarlo (o mirar lo que cierra) y pulsar E acerca la cámara a ESTE candado
    /// (InspeccionCandado); al resolverlo se suelta y cae. También es lo que Mateo fotografía.
    /// </summary>
    public class CandadoVisual : Interactable
    {
        public CombinationLock cerradura;
        public AudioClip golpe;
        bool caido;

        static readonly Dictionary<CombinationLock, CandadoVisual> registro = new Dictionary<CombinationLock, CandadoVisual>();
        /// <summary>El candado 3D de una cerradura (null si no tiene: entonces se usa la pantalla clásica).</summary>
        public static CandadoVisual De(CombinationLock cl) => cl != null && registro.TryGetValue(cl, out var v) && v != null && !v.caido ? v : null;

        void Awake()
        {
            if (cerradura == null) return;
            registro[cerradura] = this;
            cerradura.onSolved.AddListener(Soltar);
        }

        void OnDestroy()
        {
            if (cerradura == null) return;
            cerradura.onSolved.RemoveListener(Soltar);
            if (registro.TryGetValue(cerradura, out var v) && v == this) registro.Remove(cerradura);
        }

        public override void Interact()
        {
            if (caido || cerradura == null) return;
            cerradura.Interact();
        }

        /// <summary>Se abre y cae (una sola vez).</summary>
        public void Soltar()
        {
            if (caido) return;
            caido = true;
            transform.SetParent(null, true);
            foreach (var c in GetComponentsInChildren<Collider>()) Destroy(c);   // ruedas incluidas
            var mc = gameObject.AddComponent<MeshCollider>(); mc.convex = true;
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.AddForce(transform.forward * 0.25f + Vector3.up * 0.1f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 0.02f, ForceMode.Impulse);
            if (golpe) AudioSource.PlayClipAtPoint(golpe, transform.position, 0.8f);
        }
    }
}
