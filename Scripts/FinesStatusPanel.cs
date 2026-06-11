using System.Text;
using TMPro;
using UnityEngine;

namespace BetterFines
{
    internal static class FinesStatusPanel
    {
        private const string RootName = "BetterFines_StatusPanel_v1";
        private const float PanelGapAboveVoogle = 8f;
        private const float DefaultVoogleHeight = 101f;
        private const float RowHeight = 22f;
        private const float SummaryLineHeight = 20f;

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static TextMeshProUGUI _titleLabel;
        private static TextMeshProUGUI _bodyLabel;
        private static bool _lastVisible;
        private static string _lastBody = string.Empty;
        private static RectTransform _cachedVooglePanel;
        private static float _nextVoogleLookupAt;

        internal static void EnsureCreated()
        {
            if (_root != null)
                return;

            BaGameUiChrome.EnsureInitialized();
            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            BaGameUiChrome.SetupOverlayCanvas(_root, 8999);

            _panelRect = BaGameUiChrome.BuildPanel(_root.transform, BaGameUiChrome.PanelWidth, 120f, "FinesPanel", out var header);
            _panelRect.anchorMin = _panelRect.anchorMax = Vector2.zero;
            _panelRect.pivot = Vector2.zero;

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(header, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(18f, 7f);
            titleRect.offsetMax = new Vector2(-18f, -7f);
            _titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyTitleStyle(_titleLabel, 1f);

            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(_panelRect, false);
            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(18f, 10f);
            bodyRect.offsetMax = new Vector2(-18f, -56f);
            _bodyLabel = bodyGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyBodyStyle(_bodyLabel, 1f);
            _bodyLabel.enableWordWrapping = true;
            _bodyLabel.richText = true;

            _root.SetActive(false);
            _lastVisible = false;
        }

        internal static void UpdateDisplay()
        {
            var visible = ShouldShow();
            if (!visible)
            {
                if (_root != null && _root.activeSelf)
                    _root.SetActive(false);
                _lastVisible = false;
                return;
            }

            EnsureCreated();
            ApplyLayout();

            if (!_lastVisible)
            {
                _lastVisible = true;
                _root.SetActive(true);
            }

            var title = ModUiText.FormatFinesPanelTitle(FineRecordStore.ActiveCount);
            if (_titleLabel.text != title)
                _titleLabel.text = title;

            var body = BuildBodyText();
            if (body != _lastBody)
            {
                _lastBody = body;
                _bodyLabel.text = body;
            }
        }

        internal static void RefreshLocalizedText()
        {
            _lastBody = string.Empty;
            UpdateDisplay();
        }

        internal static void Destroy()
        {
            if (_root == null)
                return;

            Object.Destroy(_root);
            _root = null;
            _panelRect = null;
            _titleLabel = null;
            _bodyLabel = null;
            _lastVisible = false;
            _lastBody = string.Empty;
            _cachedVooglePanel = null;
            _nextVoogleLookupAt = 0f;
        }

        private static bool ShouldShow()
        {
            if (!GameState.ShouldShowFinesPanel())
                return false;

            return FineRecordStore.ActiveCount > 0 || RecidivismService.IsLicenseSuspended;
        }

        private static void ApplyLayout()
        {
            if (_panelRect == null)
                return;

            var activeCount = FineRecordStore.ActiveCount;
            var summaryLines = 1;
            if (RecidivismService.GetCurrentSurchargePercent() > 0 || GetDisplayedSurchargePercent() > 0)
                summaryLines++;
            if (RecidivismService.IsLicenseSuspended)
                summaryLines++;

            var bodyHeight = Mathf.Max(1, activeCount) * RowHeight + summaryLines * SummaryLineHeight + 8f;
            var panelHeight = BaGameUiChrome.HeaderHeight + bodyHeight + 12f;
            if (_panelRect.sizeDelta.y != panelHeight)
                _panelRect.sizeDelta = new Vector2(BaGameUiChrome.PanelWidth, panelHeight);

            _panelRect.anchoredPosition = ResolveScreenPosition();
        }

        private static Vector2 ResolveScreenPosition()
        {
            var vooglePanel = FindVooglePanelRect();
            if (vooglePanel != null)
            {
                var y = vooglePanel.anchoredPosition.y + vooglePanel.rect.height + PanelGapAboveVoogle;
                return new Vector2(BaGameUiChrome.ScreenMarginX, y);
            }

            var fallbackY = BaGameUiChrome.ScreenMarginMinY + DefaultVoogleHeight + PanelGapAboveVoogle;
            return BaGameUiChrome.GetFallbackScreenPosition(fallbackY);
        }

        private static RectTransform FindVooglePanelRect()
        {
            if (_cachedVooglePanel != null)
                return _cachedVooglePanel;

            if (Time.unscaledTime < _nextVoogleLookupAt)
                return null;

            _nextVoogleLookupAt = Time.unscaledTime + 2f;

            var root = GameObject.Find("VoogleRoute_HudRoot_v55");
            if (root == null)
                return null;

            var navPanel = root.transform.Find("NavPanel");
            _cachedVooglePanel = navPanel != null ? navPanel.GetComponent<RectTransform>() : null;
            return _cachedVooglePanel;
        }

        private static string BuildBodyText()
        {
            var save = SaveGameManager.Current;
            var currentDay = save != null ? save.Day : 0;
            var sb = new StringBuilder();

            foreach (var fine in FineRecordStore.ActiveFines)
            {
                if (!fine.IsActive(currentDay))
                    continue;

                sb.Append(ModUiText.FormatFineLine(
                    fine.Type,
                    fine.Amount,
                    fine.DaysRemaining(currentDay)));
                sb.Append('\n');
            }

            sb.Append("<color=#B8C0C8>");
            sb.Append(ModUiText.FormatTotal(FineRecordStore.TotalActiveAmount()));
            sb.Append("</color>");

            var surchargePercent = GetDisplayedSurchargePercent();
            if (surchargePercent > 0)
            {
                sb.Append("\n<color=#FF9A84>");
                sb.Append(ModUiText.FormatSurcharge(surchargePercent));
                sb.Append("</color>");
            }

            if (RecidivismService.IsLicenseSuspended)
            {
                sb.Append("\n<color=#FF9A84>");
                sb.Append(ModUiText.FormatLicenseSuspended(RecidivismService.DaysUntilLicenseRestored()));
                sb.Append("</color>");
            }

            return sb.ToString().TrimEnd();
        }

        private static int GetDisplayedSurchargePercent()
        {
            var activeCount = FineRecordStore.ActiveCount;
            if (activeCount <= 0)
                return 0;

            return RecidivismService.GetSurchargePercent(activeCount);
        }
    }
}
