using UnityEditor;
using UnityEngine;
using EstanDentro.Interaction;

namespace EstanDentro.EditorTools
{
    /// <summary>
    /// Editor menu para spawnear las 6 cartas del Capitulo 1 con sus textos definitivos
    /// (segun Document/Diseno_Narrativo_Capitulo1.md sec 7).
    ///
    /// Uso: abrir la escena correspondiente, EstanDentro/Cartas/Spawn ... .
    /// Spawnea GameObjects con componente Note + BoxCollider + cubo placeholder visual.
    /// El usuario despues mueve cada carta a su ubicacion definitiva en la escena.
    /// </summary>
    public static class SpawnCartas
    {
        // ---------- TEXTOS DEFINITIVOS (canon Cap 1) ----------

        const string PISTA_PA_TITLE = "Pista de Pa";
        const string PISTA_PA_TEXT =
            "Mateo,\n\n" +
            "Te dejo el destornillador.\n" +
            "La linterna esta en mi lonchera, la vas\n" +
            "a necesitar cuando se apaguen las luces.\n\n" +
            "El codigo son tres digitos:\n" +
            "0 - 4 - 6\n\n" +
            "Cuidate.\n\n" +
            "— Pa";

        const string CARTA1_TITLE = "Cuaderno de matematicas";
        const string CARTA1_TEXT =
            "17 / 05 / 2023\n\n" +
            "Tarea para hoy: ejercicios pag 142.\n" +
            "Examen viernes.\n" +
            "Estudiar geometria 4 hs.\n\n" +
            "(garabato de un planeta dibujado)";

        const string CARTA2_TITLE = "Nota en el bolsillo";
        const string CARTA2_TEXT =
            "Pa,\n\n" +
            "Gracias por traerme hoy.\n" +
            "Se que el bus es mas comodo para vos\n" +
            "pero esto es mejor.\n\n" +
            "Te quiero. Nos vemos a la salida.\n\n" +
            "— M.M.";

        const string CARTA3_TITLE = "Boleto del bus";
        const string CARTA3_TEXT =
            "TRANSPORTE ESCOLAR — LINEA 5\n\n" +
            "Boleto valido: 17/05/2023\n" +
            "Salida 7:00 hs\n\n" +
            "Pase de estudiante: 0046\n\n" +
            "[matasellos: NO USADO]";

        const string CARTA4_TITLE = "Foto familiar";
        const string CARTA4_TEXT =
            "[Foto vieja: un nino pequeno con un hombre adulto. Ambos sonriendo.\n" +
            "El hombre lleva al nino en brazos. Mas atras se intuye una figura femenina\n" +
            "desenfocada — Carolina, la madre.]\n\n" +
            "[Atras de la foto, escritura a mano:]\n\n" +
            "\"Tu primer dia de cole.\n" +
            "5 anos. Vos. Yo. Mama.\"";

        const string CARTA5_TITLE = "Mensaje grabado";
        const string CARTA5_TEXT =
            "[Audio: voz adulta masculina, calmada, con leve interferencia de hospital\n" +
            "(pitido lejano de monitor cardiaco):]\n\n" +
            "\"Hijo, soy yo.\n\n" +
            "Hoy ya van... [pausa] ...tres anos desde el accidente.\n" +
            "Vine a verte como cada ano.\n\n" +
            "El doctor dice que escuchas. No se si es verdad.\n" +
            "Pero te hablo igual.\n\n" +
            "Cumplis 19 hoy.\n" +
            "Te traje torta. La voy a comer al lado tuyo.\n\n" +
            "Cuando estes listo, despertate.\n" +
            "Te espero.\n\n" +
            "— Pa\"\n\n" +
            "[Pitido del monitor cardiaco continua. Fade out.]";

        const string CARTA6_TITLE = "Cuaderno de visitas";
        const string CARTA6_TEXT =
            "[Cuaderno de visitas al hospital. Ultimas paginas:]\n\n" +
            "ENERO 2024 — Sigue dormido. Le hable de mama. Esperaba que reaccionara.\n" +
            "JUNIO 2024 — Le traje su libro favorito. Lo deje al lado.\n" +
            "MAYO 2025 — 17 de mayo. Cumple 18. Le cante.\n" +
            "DICIEMBRE 2025 — Esta noche fue Navidad. Vine igual.\n" +
            "HOY (17/05/2026) — Cumple 19. Le voy a leer la carta que le escribi.\n\n" +
            "[Hay un sobre cerrado pegado al cuaderno. Sin abrir.]\n\n" +
            "[En el sobre, escrito a mano:]\n" +
            "\"Para cuando despiertes.\"";

        // ---------- MENUS ----------

        [MenuItem("EstanDentro/Cartas/Spawn Cartas 1 + 2 (Salon Principal)")]
        public static void SpawnActo1()
        {
            SpawnCarta("Carta_01_CuadernoMatematicas", CARTA1_TITLE, CARTA1_TEXT, new Vector3(0, 1f, 0));
            SpawnCarta("Carta_02_NotaBolsillo",         CARTA2_TITLE, CARTA2_TEXT, new Vector3(0.6f, 1f, 0));
            EditorUtility.DisplayDialog("Cartas Acto 1",
                "Cartas 1 y 2 spawneadas en el origen.\n\nMovelas a sus ubicaciones definitivas:\n" +
                "• Carta 1: sobre el pupitre del protagonista\n" +
                "• Carta 2: en el bolsillo de la chaqueta colgada en la silla\n\n" +
                "Despues guarda la escena.", "OK");
        }

