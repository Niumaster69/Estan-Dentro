using UnityEngine;
using UnityEngine.InputSystem;

namespace EstanDentro.Interaction
{
    [RequireComponent(typeof(Light))]
    public class Flashlight : MonoBehaviour
    {
        [Header("Estado")]
        [SerializeField] private bool startEnabled = false;
        [SerializeField, Tooltip("Si false, la tecla F / Square no hace nada. Por default la linterna esta bloqueada hasta que el jugador la recoge (la lonchera del Acto 1).")]
        private bool unlocked = false;
        [SerializeField, Tooltip("Si esta seteado, la linterna se desbloquea automaticamente cuando este item este en Inventory. Vacio = ignorar (solo desbloquea con Unlock()).")]
        private string requireInventoryItem = "linterna";

        [Header("Visibilidad del modelo")]
        [SerializeField, Tooltip("Si true, esconde los meshes hijos mientras unlocked=false (la linterna aparece en la mano del player solo al recogerla). Asignar manualmente en hideWhileLocked si los meshes no son hijos directos.")]
        private bool hideMeshWhileLocked = true;
        [SerializeField, Tooltip("MeshRenderers a ocultar mientras la linterna no este desbloqueada. Si esta vacio, se buscan automaticamente en hijos.")]
        private Renderer[] hideWhileLocked;

        [Header("Audio")]
        [SerializeField, Tooltip("Click mecanico al prender/apagar. Se reproduce por AudioManager si esta disponible, si no por AudioSource.PlayClipAtPoint.")]
        private AudioClip toggleClip;
        [SerializeField, Range(0f, 1f)] private float toggleVolume = 0.85f;

        [Header("Apariencia (se aplica al Light al iniciar)")]
        [SerializeField] private bool applyDefaultLook = true;
        [SerializeField, Tooltip("HDRP usa lumenes. 1500-3500 es razonable para una linterna.")]
        private float intensityLumens = 2000f;
        [SerializeField, Tooltip("Distancia maxima en metros.")]
        private float range = 10f;
        [SerializeField, Tooltip("Apertura del cono en grados. 35-50 para horror.")]
        private float spotAngle = 50f;
        [SerializeField, Tooltip("Tinte calido. Blanco puro = (1,1,1). Calido linterna = (1, 0.92, 0.78).")]
        private Color tint = new Color(1f, 0.92f, 0.78f);
        [SerializeField] private bool softShadows = true;

        private Light spotLight;
        private bool wasUnlocked;

        private void Awake()
        {
            spotLight = GetComponent<Light>();
            if (applyDefaultLook) ApplyLook();
            spotLight.enabled = startEnabled;

            if (hideMeshWhileLocked && (hideWhileLocked == null || hideWhileLocked.Length == 0))
                hideWhileLocked = GetComponentsInChildren<Renderer>(true);

            ApplyMeshVisibility();
            wasUnlocked = unlocked;
        }

        private void ApplyMeshVisibility()
        {
            if (!hideMeshWhileLocked || hideWhileLocked == null) return;
            foreach (var r in hideWhileLocked)
                if (r != null) r.enabled = unlocked;
        }

        private void ApplyLook()
        {
            spotLight.type = LightType.Spot;
            spotLight.intensity = intensityLumens;
            spotLight.range = range;
            spotLight.spotAngle = spotAngle;
            spotLight.color = tint;
            spotLight.shadows = softShadows ? LightShadows.Soft : LightShadows.Hard;
        }

        private void Update()
        {
            // Auto-unlock cuando el item este en Inventory (no requiere llamada manual a Unlock()).
            if (!unlocked
                && !string.IsNullOrEmpty(requireInventoryItem)
                && Inventory.Inventory.Instance != null
                && Inventory.Inventory.Instance.HasItem(requireInventoryItem))
            {
                unlocked = true;
            }

            if (unlocked != wasUnlocked)
            {
                ApplyMeshVisibility();
                wasUnlocked = unlocked;
            }

            if (!unlocked) return;
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool toggle = (kb != null && kb.fKey.wasPressedThisFrame)
                       || (gp != null && gp.buttonWest.wasPressedThisFrame);
            if (toggle) Toggle();
        }

        public void Toggle()
        {
            spotLight.enabled = !spotLight.enabled;
            PlayToggleClip();
        }
        public void SetEnabled(bool e)
        {
            if (spotLight.enabled != e) { spotLight.enabled = e; PlayToggleClip(); }
            else spotLight.enabled = e;
        }
        public void Unlock() { unlocked = true; ApplyMeshVisibility(); wasUnlocked = true; }
        public void Lock() { unlocked = false; if (spotLight.enabled) { spotLight.enabled = false; PlayToggleClip(); } ApplyMeshVisibility(); wasUnlocked = false; }

        private void PlayToggleClip()
        {
            if (toggleClip == null) return;
            var am = EstanDentro.Audio.AudioManager.Instance;
            if (am != null) am.PlaySFX(toggleClip, toggleVolume);
            else AudioSource.PlayClipAtPoint(toggleClip, transform.position, toggleVolume);
        }

        public bool IsUnlocked => unlocked;
        public bool IsOn => spotLight.enabled;
    }
}
