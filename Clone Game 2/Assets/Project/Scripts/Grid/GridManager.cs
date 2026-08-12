using PipeHack.Data;
using PipeHack.Tiles;
using PipeHack.Validation;
using System.Collections.Generic;
using UnityEngine;

namespace PipeHack.Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("Level Config")]
        [Tooltip("Grid size for this level. Level 1 = 5, Level 2 = 6, Level 3 = 7.")]
        [SerializeField] private int gridSize = 5;

        [Tooltip("Minimum distance (in grid steps) required between Start and End.")]
        [SerializeField] private int minStartEndDistance = 4;

        [Header("Piece Pool")]
        [Tooltip("All available pipe variants (straights + elbows) to randomly fill the grid with.")]
        [SerializeField] private List<PipeDefinition> pipePool;

        [Header("Prefab / Layout")]
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private EdgeNodeView edgeNodePrefab;
        [SerializeField] private Transform gridRoot;
        [SerializeField] private float cellSize = 1f;

        [Header("Camera")]
        [Tooltip("Camera to auto-fit around the grid. Defaults to Camera.main if left empty.")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("Extra world-space padding added around the grid so tiles aren't flush with the screen edge.")]
        [SerializeField] private float cameraPadding = 1f;

        private TileData[,] _grid;
        private TileView[,] _views;
        private TileView _firstSelected;
        private EdgeNodeData _startNode;
        private EdgeNodeData _endNode;

        private void Start()
        {
            GenerateLevel(gridSize);
        }

        public void GenerateLevel(int size)
        {
            gridSize = size;
            _grid = new TileData[gridSize, gridSize];
            _views = new TileView[gridSize, gridSize];

            // 1. Fill every cell with a random pipe piece.
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    PipeDefinition piece = pipePool[Random.Range(0, pipePool.Count)];
                    var pos = new Vector2Int(x, y);
                    _grid[x, y] = new TileData(pos, piece);
                }
            }

            // 2. Pick Start/End on the perimeter, respecting minimum distance.
            PlaceStartAndEnd();

            // 3. Spawn visuals.
            SpawnViews();

            // 4. Fit the camera to whatever size grid we just built.
            FitCameraToGrid();
        }

        /// <summary>
        /// Every (cell, side) pair where an external node could attach.
        /// Corner cells appear twice - once per adjacent side - so either
        /// orientation is a valid, independent placement option.
        /// </summary>
        private List<(Vector2Int cell, GridSide side)> GetEdgeAttachCandidates()
        {
            var candidates = new List<(Vector2Int, GridSide)>();

            for (int x = 0; x < gridSize; x++)
            {
                candidates.Add((new Vector2Int(x, 0), GridSide.South));
                candidates.Add((new Vector2Int(x, gridSize - 1), GridSide.North));
            }
            for (int y = 0; y < gridSize; y++)
            {
                candidates.Add((new Vector2Int(0, y), GridSide.West));
                candidates.Add((new Vector2Int(gridSize - 1, y), GridSide.East));
            }

            return candidates;
        }

        private void PlaceStartAndEnd()
        {
            var candidates = GetEdgeAttachCandidates();

            var startCandidate = candidates[Random.Range(0, candidates.Count)];

            // Only keep candidates whose CELL (ignoring which side) is far
            // enough from Start's cell - distance is measured between the
            // attach cells, not the nodes themselves.
            var validEndCandidates = new List<(Vector2Int cell, GridSide side)>();
            foreach (var c in candidates)
            {
                if (c.cell == startCandidate.cell) continue;
                int dist = Mathf.Abs(c.cell.x - startCandidate.cell.x) + Mathf.Abs(c.cell.y - startCandidate.cell.y);
                if (dist >= minStartEndDistance)
                    validEndCandidates.Add(c);
            }

            if (validEndCandidates.Count == 0)
            {
                Debug.LogWarning("No edge candidate satisfies minStartEndDistance - relaxing constraint.");
                foreach (var c in candidates)
                    if (c.cell != startCandidate.cell) validEndCandidates.Add(c);
            }

            var endCandidate = validEndCandidates[Random.Range(0, validEndCandidates.Count)];

            _startNode = new EdgeNodeData(startCandidate.cell, startCandidate.side, isStart: true);
            _endNode = new EdgeNodeData(endCandidate.cell, endCandidate.side, isStart: false);
        }

        private void SpawnViews()
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector3 worldPos = new Vector3(x * cellSize, y * cellSize, 0f);
                    TileView view = Instantiate(tilePrefab, worldPos, Quaternion.identity, gridRoot);
                    view.Initialize(_grid[x, y], this, cellSize);
                    _views[x, y] = view;
                }
            }

            SpawnEdgeNode(_startNode);
            SpawnEdgeNode(_endNode);
        }

        private void SpawnEdgeNode(EdgeNodeData node)
        {
            Vector3 attachCellWorldPos = new Vector3(node.AttachCell.x * cellSize, node.AttachCell.y * cellSize, 0f);
            Vector2Int offset = GridSideUtil.ToOffset(node.Side);
            Vector3 worldPos = attachCellWorldPos + new Vector3(offset.x, offset.y, 0f) * cellSize;
            float rotation = GridSideUtil.ToInwardRotation(node.Side);

            EdgeNodeView view = Instantiate(edgeNodePrefab, worldPos, Quaternion.identity, gridRoot);
            view.Initialize(node, worldPos, rotation, cellSize);
        }

        /// <summary>
        /// Centers the camera on the grid and sets its orthographic size so
        /// the whole grid is always visible, no matter the grid dimensions
        /// (5x5, 6x6, 7x7, ...). Recalculated every time a level is generated.
        /// </summary>
        private void FitCameraToGrid()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("GridManager: no camera assigned and no Camera.main found - cannot auto-fit.");
                return;
            }

            if (!cam.orthographic)
            {
                Debug.LogWarning("GridManager: FitCameraToGrid only supports an orthographic camera.");
                return;
            }

            // Grid spans from (0,0) to (gridSize-1, gridSize-1) in cell steps.
            // Add one extra cell on every side since Start/End nodes sit
            // just outside the grid border - without this they'd render
            // partially or fully off-screen.
            float gridWorldWidth = (gridSize + 2) * cellSize;
            float gridWorldHeight = (gridSize + 2) * cellSize;

            Vector3 gridCenter = new Vector3(
                (gridSize - 1) * cellSize * 0.5f,
                (gridSize - 1) * cellSize * 0.5f,
                cam.transform.position.z);

            cam.transform.position = gridCenter;

            // Orthographic size is half the vertical view height, so fit
            // both height and width (accounting for aspect ratio), then
            // add padding and use whichever dimension needs more room.
            float sizeForHeight = (gridWorldHeight * 0.5f) + cameraPadding;
            float sizeForWidth = ((gridWorldWidth * 0.5f) + cameraPadding) / cam.aspect;

            cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
        }

        /// <summary>
        /// Called by TileView when a revealed tile is clicked.
        /// Handles the click-click-swap-from-anywhere interaction.
        /// </summary>
        public void OnTileClicked(TileView view)
        {
            if (!view.Data.IsRevealed)
            {
                view.Reveal();
                return;
            }

            if (_firstSelected == null)
            {
                _firstSelected = view;
                view.SetSelected(true);
                return;
            }

            if (_firstSelected == view)
            {
                // Clicked the same tile twice - deselect.
                view.SetSelected(false);
                _firstSelected = null;
                return;
            }

            SwapPieces(_firstSelected, view);
            _firstSelected.SetSelected(false);
            _firstSelected = null;
        }

        private void SwapPieces(TileView a, TileView b)
        {
            PipeDefinition temp = a.Data.Pipe;
            a.Data.Pipe = b.Data.Pipe;
            b.Data.Pipe = temp;

            a.RefreshVisual();
            b.RefreshVisual();

            bool isSolved = PathValidator.ValidatePath(this);
            Debug.Log($"[PathValidator] Solved: {isSolved}");


            // TODO: notify Flow Validation system that the grid state changed,
            // e.g. via an event: OnGridChanged?.Invoke();
        }

        public TileData[,] GetGridData() => _grid;
        public EdgeNodeData GetStartNode() => _startNode;
        public EdgeNodeData GetEndNode() => _endNode;
    }
}