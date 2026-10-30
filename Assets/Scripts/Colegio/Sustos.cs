using System.Collections;
using System.Linq;
using UnityEngine;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Sustos de los casilleros. Regla del juego: todo evento tiene ENTRADA (aviso), DURANTE (el susto) y SALIDA (calma).
    ///   portazo: crujido → la puerta se cierra sola de golpe, la cámara tiembla → respiración.
    ///   cae:     golpe seco y algo rueda por el piso, jadeo → respiración.
    ///   celular: latido → un celular vibra adentro: «Pa: ¿Dónde estás, mijo?» → se apaga, silencio.
    ///   susurro: entrada sorda → un susurro detrás de ti y las luces cercanas parpadean → vuelven, respiración.
    /// </summary>
    public static class Sustos
    {
        public static float UltimoSusto { get; private set; } = -999f;

        public static IEnumerator Ejecutar(string tipo, Casillero cas)
        {
            UltimoSusto = Time.time;
            var pos = cas.transform.position + Vector3.up * 1.2f;
            switch (tipo)
            {
                case "portazo":
                    Sonar("crujido", pos, 0.8f);                                   // entrada
                    yield return new WaitForSeconds(0.9f);
                    Casillero.CerrarPuertas(cas.gameObject);                        // durante
                    Juego("casillero/portazo", pos, 1f); Sonar("jadeo", Camera.main.transform.position, 0.7f);
                    Estres(14f); yield return Temblar(0.35f, 0.035f);
                    yield return new WaitForSeconds(1.2f);
                    Sonar("respiro", Camera.main.transform.position, 0.5f);         // salida
                    ObjectiveHUD.Notify("…Fue el viento. Fue el viento.", 2.5f);
                    break;

                case "cae":
                    Sonar("entrada", pos, 0.5f);
                    yield return new WaitForSeconds(0.25f);
                    Rodar(cas);                                                     // algo cae y rueda
                    Juego("casillero/lata_rueda", pos, 0.9f); Sonar("jadeo", Camera.main.transform.position, 0.6f);
                    Estres(10f); yield return Temblar(0.25f, 0.02f);
                    yield return new WaitForSeconds(1.4f);
                    Sonar("respiro", Camera.main.transform.position, 0.45f);
                    break;

                case "celular":
                    var latido = Sonar("latido", Camera.main.transform.position, 0.8f, bucle: true);
                    yield return new WaitForSeconds(0.8f);
                    ObjectiveHUD.Notify("Algo vibra adentro. Un celular con la pantalla rota…", 3f);
                    Juego("sustos/celular_vibra", pos, 0.8f);
                    for (int i = 0; i < 3; i++) { yield return Temblar(0.18f, 0.006f); yield return new WaitForSeconds(0.9f); }
                    ObjectiveHUD.Notify("«Pa: ¿Dónde estás, mijo?»", 4f);
                    Estres(8f);
                    yield return new WaitForSeconds(3.2f);
                    if (latido) Object.Destroy(latido.gameObject);                    // salida: silencio
                    ObjectiveHUD.Notify("La pantalla se apaga. No vuelve a prender.", 2.5f);
                    break;

                default: // "susurro"
                    Sonar("entrada", pos, 0.45f);
                    yield return new WaitForSeconds(0.6f);
                    Juego("sustos/susurro", Camera.main.transform.position - Camera.main.transform.forward * 1.2f, 0.7f, 1f);   // detrás de ti
                    var luces = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                                      .Where(l => l.enabled && l.type != LightType.Directional && Vector3.Distance(l.transform.position, pos) < 9f
                                                  && l.GetComponentInParent<Interaction.Flashlight>() == null).ToArray();
                    Estres(12f);
                    for (int i = 0; i < 9; i++)
                    {
                        foreach (var l in luces) if (l) l.enabled = i % 2 == 1;
                        yield return new WaitForSeconds(Random.Range(0.06f, 0.18f));
                    }
                    foreach (var l in luces) if (l) l.enabled = true;
                    yield return new WaitForSeconds(0.8f);
                    Sonar("respiro", Camera.main.transform.position, 0.5f);
                    break;
            }
        }

        static void Juego(string n, Vector3 donde, float vol, float espacial = 0.8f) => EstanDentro.Audio.SonidoCorto.Juego(n, donde, vol, espacial);

        static void Estres(float x) => Stress.StressSystem.Instance?.Add(x);

        // los clips de Resources/Sonidos son largos (stinger 28 s, latido 3 min, grito 8 s): se cortan con fundido
        static readonly System.Collections.Generic.Dictionary<string, float> maximo = new System.Collections.Generic.Dictionary<string, float>
        { ["entrada"] = 3f, ["crujido"] = 2.5f, ["susurro"] = 4f, ["jadeo"] = 3f, ["respiro"] = 4.5f };

        static AudioSource Sonar(string nombre, Vector3 donde, float vol, bool bucle = false)
        {
            var clip = Resources.Load<AudioClip>("Sonidos/" + nombre);
            float max = bucle ? 999f : (maximo.TryGetValue(nombre, out var m) ? m : 999f);
            return EstanDentro.Audio.SonidoCorto.Play(clip, donde, vol, max, 0.7f, bucle);
        }

        static IEnumerator Temblar(float dur, float fuerza)
        {
            var cam = Camera.main ? Camera.main.transform : null;
            if (!cam) yield break;
            Vector3 p0 = cam.localPosition;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                cam.localPosition = p0 + Random.insideUnitSphere * fuerza * (1f - t / dur);
                yield return null;
            }
            cam.localPosition = p0;
        }

        // una lata/pelota que cae del casillero y rueda hacia el jugador
        static void Rodar(Casillero cas)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = "LataQueRueda";
            g.transform.localScale = new Vector3(0.07f, 0.06f, 0.07f);
            var cam = Camera.main.transform;
            Vector3 hacia = cam.position - cas.transform.position; hacia.y = 0f; hacia.Normalize();
            // nace DENTRO, en la repisa de arriba junto a la puerta, parada; se vuelca y cae (antes aparecía en el aire y saltaba)
            var b = cas.GetComponent<Renderer>().bounds;
            Vector3 eje = Mathf.Abs(hacia.x) > Mathf.Abs(hacia.z) ? new Vector3(Mathf.Sign(hacia.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(hacia.z));
            float fondo = Mathf.Abs(eje.x) > 0f ? b.extents.x : b.extents.z;
            g.transform.position = new Vector3(b.center.x, b.max.y - 0.32f, b.center.z) + eje * (fondo - 0.08f);
            g.transform.rotation = Quaternion.identity;
            hacia = eje;
            var mr = g.GetComponent<Renderer>();
            var baseM = cas.GetComponent<Renderer>().sharedMaterial;
            if (baseM) { var m = new Material(baseM); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.6f, 0.08f, 0.06f)); mr.sharedMaterial = m; }
            Object.Destroy(g.GetComponent<Collider>());
            g.AddComponent<CapsuleCollider>();
            var rb = g.AddComponent<Rigidbody>(); rb.mass = 0.3f;
            rb.AddForce(hacia * 0.18f, ForceMode.Impulse);                               // un empujoncito: se vuelca hacia afuera
            rb.AddTorque(Vector3.Cross(Vector3.up, hacia) * 0.05f, ForceMode.Impulse);
        }
    }
}
