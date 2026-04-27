using UnityEngine;
using UnityEngine.InputSystem;

namespace EstanDentro.Interaction
{
    [RequireComponent(typeof(Light))]
    public class Flashlight : MonoBehaviour
    {
        [Header("Estado")]
        [SerializeField] private bool startEnabled = false;
        [SerializeField, Tooltip("Si false, la tecla F / Square no hace nada. Util para bloquearla hasta el apagon.")]
        private bool unlocked = true;

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

        private void Awake()
        {
            spotLight = GetComponent<Light>();
            if (applyDefaultLook) ApplyLook();
            spotLight.enabled = startEnabled;
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
            if (!unlocked) return;
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool toggle = (kb != null && kb.fKey.wasPressedThisFrame)
                       || (gp != null && gp.buttonWest.wasPressedThisFrame);
            if (toggle) Toggle();
        }

        public void Toggle() => spotLight.enabled = !spotLight.enabled;
        public void SetEnabled(bool e) => spotLight.enabled = e;
        public void Unlock() => unlocked = true;
        public void Lock() => unlocked = false;
        public bool IsUnlocked => unlocked;
        public bool IsOn => spotLight.enabled;
    }
}
