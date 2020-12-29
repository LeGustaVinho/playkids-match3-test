using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Playkids.Match3
{
    public class BoardBehaviour : SerializedMonoBehaviour
    {
        [BoxGroup("Board Refs")] public RectTransform BoardArea;
        [BoxGroup("Board Refs")] public GridLayoutGroup GridLayoutGroup;
        [BoxGroup("Board Refs")] public TileBehaviour TilePrefab;
        [BoxGroup("Board Refs")] public PieceBehaviour PiecePrefab;

        [BoxGroup("Board Settings")] public bool AutoZoom;

        [BoxGroup("Board Settings"), ShowIf("@!AutoZoom")]
        public int PreferredCellSize = 256;

        [BoxGroup("Animation Settings")] public float PieceMoveDuration = 1;
        [BoxGroup("Animation Settings")] public Ease PieceMoveEase = Ease.InCubic;

        [BoxGroup("Debug")] public BoardConfig StartBoard;
        [BoxGroup("Debug"), ShowInInspector] private Board board;
        [BoxGroup("Debug")] public List<BoardChangeLogEntry> lastBoardChanges;
        private TileBehaviour[][] tileBehaviours;

        private bool acceptingInputs = true;
        private Dictionary<Guid, PieceBehaviour> piecesViewLookUp = new Dictionary<Guid, PieceBehaviour>();
        private Coroutine boardPhasesRoutine;
        private Coroutine swapPiecesRoutine;
        
        private static string TILE_NAME_FORMAT = "[Group] - Tile [{0},{1}]";
        private static string PIECE_NAME_FORMAT = "[Group] - Piece {0} # {1}";

        public Vector2Int CalculateCellSize(Vector2Int boardSize)
        {
            Vector2 gridSpacing = GridLayoutGroup.spacing;
            RectOffset gridPadding = GridLayoutGroup.padding;
            Rect boardRect = BoardArea.rect;

            float totalSpacingX = (boardSize.x - 1) * gridSpacing.x;
            float totalPaddingX = gridPadding.left + gridPadding.right;
            float totalFreeWidth = boardRect.width - totalSpacingX - totalPaddingX;
            float cellSizeX = totalFreeWidth / gridSpacing.x;

            float totalSpacingY = (boardSize.y - 1) * gridSpacing.y;
            float totalPaddingY = gridPadding.top + gridPadding.bottom;
            float totalFreeHeight = boardRect.height - totalSpacingY - totalPaddingY;
            float cellSizeY = totalFreeHeight / gridSpacing.y;

            int cellSizeMin = Mathf.FloorToInt(Mathf.Min(cellSizeX, cellSizeY));

            return new Vector2Int(cellSizeMin, cellSizeMin);
        }

        public void Initialize(Board board)
        {
            this.board = board;
            tileBehaviours = new TileBehaviour[board.Config.BoardSize.x][];

            //Setup GridLayoutGroup
            GridLayoutGroup.cellSize = AutoZoom
                ? CalculateCellSize(board.Config.BoardSize)
                : new Vector2(PreferredCellSize, PreferredCellSize);
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

        public void RunBoardPhases()
        {
            boardPhasesRoutine = StartCoroutine(RunBoardPhasesRoutine());
        }

        public TileBehaviour CreateTile(Tile tile, bool autoCreatePiece = true)
        {
            TileBehaviour newTileBehaviour = Instantiate(TilePrefab, BoardArea);
            newTileBehaviour.Initialize(this, tile);
            newTileBehaviour.name = string.Format(TILE_NAME_FORMAT, tile.Position.x, tile.Position.y);
            newTileBehaviour.transform.localScale = TilePrefab.transform.localScale;

            if (autoCreatePiece)
            {
                CreatePiece(tile.Piece, newTileBehaviour);
            }

            return newTileBehaviour;
        }

        public PieceBehaviour CreatePiece(Piece piece, TileBehaviour tileParent)
        {
            PieceBehaviour newPieceBehaviour = Instantiate(PiecePrefab, tileParent.PieceParent);
            newPieceBehaviour.Initialize(piece);
            newPieceBehaviour.transform.localScale = TilePrefab.transform.localScale;
            newPieceBehaviour.transform.localPosition = PiecePrefab.transform.position;
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
        
        private IEnumerator RunBoardPhasesRoutine()
        {
            yield return new WaitForEndOfFrame();
            
            acceptingInputs = false;
            List<IEnumerator> routines = new List<IEnumerator>();
            List<Tweener> tweeners = new List<Tweener>();
            List<PieceBehaviour> destroyedPiecesInPhase = new List<PieceBehaviour>();
            List<Tuple<PieceBehaviour, TileBehaviour>> movedPiecesInPhase =
                new List<Tuple<PieceBehaviour, TileBehaviour>>();
            HashSet<PieceBehaviour> unattachedPieces = new HashSet<PieceBehaviour>();

            List<BoardChangeLogEntry> boardChanges = board.RunBoardPhases();
            lastBoardChanges = boardChanges;

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
                            TileBehaviour fromTile = GetTileAt(boardChange.FromTile.Position);
                            TileBehaviour toTile = GetTileAt(boardChange.ToTile.Position);

                            if (toTile.HasPiece)
                            {
                                unattachedPieces.Add(toTile.ReleasePiece());
                            }

                            if (pieceView.IsPlaced)
                            {
                                pieceView.TileView.ReleasePiece();
                            }

                            movedPiecesInPhase.Add(new Tuple<PieceBehaviour, TileBehaviour>(pieceView, toTile));
                                TweenerCore<Vector3, Vector3, VectorOptions> moveTween = pieceView.Transform
                                    .DOMove(toTile.Transform.position, PieceMoveDuration)
                                    .SetEase(PieceMoveEase);
                            moveTween.Play();
                            tweeners.Add(moveTween);
                        }

                        break;
                    }
                    case BoardChangeAction.PieceDestroy:
                    {
                        if (piecesViewLookUp.TryGetValue(boardChange.Piece.GUID, out PieceBehaviour pieceView))
                        {
                            pieceView.TileView.ReleasePiece();
                            routines.Add(pieceView.PlayAnimationRoutine(boardChange.Action));
                            destroyedPiecesInPhase.Add(pieceView);
                        }

                        break;
                    }
                    case BoardChangeAction.BoardShuffle:
                    {
                        //TODO: Show shuffling feedback
                        Debug.Log("Shuffling board");
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

                        //Clean board destroying pieces
                        foreach (PieceBehaviour destroyedPieceInPhase in destroyedPiecesInPhase)
                        {
                            Destroy(destroyedPieceInPhase.gameObject);
                        }
                        destroyedPiecesInPhase.Clear();

                        //ReAttach moved pieces to board
                        foreach (Tuple<PieceBehaviour, TileBehaviour> movedPieceInPhase in movedPiecesInPhase)
                        {
                            movedPieceInPhase.Item2.PutPiece(movedPieceInPhase.Item1);

                            if (unattachedPieces.Contains(movedPieceInPhase.Item1))
                            {
                                unattachedPieces.Remove(movedPieceInPhase.Item1);
                            }
                        }
                        movedPiecesInPhase.Clear();
                        
                        //ReAttach pieces that were detached from the board during the movement
                        foreach (PieceBehaviour unattachedPiece in unattachedPieces)
                        {
                            TileBehaviour tileView = GetTileAt(unattachedPiece.Piece.Tile.Position);
                            tileView.PutPiece(unattachedPiece);
                        }

                        break;
                    }
                    case BoardChangeAction.BoardShuffleLimitReached:
                    {
                        //TODO: Player lost this match
                        Debug.Log("Shuffle limit reached");
                        break;
                    }
                }
            }
            acceptingInputs = true;

            // if (!ValidateBoard())
            // {
            //     Debug.Break();
            //     Debug.LogError("Board visual desync");
            // }
        }
        
        private IEnumerator TrySwapPieceRoutine(TileBehaviour fromTile, TileBehaviour toTile)
        {
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
        
        private void Start()
        {
            Initialize(new Board(StartBoard));
            RunBoardPhases();
        }
    }
}