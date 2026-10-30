using UnityEngine;

namespace EstanDentro.Interaction
{
    /// <summary>
    /// Mensaje oculto que solo se ve cuando una linterna lo ilumina.
    /// Renderiza un TextMesh 3D en la posicion del GameObject. El alpha del color
    /// hace fade-in cuando el cono de luz de la Flashlight de la escena cae sobre
    /// este punto, y fade-out cuando deja de iluminarlo.
    ///
    /// Setup en Unity:
    ///   1. Crear empty GameObject en la escena
    ///   2. Posicionarlo sobre una pared
    ///   3. Rotarlo para que el texto mire hacia el cuarto (Y forward)
    ///   4. Add Component → HiddenMessage
    ///   5. Editar el campo "Message Text" con el contenido
    ///
    /// El script crea automaticamente un TextMesh si no existe.
    /// </summary>
    public class HiddenMessage : MonoBehaviour
    {
        [Header("Mensaje")]
        [SerializeField, TextArea(2, 5)]
        private string messageText = "Subi, hijo. Todavia estas abajo.";
        [SerializeField] private Color messageColor = new Color(0.85f, 0.7f, 0.28f, 1f);
        [SerializeField] private int fontSize = 60;
        [SerializeField, Tooltip("Tamano del caracter en mundo. 0.05 es un texto pequeno-mediano.")]
        private float characterSize = 0.05f;

        [Header("Fade")]
        [SerializeField, Tooltip("Velocidad del fade en alpha-units por segundo.")]
        private float fadeSpeed = 1.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Alpha maximo cuando esta totalmente revelado.")]
        private float maxAlpha = 0.95f;

        [Header("Linterna")]
        [SerializeField, Tooltip("Override: Light especifica a usar como fuente. Si esta vacio, busca el Flashlight de la escena automaticamente.")]
        private Light targetLight;
        [SerializeField, Tooltip("Distancia maxima a la que la luz revela el mensaje. Si <= 0 usa el range de la Light.")]
        private float maxDistance = 0f;

        private TextMesh textMesh;
        private float currentAlpha;
        private Light cachedFlashlight;

        private void Awake()
        {
            EnsureTextMesh();
            ApplyAlpha(0f);
        }

        // letra a mano (imagen de 02_Arte3D_Blender/scripts_blender/textos_pared.py) en un plano: el TextMesh viejo salía
        // comprimido y raro en HDRP. Si no hay imagen para el texto, se usa el TextMesh como antes.
        private Material matTexto;
        [SerializeField, Tooltip("Ancho del texto en la pared (m)")] private float anchoMetros = 1.1f;

        private bool CrearPlano()
        {
            var mat = Resources.Load<Material>("Textos/Mat_" + EstanDentro.Audio.Voz.Clave(messageText));
            if (!mat) return false;
            var viejo = GetComponent<MeshRenderer>(); if (viejo) viejo.enabled = false;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Texto";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform, false);
            var tex = mat.GetTexture("_BaseColorMap");
            float alto = anchoMetros * (tex ? (float)tex.height / tex.width : 0.25f);
            Vector3 ls = transform.lossyScale;                                   // tamaño real en el mundo, sin importar la escala del padre
            q.transform.localScale = new Vector3(anchoMetros / Mathf.Max(1e-4f, Mathf.Abs(ls.x)), alto / Mathf.Max(1e-4f, Mathf.Abs(ls.y)), 1f);
            matTexto = new Material(mat);
            q.GetComponent<MeshRenderer>().sharedMaterial = matTexto;
            q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return true;
        }

        private void EnsureTextMesh()
        {
            if (CrearPlano()) return;
            textMesh = GetComponent<TextMesh>();
            if (textMesh == null) textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = messageText;
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(messageColor.r, messageColor.g, messageColor.b, 0f);
        }

        private void Update()
        {
            // Refrescar texto si se cambio en Inspector durante runtime
            if (textMesh != null && matTexto == null && textMesh.text != messageText) textMesh.text = messageText;

            if (cachedFlashlight == null) cachedFlashlight = ResolveFlashlightLight();

            bool illuminated = IsIlluminated();
            float target = illuminated ? maxAlpha : 0f;
            currentAlpha = Mathf.MoveTowards(currentAlpha, target, fadeSpeed * Time.deltaTime);
            ApplyAlpha(currentAlpha);
        }

        private void ApplyAlpha(float a)
        {
            if (matTexto != null) { var c = matTexto.GetColor("_BaseColor"); c.a = a; matTexto.SetColor("_BaseColor", c); return; }
            if (textMesh == null) return;
            textMesh.color = new Color(messageColor.r, messageColor.g, messageColor.b, a);
        }

        private Light ResolveFlashlightLight()
        {
            if (targetLight != null) return targetLight;
            // Buscar la Flashlight de la escena y obtener su Light component (mismo GO).
            var f = FindFirstObjectByType<Flashlight>();
            if (f == null) return null;
            return f.GetComponent<Light>();
        }

        private bool IsIlluminated()
        {
            if (cachedFlashlight == null) return false;
            if (!cachedFlashlight.enabled) return false;

            Vector3 toMe = transform.position - cachedFlashlight.transform.position;
            float dist = toMe.magnitude;
            float maxRange = maxDistance > 0f ? maxDistance : cachedFlashlight.range;
            if (dist > maxRange) return false;

            // Solo cuenta si el mensaje esta dentro del cono de la spotlight.
            float angle = Vector3.Angle(cachedFlashlight.transform.forward, toMe.normalized);
            if (angle > cachedFlashlight.spotAngle * 0.5f) return false;

            return true;
        }

        // Dibuja el cono visualmente en Scene view para ayudar a posicionar.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(messageColor.r, messageColor.g, messageColor.b, 0.6f);
            Gizmos.DrawWireSphere(transform.position, 0.15f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.5f);
        }
    }
}
