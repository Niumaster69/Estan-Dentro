using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace EstanDentro.Mente
{
    /// <summary>
    /// Los tres estados de la mente de Mateo y el paso entre ellos (Henry 21-oct: "exprimir HDRP… como le pasa a Jinx en Arcane").
    ///   Normal     → el colegio como es.
    ///   Pesadilla  → el colegio "se rompe" (teal frío, texturas dañadas, cosas en otros lugares, el Observador).
    ///   Alterado   → la persecución (luz roja, visión en túnel, corazón).
    /// Lo que se ve encima de la imagen: Resources/Mente/MenteFullscreen.shader (pase de pantalla completa) + un volumen de
    /// HDRP animado (distorsión de lente, aberración, grano, viñeta, color, bloom). Los garabatos son de lápiz, como los
    /// dibujos del cuaderno de Mateo, y "hierven" a 12 cuadros por segundo.
    /// El cambio de texturas del mundo se hace dentro del destello del glitch: OnCambioMundo(true = Pesadilla).
    /// </summary>
    public class EstadoMente : MonoBehaviour
    {
        public enum Estado { Normal, Pesadilla, Alterado }
        public static EstadoMente Instancia { get; private set; }
        public static Estado Actual => Instancia ? Instancia.actual : Estado.Normal;
        public static event Action<bool> OnCambioMundo;
        public static bool EnTransicion => Instancia && Instancia.transicion;

        Estado actual = Estado.Normal;
        bool transicion;
        Material mat; CustomPassVolume cpv; Volume vol; VolumeProfile perfil;
        Fog niebla; LensDistortion lente; ChromaticAberration aber; FilmGrain grano; ColorAdjustments color; Vignette vin; Bloom bloom; DepthOfField dof; SplitToning tono;
        AudioSource tinnitus, susurros, latido;

        // lo que se ve ahora (se interpola hacia "meta")
        struct P
        {
            public float contorno, fov, desenfoque, niebla, aberr, glitch, garab, borde, tinteA, destello, negativo, lente, grano, sat, vin, bloom, expo;
            public Color tinte, colGarab, colVin, colContorno, sombras;
        }
        P ahora, meta;

        public static void Ir(Estado e) { Asegurar(); Instancia.StartCoroutine(Instancia.Hacia(e)); }

        public static void Asegurar()
        {
            if (Instancia) return;
            var go = new GameObject("EstadoMente"); DontDestroyOnLoad(go);
            Instancia = go.AddComponent<EstadoMente>(); Instancia.Construir();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, modo) => { if (modo == UnityEngine.SceneManagement.LoadSceneMode.Single && Instancia) Instancia.Reiniciar(); };
        }

        void Construir()
        {
            var sh = Resources.Load<Shader>("Mente/MenteFullscreen");
            mat = new Material(sh);
            mat.SetTexture("_Garabatos", Garabatos.Crear(1024, 576, 7));
            cpv = gameObject.AddComponent<CustomPassVolume>();
            cpv.isGlobal = true; cpv.injectionPoint = CustomPassInjectionPoint.BeforePostProcess; cpv.priority = 50;
            var pase = (FullScreenCustomPass)cpv.AddPassOfType(typeof(FullScreenCustomPass));
            pase.name = "Mente"; pase.fullscreenPassMaterial = mat; pase.fetchColorBuffer = true;
            pase.targetColorBuffer = CustomPass.TargetBuffer.Camera; pase.targetDepthBuffer = CustomPass.TargetBuffer.None;
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            lente = perfil.Add<LensDistortion>(true); aber = perfil.Add<ChromaticAberration>(true); grano = perfil.Add<FilmGrain>(true);
            color = perfil.Add<ColorAdjustments>(true); vin = perfil.Add<Vignette>(true); bloom = perfil.Add<Bloom>(true);
            grano.type.Override(FilmGrainLookup.Thin2);
            niebla = perfil.Add<Fog>(true); dof = perfil.Add<DepthOfField>(true); tono = perfil.Add<SplitToning>(true);
            vol = gameObject.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 300; vol.sharedProfile = perfil; vol.weight = 0f;
            tinnitus = Fuente("SonidosJuego/cuerpo/tinnitus"); susurros = Fuente("SonidosJuego/sustos/susurro"); latido = Fuente("Sonidos/latido");
            meta = ahora = Base();
        }

        /// <summary>Escena nueva (o se recarga tras el game over): todo vuelve a lo normal de una.</summary>
        void Reiniciar()
        {
            StopAllCoroutines(); transicion = false; actual = Estado.Normal; mundoRoto = false; mundoVuelto = false;
            meta = ahora = Base(); ondaT = 9f; fovTomado = false;
            foreach (var a in new[] { tinnitus, susurros, latido }) if (a) a.Stop();
            Aplicar();
        }
        bool mundoRoto;
        static readonly bool enPrueba = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-autoprueba") >= 0;

        AudioSource Fuente(string ruta)
        {
            var a = gameObject.AddComponent<AudioSource>(); a.clip = Resources.Load<AudioClip>(ruta); a.loop = true; a.volume = 0f; a.spatialBlend = 0f; a.playOnAwake = false;
            return a;
        }

        static P Base() => new P { tinte = Color.white, colGarab = new Color(0.9f, 0.9f, 0.85f, 0f), colVin = Color.black, colContorno = new Color(0.4f, 1f, 0.95f, 0.6f), sombras = new Color(0.5f, 0.5f, 0.5f), sat = 0f, expo = 0f };

        static P Del(Estado e)
        {
            var p = Base();
            switch (e)
            {
                case Estado.Pesadilla:
                    p.aberr = 0.2f; p.garab = 0.32f; p.borde = 0.2f; p.tinte = new Color(0.35f, 0.95f, 0.88f); p.tinteA = 0.38f;
                    p.colGarab = new Color(0.25f, 0.95f, 0.9f, 0.55f); p.lente = -0.1f; p.grano = 0.5f; p.sat = -20f; p.vin = 0.4f;
                    p.colVin = new Color(0.0f, 0.1f, 0.1f); p.bloom = 0.35f; p.expo = 0.35f; p.niebla = 1f; p.colContorno = new Color(0.3f, 1f, 0.92f, 0.6f); p.sombras = new Color(0.1f, 0.75f, 0.7f); break;
                case Estado.Alterado:
                    p.aberr = 0.45f; p.garab = 0.45f; p.borde = 0.26f; p.tinte = new Color(1f, 0.18f, 0.12f); p.tinteA = 0.55f;
                    p.colGarab = new Color(1f, 0.12f, 0.08f, 0.7f); p.lente = -0.2f; p.grano = 0.6f; p.sat = -20f; p.vin = 0.55f;
                    p.colVin = new Color(0.25f, 0f, 0f); p.bloom = 0.6f; p.expo = -0.5f; p.niebla = 0.4f; p.colContorno = new Color(1f, 0.15f, 0.1f, 0.8f); p.sombras = new Color(0.85f, 0.05f, 0.05f); p.desenfoque = 0.35f; break;
            }
            return p;
        }

        IEnumerator Hacia(Estado e)
        {
            Debug.Log($"[Mente] pedido {actual} -> {e} (transición en curso={transicion}, timeScale={Time.timeScale})");
            while (transicion) yield return null;
            if (e == actual) yield break;
            transicion = true;
            var antes = actual;
            if (e == Estado.Pesadilla && antes == Estado.Alterado && mundoRoto) yield return Calmar();
            else if (e == Estado.Pesadilla) yield return EntrarPesadilla();
            else if (e == Estado.Alterado) yield return EntrarAlterado();
            else yield return Volver(antes);
            actual = e; transicion = false;
            Debug.Log($"[Mente] ahora {actual}");
        }

        // ----- Pesadilla: susurros que crecen, el borde se llena de garabatos, todo se tuerce, glitch y destello: cambia el mundo
        IEnumerator EntrarPesadilla()
        {
            Arrancar(tinnitus); Arrancar(susurros); Arrancar(latido);
            float T = 1.6f;
            for (float t = 0f; t < T; t += Time.deltaTime)
            {
                float k = t / T, pulso = Mathf.Sin(t * 9f) * 0.5f + 0.5f;
                meta = Del(Estado.Pesadilla);
                meta.aberr = Mathf.Lerp(0.1f, 1f, k * k); meta.garab = Mathf.Lerp(0.2f, 1f, k); meta.borde = Mathf.Lerp(0.15f, 0.95f, k * k);
                meta.lente = -0.1f - 0.45f * k * pulso; meta.glitch = k > 0.6f ? (k - 0.6f) * 2.2f : 0f; meta.tinteA = 0.15f * k;
                meta.fov = 14f * k * k; meta.desenfoque = k; meta.contorno = k > 0.55f ? (k - 0.55f) / 0.45f : 0f;
                if (t < Time.deltaTime * 1.5f) Onda(1.1f, 1.2f);
                tinnitus.volume = 0.35f * k; susurros.volume = 0.7f * k; latido.volume = 0.6f * k; latido.pitch = 1f + 0.4f * k;
                yield return null;
            }
            // el golpe: 3 cuadros de negativo + glitch fuerte; dentro de eso cambia el mundo
            meta.glitch = 1f; meta.negativo = 1f; ahora.negativo = 1f; Inmediato();
            yield return null;
            OnCambioMundo?.Invoke(true); mundoRoto = true; Onda(1f, 0.55f); ahora.fov = -8f;
            yield return null; yield return null;
            meta.negativo = 0f; ahora.negativo = 0f; meta.destello = 0.5f; ahora.destello = 0.5f;
            // se asienta: la Pesadilla queda, los garabatos se retiran a los bordes
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                float k = t / 1.4f;
                var d = Del(Estado.Pesadilla);
                meta = d; meta.garab = Mathf.Lerp(1f, d.garab, k); meta.borde = Mathf.Lerp(0.95f, d.borde, k); meta.glitch = Mathf.Lerp(0.6f, 0f, k * 2f); meta.contorno = Mathf.Lerp(1f, 0f, k * 1.4f);
                tinnitus.volume = Mathf.Lerp(0.35f, 0.08f, k); susurros.volume = Mathf.Lerp(0.7f, 0.25f, k); latido.volume = Mathf.Lerp(0.6f, 0.35f, k);
                yield return null;
            }
        }

        // ----- Alterado: golpe rojo inmediato, corazón fuerte
        IEnumerator EntrarAlterado()
        {
            Arrancar(latido); Arrancar(tinnitus);
            meta = Del(Estado.Alterado); meta.destello = 0f; meta.glitch = 0.7f; meta.aberr = 1f;
            ahora.tinte = meta.tinte; ahora.tinteA = 0.9f; ahora.negativo = 0.6f; ahora.contorno = 1f; ahora.colContorno = meta.colContorno; ahora.fov = -12f; Onda(1f, 0.45f); Inmediato();
            latido.volume = 0.9f; latido.pitch = 1.5f; tinnitus.volume = 0.25f;
            yield return null; yield return null;
            ahora.negativo = 0f;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { meta.glitch = Mathf.Lerp(0.7f, 0f, t / 0.5f); meta.aberr = Mathf.Lerp(1f, 0.5f, t / 0.5f); meta.contorno = Mathf.Lerp(0.8f, 0f, t / 0.5f); yield return null; }
            meta = Del(Estado.Alterado);
        }

        // ----- Calmar: de la persecución a la Pesadilla (te perdió): el rojo se apaga, el latido baja
        IEnumerator Calmar()
        {
            var d0 = Del(Estado.Alterado); var d1 = Del(Estado.Pesadilla);
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                float k = t / 1.6f;
                meta = Mezcla(d0, d1, k);
                latido.volume = Mathf.Lerp(0.9f, 0.35f, k); latido.pitch = Mathf.Lerp(1.5f, 1.1f, k); tinnitus.volume = Mathf.Lerp(0.25f, 0.08f, k);
                yield return null;
            }
            Arrancar(susurros); susurros.volume = 0.25f;
            meta = d1;
        }

        // ----- Volver: Mateo respira; los garabatos se borran hacia afuera, un destello suave, vuelven los colores
        IEnumerator Volver(Estado desde)
        {
            float T = 1.4f;
            var d0 = Del(desde); Onda(0.6f, 1.3f);
            for (float t = 0f; t < T; t += Time.deltaTime)
            {
                float k = t / T;
                meta = Mezcla(d0, Base(), k);
                if (k > 0.55f && mundoRoto && !mundoVuelto) { meta.destello = 0.1f; ahora.destello = 0.1f; OnCambioMundo?.Invoke(false); mundoVuelto = true; mundoRoto = false; }
                foreach (var a in new[] { tinnitus, susurros, latido }) a.volume = Mathf.Lerp(a.volume, 0f, Time.deltaTime * 3f);
                yield return null;
            }
            mundoVuelto = false;
            meta = Base();
            foreach (var a in new[] { tinnitus, susurros, latido }) a.Stop();
        }
        bool mundoVuelto;
        bool fovTomado; float fovBase;
        float ondaT = 9f, ondaDur = 1f, ondaFuerza;
        void Onda(float fuerza, float duracion) { ondaFuerza = fuerza; ondaDur = duracion; ondaT = 0f; }

        static P Mezcla(P a, P b, float k)
        {
            return new P {
                contorno = Mathf.Lerp(a.contorno, b.contorno, k), fov = Mathf.Lerp(a.fov, b.fov, k), desenfoque = Mathf.Lerp(a.desenfoque, b.desenfoque, k),
                colContorno = Color.Lerp(a.colContorno, b.colContorno, k), sombras = Color.Lerp(a.sombras, b.sombras, k),
                niebla = Mathf.Lerp(a.niebla, b.niebla, k), aberr = Mathf.Lerp(a.aberr, b.aberr, k), glitch = Mathf.Lerp(a.glitch, b.glitch, k), garab = Mathf.Lerp(a.garab, b.garab, k), borde = Mathf.Lerp(a.borde, b.borde, k),
                tinteA = Mathf.Lerp(a.tinteA, b.tinteA, k), lente = Mathf.Lerp(a.lente, b.lente, k), grano = Mathf.Lerp(a.grano, b.grano, k), sat = Mathf.Lerp(a.sat, b.sat, k),
                vin = Mathf.Lerp(a.vin, b.vin, k), bloom = Mathf.Lerp(a.bloom, b.bloom, k), expo = Mathf.Lerp(a.expo, b.expo, k),
                tinte = Color.Lerp(a.tinte, b.tinte, k), colGarab = Color.Lerp(a.colGarab, b.colGarab, k), colVin = Color.Lerp(a.colVin, b.colVin, k) };
        }

        void Arrancar(AudioSource a) { if (a && a.clip && !a.isPlaying) { a.volume = 0f; a.Play(); } }
        void Inmediato() { Aplicar(); }

        void Update()
        {
            float v = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f);
            ahora.contorno = Mathf.Lerp(ahora.contorno, meta.contorno, v * 1.5f); ahora.fov = Mathf.Lerp(ahora.fov, meta.fov, v * 0.8f); ahora.desenfoque = Mathf.Lerp(ahora.desenfoque, meta.desenfoque, v);
            ahora.colContorno = Color.Lerp(ahora.colContorno, meta.colContorno, v); ahora.sombras = Color.Lerp(ahora.sombras, meta.sombras, v);
            if (ondaT < ondaDur) ondaT += Time.unscaledDeltaTime;
            ahora.niebla = Mathf.Lerp(ahora.niebla, meta.niebla, v * 0.5f); ahora.aberr = Mathf.Lerp(ahora.aberr, meta.aberr, v); ahora.glitch = Mathf.Lerp(ahora.glitch, meta.glitch, v * 2f);
            ahora.garab = Mathf.Lerp(ahora.garab, meta.garab, v); ahora.borde = Mathf.Lerp(ahora.borde, meta.borde, v);
            ahora.tinteA = Mathf.Lerp(ahora.tinteA, meta.tinteA, v); ahora.tinte = Color.Lerp(ahora.tinte, meta.tinte, v);
            ahora.destello = Mathf.Lerp(ahora.destello, meta.destello, v * 1.5f); meta.destello = Mathf.MoveTowards(meta.destello, 0f, Time.unscaledDeltaTime * 2f);
            ahora.negativo = Mathf.Lerp(ahora.negativo, meta.negativo, v * 3f);
            ahora.lente = Mathf.Lerp(ahora.lente, meta.lente, v); ahora.grano = Mathf.Lerp(ahora.grano, meta.grano, v); ahora.sat = Mathf.Lerp(ahora.sat, meta.sat, v);
            ahora.vin = Mathf.Lerp(ahora.vin, meta.vin, v); ahora.bloom = Mathf.Lerp(ahora.bloom, meta.bloom, v); ahora.expo = Mathf.Lerp(ahora.expo, meta.expo, v);
            ahora.colGarab = Color.Lerp(ahora.colGarab, meta.colGarab, v); ahora.colVin = Color.Lerp(ahora.colVin, meta.colVin, v);
            Aplicar();
            // pruebas (quitar después): F6 Pesadilla, F7 Alterado, F8 normal
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && !transicion && !enPrueba)
            {
                if (kb.f6Key.wasPressedThisFrame) { if (ControlMente.Instancia) ControlMente.Recuerdo(null, "_prueba"); else Ir(Estado.Pesadilla); }
                if (kb.f7Key.wasPressedThisFrame) Ir(Estado.Alterado);
                if (kb.f8Key.wasPressedThisFrame) { ControlMente.Soltar(); Ir(Estado.Normal); }
            }
        }

        void Aplicar()
        {
            float ondaK = ondaT / ondaDur, ondaA = ondaK < 1f ? ondaFuerza * (1f - ondaK) : 0f;
            bool algo = ahora.contorno > 0.001f || ondaA > 0.001f || ahora.garab > 0.001f || ahora.aberr > 0.001f || ahora.tinteA > 0.001f || ahora.destello > 0.001f || ahora.negativo > 0.001f || ahora.glitch > 0.001f;
            cpv.enabled = algo;
            mat.SetFloat("_Aberracion", ahora.aberr); mat.SetFloat("_Glitch", ahora.glitch);
            mat.SetFloat("_GarabatosFuerza", ahora.garab); mat.SetFloat("_GarabatosBorde", ahora.borde);
            mat.SetFloat("_Cuadro", Mathf.Floor(Time.unscaledTime * 12f) % 3f);
            mat.SetColor("_ColorGarabato", ahora.colGarab);
            mat.SetVector("_Tinte", new Vector4(ahora.tinte.r, ahora.tinte.g, ahora.tinte.b, ahora.tinteA));
            mat.SetFloat("_Destello", ahora.destello); mat.SetFloat("_Negativo", ahora.negativo);
            mat.SetFloat("_Contorno", ahora.contorno); mat.SetColor("_ColorContorno", ahora.colContorno);
            mat.SetFloat("_Onda", ondaA); mat.SetFloat("_OndaFase", Mathf.Clamp01(ondaK));
            mat.SetFloat("_Semilla", Mathf.Floor(Time.unscaledTime * 24f) * 0.1f);
            vol.weight = algo || ahora.vin > 0.01f ? 1f : 0f;
            lente.intensity.Override(ahora.lente); lente.scale.Override(1f + Mathf.Abs(ahora.lente) * 0.3f);
            aber.intensity.Override(Mathf.Clamp01(ahora.aberr * 0.8f));
            grano.intensity.Override(Mathf.Clamp01(ahora.grano));
            color.saturation.Override(ahora.sat); color.postExposure.Override(ahora.expo);
            vin.intensity.Override(ahora.vin); vin.color.Override(ahora.colVin); vin.smoothness.Override(0.6f);
            bloom.intensity.Override(ahora.bloom);
            // desenfoque: el fondo se va (enfoque manual cerca de Mateo)
            bool hayDof = ahora.desenfoque > 0.02f;
            dof.focusMode.overrideState = dof.farFocusStart.overrideState = dof.farFocusEnd.overrideState = hayDof;
            dof.focusMode.value = DepthOfFieldMode.Manual; dof.farFocusStart.value = Mathf.Lerp(30f, 1.2f, ahora.desenfoque); dof.farFocusEnd.value = Mathf.Lerp(60f, 6f, ahora.desenfoque); dof.farMaxBlur = 8f;
            dof.nearFocusStart.overrideState = dof.nearFocusEnd.overrideState = hayDof; dof.nearFocusStart.value = 0f; dof.nearFocusEnd.value = 0.01f;
            // las sombras se tiñen (teal en la Pesadilla, rojo en Alterado)
            float tonoK = Mathf.Max(ahora.tinteA, ahora.niebla * 0.4f);
            tono.shadows.overrideState = tono.balance.overrideState = tonoK > 0.02f; tono.shadows.value = Color.Lerp(new Color(0.5f, 0.5f, 0.5f), ahora.sombras, Mathf.Clamp01(tonoK * 1.8f)); tono.balance.value = 40f;
            // el vértigo: el FOV se abre o se cierra encima del que tenga la cámara
            var cam = Camera.main;
            if (cam && Mathf.Abs(ahora.fov) > 0.05f) { if (!fovTomado) { fovBase = cam.fieldOfView; fovTomado = true; } cam.fieldOfView = fovBase + ahora.fov; }
            else if (fovTomado) { if (cam) cam.fieldOfView = fovBase; fovTomado = false; }
            // niebla espesa en la Pesadilla (solo se fuerza mientras dura; si no, manda la del colegio)
            niebla.enabled.overrideState = niebla.meanFreePath.overrideState = ahora.niebla > 0.02f;
            niebla.enabled.value = true; niebla.meanFreePath.value = Mathf.Lerp(90f, 14f, ahora.niebla);
            niebla.albedo.Override(Color.Lerp(new Color(0.6f, 0.6f, 0.6f), new Color(0.35f, 0.5f, 0.5f), ahora.niebla));
            niebla.albedo.overrideState = ahora.niebla > 0.02f;
        }
    }
}
