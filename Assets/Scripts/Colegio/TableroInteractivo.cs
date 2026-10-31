using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Mirar el tablero de cerca (Henry: "el tablero no puedo entrar en él, por lo menos debería poder aunque no tenga tiza").
    /// La cámara se acerca, Mateo lee lo escrito (la fecha y la tarea: la pista del 142) y con E o Esc vuelve.
    /// </summary>
    public class TableroInteractivo : Interactable
    {
        public Vector3 puntoCamara;
        public Vector3 mirarA;
        [TextArea] public string lectura = "17 de mayo… Tarea: ejercicios de la página 142.";
        [Tooltip("Lo que lee Mateo si mira el tablero en la Pesadilla (vacío = lo mismo que en normal).")]
        [TextArea] public string lecturaPesadilla = "";
        bool mirando, salir;
        public static bool Mirando { get; private set; }     // la pausa no se abre con el mismo Esc que sale del tablero
        public void Salir() => salir = true;                // (la prueba automática no puede apretar teclas)

        public override string Accion => "Mirar el tablero";

        public override void Interact()
        {
            if (mirando) return;
            var cam = Camera.main; var j = FindAnyObjectByType<Player.PlayerController>();
            if (!cam || !j) return;
            StartCoroutine(Mirar(cam, j));
        }

        IEnumerator Mirar(Camera cam, Player.PlayerController j)
        {
            mirando = true;
            j.InputEnabled = false; Mirando = true;            // (sin OverlayBlocker: taparía el pensamiento de Mateo)
            var t = cam.transform; Vector3 p0 = t.position; Quaternion r0 = t.rotation; float fov0 = cam.fieldOfView;
            Quaternion r1 = Quaternion.LookRotation(mirarA - puntoCamara);
            for (float k = 0f; k < 0.6f; k += Time.deltaTime)
            {
                float a = Mathf.SmoothStep(0f, 1f, k / 0.6f);
                t.SetPositionAndRotation(Vector3.Lerp(p0, puntoCamara, a), Quaternion.Slerp(r0, r1, a)); cam.fieldOfView = Mathf.Lerp(fov0, 50f, a);
                yield return null;
            }
            t.SetPositionAndRotation(puntoCamara, r1);
            // en la Pesadilla el borrón de la esquina dice algo (la clave del aula de música)
            bool pesadilla = Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal && !string.IsNullOrEmpty(lecturaPesadilla);
            string texto = pesadilla ? lecturaPesadilla : lectura;
            ObjectiveHUD.Show(texto + "\n<size=80%>[E] o [Esc] volver</size>", 60f);
            var clip = EstanDentro.Audio.Voz.Clip("Colegio", texto);
            if (clip) EstanDentro.Audio.Voz.Sonar(clip, t.position, EstanDentro.Audio.Voz.Lugar.Pensamiento);
            yield return null;
            while (true)
            {
                var kb = Keyboard.current; var gp = Gamepad.current;
                if (salir || (kb != null && (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) || (gp != null && (gp.buttonEast.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame))) break;
                yield return null;
            }
            salir = false;
            ObjectiveHUD.Show("", 0.01f);
            for (float k = 0f; k < 0.4f; k += Time.deltaTime)
            {
                float a = Mathf.SmoothStep(0f, 1f, k / 0.4f);
                t.SetPositionAndRotation(Vector3.Lerp(puntoCamara, p0, a), Quaternion.Slerp(r1, r0, a)); cam.fieldOfView = Mathf.Lerp(50f, fov0, a);
                yield return null;
            }
            t.localPosition = t.parent ? t.parent.InverseTransformPoint(p0) : p0; t.rotation = r0; cam.fieldOfView = fov0;
            j.InputEnabled = true;
            yield return null; Mirando = false;
            mirando = false;
        }
    }
}