        [MenuItem("EstanDentro/Cartas/Spawn Pista de Pa (dentro del casillero)")]
        public static void SpawnPistaPa()
        {
            SpawnCarta("Carta_PistaPa", PISTA_PA_TITLE, PISTA_PA_TEXT, new Vector3(0, 1f, 0));
            EditorUtility.DisplayDialog("Pista de Pa",
                "Carta 'Pista de Pa' spawneada en el origen.\n\n" +
                "Movela DENTRO del Casillero_REAL para que aparezca cuando se abra.\n" +
                "(la animacion del casillero la deja a la vista, el jugador interactua para leer)\n\n" +
                "Tip: ponela como hijo del GameObject del casillero asi se mueve con el.\n\n" +
                "Despues guarda la escena.", "OK");
        }

        [MenuItem("EstanDentro/Cartas/Spawn Cartas 3 + 4 (Ductos)")]
        public static void SpawnActo2()
        {
            SpawnCarta("Carta_03_BoletoBus",   CARTA3_TITLE, CARTA3_TEXT, new Vector3(0, 1f, 0));
            SpawnCarta("Carta_04_FotoFamilia", CARTA4_TITLE, CARTA4_TEXT, new Vector3(0.6f, 1f, 0));
            EditorUtility.DisplayDialog("Cartas Acto 2",
                "Cartas 3 y 4 spawneadas en el origen.\n\nMovelas a sus ubicaciones definitivas:\n" +
                "• Carta 3: caja escondida en la bifurcacion del ducto\n" +
                "• Carta 4: cerca de la salida del ducto\n\n" +
                "Importante: la Carta 3 al cerrarse dispara la Cinematica 2 (Flashback).\n\n" +
                "Despues guarda la escena.", "OK");
        }

        [MenuItem("EstanDentro/Cartas/Spawn Carta 5 (Sala de Descanso, Acto 3)")]
        public static void SpawnCarta5()
        {
            SpawnCarta("Carta_05_MensajeGrabado", CARTA5_TITLE, CARTA5_TEXT, new Vector3(0, 1f, 0));
            EditorUtility.DisplayDialog("Carta 5",
                "Carta 5 spawneada en el origen.\n\nMovela a su ubicacion definitiva:\n" +
                "• Sobre la mesa de la Sala de Descanso (cafeteria), como un\n" +
                "  reproductor antiguo / celular viejo.\n\n" +
                "Despues guarda la escena.", "OK");
        }

        [MenuItem("EstanDentro/Cartas/Spawn Carta 6 (Sala de Juntas, Acto 3)")]
        public static void SpawnCarta6()
        {
            SpawnCarta("Carta_06_CuadernoVisitas", CARTA6_TITLE, CARTA6_TEXT, new Vector3(0, 1f, 0));
            EditorUtility.DisplayDialog("Carta 6",
                "Carta 6 spawneada en el origen.\n\nMovela a su ubicacion definitiva:\n" +
                "• Dentro de la caja con cerradura (CombinationLock 1705) sobre la mesa\n" +
                "  de la Sala de Juntas. Activala recien cuando se resuelve la cerradura\n" +
                "  (con el evento onSolved del CombinationLock).\n\n" +
                "Despues guarda la escena.", "OK");
        }

        // ---------- IMPLEMENTACION ----------

        private static void SpawnCarta(string goName, string title, string text, Vector3 pos)
        {
            // Verificar si ya existe (por nombre) para no duplicar.
            var existing = GameObject.Find(goName);
            if (existing != null)
            {
                Debug.LogWarning($"[SpawnCartas] '{goName}' ya existe en la escena. Skipping.");
                Selection.activeGameObject = existing;
                return;
            }

            // Root: empty GameObject con Note + BoxCollider
            var go = new GameObject(goName);
            Undo.RegisterCreatedObjectUndo(go, "Spawn Carta");
            go.transform.position = pos;

            var note = go.AddComponent<Note>();
            // Setear los SerializeFields privados via SerializedObject
            var so = new SerializedObject(note);
            so.FindProperty("noteTitle").stringValue = title;
            so.FindProperty("noteText").stringValue = text;
            so.ApplyModifiedProperties();

            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.3f, 0.05f, 0.2f); // tipo papel sobre superficie

            // Visual placeholder: cubo pequeno como hijo (user lo reemplaza por el modelo real)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual_Placeholder";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.3f, 0.05f, 0.2f);
            // Quitar el BoxCollider que crea el primitive (ya tenemos uno en el root)
            var dupCol = visual.GetComponent<Collider>();
            if (dupCol != null) Object.DestroyImmediate(dupCol);

            // Color amarillento (papel) para distinguirlo en scene view
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                // Solo crear instance si vamos a tintarlo, sino se duplica el material
                var mat = new Material(renderer.sharedMaterial);
                mat.color = new Color(0.92f, 0.85f, 0.6f, 1f);
                renderer.sharedMaterial = mat;
            }

            Selection.activeGameObject = go;
            Debug.Log($"[SpawnCartas] '{goName}' creada en la escena. Movela a su posicion definitiva.");
        }
    }
}
