using UnityEngine;
using PipeHack.Data;

namespace PipeHack.Tiles
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class EdgeNodeView : MonoBehaviour
    {
        [SerializeField] private Sprite nodeSprite; // simple arrow/marker sprite, or leave null to just use color

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Positions and colors this node. worldPos/rotation/scale are
        /// computed by GridManager since it owns cellSize and grid origin.
        /// </summary>
        public void Initialize(EdgeNodeData data, Vector3 worldPos, float rotationZ, float cellSize)
        {
            transform.position = worldPos;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            if (nodeSprite != null)
            {
                _renderer.sprite = nodeSprite;
                Vector2 nativeSize = _renderer.sprite.bounds.size;
                if (nativeSize.x > 0f && nativeSize.y > 0f)
                {
                    transform.localScale = new Vector3(cellSize / nativeSize.x, cellSize / nativeSize.y, 1f);
                }
            }
            else
            {
                transform.localScale = new Vector3(cellSize, cellSize, 1f);
            }

            _renderer.color = data.IsStart ? Color.green : Color.red;
        }
    }
}
