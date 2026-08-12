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

        private SpriteRenderer _renderer;
        private GridManager _gridManager;
        private float _cellSize = 1f;

        public TileData Data { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(TileData data, GridManager gridManager, float cellSize)
        {
            Data = data;
            _gridManager = gridManager;
            _cellSize = cellSize;
            RefreshVisual();
        }

        private void OnMouseDown()
        {
            _gridManager.OnTileClicked(this);
        }

        public void Reveal()
        {
            Data.IsRevealed = true;
            RefreshVisual();
        }

        public void SetSelected(bool selected)
        {
            if (selectionOutline != null)
                selectionOutline.enabled = selected;
        }

        public void RefreshVisual()
        {
            if (!Data.IsRevealed)
            {
                _renderer.sprite = hiddenSprite;
                return;
            }

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
