using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterFines
{
    /// <summary>Warning banner shown during violation hold countdowns (same HUD area as Voogle recalc).</summary>
    internal static class SpeedWarningBanner
    {
        private enum ActiveWarning
        {
            None,
            Speeding,
            WrongWay,
            LicenseSuspended
        }

        private const string RootName = "BetterFines_OverSpeedBanner";
        private const float PanelWidth = 500f;
        private const float PanelHeight = 64f;
        private const float CenterYOffset = -140f;
        private const float LabelPaddingX = 18f;
        private const float LabelPaddingY = 12f;

        private static GameObject _root;
        private static GameObject _panel;
        private static Canvas _canvas;
        private static Image _panelImage;
        private static TextMeshProUGUI _label;
        private static ActiveWarning _activeWarning = ActiveWarning.None;
        private static bool _visible;
        private static string _cachedText = string.Empty;
        private static float _autoHideAt = -1f;
        private const float LicenseWarningDurationSec = 3f;

        internal static void EnsureCreated()
        {
            if (_root != null)
                return;

            BaGameUiChrome.EnsureInitialized();

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            BaGameUiChrome.SetupOverlayCanvas(_root, 9101);
            _canvas = _root.GetComponent<Canvas>();

            _panel = new GameObject("Panel", typeof(RectTransform));
            _panel.transform.SetParent(_root.transform, false);

            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, CenterYOffset);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            _panelImage = _panel.AddComponent<Image>();
            _panelImage.raycastTarget = false;
            BaGameUiChrome.ApplyPanelBackground(_panelImage);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(_panel.transform, false);

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(LabelPaddingX, LabelPaddingY);
            labelRect.offsetMax = new Vector2(-LabelPaddingX, -LabelPaddingY);

            _label = labelGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyWarningBannerStyle(_label);

            SetVisible(false);
        }

        internal static void ShowSpeeding() => Show(ActiveWarning.Speeding);

        internal static void ShowWrongWay() => Show(ActiveWarning.WrongWay);

        internal static void HideSpeeding()
        {
            if (_activeWarning != ActiveWarning.Speeding)
                return;

            Hide();
        }

        internal static void HideWrongWay()
        {
            if (_activeWarning != ActiveWarning.WrongWay)
                return;

            Hide();
        }

        internal static void ShowLicenseSuspended()
        {
            if (_activeWarning == ActiveWarning.LicenseSuspended && _visible)
                return;

            Show(ActiveWarning.LicenseSuspended);
            _autoHideAt = Time.unscaledTime + LicenseWarningDurationSec;
        }

        internal static void TickAutoHide()
        {
            if (_activeWarning != ActiveWarning.LicenseSuspended || _autoHideAt < 0f)
                return;

            if (Time.unscaledTime < _autoHideAt)
                return;

            Hide();
        }

        internal static void Hide()
        {
            if (_activeWarning == ActiveWarning.None && !_visible)
                return;

            _activeWarning = ActiveWarning.None;
            _autoHideAt = -1f;
            _cachedText = string.Empty;
            SetVisible(false);
        }

        internal static void RefreshLocalizedText()
        {
            if (_label == null || _activeWarning == ActiveWarning.None)
                return;

            ApplyText(ResolveText(_activeWarning));
        }

        internal static void Destroy()
        {
            Hide();

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _panel = null;
                _canvas = null;
                _panelImage = null;
                _label = null;
            }

            _visible = false;
            _cachedText = string.Empty;
        }

        private static void Show(ActiveWarning warning)
        {
            EnsureCreated();

            var text = ResolveText(warning);
            var warningChanged = _activeWarning != warning;
            _activeWarning = warning;

            if (warningChanged || !_visible)
                SetVisible(true);

            ApplyText(text);
        }

        private static void ApplyText(string text)
        {
            if (_label == null || text == _cachedText)
                return;

            _cachedText = text;
            _label.SetText(text);
        }

        private static void SetVisible(bool visible)
        {
            if (!visible)
            {
                _visible = false;
                if (_root == null)
                    return;

                if (_label != null)
                {
                    _label.SetText(string.Empty);
                    _label.ForceMeshUpdate(true);
                    _label.enabled = false;
                }

                if (_panel != null)
                    _panel.SetActive(false);

                _root.SetActive(false);
                return;
            }

            if (_visible && _root != null && _root.activeSelf)
                return;

            _visible = true;
            if (_root == null)
                return;

            _root.SetActive(true);
            if (_panel != null)
                _panel.SetActive(true);
            if (_panelImage != null)
                BaGameUiChrome.ApplyPanelBackground(_panelImage);
            if (_label != null)
                _label.enabled = true;
            if (_canvas != null)
                _canvas.enabled = true;
        }

        private static string ResolveText(ActiveWarning warning) =>
            warning switch
            {
                ActiveWarning.WrongWay => ModUiText.WrongWayWarning,
                ActiveWarning.LicenseSuspended => ModUiText.LicenseSuspendedWarning,
                _ => ModUiText.OverSpeedLimitWarning
            };
    }
}
