using UnityEngine;
using UnityEngine.UI;

namespace BetterFines
{
    /// <summary>Full-screen white flash shown when a traffic fine SMS is issued.</summary>
    internal static class RedLightCameraFlash
    {
        private const string RootName = "BetterFines_RedLightCameraFlash";
        private const float PeakAlpha = 0.94f;
        private const float PeakHoldSec = 0.05f;
        private const float FadeDurationSec = 0.38f;

        private static GameObject _root;
        private static Image _overlay;
        private static Sprite _whiteSprite;
        private static float _fadeStartAt = -1f;
        private static float _fadeEndAt = -1f;
        private static bool _active;

        internal static void TryPlay()
        {
            if (!BetterFinesConfig.VisualFlashEnabled)
                return;

            try
            {
                Play();
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("Visual flash failed: " + ex.Message);
            }
        }

        internal static void Play()
        {
            EnsureCreated();
            _overlay.color = new Color(1f, 1f, 1f, PeakAlpha);
            _root.SetActive(true);

            var now = Time.unscaledTime;
            _fadeStartAt = now + PeakHoldSec;
            _fadeEndAt = _fadeStartAt + FadeDurationSec;
            _active = true;
        }

        internal static void Tick()
        {
            if (!_active || _overlay == null)
                return;

            var now = Time.unscaledTime;
            if (now < _fadeStartAt)
                return;

            if (now >= _fadeEndAt)
            {
                Stop();
                return;
            }

            var t = (now - _fadeStartAt) / FadeDurationSec;
            var alpha = Mathf.Lerp(PeakAlpha, 0f, t);
            _overlay.color = new Color(1f, 1f, 1f, alpha);
        }

        internal static void Destroy()
        {
            Stop();

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _overlay = null;
            }

            if (_whiteSprite != null)
            {
                Object.Destroy(_whiteSprite.texture);
                Object.Destroy(_whiteSprite);
                _whiteSprite = null;
            }
        }

        private static void Stop()
        {
            _active = false;
            _fadeStartAt = -1f;
            _fadeEndAt = -1f;

            if (_root != null)
                _root.SetActive(false);
        }

        private static void EnsureCreated()
        {
            if (_root != null)
                return;

            BaGameUiChrome.EnsureInitialized();

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            BaGameUiChrome.SetupOverlayCanvas(_root, 9200);

            var overlayGo = new GameObject("Flash", typeof(RectTransform));
            overlayGo.transform.SetParent(_root.transform, false);

            var rect = overlayGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _overlay = overlayGo.AddComponent<Image>();
            _overlay.sprite = GetWhiteSprite();
            _overlay.color = new Color(1f, 1f, 1f, 0f);
            _overlay.raycastTarget = false;

            _root.SetActive(false);
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
    }
}
