using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterFines
{
    /// <summary>
    /// Minimal HUD that deliberately has no LIB_BaUnifiedUI dependency. It is only used when
    /// another mod caused an incompatible shared UI assembly to win Unity's load order.
    /// </summary>
    internal static class FinesStatusFallbackPanel
    {
        private const string RootName = "BetterFines_StatusPanel_Compatibility";
        private const float PanelWidth = 310f;
        private const float HeaderHeight = 32f;
        private const float RowHeight = 22f;
        private const float SummaryLineHeight = 20f;
        private const float ScreenMarginX = 14f;
        private const float ScreenMarginY = 117f;
        private const float PanelGapAboveVoogle = 8f;

        private static GameObject _root;
        private static RectTransform _panel;
        private static TextMeshProUGUI _title;
        private static TextMeshProUGUI _body;
        private static string _lastTitle = string.Empty;
        private static string _lastBody = string.Empty;
        private static float _lastHeight;

        internal static void UpdateDisplay(
            string title,
            string body,
            int activeCount,
            int summaryLines,
            bool licenseSuspended)
        {
            EnsureCreated();

            var height = ComputeHeight(activeCount, summaryLines, licenseSuspended);
            if (!Mathf.Approximately(height, _lastHeight))
            {
                _lastHeight = height;
                _panel.sizeDelta = new Vector2(PanelWidth, height);
            }

            _panel.anchoredPosition = ResolveScreenPosition();

            if (_lastTitle != title)
            {
                _lastTitle = title;
                _title.text = title;
            }

            if (_lastBody != body)
            {
                _lastBody = body;
                _body.text = body;
            }

            if (!_root.activeSelf)
                _root.SetActive(true);
        }

        internal static void Hide()
        {
            if (_root != null && _root.activeSelf)
                _root.SetActive(false);
        }

        internal static void InvalidateText()
        {
            _lastTitle = string.Empty;
            _lastBody = string.Empty;
        }

        internal static void Destroy()
        {
            if (_root != null)
                Object.Destroy(_root);

            _root = null;
            _panel = null;
            _title = null;
            _body = null;
            _lastHeight = 0f;
            InvalidateText();
        }

        private static void EnsureCreated()
        {
            if (_root != null)
                return;

            var stale = GameObject.Find(RootName);
            if (stale != null)
                Object.Destroy(stale);

            _root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Object.DontDestroyOnLoad(_root);

            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 8999;

            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var panelGo = CreateRect(_root.transform, "Panel");
            _panel = panelGo.GetComponent<RectTransform>();
            _panel.anchorMin = Vector2.zero;
            _panel.anchorMax = Vector2.zero;
            _panel.pivot = Vector2.zero;
            _panel.sizeDelta = new Vector2(PanelWidth, 100f);

            var background = panelGo.AddComponent<Image>();
            background.color = new Color32(30, 37, 43, 246);
            background.raycastTarget = false;

            var headerGo = CreateRect(_panel, "Header");
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = Vector2.one;
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, HeaderHeight);
            headerRect.anchoredPosition = Vector2.zero;

            var headerBackground = headerGo.AddComponent<Image>();
            headerBackground.color = new Color32(20, 25, 30, 255);
            headerBackground.raycastTarget = false;

            _title = CreateLabel(headerRect, "Title", 15f, FontStyles.Bold);
            var titleRect = _title.rectTransform;
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(12f, 0f);
            titleRect.offsetMax = new Vector2(-8f, 0f);
            _title.alignment = TextAlignmentOptions.MidlineLeft;
            _title.color = new Color32(238, 241, 243, 255);

            _body = CreateLabel(_panel, "Body", 14f, FontStyles.Normal);
            var bodyRect = _body.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(12f, 10f);
            bodyRect.offsetMax = new Vector2(-10f, -(HeaderHeight + 8f));
            _body.alignment = TextAlignmentOptions.TopLeft;
            _body.color = new Color32(224, 228, 232, 255);
            _body.enableWordWrapping = true;
            _body.richText = true;

            _root.SetActive(false);
            ModLog.Info("Compatibility fines status HUD created.");
        }

        private static GameObject CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            float fontSize,
            FontStyles style)
        {
            var go = CreateRect(parent, name);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.raycastTarget = false;
            return label;
        }

        private static float ComputeHeight(int activeCount, int summaryLines, bool licenseSuspended)
        {
            if (licenseSuspended && activeCount <= 0)
                summaryLines = Mathf.Max(summaryLines, 1);

            return HeaderHeight
                   + 18f
                   + Mathf.Max(1, activeCount) * RowHeight
                   + summaryLines * SummaryLineHeight;
        }

        private static Vector2 ResolveScreenPosition()
        {
            var voogleRoot = GameObject.Find("VoogleRoute_ActionPanel_v85")
                             ?? GameObject.Find("VoogleRoute_HudRoot_v55");
            if (voogleRoot != null)
            {
                var navPanel = voogleRoot.transform.Find("Panel") ?? voogleRoot.transform.Find("NavPanel");
                var rect = navPanel != null ? navPanel.GetComponent<RectTransform>() : null;
                if (rect != null)
                {
                    return new Vector2(
                        ScreenMarginX,
                        rect.anchoredPosition.y + rect.rect.height + PanelGapAboveVoogle);
                }
            }

            return new Vector2(ScreenMarginX, ScreenMarginY);
        }
    }
}
