using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// El aula de música: un recuerdo bonito con Pa y el final del capítulo 1 (Henry 20-oct).
    /// Al entrar, la sala se entibia, suena la cumbia de la radio del prólogo y Pa canta (mal) como el Día de la familia;
    /// Mateo lo recuerda. Luego todo se pone blanco y sale el cierre del capítulo; vuelve al menú.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RecuerdoAula : MonoBehaviour
    {
        public Vector3 puntoPa;                 // donde "estaba" Pa (frente al tablero del aula)
        bool hecho;
        Image blanco; Text cierre;

        void Awake() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnTriggerEnter(Collider c)
        {
            if (hecho || !c.GetComponentInParent<Player.PlayerController>()) return;
            hecho = true;
            StartCoroutine(Recordar(c.GetComponentInParent<Player.PlayerController>()));
        }

        IEnumerator Recordar(Player.PlayerController j)
        {
            // si llega huyendo de la Pesadilla, el aula la deshace: el recuerdo bonito empieza cuando todo vuelve
            if (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal)
            {
                Mente.ControlMente.Resolver("aula_musica", "…Aquí no entra. Aquí no.");
                Mente.ControlMente.Soltar();
                if (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal && !Mente.EstadoMente.EnTransicion) Mente.EstadoMente.Ir(Mente.EstadoMente.Estado.Normal);
                float t0 = 0f;
                while ((Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal || Mente.EstadoMente.EnTransicion) && t0 < 8f) { t0 += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(1f);
            }
            Stress.StressSystem.Instance?.ResetTo(0f);
            ObjectiveHUD.Notify("…Esta es el aula de música.", 3f);
            // la sala se entibia: una luz cálida donde estaba Pa
            var luzGo = new GameObject("Luz_Recuerdo"); luzGo.transform.position = puntoPa + Vector3.up * 1.6f;
            var luz = luzGo.AddComponent<Light>(); luz.type = LightType.Point; luz.color = new Color(1f, 0.78f, 0.5f); luz.range = 9f;
            luz.lightUnit = UnityEngine.Rendering.LightUnit.Lumen; luz.intensity = 0f;
            var musica = Sonar(Resources.Load<AudioClip>("Prologo/radio_cancion"), puntoPa, 0f, true);
            if (musica) musica.time = 97.4f;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                luz.intensity = Mathf.Lerp(0f, 2600f, t / 4f);
                if (musica) musica.volume = Mathf.Lerp(0f, 0.18f, t / 4f);
                yield return null;
            }
            ObjectiveHUD.Notify("El Día de la familia. Pa se subió aquí, con una guitarra prestada…", 4.5f);
            yield return new WaitForSeconds(2.5f);
            var canto = Resources.Load<AudioClip>("Voces/Prologo/pa_canta_trompeta");
            if (canto) EstanDentro.Audio.Voz.Sonar(canto, puntoPa, EstanDentro.Audio.Voz.Lugar.Recuerdo, 0.9f, agachaMusica: false);
            yield return new WaitForSeconds(canto ? canto.length + 0.5f : 4f);
            ObjectiveHUD.Notify("…y cantó delante de todo el curso. Horrible.", 3.5f);
            yield return new WaitForSeconds(3.2f);
            var frase = Resources.Load<AudioClip>("Voces/Prologo/horrible_pero_con_sentimiento");
            if (frase) EstanDentro.Audio.Voz.Sonar(frase, puntoPa, EstanDentro.Audio.Voz.Lugar.Recuerdo);
            ObjectiveHUD.Notify("«Horrible, pero con sentimiento».", 3f);
            yield return new WaitForSeconds(frase ? frase.length + 1f : 3f);
            ObjectiveHUD.Notify("Yo me quería esconder debajo del pupitre… y al final canté con él.", 4.5f);
            yield return new WaitForSeconds(4.8f);
            ObjectiveHUD.Notify("…Pa. Quiero volver.", 3.5f);
            yield return new WaitForSeconds(2.5f);
            // todo se pone blanco: fin del capítulo
            if (j) j.InputEnabled = false;
            Construir();
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                blanco.color = new Color(1f, 0.97f, 0.9f, t / 3f);
                if (musica) musica.volume = Mathf.Lerp(0.18f, 0.3f, t / 3f);
                yield return null;
            }
            cierre.text = "ESTÁN DENTRO\n\n<size=40>Fin del capítulo 1</size>\n\n<size=30>Respira. Alguien te está esperando.</size>";
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { cierre.color = new Color(0.15f, 0.12f, 0.1f, t / 1.5f); yield return null; }
            yield return new WaitForSeconds(6f);
            for (float t = 0f; t < 2f; t += Time.deltaTime) { if (musica) musica.volume = Mathf.Lerp(0.3f, 0f, t / 2f); yield return null; }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            EstanDentro.UI.Pausa.Reiniciar();
            SceneManager.LoadScene("MainMenu");
        }

        static AudioSource Sonar(AudioClip clip, Vector3 pos, float vol, bool bucle)
        {
            if (!clip) return null;
            var g = new GameObject("Musica_Recuerdo"); g.transform.position = pos;
            var a = g.AddComponent<AudioSource>(); a.clip = clip; a.volume = vol; a.loop = bucle; a.spatialBlend = 0.6f; a.maxDistance = 14f;
            var lp = g.AddComponent<AudioLowPassFilter>(); lp.cutoffFrequency = 3200f;                       // como si sonara lejos, en la memoria
            g.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.Room;
            a.Play();
            return a;
        }

        void Construir()
        {
            var go = new GameObject("Cierre_Capitulo1");
            var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 400;
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            blanco = new GameObject("Blanco", typeof(RectTransform)).AddComponent<Image>(); blanco.transform.SetParent(go.transform, false);
            var r = (RectTransform)blanco.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            blanco.color = new Color(1, 1, 1, 0);
            cierre = new GameObject("Texto", typeof(RectTransform)).AddComponent<Text>(); cierre.transform.SetParent(go.transform, false);
            cierre.font = Fuentes.Titulo ? Fuentes.Titulo : Fuentes.Maquina; cierre.fontSize = 96; cierre.alignment = TextAnchor.MiddleCenter;
            cierre.supportRichText = true; cierre.color = new Color(0, 0, 0, 0);
            var rt = (RectTransform)cierre.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
