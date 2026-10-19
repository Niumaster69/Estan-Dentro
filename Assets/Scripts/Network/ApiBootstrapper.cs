using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EstanDentro.Network
{
    // Corre una sola vez al iniciar la app, antes de cualquier escena.
    // 1) Carga el perfil de PlayerPrefs (o lo crea silenciosamente si es primer arranque).
    // 2) Precarga el catalogo de Logros desde la API y lo cachea en GameSession.LogroIdByCodigo.
    // Si la API no responde, queda en modo offline (sin crash). Retry implicito al siguiente arranque.
    public class ApiBootstrapper : MonoBehaviour
    {
        private static bool _hasBooted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_hasBooted) return;
            var go = new GameObject("[ApiBootstrapper]");
            DontDestroyOnLoad(go);
            go.AddComponent<ApiBootstrapper>();
        }

        private void Awake()
        {
            if (_hasBooted) { Destroy(gameObject); return; }
            _hasBooted = true;
            DontDestroyOnLoad(gameObject);

            GameSession.LoadFromPlayerPrefs();
            // Forzamos creacion del singleton del cliente (DontDestroyOnLoad se aplica en su Awake).
            _ = ApiClient.Instance;

            // guardado local: el perfil de PlayerPrefs puede ser del servidor viejo (somee) y no existir aquí
            if (!ApiClient.EnNube && GameSession.CurrentJugadorId != 0 && AlmacenLocal.Jugador(GameSession.CurrentJugadorId) == null)
            {
                Debug.Log($"[ApiBootstrapper] El perfil id={GameSession.CurrentJugadorId} era del servidor viejo; creo uno local.");
                GameSession.CurrentJugadorId = 0;
            }

            if (GameSession.CurrentJugadorId == 0)
            {
                CreateAutoProfile();
            }
            else
            {
                Debug.Log($"[ApiBootstrapper] Perfil existente: nombres='{GameSession.CurrentNombres}' usuario='{GameSession.CurrentUsuario}' id={GameSession.CurrentJugadorId}");
            }

            PreloadLogrosCatalog();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (esc, _) => AsegurarPartida(esc.name);
        }

        /// <summary>
        /// Si se entra a jugar sin pasar por el menú (el ejecutable del colegio arranca directo), se crea la partida
        /// para que tiempo y logros queden guardados.
        /// </summary>
        private void AsegurarPartida(string escena)
        {
            if (escena == "MainMenu" || escena == "Cinematic_Intro" || GameSession.HasActivePartida) return;
            StartCoroutine(CrearPartidaCuandoHayaPerfil());
        }

        private System.Collections.IEnumerator CrearPartidaCuandoHayaPerfil()
        {
            float t = 0f;
            while (GameSession.CurrentJugadorId == 0 && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            if (GameSession.CurrentJugadorId == 0 || GameSession.HasActivePartida) yield break;
            var ahora = DateTime.UtcNow;
            GameSession.ResetForNewPartida();
            ApiClient.Instance.CreatePartida(
                new PartidaCreateDto { idJugador = GameSession.CurrentJugadorId, nombrePartida = "Partida " + ahora.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                                       fechaInicio = ahora.ToString("o"), estado = 0, capituloAlcanzado = 1, tiempoSegundos = 0 },
                r => { GameSession.CurrentPartidaId = r.idPartida; GameSession.PartidaStartTime = ahora;
                       Debug.Log($"[ApiBootstrapper] Partida creada sin menú: id={r.idPartida}"); },
                e => Debug.LogWarning("[ApiBootstrapper] No se pudo crear la partida: " + e));
        }

        private void CreateAutoProfile()
        {
            string nombreWindows = SafeWindowsUserName();
            string clean = SanitizeUsername(nombreWindows);
            string guidCorto = Guid.NewGuid().ToString("N").Substring(0, 6);

            // usuario UNIQUE max 30
            string usuarioGenerado = TruncateTo($"{clean}_{guidCorto}", 30);
            // nombres max 50
            string nombresParaApi = TruncateTo(clean, 50);

            var data = new JugadorCreateDto
            {
                usuario = usuarioGenerado,
                nombres = nombresParaApi,
                apellido = ""
            };

            ApiClient.Instance.CreateJugador(
                data,
                response =>
                {
                    GameSession.SaveProfileToPlayerPrefs(response.idJugador, response.usuario, response.nombres);
                    Debug.Log($"[ApiBootstrapper] Auto-perfil creado: nombres='{response.nombres}' usuario='{response.usuario}' id={response.idJugador}");
                },
                error =>
                {
                    Debug.LogWarning($"[ApiBootstrapper] No se pudo crear auto-perfil. Modo offline. {error}");
                }
            );
        }

        private void PreloadLogrosCatalog()
        {
            ApiClient.Instance.GetAllLogros(
                logros =>
                {
                    GameSession.LogroIdByCodigo.Clear();
                    GameSession.LogroByIdCache.Clear();
                    foreach (var l in logros)
                    {
                        if (string.IsNullOrEmpty(l.codigo)) continue;
                        GameSession.LogroIdByCodigo[l.codigo] = l.idLogro;
                        GameSession.LogroByIdCache[l.idLogro] = l;
                    }
                    Debug.Log($"[ApiBootstrapper] Catalogo de logros cargado: {GameSession.LogroIdByCodigo.Count} entradas");
                },
                error =>
                {
                    Debug.LogWarning($"[ApiBootstrapper] No se pudo cargar catalogo de logros. {error}");
                }
            );
        }

        // ---------- helpers ----------

        private static string SafeWindowsUserName()
        {
            try
            {
                string name = Environment.UserName;
                return string.IsNullOrWhiteSpace(name) ? "Player" : name;
            }
            catch { return "Player"; }
        }

        private static string SanitizeUsername(string raw)
        {
            string clean = Regex.Replace(raw, @"[^a-zA-Z0-9_]", "_");
            return string.IsNullOrEmpty(clean) ? "Player" : clean;
        }

        private static string TruncateTo(string s, int maxLen)
            => string.IsNullOrEmpty(s) || s.Length <= maxLen ? s : s.Substring(0, maxLen);
    }
}
