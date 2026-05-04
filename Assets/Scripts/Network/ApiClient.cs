using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace EstanDentro.Network
{
    // Cliente HTTP a la API de EstanDentro hospedada en somee.
    // Patron: coroutine + callbacks (consistente con BreathingMinigame, PlayerWakeUp, etc.).
    // Politica: best-effort. Errores logean warning, NO bloquean el juego.
    public class ApiClient : MonoBehaviour
    {
        public const string BaseUrl = "http://estandentro.somee.com";
        private const int TimeoutSeconds = 5;

        private static ApiClient _instance;
        public static ApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[ApiClient]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ApiClient>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ---------- Jugador ----------

        public Coroutine CreateJugador(JugadorCreateDto data, Action<JugadorDto> onSuccess, Action<string> onError)
            => StartCoroutine(PostJson<JugadorCreateDto, JugadorDto>("/api/Jugadores", data, onSuccess, onError));

        public Coroutine GetJugador(int id, Action<JugadorDto> onSuccess, Action<string> onError)
            => StartCoroutine(GetJson<JugadorDto>($"/api/Jugadores/{id}", onSuccess, onError));

        public Coroutine UpdateJugador(JugadorDto data, Action onSuccess, Action<string> onError)
            => StartCoroutine(PutJson($"/api/Jugadores/{data.idJugador}", data, onSuccess, onError));

        public Coroutine GetAllJugadores(Action<JugadorDto[]> onSuccess, Action<string> onError)
            => StartCoroutine(GetArray<JugadorDto, JugadorListWrapper>("/api/Jugadores", w => w.items, onSuccess, onError));

        // ---------- Partida ----------

        public Coroutine CreatePartida(PartidaCreateDto data, Action<PartidaDto> onSuccess, Action<string> onError)
            => StartCoroutine(PostJson<PartidaCreateDto, PartidaDto>("/api/Partidas", data, onSuccess, onError));

        public Coroutine UpdatePartida(PartidaDto data, Action onSuccess, Action<string> onError)
            => StartCoroutine(PutJson($"/api/Partidas/{data.idPartida}", data, onSuccess, onError));

        public Coroutine GetAllPartidas(Action<PartidaDto[]> onSuccess, Action<string> onError)
            => StartCoroutine(GetArray<PartidaDto, PartidaListWrapper>("/api/Partidas", w => w.items, onSuccess, onError));

        public Coroutine GetPartida(int id, Action<PartidaDto> onSuccess, Action<string> onError)
            => StartCoroutine(GetJson<PartidaDto>($"/api/Partidas/{id}", onSuccess, onError));

        public Coroutine DeletePartida(int id, Action onSuccess, Action<string> onError)
            => StartCoroutine(Delete($"/api/Partidas/{id}", onSuccess, onError));

        // ---------- Logro ----------

        public Coroutine GetAllLogros(Action<LogroDto[]> onSuccess, Action<string> onError)
            => StartCoroutine(GetArray<LogroDto, LogroListWrapper>("/api/Logros", w => w.items, onSuccess, onError));

        // ---------- LogroXPartida ----------

        public Coroutine UnlockLogro(int idPartida, int idLogro, Action onSuccess, Action<string> onError)
        {
            var data = new LogroXPartidaCreateDto { idPartida = idPartida, idLogro = idLogro };
            return StartCoroutine(PostJsonNoResponse("/api/LogroXPartida", data, onSuccess, onError, treatDuplicateAsSuccess: true));
        }

        public Coroutine GetAllLogroXPartida(Action<LogroXPartidaDto[]> onSuccess, Action<string> onError)
            => StartCoroutine(GetArray<LogroXPartidaDto, LogroXPartidaListWrapper>("/api/LogroXPartida", w => w.items, onSuccess, onError));

        // ---------- Internals ----------

        private IEnumerator GetJson<TResponse>(string path, Action<TResponse> onSuccess, Action<string> onError)
        {
            using var request = UnityWebRequest.Get(BaseUrl + path);
            request.timeout = TimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"GET {path} fallo: {request.error} ({request.responseCode})");
                yield break;
            }

            TResponse parsed;
            try { parsed = JsonUtility.FromJson<TResponse>(request.downloadHandler.text); }
            catch (Exception e) { onError?.Invoke($"GET {path} parse error: {e.Message}"); yield break; }
            onSuccess?.Invoke(parsed);
        }

        private IEnumerator GetArray<TItem, TWrapper>(string path, Func<TWrapper, TItem[]> selector, Action<TItem[]> onSuccess, Action<string> onError)
        {
            using var request = UnityWebRequest.Get(BaseUrl + path);
            request.timeout = TimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"GET {path} fallo: {request.error} ({request.responseCode})");
                yield break;
            }

            TItem[] items;
            try
            {
                // Workaround: JsonUtility no parsea arrays JSON root. Envolvemos en {"items": [...]}.
                string wrapped = "{\"items\":" + request.downloadHandler.text + "}";
                var wrapper = JsonUtility.FromJson<TWrapper>(wrapped);
                items = selector(wrapper) ?? Array.Empty<TItem>();
            }
            catch (Exception e) { onError?.Invoke($"GET {path} parse error: {e.Message}"); yield break; }
            onSuccess?.Invoke(items);
        }

        private IEnumerator PostJson<TRequest, TResponse>(string path, TRequest data, Action<TResponse> onSuccess, Action<string> onError)
        {
            string json = JsonUtility.ToJson(data);
            using var request = BuildJsonRequest(path, "POST", json);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"POST {path} fallo: {request.error} ({request.responseCode}) body={request.downloadHandler?.text}");
                yield break;
            }

            TResponse parsed;
            try { parsed = JsonUtility.FromJson<TResponse>(request.downloadHandler.text); }
            catch (Exception e) { onError?.Invoke($"POST {path} parse error: {e.Message}"); yield break; }
            onSuccess?.Invoke(parsed);
        }

        private IEnumerator PostJsonNoResponse<TRequest>(string path, TRequest data, Action onSuccess, Action<string> onError, bool treatDuplicateAsSuccess = false)
        {
            string json = JsonUtility.ToJson(data);
            using var request = BuildJsonRequest(path, "POST", json);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string body = request.downloadHandler?.text ?? "";
                if (treatDuplicateAsSuccess && IsDuplicateKeyError(request.responseCode, body))
                {
                    onSuccess?.Invoke();
                    yield break;
                }
                onError?.Invoke($"POST {path} fallo: {request.error} ({request.responseCode}) body={body}");
                yield break;
            }

            onSuccess?.Invoke();
        }

        private IEnumerator PutJson<TRequest>(string path, TRequest data, Action onSuccess, Action<string> onError)
        {
            string json = JsonUtility.ToJson(data);
            using var request = BuildJsonRequest(path, "PUT", json);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"PUT {path} fallo: {request.error} ({request.responseCode}) body={request.downloadHandler?.text}");
                yield break;
            }

            onSuccess?.Invoke();
        }

        private IEnumerator Delete(string path, Action onSuccess, Action<string> onError)
        {
            using var request = UnityWebRequest.Delete(BaseUrl + path);
            request.timeout = TimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"DELETE {path} fallo: {request.error} ({request.responseCode})");
                yield break;
            }

            onSuccess?.Invoke();
        }

        private static UnityWebRequest BuildJsonRequest(string path, string verb, string json)
        {
            var request = new UnityWebRequest(BaseUrl + path, verb)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = TimeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            return request;
        }

        private static bool IsDuplicateKeyError(long status, string body)
        {
            if (status != 500 && status != 409) return false;
            if (string.IsNullOrEmpty(body)) return false;
            return body.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || body.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
                || body.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
        }
    }
}
