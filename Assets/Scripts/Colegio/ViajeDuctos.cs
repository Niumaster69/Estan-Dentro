using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Ir y volver por los ductos SIN recargar el colegio (Henry 20-oct: 4°B → rectoría → 4°B). Antes el colegio se cargaba de
    /// nuevo al salir del ducto y todo volvía a como estaba al principio (candados cerrados, objetos en su sitio).
    /// Ahora: el colegio "se duerme" (sus objetos se apagan), los ductos se cargan encima y, al salir, se descargan y el
    /// colegio despierta tal cual lo dejaste; Mateo aparece en el punto de salida pedido (PlayerSpawnPoint con ese id).
    /// </summary>
    public class ViajeDuctos : MonoBehaviour
    {
        public const string ESCENA_DUCTOS = "EsecenaUno-DuctosDeVentilacion";
        static ViajeDuctos inst;
        public static bool Activo { get; private set; }
        public static string Destino { get; private set; } = "";
        Scene colegio; readonly List<GameObject> dormidos = new List<GameObject>();
        Image negro;

        static ViajeDuctos Inst
        {
            get
            {
                if (inst) return inst;
                var go = new GameObject("ViajeDuctos"); DontDestroyOnLoad(go);
                inst = go.AddComponent<ViajeDuctos>(); inst.Construir();
                return inst;
            }
        }

        /// <summary>Entrar al ducto; al salir, Mateo aparece en el spawn 'destino' del colegio.</summary>
        public static void Entrar(string destino) { if (!Activo) Inst.StartCoroutine(Inst.EntrarCo(destino)); }

        /// <summary>La salida del ducto (ExitDuctsTrigger) llama aquí cuando el viaje empezó en el colegio.</summary>
        public static void Salir() { if (Activo) Inst.StartCoroutine(Inst.SalirCo()); }

        IEnumerator EntrarCo(string destino)
        {
            Activo = true; Destino = destino;
            yield return Fundido(0f, 1f, 0.6f);
            colegio = SceneManager.GetActiveScene();
            dormidos.Clear();
            foreach (var g in colegio.GetRootGameObjects()) if (g.activeSelf) { g.SetActive(false); dormidos.Add(g); }
            var op = SceneManager.LoadSceneAsync(ESCENA_DUCTOS, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(ESCENA_DUCTOS));
            yield return new WaitForSecondsRealtime(0.3f);
            var jd = FindAnyObjectByType<Player.PlayerController>();
            if (jd) { inicioDucto = jd.transform.position; rumboDucto = jd.transform.eulerAngles.y; }
            var st = Stress.StressSystem.Instance;
            if (st) { st.OnCollapse -= Desmayo; st.OnCollapse += Desmayo; }
            yield return Fundido(1f, 0f, 0.8f);
        }

        Vector3 inicioDucto; float rumboDucto; bool desmayado;

        // en el ducto la mente también se rompe: estrés alto → Pesadilla, casi al tope → Alterado, ya calmado → Normal
        void Update()
        {
            if (!Activo || desmayado || Mente.EstadoMente.EnTransicion) return;
            var st = Stress.StressSystem.Instance; if (!st || st.IsCollapsed) return;
            var e = Mente.EstadoMente.Actual; float n = st.Normalized;
            if (n >= 0.92f && e != Mente.EstadoMente.Estado.Alterado) Mente.EstadoMente.Ir(Mente.EstadoMente.Estado.Alterado);
            else if (n >= 0.75f && n < 0.9f && e == Mente.EstadoMente.Estado.Normal) Mente.EstadoMente.Ir(Mente.EstadoMente.Estado.Pesadilla);
            else if (n < 0.85f && e == Mente.EstadoMente.Estado.Alterado) Mente.EstadoMente.Ir(Mente.EstadoMente.Estado.Pesadilla);
            else if (n < 0.45f && e != Mente.EstadoMente.Estado.Normal) Mente.EstadoMente.Ir(Mente.EstadoMente.Estado.Normal);
        }

        /// <summary>Colapso dentro del ducto: Mateo se desmaya y despierta al inicio del ducto (sin perder lo hecho en el colegio).</summary>
        void Desmayo()
        {
            if (!Activo || desmayado) return;
            StartCoroutine(DesmayoCo());
        }

        IEnumerator DesmayoCo()
        {
            desmayado = true;
            var j = FindAnyObjectByType<Player.PlayerController>();
            if (j) j.InputEnabled = false;
            yield return Fundido(negro.color.a, 1f, 1.2f);
            Time.timeScale = 1f;
            Stress.StressSystem.Instance?.ResetTo(Stress.StressSystem.Instance.MaxStress * 0.3f);
            Mente.EstadoMente.Ir(Mente.EstadoMente.Estado.Normal);
            if (j)
            {
                j.Teletransportar(inicioDucto);
                j.transform.rotation = Quaternion.Euler(0f, rumboDucto, 0f);
                j.SetPitch(0f);
            }
            yield return new WaitForSecondsRealtime(1.5f);
            if (j) j.InputEnabled = true;
            yield return Fundido(1f, 0f, 1.2f);
            EstanDentro.UI.ObjectiveHUD.Notify("…Me desmayé. Respira, Mateo. Despacio.", 3f);
            desmayado = false;
        }

        IEnumerator SalirCo()
        {
            Time.timeScale = 1f;
            if (Stress.StressSystem.Instance) Stress.StressSystem.Instance.OnCollapse -= Desmayo;
            if (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal) Mente.EstadoMente.Ir(Mente.EstadoMente.Estado.Normal);
            yield return Fundido(negro.color.a, 1f, 0.5f);
            var op = SceneManager.UnloadSceneAsync(ESCENA_DUCTOS);
            while (op != null && !op.isDone) yield return null;
            if (colegio.IsValid()) SceneManager.SetActiveScene(colegio);
            foreach (var g in dormidos) if (g) g.SetActive(true);
            dormidos.Clear();
            // Mateo en el punto de salida
            var j = FindAnyObjectByType<Player.PlayerController>();
            foreach (var sp in FindObjectsByType<Player.PlayerSpawnPoint>(FindObjectsSortMode.None))
                if (sp.Id == Destino && j)
                {
                    j.Teletransportar(sp.transform.position);
                    j.transform.rotation = Quaternion.Euler(0f, sp.transform.eulerAngles.y, 0f);
                    j.SetPitch(0f);
                    break;
                }
            if (j) j.InputEnabled = true;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            yield return new WaitForSecondsRealtime(0.3f);
            Activo = false;
            yield return Fundido(1f, 0f, 1.0f);
            // lo que piensa Mateo al llegar
            if (Destino == "rectoria_dentro") EstanDentro.UI.ObjectiveHUD.Notify("…¿La rectoría? Me caí del techo.", 3f);
            else if (Destino == "4b_desde_ducto") EstanDentro.UI.ObjectiveHUD.Notify("Otra vez en el 4°B… Ahora tengo la llave.", 3f);
        }

        IEnumerator Fundido(float de, float a, float seg)
        {
            for (float t = 0f; t < seg; t += Time.unscaledDeltaTime) { negro.color = new Color(0, 0, 0, Mathf.Lerp(de, a, t / seg)); yield return null; }
            negro.color = new Color(0, 0, 0, a);
        }

        void Construir()
        {
            var cv = gameObject.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 300;
            negro = new GameObject("Negro", typeof(RectTransform)).AddComponent<Image>(); negro.transform.SetParent(transform, false);
            var r = (RectTransform)negro.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            negro.color = new Color(0, 0, 0, 0); negro.raycastTarget = false;
        }
    }
}
