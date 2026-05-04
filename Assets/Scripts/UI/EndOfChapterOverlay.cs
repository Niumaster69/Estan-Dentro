using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using EstanDentro.Network;

namespace EstanDentro.UI
{
    // Pantalla de resumen al fin de capitulo (completado o Game Over).
    // Muestra: tiempo total, logros desbloqueados con puntos, logros pendientes con descripcion, puntos totales.
    // Cero llamadas API: todo se calcula desde GameSession + LogroByIdCache (catalogo precargado).
    // Detalle en Document/Flujo_API_Estan_Dentro.md seccion 4c.
    public class EndOfChapterOverlay : MonoBehaviour
    {
        private static EndOfChapterOverlay instance;

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.96f);
        [SerializeField] private Color titleCompletadoColor = new Color(0.55f, 0.85f, 0.45f, 1f);
        [SerializeField] private Color titleAbandonadoColor = new Color(0.85f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color textColor = new Color(0.92f, 0.89f, 0.83f, 0.95f);
        [SerializeField] private Color labelColor = new Color(0.92f, 0.89f, 0.83f, 0.7f);
        [SerializeField] private Color accentColor = new Color(0.85f, 0.7f, 0.28f, 0.95f);
        [SerializeField] private Color unlockedColor = new Color(0.55f, 0.85f, 0.45f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.6f, 0.6f, 0.6f, 0.85f);
        [SerializeField] private Color buttonNormalColor = new Color(0.10f, 0.10f, 0.10f, 0.9f);
        [SerializeField] private Color buttonHoverColor = new Color(0.85f, 0.7f, 0.28f, 0.7f);
        [SerializeField] private Color buttonPressedColor = new Color(0.62f, 0.12f, 0.12f, 0.9f);

        // ---------- runtime ----------

        private Canvas canvas;
        private Image bgImage;
        private RectTransform panel;
        private Text titleText;
        private Text tiempoText;
        private RectTransform unlockedContent;
        private RectTransform lockedContent;
        private Text puntosText;
        private Action onMenuCallback;
        private float prevTimeScale;
        private CursorLockMode prevCursorLock;
        private bool prevCursorVisible;
        private bool consumeInputThisFrame;

        // ---------- public API ----------

        // completado: true si gano (capitulo completado), false si Game Over (StressSystem.OnCollapse).
        // onMenu: que ejecutar al pulsar "Volver al Menu" (ej. SceneManager.LoadScene("MainMenu")).
        public static void Open(bool completado, Action onMenu)
        {
            EnsureInstance();
            if (instance.canvas.gameObject.activeSelf) return;

            instance.prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            instance.prevCursorLock = Cursor.lockState;
            instance.prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            instance.onMenuCallback = onMenu;
            instance.canvas.gameObject.SetActive(true);
            instance.consumeInputThisFrame = true;
            OverlayBlocker.Register();

            instance.PopulateContent(completado);
        }

        public static void Close()
        {
            if (instance == null || !instance.canvas.gameObject.activeSelf) return;
            instance.canvas.gameObject.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
            Time.timeScale = instance.prevTimeScale > 0f ? instance.prevTimeScale : 1f;
            Cursor.lockState = instance.prevCursorLock;
            Cursor.visible = instance.prevCursorVisible;
            OverlayBlocker.Unregister();
        }

        // ---------- lifecycle ----------

        private static void EnsureInstance()
        {
            if (instance != null && instance.canvas != null) return;
            if (instance != null) Destroy(instance.gameObject);
            var go = new GameObject("__EndOfChapterOverlay");
            instance = go.AddComponent<EndOfChapterOverlay>();
            instance.Build();
            instance.canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (instance == this && canvas != null && canvas.gameObject.activeSelf)
                OverlayBlocker.Unregister();
        }

        private void Update()
        {
            if (canvas == null || !canvas.gameObject.activeSelf) return;
            if (consumeInputThisFrame) { consumeInputThisFrame = false; return; }
            // No respondemos a ESC: hay que elegir Volver al Menu o Reintentar explicitamente.
        }

        // ---------- content ----------

        private void PopulateContent(bool completado)
        {
            // Titulo
            if (completado)
            {
                titleText.text = "CAPITULO 1 — COMPLETADO";
                titleText.color = titleCompletadoColor;
            }
            else
            {
                titleText.text = "CAPITULO 1 — ABANDONADO";
                titleText.color = titleAbandonadoColor;
            }

            // Tiempo
            int segundos = Mathf.Max(0, (int)(DateTime.UtcNow - GameSession.PartidaStartTime).TotalSeconds);
            tiempoText.text = $"Tiempo: {FormatTiempo(segundos)}";

            // Logros desbloqueados / pendientes
            ClearChildren(unlockedContent);
            ClearChildren(lockedContent);

            int puntosObtenidos = 0;
            int puntosTotales = 0;
            int unlockedCount = 0;
            int lockedCount = 0;

            // Iterar el catalogo y separar
            foreach (var kv in GameSession.LogroIdByCodigo)
            {
                int idLogro = kv.Value;
                if (!GameSession.LogroByIdCache.TryGetValue(idLogro, out var logroData)) continue;
                puntosTotales += logroData.puntos;
                bool desbloqueado = GameSession.UnlockedLogrosThisPartida.Contains(idLogro);
                if (desbloqueado)
                {
                    puntosObtenidos += logroData.puntos;
                    BuildLogroRow(unlockedContent, logroData, true);
                    unlockedCount++;
                }
                else
                {
                    BuildLogroRow(lockedContent, logroData, false);
                    lockedCount++;
                }
            }

            if (unlockedCount == 0)
                BuildEmptyRow(unlockedContent, "Ningun logro desbloqueado en esta partida.");
            if (lockedCount == 0)
                BuildEmptyRow(lockedContent, "¡Todos los logros desbloqueados!");

            // Puntos totales
            puntosText.text = $"PUNTOS: {puntosObtenidos} / {puntosTotales}";
        }

        private void BuildLogroRow(Transform parent, LogroDto logro, bool unlocked)
        {
            var rowGo = new GameObject("Logro_" + logro.codigo, typeof(RectTransform));
            var rt = rowGo.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = unlocked ? 28f : 46f;

            var t = rowGo.AddComponent<Text>();
            t.font = GetDefaultFont();
            t.fontSize = 16;
            t.color = unlocked ? unlockedColor : lockedColor;
            t.alignment = TextAnchor.UpperLeft;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;

            if (unlocked)
            {
                t.text = $"<b>🔓 {logro.nombreLogro}</b>     <color=#{ColorUtility.ToHtmlStringRGB(accentColor)}>+{logro.puntos}</color>";
            }
            else
            {
                string desc = string.IsNullOrEmpty(logro.descripcion) ? "" : $"\n   <i>\"{logro.descripcion}\"</i>";
                t.text = $"<b>🔒 {logro.nombreLogro}</b>{desc}";
            }
        }

        private void BuildEmptyRow(Transform parent, string msg)
        {
            var rowGo = new GameObject("Empty", typeof(RectTransform));
            var rt = rowGo.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;

            var t = rowGo.AddComponent<Text>();
            t.font = GetDefaultFont();
            t.fontSize = 14;
            t.color = new Color(textColor.r, textColor.g, textColor.b, 0.55f);
            t.alignment = TextAnchor.MiddleLeft;
            t.fontStyle = FontStyle.Italic;
            t.text = msg;
            t.raycastTarget = false;
        }

        private void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);
        }

        // ---------- BUILD UI ----------

        private void Build()
        {
            BuildCanvas();
            BuildHeader();
            BuildSections();
            BuildBottomButtons();
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("EndOfChapter_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 230;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var bgGo = new GameObject("BG", typeof(RectTransform));
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.SetParent(canvas.transform, false);
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgImage = bgGo.AddComponent<Image>();
            bgImage.color = bgColor;

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panel = panelGo.GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero; panel.offsetMax = Vector2.zero;
        }

        private void BuildHeader()
        {
            titleText = MakeText(panel, "Title", "CAPITULO", 56, FontStyle.Bold, titleColor,
                new Vector2(0f, 380f), new Vector2(1400f, 80f));
            var sh = titleText.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.85f);
            sh.effectDistance = new Vector2(3f, -4f);

            tiempoText = MakeText(panel, "Tiempo", "", 28, FontStyle.Normal, textColor,
                new Vector2(0f, 310f), new Vector2(800f, 40f));
        }

        private void BuildSections()
        {
            // LOGROS DESBLOQUEADOS (izquierda)
            var unlockedTitle = MakeText(panel, "UnlockedTitle", "LOGROS DESBLOQUEADOS", 22, FontStyle.Bold,
                accentColor, new Vector2(-360f, 240f), new Vector2(440f, 30f));
            unlockedTitle.alignment = TextAnchor.MiddleLeft;

            unlockedContent = BuildScrollContent("UnlockedScroll", new Vector2(-360f, 30f), new Vector2(540f, 380f));

            // LOGROS PENDIENTES (derecha)
            var lockedTitle = MakeText(panel, "LockedTitle", "LOGROS PENDIENTES", 22, FontStyle.Bold,
                accentColor, new Vector2(280f, 240f), new Vector2(440f, 30f));
            lockedTitle.alignment = TextAnchor.MiddleLeft;

            lockedContent = BuildScrollContent("LockedScroll", new Vector2(280f, 30f), new Vector2(540f, 380f));
        }

        private RectTransform BuildScrollContent(string name, Vector2 pos, Vector2 size)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform));
            var scrollRT = scrollGo.GetComponent<RectTransform>();
            scrollRT.SetParent(panel, false);
            scrollRT.anchorMin = scrollRT.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRT.pivot = new Vector2(0.5f, 0.5f);
            scrollRT.sizeDelta = size;
            scrollRT.anchoredPosition = pos;

            var bg = scrollGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.25f);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewportRT = viewportGo.GetComponent<RectTransform>();
            viewportRT.SetParent(scrollRT, false);
            viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero; viewportRT.offsetMax = Vector2.zero;
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0);
            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var content = contentGo.GetComponent<RectTransform>();
            content.SetParent(viewportRT, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 0f);
            content.anchoredPosition = Vector2.zero;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(12, 12, 8, 8);
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRT;
            scroll.content = content;

            return content;
        }

        private void BuildBottomButtons()
        {
            puntosText = MakeText(panel, "PuntosTotales", "PUNTOS: 0 / 0", 32, FontStyle.Bold, accentColor,
                new Vector2(0f, -240f), new Vector2(800f, 50f));

            MakeButton(panel, "Btn_Menu", "Volver al Menu",
                new Vector2(0f, -340f), new Vector2(340f, 56f),
                () => { Close(); onMenuCallback?.Invoke(); });
        }

        // ---------- helpers ----------

        private static string FormatTiempo(int segundos)
        {
            int h = segundos / 3600;
            int m = (segundos % 3600) / 60;
            int s = segundos % 60;
            return h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
        }

        private Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.AddComponent<Image>().color = buttonNormalColor;
            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.normalColor = buttonNormalColor;
            c.highlightedColor = buttonHoverColor;
            c.pressedColor = buttonPressedColor;
            c.selectedColor = buttonHoverColor;
            c.fadeDuration = 0.1f;
            btn.colors = c;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(go.transform, false);
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = GetDefaultFont();
            lbl.text = label;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.fontSize = 18;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = textColor;
            lbl.raycastTarget = false;
            return btn;
        }

        private Text MakeText(Transform parent, string name, string content, int size, FontStyle style, Color color, Vector2 pos, Vector2 size2)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size2;
            rt.anchoredPosition = pos;
            var t = go.AddComponent<Text>();
            t.font = GetDefaultFont();
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private static Font GetDefaultFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
