using PipeHack.Data;
using PipeHack.Tiles;
using PipeHack.Validation;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace PipeHack.Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("Level Config")]
        [Tooltip("Grid size for this level. Level 1 = 5, Level 2 = 6, Level 3 = 7. Also doubles as the open-tile limit for the memory mechanic.")]
        [SerializeField] private int gridSize = 5;

        [Tooltip("Minimum distance (in grid steps) required between Start and End.")]
        [SerializeField] private int minStartEndDistance = 4;

        [Tooltip("Blockers cannot spawn within this many tiles of a Start/End attach cell (in every direction, including diagonals).")]
        [SerializeField] private int blockerExclusionRadius = 1;

        [Header("Piece Pool")]
        [Tooltip("All available pipe variants (straights + elbows) to randomly fill the grid with.")]
        [SerializeField] private List<PipeDefinition> pipePool;

        [Header("Prefab / Layout")]
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private EdgeNodeView edgeNodePrefab;
        [SerializeField] private Transform gridRoot;
        [SerializeField] private float cellSize = 1f;

        [Header("Path Feedback")]
        [Tooltip("Delay before the fill animation starts, after a path completes.")]
        [SerializeField] private float fillStartDelay = 0.5f;
        [Tooltip("Delay between each tile filling in, once the animation starts.")]
        [SerializeField] private float fillStepDelay = 0.15f;

        private Coroutine _fillCoroutine;
        private List<Vector2Int> _lastPathCells = new List<Vector2Int>();
        private List<Vector2Int> _lastConnectedCells = new List<Vector2Int>();

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

        // Tracks reveal order for the memory mechanic: only tiles that are
        // revealed AND not yet connected to Start count toward the open-tile
        // limit, and only those are eligible to be auto-hidden.
        private List<TileView> _revealOrder = new List<TileView>();

        // Fired once the win fill sequence completes. LevelFlowManager and
        // LevelTimer both subscribe to this rather than GridManager knowing
        // anything about scenes or countdowns.
        public event Action OnLevelWon;

        // True once the level has ended (won or timed out). Blocks further
        // tile interaction so the board can't keep being played after that.
        private bool _levelEnded;

        /// <summary>Called externally (e.g. by LevelFailUI on timeout) to stop board interaction.</summary>
        public void LockBoard() => _levelEnded = true;

        private void Start()
        {
            GenerateLevel(gridSize);
        }

        public void GenerateLevel(int size)
        {
            gridSize = size;
            _grid = new TileData[gridSize, gridSize];
            _views = new TileView[gridSize, gridSize];
            _revealOrder.Clear();

            // 1. Decide Start/End first - fill loop below needs to know their
            //    attach cells so it can bar blockers from spawning there.
            PlaceStartAndEnd();

            HashSet<Vector2Int> blockerExcluded = GetBlockerExclusionCells();

            // 2. Fill every cell with a random pipe piece.
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    var pos = new Vector2Int(x, y);
                    bool excludeBlockers = blockerExcluded.Contains(pos);
                    _grid[x, y] = new TileData(pos, PickPiece(excludeBlockers));
                }
            }

            // 3. Spawn visuals.
            SpawnViews();

            // 4. Fit the camera to whatever size grid we just built.
            FitCameraToGrid();
        }

        private PipeDefinition PickPiece(bool excludeBlockers)
        {
            if (!excludeBlockers)
                return pipePool[UnityEngine.Random.Range(0, pipePool.Count)];

            var nonBlockerPool = pipePool.FindAll(p => p.category != PipeCategory.Blocker);
            return nonBlockerPool[UnityEngine.Random.Range(0, nonBlockerPool.Count)];
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

            var startCandidate = candidates[UnityEngine.Random.Range(0, candidates.Count)];

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

            var endCandidate = validEndCandidates[UnityEngine.Random.Range(0, validEndCandidates.Count)];

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

            EdgeNodeView view = Instantiate(edgeNodePrefab, worldPos, Quaternion.identity, gridRoot);
            view.Initialize(node, worldPos, cellSize);
        }

        private HashSet<Vector2Int> GetBlockerExclusionCells()
        {
            var excluded = new HashSet<Vector2Int>();
            AddNeighborhood(excluded, _startNode.AttachCell);
            AddNeighborhood(excluded, _endNode.AttachCell);
            return excluded;
        }

        private void AddNeighborhood(HashSet<Vector2Int> set, Vector2Int center)
        {
            for (int dx = -blockerExclusionRadius; dx <= blockerExclusionRadius; dx++)
            {
                for (int dy = -blockerExclusionRadius; dy <= blockerExclusionRadius; dy++)
                {
                    var cell = new Vector2Int(center.x + dx, center.y + dy);
                    if (cell.x >= 0 && cell.x < gridSize && cell.y >= 0 && cell.y < gridSize)
                        set.Add(cell);
                }
            }
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
            if (_levelEnded) return;

            if (!view.Data.IsRevealed)
            {
                RevealTile(view);
                return;
            }

            // Blockers can be revealed but never selected or swapped.
            if (view.Data.IsBlocker)
                return;

            // Locked tiles can't be selected/swapped - double-tap to unlock first.
            if (view.IsLocked)
                return;

            if (_firstSelected == null)
            {
                _firstSelected = view;
                view.SetSelected(true);
                return;
            }

            if (_firstSelected == view)
            {
                view.SetSelected(false);
                _firstSelected = null;
                return;
            }

            SwapPieces(_firstSelected, view);

            // SwapPieces re-evaluates the open-tile limit, which can evict
            // _firstSelected itself (and already clears its selection visual
            // when it does) - only touch it here if it's still set.
            if (_firstSelected != null)
            {
                _firstSelected.SetSelected(false);
                _firstSelected = null;
            }
        }

        /// <summary>
        /// Called by TileView on a detected double-tap. Locking a tile
        /// protects it from the open-tile eviction limit and from being
        /// selected for a swap, until it's double-tapped again to unlock.
        /// </summary>
        public void ToggleLock(TileView view)
        {
            if (_levelEnded) return;
            if (!view.Data.IsRevealed || view.Data.IsBlocker) return;

            bool newLockState = !view.IsLocked;
            view.SetLocked(newLockState);

            if (newLockState && view == _firstSelected)
            {
                // Locking a tile that was mid-selection for a swap cancels that selection.
                view.SetSelected(false);
                _firstSelected = null;
            }
        }

        private void RevealTile(TileView view)
        {
            view.Reveal();
            _revealOrder.Add(view);

            // A freshly revealed tile could already be correctly placed and
            // connected without any swap happening, so re-evaluate here too,
            // not just after swaps.
            EvaluatePathState();
        }

        private void SwapPieces(TileView a, TileView b)
        {
            PipeDefinition temp = a.Data.Pipe;
            a.Data.Pipe = b.Data.Pipe;
            b.Data.Pipe = temp;

            a.RefreshVisual();
            b.RefreshVisual();

            EvaluatePathState();
        }

        private void EvaluatePathState()
        {
            if (_fillCoroutine != null)
            {
                StopCoroutine(_fillCoroutine);
                _fillCoroutine = null;
            }

            // Clear whatever was previously highlighted/filled before re-evaluating.
            foreach (var cell in _lastConnectedCells)
                _views[cell.x, cell.y].ResetToRevealed();
            _lastConnectedCells.Clear();
            _lastPathCells.Clear();

            // Highlight every tile reachable from Start, regardless of whether it reaches End.
            List<Vector2Int> connectedCells = PathValidator.GetConnectedFromStart(this);
            _lastConnectedCells = connectedCells;
            foreach (var cell in connectedCells)
                _views[cell.x, cell.y].SetConnected();

            // Connected tiles are "locked in" and exempt from the memory
            // mechanic's open-tile limit - enforce the cap on everything else.
            EnforceOpenTileLimit();

            // Separately, check whether Start actually reaches End - only this triggers the fill.
            PathResult result = PathValidator.ValidatePath(this);
            if (!result.IsConnected) return;

            _lastPathCells = result.PathCells;
            _fillCoroutine = StartCoroutine(FillSequence(_lastPathCells));
        }

        /// <summary>
        /// Memory mechanic: only gridSize tiles can be open (revealed and
        /// NOT yet connected to Start, and NOT manually locked) at once.
        /// Locked-in (connected) tiles and player-locked tiles never count
        /// and are never evicted. When over the cap, the oldest-revealed
        /// eligible tile is auto-hidden, repeated until back within limit.
        /// </summary>
        private void EnforceOpenTileLimit()
        {
            var lockedSet = new HashSet<Vector2Int>(_lastConnectedCells);

            int openUnlockedCount = 0;
            foreach (var v in _revealOrder)
                if (v.Data.IsRevealed && !v.IsLocked && !lockedSet.Contains(v.Data.GridPosition))
                    openUnlockedCount++;

            while (openUnlockedCount > gridSize)
            {
                TileView toHide = null;
                foreach (var v in _revealOrder)
                {
                    if (!v.Data.IsRevealed) continue;
                    if (v.IsLocked) continue;
                    if (lockedSet.Contains(v.Data.GridPosition)) continue;
                    toHide = v;
                    break;
                }

                if (toHide == null) break; // everything still open is locked in - nothing left to evict

                // If the tile about to be hidden is mid-selection for a swap,
                // cancel that selection so _firstSelected never points at a
                // tile that's no longer revealed.
                if (toHide == _firstSelected)
                {
                    toHide.SetSelected(false);
                    _firstSelected = null;
                }

                toHide.Hide();
                _revealOrder.Remove(toHide);
                openUnlockedCount--;
            }
        }

        private IEnumerator FillSequence(List<Vector2Int> pathCells)
        {
            yield return new WaitForSeconds(fillStartDelay);

            foreach (var cell in pathCells)
            {
                _views[cell.x, cell.y].SetFilled();
                yield return new WaitForSeconds(fillStepDelay);
            }

            _fillCoroutine = null;

            _levelEnded = true;
            OnLevelWon?.Invoke();
        }

        public TileData[,] GetGridData() => _grid;
        public EdgeNodeData GetStartNode() => _startNode;
        public EdgeNodeData GetEndNode() => _endNode;
    }
}