using UnityEngine;
using PipeHack.Data;

namespace PipeHack.Tiles
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class EdgeNodeView : MonoBehaviour
    {
        [Header("Start node art (per direction)")]
        [SerializeField] private Sprite startNorthSprite;
        [SerializeField] private Sprite startEastSprite;
        [SerializeField] private Sprite startSouthSprite;
        [SerializeField] private Sprite startWestSprite;

        [Header("End node art (per direction)")]
        [SerializeField] private Sprite endNorthSprite;
        [SerializeField] private Sprite endEastSprite;
        [SerializeField] private Sprite endSouthSprite;
        [SerializeField] private Sprite endWestSprite;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Positions and colors this node. worldPos/rotation/scale are
        /// computed by GridManager since it owns cellSize and grid origin.
        /// </summary>
        public void Initialize(EdgeNodeData data, Vector3 worldPos, float cellSize)
        {
            transform.position = worldPos;
            transform.rotation = Quaternion.identity; // art is pre-oriented per side

            _renderer.sprite = GetSpriteForSide(data.Side, data.IsStart);

            if (_renderer.sprite != null)
            {
                Vector2 nativeSize = _renderer.sprite.bounds.size;
                if (nativeSize.x > 0f && nativeSize.y > 0f)
                    transform.localScale = new Vector3(cellSize / nativeSize.x, cellSize / nativeSize.y, 1f);
            }
            else
            {
                transform.localScale = new Vector3(cellSize, cellSize, 1f);
            }
        }

        private Sprite GetSpriteForSide(GridSide side, bool isStart)
        {
            if (isStart)
            {
                switch (side)
                {
                    case GridSide.North: return startNorthSprite;
                    case GridSide.East: return startEastSprite;
                    case GridSide.South: return startSouthSprite;
                    case GridSide.West: return startWestSprite;
                }
            }
            else
            {
                switch (side)
                {
                    case GridSide.North: return endNorthSprite;
                    case GridSide.East: return endEastSprite;
                    case GridSide.South: return endSouthSprite;
                    case GridSide.West: return endWestSprite;
                }
            }
            return null;
        }
    }
}
