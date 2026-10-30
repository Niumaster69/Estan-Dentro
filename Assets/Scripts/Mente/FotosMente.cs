using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EstanDentro.Mente
{
    /// <summary>Prueba de las transiciones: con  -fotosmente "carpeta"  abre el colegio y fotografía cada paso.</summary>
    public class FotosMente : MonoBehaviour
    {
        string carpeta; int n;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Arrancar()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-fotosmente");
            if (i < 0 || i + 1 >= args.Length) return;
            var go = new GameObject("_FotosMente"); DontDestroyOnLoad(go);
            var f = go.AddComponent<FotosMente>(); f.carpeta = args[i + 1]; Directory.CreateDirectory(f.carpeta);
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            if (SceneManager.GetActiveScene().name != "Colegio") SceneManager.LoadScene("Colegio");
            while (SceneManager.GetActiveScene().name != "Colegio") yield return null;
            yield return new WaitForSecondsRealtime(6f);
            Stress.StressSystem.Instance?.ResetTo(0f);
            var aguante = FindAnyObjectByType<Stress.Aguante>(); if (aguante) aguante.enabled = false;   // que la oscuridad no meta su viñeta
            var cam = Camera.main.transform;
            cam.parent.rotation = Quaternion.Euler(0f, cam.parent.eulerAngles.y + 150f, 0f);              // mirar hacia el salón
            var lin = FindAnyObjectByType<Interaction.Flashlight>(); if (lin) { lin.Unlock(); lin.SetEnabled(true); }
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Foto("0_normal");
            ControlMente.Recuerdo(null, "_prueba");
            foreach (var t in new[] { 0.5f, 1.0f, 1.3f, 1.55f, 1.75f }) { yield return Esperar(t); yield return Foto($"1_entrando_{t:0.00}s"); }
            while (EstadoMente.EnTransicion) yield return null;
            yield return new WaitForSecondsRealtime(1f); yield return Foto("2_pesadilla");
            var ob = FindAnyObjectByType<Observador>();
            if (ob)
            {
                ob.enabled = false;
                foreach (var r in ob.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
                foreach (var dist in new[] { 2.6f, 4.5f, 7f })
                {
                    Vector3 f = cam.forward; f.y = 0f; f.Normalize();
                    Vector3 pie = cam.position + f * dist; pie.y = FindAnyObjectByType<Player.PlayerController>().transform.position.y;
                    ob.transform.position = pie; ob.transform.rotation = Quaternion.LookRotation(-f);
                    yield return new WaitForSecondsRealtime(0.6f); yield return Foto($"2b_observador_{dist:0.0}m");
                }
                foreach (var r in ob.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                yield return new WaitForSecondsRealtime(0.6f); yield return Foto("2c_sin_observador"); yield return new WaitForSecondsRealtime(2f); yield return Foto("2d_dos_segundos_despues");
            }
            EstadoMente.Ir(EstadoMente.Estado.Alterado);
            yield return new WaitForSecondsRealtime(0.08f); yield return Foto("3_alterado_golpe");
            while (EstadoMente.EnTransicion) yield return null;
            yield return new WaitForSecondsRealtime(1f); yield return Foto("4_alterado");
            ControlMente.Soltar(); EstadoMente.Ir(EstadoMente.Estado.Normal);
            yield return new WaitForSecondsRealtime(0.7f); yield return Foto("5_volviendo");
            while (EstadoMente.EnTransicion) yield return null;
            yield return new WaitForSecondsRealtime(1.5f); yield return Foto("6_normal_otra_vez");
            Application.Quit();
        }

        float t0;
        IEnumerator Esperar(float hasta)
        {
            if (hasta <= 0.5f) t0 = Time.unscaledTime;
            while (Time.unscaledTime - t0 < hasta) yield return null;
        }

        IEnumerator Foto(string nombre)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, $"{++n:00}_{nombre}.png"));
            yield return null;
        }
    }
}
