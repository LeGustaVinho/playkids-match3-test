using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Playkids.Match3
{
    public class BoardBehaviour : SerializedMonoBehaviour, IDisposable
    {
        [BoxGroup("Board Refs")] public RectTransform BoardArea;
        [BoxGroup("Board Refs")] public GridLayoutGroup GridLayoutGroup;
        [BoxGroup("Board Refs")] public TileBehaviour TilePrefab;
        [BoxGroup("Board Refs")] public PieceBehaviour PiecePrefab;

        [BoxGroup("Board Settings")] public bool AutoZoom;

        [BoxGroup("Board Settings"), ShowIf("@!AutoZoom")]
        public int PreferredCellSize = 256;
        [BoxGroup("Board Settings")]
        public Vector2Int CellSize { private set; get; }

        [BoxGroup("Animation Settings"), SuffixLabel("seconds")] public float BoardStartDelay = 1;
        [BoxGroup("Animation Settings")] public float PieceMoveDuration = 1;
        [BoxGroup("Animation Settings")] public Ease PieceMoveEase = Ease.InCubic;
        [BoxGroup("Animation Settings")] public float PieceMoveShuffleDuration = 1;
        
        [BoxGroup("Audio")] public AudioSource SFX;
        [BoxGroup("Audio")] public AudioClip SwapSFX;
        [BoxGroup("Audio")] public AudioClip MatchSFX;
        
        public event Action<PatternFound> OnMatch;
        public event Action OnShuffle;
        public event Action OnShuffleLimitReached;
        
        [ShowInInspector]
        private Board board;
        [ShowInInspector]
        private TileBehaviour[][] tileBehaviours;
        private bool acceptingInputs = true;
        private readonly Dictionary<Guid, PieceBehaviour> piecesViewLookUp = new Dictionary<Guid, PieceBehaviour>();
        private Coroutine boardPhasesRoutine;
        private Coroutine swapPiecesRoutine;

        private static string TILE_NAME_FORMAT = "[Group] - Tile [{0},{1}]";
        private static string PIECE_NAME_FORMAT = "[Group] - Piece {0} # {1}";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="boardConfig"></param>
        public void LoadBoard(BoardConfig boardConfig)
        {
            Initialize(new Board(boardConfig));
        }
        
        public void Dispose()
        {
            board.Dispose();
            
            if (boardPhasesRoutine != null)
            {
                StopCoroutine(boardPhasesRoutine);
                boardPhasesRoutine = null;
            }
            
            if (swapPiecesRoutine != null)
            {
                StopCoroutine(swapPiecesRoutine);
                swapPiecesRoutine = null;
            }

            foreach (KeyValuePair<Guid, PieceBehaviour> piecePair in piecesViewLookUp)
            {
                if (piecePair.Value != null)
                {
                    Destroy(piecePair.Value.gameObject);
                }
            }
            piecesViewLookUp.Clear();
            
            foreach (TileBehaviour[] columns in tileBehaviours)
            {
                foreach (TileBehaviour tileView in columns)
                {
                    if (tileView.HasPiece)
                    {
                        Destroy(tileView.PieceView.gameObject);
                    }
                    
                    Destroy(tileView.gameObject);
                }
            }
            tileBehaviours = null;
            board = null;
            
            piecesViewLookUp.Clear();
        }
        
        public TileBehaviour GetTileAt(Vector2Int position)
        {
            return GetTileAt(position.x, position.y);
        }

        public TileBehaviour GetTileAt(int x, int y)
        {
            if (x >= 0 && x < tileBehaviours.Length)
            {
                if (y >= 0 && y < tileBehaviours[x].Length)
                {
                    return tileBehaviours[x][y];
                }
            }

            return null;
        }

        [Button]
        public void RunBoardPhases(float delay = 0)
        {
            boardPhasesRoutine = StartCoroutine(RunBoardPhasesRoutine(delay));
        }

        public TileBehaviour CreateTile(Tile tile, bool autoCreatePiece = true)
        {
            TileBehaviour newTileBehaviour = Instantiate(TilePrefab, BoardArea);
            newTileBehaviour.Initialize(this, tile);
            newTileBehaviour.name = string.Format(TILE_NAME_FORMAT, tile.Position.x, tile.Position.y);
            newTileBehaviour.transform.localScale = TilePrefab.transform.localScale;

            if (autoCreatePiece && tile.Piece != null)
            {
                CreatePiece(tile.Piece, newTileBehaviour);
            }

            return newTileBehaviour;
        }

        public PieceBehaviour CreatePiece(Piece piece, TileBehaviour tileParent)
        {
            PieceBehaviour newPieceBehaviour = Instantiate(PiecePrefab, tileParent.PieceParent);
            newPieceBehaviour.Initialize(piece);
            newPieceBehaviour.Transform.localScale = TilePrefab.transform.localScale;
            newPieceBehaviour.Transform.localPosition = PiecePrefab.transform.position;
            newPieceBehaviour.RectTransform.sizeDelta = CellSize;
            
            newPieceBehaviour.name = string.Format(PIECE_NAME_FORMAT, newPieceBehaviour.Piece.Config.Type,
                newPieceBehaviour.Piece.GUID);
            
            tileParent.PutPiece(newPieceBehaviour);

            return newPieceBehaviour;
        }

        public void TrySwapPiece(TileBehaviour fromTile, TileBehaviour toTile)
        {
            if (acceptingInputs)
            {
                swapPiecesRoutine = StartCoroutine(TrySwapPieceRoutine(fromTile, toTile));
            }
        }
        
        private void Initialize(Board board)
        {
            acceptingInputs = false;
            this.board = board;
            tileBehaviours = new TileBehaviour[board.Config.BoardSize.x][];
            CellSize = CalculateCellSize(board.Config.BoardSize);

            //Setup GridLayoutGroup
            GridLayoutGroup.cellSize = AutoZoom ? CellSize : new Vector2(PreferredCellSize, PreferredCellSize);
            GridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            GridLayoutGroup.constraintCount = board.Config.BoardSize.y;
            GridLayoutGroup.startAxis =
                GridLayoutGroup.Axis.Vertical; //Because we always iterate in y inside x in loops

            for (int x = 0; x < board.Config.BoardSize.x; x++)
            {
                tileBehaviours[x] = new TileBehaviour[board.Config.BoardSize.y];
                for (int y = 0; y < board.Config.BoardSize.y; y++)
                {
                    Tile tile = board.GetTileAt(x, y);
                    TileBehaviour newTileView = CreateTile(tile);
                    tileBehaviours[x][y] = newTileView;

                    if (newTileView.HasPiece)
                    {
                        piecesViewLookUp.Add(newTileView.PieceView.Piece.GUID, newTileView.PieceView);
                    }
                }
            }
            
            RunBoardPhases(BoardStartDelay);
        }
        
        private Vector2Int CalculateCellSize(Vector2Int boardSize)
        {
            Vector2 gridSpacing = GridLayoutGroup.spacing;
            RectOffset gridPadding = GridLayoutGroup.padding;
            Rect boardRect = BoardArea.rect;

            float totalSpacingX = (boardSize.x - 1) * gridSpacing.x;
            float totalPaddingX = gridPadding.left + gridPadding.right;
            float totalFreeWidth = boardRect.width - totalSpacingX - totalPaddingX;
            float cellSizeX = totalFreeWidth / boardSize.x;

            float totalSpacingY = (boardSize.y - 1) * gridSpacing.y;
            float totalPaddingY = gridPadding.top + gridPadding.bottom;
            float totalFreeHeight = boardRect.height - totalSpacingY - totalPaddingY;
            float cellSizeY = totalFreeHeight / boardSize.y;

            int cellSizeMin = Mathf.FloorToInt(Mathf.Min(cellSizeX, cellSizeY));

            return new Vector2Int(cellSizeMin, cellSizeMin);
        }

        private IEnumerator RunBoardPhasesRoutine(float delay = 0)
        {
            acceptingInputs = false;

            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            List<Tweener> tweeners = new List<Tweener>();
            HashSet<PieceBehaviour> destroyedPiecesInPhase = new HashSet<PieceBehaviour>();
            HashSet<PieceBehaviour> touchedPieces = new HashSet<PieceBehaviour>();
            Dictionary<PieceBehaviour, TileBehaviour> movingPieces = new Dictionary<PieceBehaviour, TileBehaviour>();

            List<BoardChangeLogEntry> boardChanges = board.RunBoardPhases();

            foreach (BoardChangeLogEntry boardChange in boardChanges)
            {
                switch (boardChange.Action)
                {
                    case BoardChangeAction.PieceCreation:
                    {
                        TileBehaviour tileView = GetTileAt(boardChange.ToTile.Position);
                        if (tileView != null)
                        {
                            PieceBehaviour newPieceView = CreatePiece(boardChange.Piece, tileView);
                            piecesViewLookUp.Add(newPieceView.Piece.GUID, newPieceView);
                            routines.Add(newPieceView.PlayAnimationRoutine(boardChange.Action));
                        }
                        break;
                    }
                    case BoardChangeAction.PieceMove:
                    {
                        if (piecesViewLookUp.TryGetValue(boardChange.Piece.GUID, out PieceBehaviour pieceView))
                        {
                            TileBehaviour toTile = GetTileAt(boardChange.ToTile.Position);

                            if (pieceView.IsPlaced)
                            {
                                pieceView.TileView.ReleasePiece();
                            }

                            if (toTile.HasPiece)
                            {
                                touchedPieces.Add(toTile.ReleasePiece());
                            }

                            movingPieces.Add(pieceView, toTile);
                            TweenerCore<Vector3, Vector3, VectorOptions> moveTween = pieceView.Transform
                                .DOMove(toTile.Transform.position, PieceMoveDuration)
                                .SetEase(PieceMoveEase);

                            tweeners.Add(moveTween);
                        }

                        break;
                    }
                    case BoardChangeAction.PieceDestroy:
                    {
                        if (piecesViewLookUp.TryGetValue(boardChange.Piece.GUID, out PieceBehaviour pieceView))
                        {
                            if (pieceView.IsPlaced)
                            {
                                pieceView.TileView.ReleasePiece();
                            }
                            routines.Add(pieceView.PlayAnimationRoutine(boardChange.Action));
                            destroyedPiecesInPhase.Add(pieceView);
                        }

                        break;
                    }
                    case BoardChangeAction.BoardShuffle:
                    {
                        OnShuffle?.Invoke();
                        break;
                    }
                    case BoardChangeAction.PhaseTransition:
                    {
                        //Wait all tasks
                        int completed = 0;
                        while (completed != routines.Count + tweeners.Count)
                        {
                            completed = 0;
                            foreach (IEnumerator routine in routines)
                            {
                                if (!routine.MoveNext())
                                {
                                    completed++;
                                }
                            }

                            foreach (Tweener tweener in tweeners)
                            {
                                if (!tweener.IsPlaying())
                                {
                                    completed++;
                                }
                            }

                            yield return null;
                        }

                        //ReAttach pieces that were detached from the board during the movement
                        foreach (KeyValuePair<PieceBehaviour, TileBehaviour> piece in movingPieces)
                        {
                            piece.Value.PutPiece(piece.Key);

                            if (touchedPieces.Contains(piece.Key))
                            {
                                touchedPieces.Remove(piece.Key);
                            }
                        }

                        if (touchedPieces.Count > 0)
                        {
                            Debug.LogError("Touched piece issue");
                        }

                        movingPieces.Clear();
                        touchedPieces.Clear();

                        //Clean board destroying pieces
                        foreach (PieceBehaviour destroyedPieceInPhase in destroyedPiecesInPhase)
                        {
                            Destroy(destroyedPieceInPhase.gameObject);
                        }
                        destroyedPiecesInPhase.Clear();

                        break;
                    }
                    case BoardChangeAction.BoardShuffleLimitReached:
                    {
                        OnShuffleLimitReached?.Invoke();
                        break;
                    }
                    case BoardChangeAction.PieceMoveShuffle:
                    {
                        TileBehaviour fromTile = GetTileAt(boardChange.FromTile.Position);
                        TileBehaviour toTile = GetTileAt(boardChange.ToTile.Position);

                        PieceBehaviour fromPiece = fromTile.ReleasePiece();
                        PieceBehaviour toPiece = toTile.ReleasePiece();

                        movingPieces.Add(fromPiece, toTile);
                        movingPieces.Add(toPiece, fromTile);

                        TweenerCore<Vector3, Vector3, VectorOptions> move1Tween = fromPiece.Transform
                            .DOMove(toTile.Transform.position, PieceMoveShuffleDuration)
                            .SetEase(PieceMoveEase);

                        TweenerCore<Vector3, Vector3, VectorOptions> move2Tween = toPiece.Transform
                            .DOMove(fromTile.Transform.position, PieceMoveShuffleDuration)
                            .SetEase(PieceMoveEase);

                        tweeners.Add(move1Tween);
                        tweeners.Add(move2Tween);
                        break;
                    }
                    case BoardChangeAction.PieceMatch:
                    {
                        OnMatch?.Invoke(boardChange.PieceMatchPattern);
                        
                        SFX.clip = MatchSFX;
                        SFX.Play();
                        
                        break;
                    }
                }
            }
            acceptingInputs = true;

            if (!ValidateBoard())
            {
                Debug.Break();
                Debug.LogError("Board visual desync");
            }
        }
        
        private IEnumerator TrySwapPieceRoutine(TileBehaviour fromTile, TileBehaviour toTile)
        {
            acceptingInputs = false;
            Sequence swapSequence = DOTween.Sequence();
            bool swapResult = board.Swap(fromTile.Tile, toTile.Tile);

            PieceBehaviour piece1View = fromTile.ReleasePiece();
            PieceBehaviour piece2View = toTile.ReleasePiece();

            swapSequence.Insert(0,
                piece1View.transform.DOMove(toTile.Transform.position, PieceMoveDuration).SetEase(PieceMoveEase));
            swapSequence.Insert(0,
                piece2View.transform.DOMove(fromTile.Transform.position, PieceMoveDuration).SetEase(PieceMoveEase));
            swapSequence.AppendInterval(1);

            if (!swapResult)
            {
                swapSequence.Insert(2,
                    piece1View.transform.DOMove(fromTile.Transform.position, PieceMoveDuration).SetEase(PieceMoveEase));
                swapSequence.Insert(2,
                    piece2View.transform.DOMove(toTile.Transform.position, PieceMoveDuration).SetEase(PieceMoveEase));
            }
            
            SFX.clip = SwapSFX;
            SFX.Play();

            while (swapSequence.IsPlaying())
            {
                yield return null;
            }

            if (swapResult)
            {
                fromTile.PutPiece(piece2View);
                toTile.PutPiece(piece1View);

                RunBoardPhases();
            }
            else
            {
                fromTile.PutPiece(piece1View);
                toTile.PutPiece(piece2View);
            }
            acceptingInputs = true;
        }

        private bool ValidateBoard()
        {
            for (int x = 0; x < board.Config.BoardSize.x; x++)
            {
                for (int y = 0; y < board.Config.BoardSize.y; y++)
                {
                    Tile currentTile = board.GetTileAt(x, y);

                    if (tileBehaviours[x][y].PieceView.Piece != currentTile.Piece)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}