using System.Text;
using Capisoft.Lib.BaUnifiedUI.Chrome;
using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using Capisoft.Lib.BaUnifiedUI.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterFines
{
    internal static class FinesStatusPanel
    {
        private const string RootName = "BetterFines_StatusPanel_v2";
        private const int CanvasSortOrder = 8999;
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
        private static float _lastPanelHeight;
        private static Vector2 _lastAnchoredPosition = new Vector2(float.NaN, float.NaN);
        private static RectTransform _cachedVooglePanel;
        private static float _nextVoogleLookupAt;

        internal static void EnsureCreated()
        {
            DestroyIfStale();
            if (_root != null)
                return;

            BaUi.EnsureReady();
            BaUiPanelHost.PurgeNamedRoots("BetterFines_StatusPanel_v1");

            var built = BaUi.Overlay(RootName, CanvasSortOrder)
                .NonInteractive()
                .Dock(BaDock.BottomLeft)
                .Panel(BaPanelRecipe.ActionPanel, BaUiLayout.PanelWidth, height: ComputePanelHeight(1, 1, false))
                .Header(h => h.TitleLeft(ModUiText.FinesPanelTitle))
                .Body(_ => { })
                .Build();

            _root = built.Root;
            _panelRect = built.Panel;
            _titleLabel = built.Header.Find("Title")?.GetComponent<TextMeshProUGUI>();

            var bodyRect = built.Body;
            var bodyTextGo = new GameObject("BodyText", typeof(RectTransform));
            bodyTextGo.transform.SetParent(bodyRect, false);
            var bodyTextRect = bodyTextGo.GetComponent<RectTransform>();
            bodyTextRect.anchorMin = Vector2.zero;
            bodyTextRect.anchorMax = Vector2.one;
            bodyTextRect.offsetMin = new Vector2(0f, 0f);
            bodyTextRect.offsetMax = new Vector2(0f, 0f);
            _bodyLabel = bodyTextGo.AddComponent<TextMeshProUGUI>();
            BaUiControls.ApplyBodyLabelStyle(_bodyLabel, built.Scale);
            _bodyLabel.enableWordWrapping = true;
            _bodyLabel.richText = true;
            _bodyLabel.alignment = TextAlignmentOptions.TopLeft;

            DisablePanelRaycasts(_panelRect);
            BaUi.ApplyLayer(_root);

            _lastPanelHeight = 0f;
            _lastAnchoredPosition = new Vector2(float.NaN, float.NaN);
            _root.SetActive(false);
            _lastVisible = false;

            if (BaUi.ShouldRebuildChrome)
                BaUi.MarkRebuildHandled();

            ModLog.Info("Fines status panel created (" + RootName + ").");
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
            if (_titleLabel != null && _titleLabel.text != title)
                _titleLabel.text = title;

            var body = BuildBodyText();
            if (body != _lastBody)
            {
                _lastBody = body;
                if (_bodyLabel != null)
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
            _lastPanelHeight = 0f;
            _lastAnchoredPosition = new Vector2(float.NaN, float.NaN);
            _cachedVooglePanel = null;
            _nextVoogleLookupAt = 0f;
        }

        private static void DestroyIfStale()
        {
            if (_root == null)
                return;

            if (!BaUiPanelHost.ShouldRecreate(_root, RootName))
                return;

            Destroy();
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

            var panelHeight = ComputePanelHeight(activeCount, summaryLines, RecidivismService.IsLicenseSuspended);
            if (!Mathf.Approximately(_lastPanelHeight, panelHeight))
            {
                _lastPanelHeight = panelHeight;
                _panelRect.sizeDelta = new Vector2(BaUiLayout.PanelWidth, panelHeight);
                BaUiWidgets.RestoreDockedPanelChrome(_panelRect, BaUiLayout.PanelWidth);
            }

            var position = ResolveScreenPosition();
            if (!Approximately(_lastAnchoredPosition, position))
            {
                _lastAnchoredPosition = position;
                _panelRect.anchoredPosition = position;
            }
        }

        private static float ComputePanelHeight(int activeCount, int summaryLines, bool licenseSuspended)
        {
            if (licenseSuspended && activeCount <= 0)
                summaryLines = Mathf.Max(summaryLines, 1);

            var bodyHeight = Mathf.Max(1, activeCount) * RowHeight + summaryLines * SummaryLineHeight + 8f;
            return BaUiLayout.HeaderHeight
                   + BaUiLayout.BodyTopPadding
                   + BaUiLayout.BodyBottomPadding
                   + bodyHeight;
        }

        private static Vector2 ResolveScreenPosition()
        {
            var vooglePanel = FindVooglePanelRect();
            if (vooglePanel != null)
            {
                var y = vooglePanel.anchoredPosition.y + vooglePanel.rect.height + PanelGapAboveVoogle;
                return new Vector2(BaUiLayout.ScreenMarginX, y);
            }

            var fallbackY = BaUiLayout.ScreenMarginMinY + DefaultVoogleHeight + PanelGapAboveVoogle;
            return BaUiLayout.GetScreenPosition(fallbackY);
        }

        private static RectTransform FindVooglePanelRect()
        {
            if (_cachedVooglePanel != null)
                return _cachedVooglePanel;

            if (Time.unscaledTime < _nextVoogleLookupAt)
                return null;

            _nextVoogleLookupAt = Time.unscaledTime + 2f;

            var root = GameObject.Find("VoogleRoute_ActionPanel_v85")
                       ?? GameObject.Find("VoogleRoute_HudRoot_v55");
            if (root == null)
                return null;

            var navPanel = root.transform.Find("Panel") ?? root.transform.Find("NavPanel");
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

        private static void DisablePanelRaycasts(RectTransform panel)
        {
            if (panel == null)
                return;

            foreach (var graphic in panel.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private static bool Approximately(Vector2 a, Vector2 b) =>
            Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    }
}
