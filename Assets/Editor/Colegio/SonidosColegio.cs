using System.Linq;
using UnityEditor;
using UnityEngine;
using EstanDentro.Colegio;

namespace EstanDentro.EditorColegio
{
    /// <summary>
    /// Pone los sonidos del colegio en un solo lugar (auditoría de sonido, 19-oct-2026).
    /// Solo rellena lo que estaba vacío y quita lo que no tiene sentido (el pizarrón sonando en todo el colegio).
    /// Sonidos nuevos (CC0) en Resources/SonidosJuego; lo que aún no tiene reemplazo usa Assets/Audio.
    /// </summary>
    public static class SonidosColegio
    {
        const string A = "Assets/Audio/Ambient/";
        static AudioClip C(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(A + n);
        // sonidos nuevos (Freesound CC0 + Kenney CC0), preparados con 04_Audio/preparar_sonidos.py
        static AudioClip J(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/SonidosJuego/" + n + ".wav");

        /// <summary>Solo re-aplica los sonidos a la escena del colegio ya armada (sin reconstruirla).</summary>
        public static void AplicarBatch()
        {
            var esc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity");
            Aplicar();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(esc);
            EditorApplication.Exit(0);
        }

        public static void Aplicar()
        {
            int n = 0;

            // ambiente: brisa audible y eventos sueltos que sí tienen sentido en un pasillo (sin pizarrón ni casillero de 17 s)
            foreach (var amb in Object.FindObjectsByType<Audio.AmbientLoopWithRandomEvents>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(amb);
                so.FindProperty("loopVolume").floatValue = 0.35f;
                var ev = so.FindProperty("randomEventClips");
                // fondo: lluvia en las ventanas (llueve desde el prólogo); antes era una brisa inaudible
                if (J("ambiente/lluvia_ventana")) so.FindProperty("loopClip").objectReferenceValue = J("ambiente/lluvia_ventana");
                var lista = new[] { C("sonidoGolpe.mp3"), C("Crujido 1.flac"), C("Crujido 3.mp3"), C("otros/silla dos arrastrandose.wav") }.Where(c => c).ToArray();
                ev.arraySize = lista.Length;
                for (int i = 0; i < lista.Length; i++) ev.GetArrayElementAtIndex(i).objectReferenceValue = lista[i];
                so.FindProperty("randomEventVolume").floatValue = 0.45f;
                so.FindProperty("randomEventPauseRange").vector2Value = new Vector2(30f, 70f);
                so.FindProperty("initialDelayBeforeFirstEvent").floatValue = 25f;
                so.ApplyModifiedPropertiesWithoutUndo(); n++;
            }

            // reemplazos: el sonido viejo no correspondía a lo que se ve
            n += Poner<Interaction.Note>("pickupClip", J("objetos/papel_tomar"));          // era un "pickup" de videojuego; es papel
            n += Poner<Recogible>("sonido", J("objetos/objeto_tomar"));
            n += Poner<MochilaEquipable>("sonido", J("objetos/mochila"));
            n += Poner<Interaction.Flashlight>("toggleClip", J("objetos/linterna"));       // era el mismo clic del menú
            n += Poner<CandadoVisual>("golpe", J("candado/cae"));
            n += Poner<Casillero>("sonidoAbrir", J("casillero/pestillo"));                 // era una bisagra de 17 s
            n += Poner<Casillero>("sonidoBisagra", C("casillero-abriendose.mp3"));       // el chirrido de la bisagra (solo el comienzo)
            n += Poner<Casillero>("sonidoTomar", J("objetos/objeto_tomar"));

            n += Rellenar<Interaction.CombinationLock>("digitClickClip", C("CombinationLock click digito.wav"));
            n += Rellenar<Interaction.CombinationLock>("solvedClip", C("CombinationLock solved.wav"));
            n += Rellenar<Interaction.LockedDoor>("lockedAudioClip", C("LockedDoor jaloneo.ogg"));
            n += Rellenar<Player.PlayerWakeUp>("heartbeatLoopClip", C("Heartbeat (despertar).ogg"));
            n += Rellenar<Player.PlayerWakeUp>("gaspClip", C("Gasp dramatico.mp3"));
            n += Rellenar<Contenedor>("sonidoAbrir", C("otros/Cajon Abriendo 1.mp3"));
            Debug.Log($"[Sonidos] {n} campos de sonido revisados/puestos en el colegio");
        }

        static int Poner<T>(string campo, AudioClip clip) where T : Object
        {
            if (!clip) { Debug.LogWarning($"[Sonidos] falta el clip nuevo para {typeof(T).Name}.{campo}"); return 0; }
            int n = 0;
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(c);
                var p = so.FindProperty(campo);
                if (p == null) continue;
                p.objectReferenceValue = clip; so.ApplyModifiedPropertiesWithoutUndo(); n++;
            }
            return n;
        }

        static int Rellenar<T>(string campo, AudioClip clip) where T : Object
        {
            if (!clip) { Debug.LogWarning($"[Sonidos] falta el clip para {typeof(T).Name}.{campo}"); return 0; }
            int n = 0;
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(c);
                var p = so.FindProperty(campo);
                if (p == null || p.objectReferenceValue) continue;
                p.objectReferenceValue = clip; so.ApplyModifiedPropertiesWithoutUndo(); n++;
            }
            return n;
        }
    }
}
