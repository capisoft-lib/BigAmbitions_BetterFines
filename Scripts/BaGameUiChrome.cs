using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterFines
{
    /// <summary>Minimal VoogleRoute-style panel chrome using in-game sprites when available.</summary>
    internal static class BaGameUiChrome
    {
        internal const float PanelWidth = 370f;
        internal const float ScreenMarginX = 16f;
        internal const float ScreenMarginMinY = 36f;
        internal const float HeaderHeight = 48f;
        internal const float ContentInset = 18f;
        internal const float FrameBleedWidth = 24f;
        internal const float FrameBleedHeight = 26f;
        internal const float FrameOffsetX = -2f;
        internal const float FrameOffsetY = -13f;

        internal static readonly Color TitleColor = new Color(0.15f, 0.17f, 0.22f, 1f);
        internal static readonly Color BodyTextColor = new Color(0.92f, 0.94f, 0.96f, 1f);
        internal static readonly Color MutedTextColor = new Color(0.72f, 0.76f, 0.8f, 1f);
        internal static readonly Color WarningTextColor = new Color(1f, 0.55f, 0.45f, 1f);

        private static Sprite _panelBg;
        private static Sprite _headerBg;
        private static TMP_FontAsset _fontRegular;
        private static TMP_FontAsset _fontBold;
        private static bool _discovered;

        internal static void EnsureInitialized()
        {
            if (_discovered)
                return;

            _discovered = true;
            DiscoverAssets();
        }

        internal static void SetupOverlayCanvas(GameObject root, int sortingOrder)
        {
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var group = root.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        internal static RectTransform BuildPanel(Transform parent, float panelWidth, float panelHeight, string panelName, out RectTransform header)
        {
            EnsureInitialized();

            var scale = panelWidth / PanelWidth;
            var panel = CreateRect(parent, panelName);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(panelWidth, panelHeight);

            var background = CreateRect(panel, "Background");
            ApplyBodyFrame(background, scale);
            var bgImage = background.gameObject.AddComponent<Image>();
            bgImage.raycastTarget = false;
            ApplyPanelBg(bgImage);

            header = CreateRect(panel, "Header");
            ApplyHeaderFrame(header, scale);
            var headerImage = header.gameObject.AddComponent<Image>();
            headerImage.raycastTarget = false;
            ApplyHeaderBg(headerImage);

            return panel;
        }

        internal static void ApplyTitleStyle(TextMeshProUGUI text, float scale)
        {
            text.fontSize = 18f * scale;
            text.fontStyle = FontStyles.Bold;
            text.color = TitleColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            ApplyTitleFont(text);
        }

        internal static void ApplyBodyStyle(TextMeshProUGUI text, float scale, bool muted = false)
        {
            text.fontSize = 14f * scale;
            text.fontStyle = FontStyles.Normal;
            text.color = muted ? MutedTextColor : BodyTextColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            ApplyTitleFont(text);
        }

        internal static Vector2 GetFallbackScreenPosition(float offsetY)
        {
            var y = offsetY > 0f ? Mathf.Max(ScreenMarginMinY, offsetY) : ScreenMarginMinY;
            return new Vector2(ScreenMarginX, y);
        }

        private static void ApplyBodyFrame(RectTransform rect, float scale)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(FrameOffsetX * scale, FrameOffsetY * scale);
            rect.sizeDelta = new Vector2(FrameBleedWidth * scale, FrameBleedHeight * scale);
        }

        private static void ApplyHeaderFrame(RectTransform header, float scale)
        {
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = new Vector2(-1.5f * scale, 0f);
            header.sizeDelta = new Vector2(-9f * scale, HeaderHeight * scale);
        }

        private static void ApplyPanelBg(Image image)
        {
            ApplySliced(image, _panelBg, new Color(0.2f, 0.24f, 0.3f, 1f), Color.white);
            image.pixelsPerUnitMultiplier = 2.45f;
        }

        private static void ApplyHeaderBg(Image image)
        {
            ApplySliced(image, _headerBg, new Color(0.78f, 0.8f, 0.83f, 1f), Color.white);
            image.pixelsPerUnitMultiplier = 2.45f;
        }

        private static void ApplySliced(Image image, Sprite sprite, Color fallbackTint, Color spriteTint)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = spriteTint;
                var border = sprite.border;
                image.type = border.x > 0.01f || border.y > 0.01f || border.z > 0.01f || border.w > 0.01f
                    ? Image.Type.Sliced
                    : Image.Type.Simple;
            }
            else
            {
                image.color = fallbackTint;
            }

            image.pixelsPerUnitMultiplier = 1f;
            image.preserveAspect = false;
        }

        private static void ApplyTitleFont(TextMeshProUGUI text)
        {
            var font = _fontRegular != null ? _fontRegular : _fontBold;
            if (font != null)
                text.font = font;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void DiscoverAssets()
        {
            try
            {
                foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    CaptureSprite(sprite);
                    if (HasAllAssets())
                        return;
                }

                foreach (var image in Resources.FindObjectsOfTypeAll<Image>())
                {
                    if (image != null)
                        CaptureSprite(image.sprite);
                    if (HasAllAssets())
                        return;
                }
            }
            catch
            {
                // Sprites may not be loaded yet.
            }

            try
            {
                foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                {
                    if (font == null)
                        continue;

                    if (font.name == "Rubik-Regular SDF" && _fontRegular == null)
                        _fontRegular = font;
                    else if (font.name == "Rubik-Bold SDF" && _fontBold == null)
                        _fontBold = font;

                    if (HasAllAssets())
                        return;
                }
            }
            catch
            {
                // Fonts may not be loaded yet.
            }
        }

        private static bool HasAllAssets() =>
            _panelBg != null && _headerBg != null && (_fontRegular != null || _fontBold != null);

        private static void CaptureSprite(Sprite sprite)
        {
            if (sprite == null)
                return;

            if (sprite.name == "grey-round-bordered" && _panelBg == null)
                _panelBg = sprite;
            if (sprite.name == "darkgreybox-header@2x" && _headerBg == null)
                _headerBg = sprite;
        }
    }
}
