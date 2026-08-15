using System.Collections;
using UnityEngine;
using PipeHack.Data;
using PipeHack.Grid;

namespace PipeHack.Tiles
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TileView : MonoBehaviour
    {
        [SerializeField] private Sprite hiddenSprite;
        [SerializeField] private SpriteRenderer selectionOutline; // optional, can be null
        [SerializeField] private SpriteRenderer lockIndicator;    // optional, can be null - shown while locked

        [Header("Glow Colors")]
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color lockedColor = Color.red;

        [Header("Glow Pulse")]
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseMinAlpha = 0.35f;
        [SerializeField] private float pulseMaxAlpha = 1f;
        [Tooltip("How much larger than the tile the glow sprite renders, as a multiple of cell size.")]
        [SerializeField] private float glowSizeMultiplier = 1.15f;

        private static Sprite _proceduralGlowSprite;
        private Coroutine _selectionPulseCoroutine;
        private Coroutine _lockPulseCoroutine;

        [Tooltip("Max gap (seconds) between two taps for it to count as a double-tap.")]
        [SerializeField] private float doubleTapThreshold = 0.25f;

        private SpriteRenderer _renderer;
        private GridManager _gridManager;
        private float _cellSize = 1f;

        private int _clickCount;
        private Coroutine _clickTimerCoroutine;

        public TileData Data { get; private set; }
        public bool IsLocked { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(TileData data, GridManager gridManager, float cellSize)
        {
            Data = data;
            _gridManager = gridManager;
            _cellSize = cellSize;

            if (selectionOutline != null)
            {
                EnsureGlowSpriteAssigned(selectionOutline);
                ScaleGlowToCell(selectionOutline);
            }

            if (lockIndicator != null)
            {
                EnsureGlowSpriteAssigned(lockIndicator);
                ScaleGlowToCell(lockIndicator);
            }

            RefreshVisual();
        }

        private void OnMouseDown()
        {
            _clickCount++;

            if (_clickCount == 1)
            {
                _clickTimerCoroutine = StartCoroutine(ClickTimer());
            }
        }

        /// <summary>
        /// Waits to see whether a second tap arrives within the threshold.
        /// One tap -> normal reveal/select/swap behavior via GridManager.
        /// Two taps within the window -> toggle this tile's lock instead,
        /// and the single-tap action is skipped entirely for that press.
        /// </summary>
        private IEnumerator ClickTimer()
        {
            yield return new WaitForSeconds(doubleTapThreshold);

            if (_clickCount == 1)
            {
                _gridManager.OnTileClicked(this);
            }
            else
            {
                _gridManager.ToggleLock(this);
            }

            _clickCount = 0;
            _clickTimerCoroutine = null;
        }

        public void Reveal()
        {
            Data.IsRevealed = true;
            RefreshVisual();
        }

        public void Hide()
        {
            Data.IsRevealed = false;
            RefreshVisual();
        }

        public void SetSelected(bool selected)
        {
            if (selectionOutline == null) return;

            selectionOutline.enabled = selected;

            if (_selectionPulseCoroutine != null)
            {
                StopCoroutine(_selectionPulseCoroutine);
                _selectionPulseCoroutine = null;
            }

            if (selected)
                _selectionPulseCoroutine = StartCoroutine(PulseGlow(selectionOutline, selectedColor));
        }

        /// <summary>Called by GridManager.ToggleLock - just handles the visual, GridManager owns the rules around what locking blocks.</summary>
        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            if (lockIndicator == null) return;

            lockIndicator.enabled = locked;

            if (_lockPulseCoroutine != null)
            {
                StopCoroutine(_lockPulseCoroutine);
                _lockPulseCoroutine = null;
            }

            if (locked)
                _lockPulseCoroutine = StartCoroutine(PulseGlow(lockIndicator, lockedColor));
        }

        /// <summary>Smoothly oscillates the renderer's alpha between pulseMinAlpha and pulseMaxAlpha, giving the glow effect. Runs until stopped by SetSelected/SetLocked turning it off.</summary>
        private IEnumerator PulseGlow(SpriteRenderer glowRenderer, Color baseColor)
        {
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime * pulseSpeed;
                float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, (Mathf.Sin(t) + 1f) * 0.5f);
                glowRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }

        /// <summary>If no sprite was manually assigned in the Inspector, generates a soft white radial-gradient sprite at runtime so no art asset is required. Shared across all tiles.</summary>
        private void EnsureGlowSpriteAssigned(SpriteRenderer glowRenderer)
        {
            if (glowRenderer.sprite != null) return; // artist already provided one, don't override it
            glowRenderer.sprite = GetOrCreateProceduralGlowSprite();
        }

        private static Sprite GetOrCreateProceduralGlowSprite()
        {
            if (_proceduralGlowSprite != null) return _proceduralGlowSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (dist / maxDist));
                    alpha = Mathf.Pow(alpha, 2f); // soften falloff so the edge fades rather than cuts off sharply
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();

            _proceduralGlowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _proceduralGlowSprite;
        }

        /// <summary>Sizes a glow sprite slightly larger than the tile itself so it reads as a surrounding glow rather than sitting flush with the tile edges.</summary>
        private void ScaleGlowToCell(SpriteRenderer glowRenderer)
        {
            if (glowRenderer.sprite == null || _cellSize <= 0f) return;
            Vector2 nativeSize = glowRenderer.sprite.bounds.size;
            if (nativeSize.x <= 0f || nativeSize.y <= 0f) return;

            float targetSize = _cellSize * glowSizeMultiplier;
            glowRenderer.transform.localScale = new Vector3(
                targetSize / nativeSize.x,
                targetSize / nativeSize.y,
                1f);
        }

        public void RefreshVisual()
        {
            if (!Data.IsRevealed)
            {
                _renderer.sprite = hiddenSprite;
            }
            else
            {
                _renderer.sprite = Data.Pipe.revealedSprite;
            }
            FitSpriteToCell();
        }

        public void SetConnected()
        {
            _renderer.sprite = Data.Pipe.connectedSprite != null ? Data.Pipe.connectedSprite : Data.Pipe.revealedSprite;
            FitSpriteToCell();
        }

        public void SetFilled()
        {
            _renderer.sprite = Data.Pipe.filledSprite != null ? Data.Pipe.filledSprite : Data.Pipe.revealedSprite;
            FitSpriteToCell();
        }

        public void ResetToRevealed()
        {
            _renderer.sprite = Data.Pipe.revealedSprite;
            FitSpriteToCell();
        }

        /// <summary>
        /// Scales this tile's transform so whatever sprite is currently
        /// assigned renders at exactly cellSize x cellSize world units -
        /// regardless of the sprite's native pixel dimensions or Pixels
        /// Per Unit setting. This is what removes the gaps between tiles:
        /// previously tiles were POSITIONED cellSize apart but each sprite
        /// only rendered at its own native size, which didn't match.
        /// </summary>
        private void FitSpriteToCell()
        {
            if (_renderer.sprite == null || _cellSize <= 0f) return;
            Vector2 nativeSize = _renderer.sprite.bounds.size; // world units at scale (1,1,1)
            if (nativeSize.x <= 0f || nativeSize.y <= 0f) return;
            transform.localScale = new Vector3(
                _cellSize / nativeSize.x,
                _cellSize / nativeSize.y,
                1f);
        }
    }
}