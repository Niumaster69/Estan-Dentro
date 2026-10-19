using System.Linq;
using UnityEditor;
using UnityEngine;
using EstanDentro.Colegio;
using EstanDentro.Interaction;
using EstanDentro.Stress;

namespace EstanDentro.EditorColegio
{
    /// <summary>
    /// TODOS los textos del Capítulo 1 en un solo lugar (narrativa unificada, 01_Documentacion/NARRATIVA_UNIFICADA.md §4).
    /// Historia: coma + Pa vivo. Se aplica al final de PortarCapitulo1.Portar(); para cambiar un texto, cambiarlo aquí
    /// y volver a armar la escena (menú Estan Dentro / 3).
    /// </summary>
    public static class TextosCapitulo1
    {
        // ---------- notas ----------
        public const string CuadernoTitulo = "Cuaderno de Mateo";
        public const string Cuaderno =
            "17 / 05 / 2023\n\nTarea: ejercicios pág. 142.\nExamen el viernes.\n\n" +
            "(en la esquina, con otra letra:)\n¡Feliz cumple, Mendoza!";

        public const string CartaPaTitulo = "Una carta de Pa";
        public const string CartaPa =
            "Mateo:\n\nSi estás leyendo esto, es que ya sabes buscar.\n\n" +
            "Tu lonchera tiene la clave de siempre: tu ruta.\n0 - 4 - 6.\n\n" +
            "No te asustes de la oscuridad. Yo estoy aquí.\n\n— Pa";

        public const string NotaParaPaTitulo = "Una nota que nunca le di";
        public const string NotaParaPa =
            "Pa:\n\nGracias por traerme hoy.\nSé que el bus es más cómodo para vos,\npero esto es mejor.\n\n" +
            "Te quiero. Nos vemos a la salida.\n\n— M.M.";

        public const string HojaTitulo = "Letra temblorosa";
        public const string Hoja =
            "Lo que olvidaste te trajo aquí.\n\nCuatro dígitos. Día y mes.\nEl día que cumpliste dieciséis.\n\nNo lo olvides otra vez.";

        // ---------- escritos en la pared (se ven con la linterna) ----------
        public static readonly string[] Paredes = { "Fue tu culpa", "No fue tu culpa", "Aún hay una salida", "Sigue el aire" };

        // ---------- final (provisional hasta que esté la escena del hospital) ----------
        public static readonly (string texto, float seg)[] Final =
        {
            ("Diecisiete de mayo. Tu cumpleaños.", 3.5f),
            ("Ese día no murió nadie.", 3.5f),
            ("Pa está vivo. Te visita todos los días.", 4f),
            ("El que no ha vuelto eres tú.", 4f),
            ("Tres años dormido.", 3.5f),
            ("Respira conmigo, mijo. Cuatro… sostén… seis.", 5f),
            ("Algo no quiere que despiertes.", 4f),
        };

        // ---------- ductos y game over ----------
        public static readonly string[] SalidaDuctos = { "Saliste.", "Pero no al mismo lugar." };
        public const string GameOverCuerpo = "El miedo no decide por ti.\nRespira y vuelve a intentarlo.";

        public static void Aplicar()
        {
            var datos = Object.FindObjectsByType<DatosBlender>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool EnSalon(Vector3 p) => p.x > 19.33f && p.x < 28.67f && p.z > 29f && p.z < 36f && p.y > 6.5f && p.y < 11f;

            int n = 0;
            var esc = datos.FirstOrDefault(d => d.Get("prop_tipo") == "escritorio_profesor" && EnSalon(d.transform.position));
            if (Nota(esc, CuadernoTitulo, Cuaderno)) n++;
            if (Nota(datos.FirstOrDefault(d => d.Get("interactuable") == "carta 1"), NotaParaPaTitulo, NotaParaPa)) n++;

            var cas = datos.FirstOrDefault(d => d.Get("interactuable").StartsWith("casillero: candado 142"));
            var lc = cas ? cas.GetComponent<LockedContainer>() : null;
            if (lc)
            {
                var so = new SerializedObject(lc);
                so.FindProperty("interiorNoteTitle").stringValue = CartaPaTitulo;
                so.FindProperty("interiorNoteBody").stringValue = CartaPa;
                so.ApplyModifiedPropertiesWithoutUndo(); n++;
            }

            var hoja = Object.FindObjectsByType<Note>(FindObjectsSortMode.None)
                             .Where(x => x.transform.position.x > 28.67f && x.transform.position.z < 7f && x.transform.position.y < 2f)
                             .OrderBy(x => x.transform.position.x).FirstOrDefault();
            if (hoja && Nota(hoja.GetComponent<DatosBlender>(), HojaTitulo, Hoja)) n++;

            var muros = Object.FindObjectsByType<HiddenMessage>(FindObjectsSortMode.None).OrderBy(h => h.name).ToArray();
            for (int i = 0; i < muros.Length && i < Paredes.Length; i++)
            {
                var so = new SerializedObject(muros[i]);
                so.FindProperty("messageText").stringValue = Paredes[i];
                so.ApplyModifiedPropertiesWithoutUndo(); n++;
            }

            foreach (var fin in Object.FindObjectsByType<EndGameTrigger>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(fin);
                var sl = so.FindProperty("slides");
                sl.arraySize = Final.Length;
                for (int i = 0; i < Final.Length; i++)
                {
                    sl.GetArrayElementAtIndex(i).FindPropertyRelative("text").stringValue = Final[i].texto;
                    sl.GetArrayElementAtIndex(i).FindPropertyRelative("durationSeconds").floatValue = Final[i].seg;
                }
                so.ApplyModifiedPropertiesWithoutUndo(); n++;
            }

            foreach (var go in Object.FindObjectsByType<GameOverHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(go);
                so.FindProperty("bodyText").stringValue = GameOverCuerpo;
                so.FindProperty("hintText").stringValue = "[Enter] intentar otra vez";
                so.ApplyModifiedPropertiesWithoutUndo(); n++;
            }
            Debug.Log($"[Textos] textos del capítulo 1 aplicados: {n}");
        }

        public static void AplicarDuctos(ExitDuctsTrigger t)
        {
            var so = new SerializedObject(t);
            var sl = so.FindProperty("slides");
            sl.arraySize = SalidaDuctos.Length;
            for (int i = 0; i < SalidaDuctos.Length; i++) sl.GetArrayElementAtIndex(i).FindPropertyRelative("text").stringValue = SalidaDuctos[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool Nota(DatosBlender d, string titulo, string texto)
        {
            var nota = d ? d.GetComponent<Note>() : null;
            if (!nota) return false;
            var so = new SerializedObject(nota);
            so.FindProperty("noteTitle").stringValue = titulo;
            so.FindProperty("noteText").stringValue = texto;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }
}
