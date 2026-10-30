using UnityEngine;
using UnityEngine.UI;

namespace EstanDentro.Stress
{
    /// <summary>
    /// El estrés como mecánica (Henry 20-oct: "tener el estrés alto es una mecánica… si se mantiene mucho tiempo con estrés
    /// la vida empieza a bajar").
    ///  - Borde ROJO de la pantalla según el estrés; late con el corazón cuando está alto.
    ///  - AGUANTE (la "vida", sin barra): con el estrés sobre 80 % baja; bajo 50 % se recupera. Se nota porque la vista
    ///    se va cerrando en negro. Si llega a 0, Mateo colapsa.
    ///  - OSCURIDAD: con la linterna (ya recogida) apagada, a los 2 s el estrés empieza a subir.
    /// Respirar hondo (Q) baja el estrés.
    /// </summary>
    public class Aguante : MonoBehaviour
    {
        static Aguante inst;
        public static float Valor => inst ? inst.aguante : 1f;
        float aguante = 1f, oscuro;
        Image rojo, negro;
        Interaction.Flashlight linterna; float buscar;

        public float bajaPorSegundo = 1f / 45f;     // ~45 s con el estrés al tope hasta colapsar
        public float subePorSegundo = 1f / 30f;
        public float miedoOscuridad = 1.2f;         // estrés por segundo a oscuras (hasta 70 %: la oscuridad sola no mata)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (inst) return;
            var go = new GameObject("Aguante"); DontDestroyOnLoad(go);
            inst = go.AddComponent<Aguante>(); inst.Construir();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => { if (inst) inst.aguante = 1f; };
        }

        void Update()
        {
            var st = StressSystem.Instance;
            bool hay = st != null && !st.IsCollapsed;
            float e = hay ? st.Normalized : 0f;
            float dt = Time.deltaTime;
            if (hay && dt > 0f)
            {
                if (e >= 0.8f) aguante -= bajaPorSegundo * dt * Mathf.Lerp(0.6f, 1.4f, (e - 0.8f) / 0.2f);
                else if (e < 0.5f) aguante += subePorSegundo * dt;
                aguante = Mathf.Clamp01(aguante);
                if (aguante <= 0f) { st.Add(1000f); aguante = 1f; }                       // colapso -> pantalla de game over
                // oscuridad: linterna recogida y apagada
                if (!linterna && (buscar -= Time.unscaledDeltaTime) <= 0f) { buscar = 1f; linterna = FindAnyObjectByType<Interaction.Flashlight>(); }
                bool aOscuras = linterna && linterna.Desbloqueada && !linterna.Encendida;
                oscuro = aOscuras ? oscuro + dt : 0f;
                if (oscuro > 2f && e < 0.7f) st.AddSilencioso(miedoOscuridad * dt);
            }
            // pantalla: rojo por estrés (late), negro por aguante perdido
            float latido = 1f + (e > 0.7f ? 0.25f * Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.Lerp(2.5f, 5f, e))), 6f) : 0f);
            float aRojo = Mathf.SmoothStep(0f, 0.75f, Mathf.InverseLerp(0.35f, 1f, e)) * latido;
            rojo.color = new Color(0.55f, 0.03f, 0.02f, hay ? aRojo : 0f);
            negro.color = new Color(0f, 0f, 0f, hay ? (1f - aguante) * 0.9f : 0f);
            float esc = Mathf.Lerp(1.25f, 0.8f, 1f - aguante);                                  // el túnel se cierra
            negro.rectTransform.localScale = new Vector3(esc, esc, 1f);
        }

        void Construir()
        {
            var cv = gameObject.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 70;
            var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            rojo = Borde("Rojo", 0.55f); negro = Borde("Negro", 0.35f);
        }

        Image Borde(string n, float interior)
        {
            const int N = 128;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(interior, 1.25f, d))));
            }
            t.Apply();
            var i = new GameObject(n, typeof(RectTransform)).AddComponent<Image>(); i.transform.SetParent(transform, false);
            i.sprite = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f)); i.raycastTarget = false;
            var r = (RectTransform)i.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            i.color = Color.clear;
            return i;
        }
    }
}
