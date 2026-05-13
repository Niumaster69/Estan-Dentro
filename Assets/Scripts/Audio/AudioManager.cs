using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using EstanDentro.UI;

namespace EstanDentro.Audio
{
    /// <summary>
    /// Singleton de audio que persiste entre escenas. Controla 4 categorias:
    ///   - Music: tema de menu, musica de momento (loop con fade in/out)
    ///   - Ambient: loops ambientales (classroom, drip, viento) (loop con fade)
    ///   - SFX: efectos puntuales 3D-friendly (puerta, jadeo, click linterna) (one-shot)
    ///   - UI: clicks de menu, hover, abrir inventario (one-shot, 2D)
    ///
    /// Volumen por categoria: lee Settings.MasterVolume + Settings.MusicVolume / SfxVolume cada frame.
    /// Si asignas AudioMixerGroups en Inspector, los AudioSources se rutean al MainMixer (visual + efectos).
    /// Si no, igual funciona porque ajusta volumen directo en cada source.
    ///
    /// Uso desde codigo:
    ///   AudioManager.Instance.PlayMusic(myMusicClip, fadeIn: 1.5f);
    ///   AudioManager.Instance.PlayAmbient(myAmbientClip);
    ///   AudioManager.Instance.PlaySFX(doorOpenClip);
    ///   AudioManager.Instance.PlayUI(buttonClickClip);
    ///
    /// Setup minimo:
    ///   - Crear un GameObject en MainMenu llamado "_AudioManager"
    ///   - Add Component → AudioManager
    ///   - (opcional) Asignar grupos del MainMixer en Inspector
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("AudioMixer (opcional)")]
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup uiGroup;

        [Header("Defaults")]
        [SerializeField, Range(0f, 1f), Tooltip("Volumen base de SFX cuando no se especifica volumeScale en PlaySFX.")]
        private float sfxBaseVolume = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("Volumen base de UI clicks.")]
        private float uiBaseVolume = 0.85f;

        [Header("Auto-play en escena")]
        [SerializeField, Tooltip("Si esta seteado, se reproduce automaticamente como musica al cargar la escena donde vive este AudioManager.")]
        private AudioClip autoPlayMusicOnAwake;
        [SerializeField, Tooltip("Si esta seteado, se reproduce automaticamente como ambient al cargar la escena.")]
        private AudioClip autoPlayAmbientOnAwake;
        [SerializeField] private float autoPlayFadeIn = 1.2f;

        [Header("Persistencia")]
        [SerializeField, Tooltip("Si true, sobrevive a cambios de escena (DontDestroyOnLoad). DEFAULT FALSE para que cada escena tenga su propio AudioManager con su propia musica/ambient. Solo poner true si quieres que UNA musica concreta sobreviva a una transicion (ej. continuar musica entre Cinematic_Intro y MainMenu).")]
        private bool dontDestroyOnLoad = false;

        // AudioSources internos
        private AudioSource musicSrc;
        private AudioSource ambientSrc;
        private AudioSource sfxSrc;
        private AudioSource uiSrc;

        private Coroutine musicFadeCo;
        private Coroutine ambientFadeCo;

        // Targets de volumen (post-fade) — el Update multiplica por settings cada frame
        private float musicTargetVolume;
        private float ambientTargetVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            EnsureAudioListenerExists();
            BuildSources();

            if (autoPlayMusicOnAwake != null) PlayMusic(autoPlayMusicOnAwake, autoPlayFadeIn);
            if (autoPlayAmbientOnAwake != null) PlayAmbient(autoPlayAmbientOnAwake, autoPlayFadeIn);
        }

        // Garantiza que haya UN AudioListener activo en la escena. Sin AudioListener, ningun audio se escucha.
        // En escenas como MainMenu que no tienen Camera/AudioListener propio, este metodo lo crea.
        private void EnsureAudioListenerExists()
        {
            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int activeCount = 0;
            foreach (var l in listeners) if (l.enabled) activeCount++;

            if (listeners.Length == 0)
            {
                gameObject.AddComponent<AudioListener>();
                Debug.Log("[AudioManager] No habia AudioListener en la escena. Agregue uno a _AudioManager.");
                return;
            }
            if (activeCount == 0)
            {
                listeners[0].enabled = true;
                Debug.Log($"[AudioManager] Habia {listeners.Length} AudioListener(s) pero ninguno activo. Active '{listeners[0].gameObject.name}'.");
            }
            else if (activeCount > 1)
            {
                Debug.LogWarning($"[AudioManager] Hay {activeCount} AudioListeners activos en la escena. Unity solo usa uno (puede causar audio raro).");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildSources()
        {
            // music/ambient empiezan en volume=0 porque tienen fade-in via SwapAndFade.
            musicSrc = CreateSource("Music_Source", musicGroup, loop: true, spatial: false, initialVolume: 0f);
            ambientSrc = CreateSource("Ambient_Source", ambientGroup, loop: true, spatial: false, initialVolume: 0f);
            // sfx/ui empiezan en volume=1 porque PlayOneShot multiplica source.volume * volumeScale.
            // Si source.volume=0 -> PlayOneShot suena MUDO sin importar el volumeScale.
            sfxSrc = CreateSource("SFX_Source", sfxGroup, loop: false, spatial: false, initialVolume: 1f);
            uiSrc = CreateSource("UI_Source", uiGroup, loop: false, spatial: false, initialVolume: 1f);
        }

        private AudioSource CreateSource(string name, AudioMixerGroup group, bool loop, bool spatial, float initialVolume = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = group;
            src.loop = loop;
            src.playOnAwake = false;
            src.spatialBlend = spatial ? 1f : 0f;
            src.volume = initialVolume;
            return src;
        }

        private float lastLoggedListenerVolume = -1f;
        private bool lastLoggedListenerPause;
        private void Update()
        {
            // DIAG: avisar si algo modifica AudioListener.volume o pause en runtime
            if (Mathf.Abs(AudioListener.volume - lastLoggedListenerVolume) > 0.01f)
            {
                Debug.Log($"[AudioManager] AudioListener.volume cambio: {lastLoggedListenerVolume:F2} -> {AudioListener.volume:F2}");
                lastLoggedListenerVolume = AudioListener.volume;
            }
            if (AudioListener.pause != lastLoggedListenerPause)
            {
                Debug.LogWarning($"[AudioManager] AudioListener.pause cambio: {lastLoggedListenerPause} -> {AudioListener.pause}");
                lastLoggedListenerPause = AudioListener.pause;
            }

            // Aplica volumen de categoria de Settings cada frame para que cambios desde el menu se reflejen en vivo.
            // Master NO se multiplica aqui — ya se aplica globalmente via AudioListener.volume (Settings.MasterVolume.set lo setea).
            float music = Mathf.Clamp01(Settings.MusicVolume);

            if (musicSrc != null) musicSrc.volume = musicTargetVolume * music;
            if (ambientSrc != null) ambientSrc.volume = ambientTargetVolume * music;
        }

        // ---------- API publica ----------

        public void PlayMusic(AudioClip clip, float fadeIn = 1f)
        {
            if (clip == null || musicSrc == null) return;
            if (musicSrc.clip == clip && musicSrc.isPlaying) return;
            if (musicFadeCo != null) StopCoroutine(musicFadeCo);
            musicFadeCo = StartCoroutine(SwapAndFade(musicSrc, clip, fadeIn, v => musicTargetVolume = v));
        }

        public void StopMusic(float fadeOut = 1f)
        {
            if (musicFadeCo != null) StopCoroutine(musicFadeCo);
            if (musicSrc == null) return;
            musicFadeCo = StartCoroutine(FadeOutThenStop(musicSrc, fadeOut, v => musicTargetVolume = v));
        }

        public void PlayAmbient(AudioClip clip, float fadeIn = 1f)
        {
            if (clip == null || ambientSrc == null) return;
            if (ambientSrc.clip == clip && ambientSrc.isPlaying) return;
            if (ambientFadeCo != null) StopCoroutine(ambientFadeCo);
            ambientFadeCo = StartCoroutine(SwapAndFade(ambientSrc, clip, fadeIn, v => ambientTargetVolume = v));
        }

        public void StopAmbient(float fadeOut = 1f)
        {
            if (ambientFadeCo != null) StopCoroutine(ambientFadeCo);
            if (ambientSrc == null) return;
            ambientFadeCo = StartCoroutine(FadeOutThenStop(ambientSrc, fadeOut, v => ambientTargetVolume = v));
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSrc == null) return;
            // Master NO se multiplica aqui — ya se aplica globalmente via AudioListener.volume.
            float sfx = Mathf.Clamp01(Settings.SfxVolume);
            sfxSrc.PlayOneShot(clip, sfxBaseVolume * volumeScale * sfx);
        }

        public void PlayUI(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || uiSrc == null) return;
            float sfx = Mathf.Clamp01(Settings.SfxVolume);
            uiSrc.PlayOneShot(clip, uiBaseVolume * volumeScale * sfx);
        }

        // ---------- helpers ----------

        private IEnumerator SwapAndFade(AudioSource src, AudioClip newClip, float fadeIn, System.Action<float> targetVolumeSetter)
        {
            // Fade out lo que esta sonando
            if (src.isPlaying && src.clip != null)
            {
                float t = 0f;
                float startVol = src.volume;
                while (t < 0.4f)
                {
                    t += Time.unscaledDeltaTime;
                    src.volume = Mathf.Lerp(startVol, 0f, t / 0.4f);
                    yield return null;
                }
                src.Stop();
            }
            // Fade in del nuevo clip
            src.clip = newClip;
            src.volume = 0f;
            targetVolumeSetter(0f);
            src.Play();
            float ft = 0f;
            float dur = Mathf.Max(0.05f, fadeIn);
            while (ft < dur)
            {
                ft += Time.unscaledDeltaTime;
                targetVolumeSetter(Mathf.Clamp01(ft / dur));
                yield return null;
            }
            targetVolumeSetter(1f);
        }

        private IEnumerator FadeOutThenStop(AudioSource src, float fadeOut, System.Action<float> targetVolumeSetter)
        {
            float dur = Mathf.Max(0.05f, fadeOut);
            float t = 0f;
            float startTarget = 1f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                targetVolumeSetter(Mathf.Lerp(startTarget, 0f, t / dur));
                yield return null;
            }
            targetVolumeSetter(0f);
            src.Stop();
        }
    }
}
