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

        [BoxGroup("Animation Settings")] public int PieceMoveDuration = 1;
        [BoxGroup("Animation Settings")] public Ease PieceMoveEase = Ease.InCubic;

        [BoxGroup("Debug")] public BoardConfig StartBoard;
        [BoxGroup("Debug"), ShowInInspector] private Board board;
        private TileBehaviour[][] tileBehaviours;
        private Dictionary<Guid, PieceBehaviour> piecesViewLookUp = new Dictionary<Guid, PieceBehaviour>();

        private static string TILE_NAME_FORMAT = "[Group] - Tile [{0},{1}]";

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

        public IEnumerator RunBoardPhases()
        {
            yield return new WaitForSeconds(2); 
            
            List<BoardChangeLogEntry> boardChanges = board.RunBoardPhases();
            if (boardChanges == null)
            {
                Debug.LogError("Shuffle limit exceed");
                yield break;
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            List<Tweener> tweeners = new List<Tweener>();
            
            List<PieceBehaviour> destroyedPiecesInPhase = new List<PieceBehaviour>();
            List<Tuple<PieceBehaviour, TileBehaviour>> movedPiecesInPhase =
                new List<Tuple<PieceBehaviour, TileBehaviour>>();

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

                            if (pieceView.IsPlaced)
                            {
                                pieceView.TileView.ReleasePiece();
                                movedPiecesInPhase.Add(new Tuple<PieceBehaviour, TileBehaviour>(pieceView, toTile));
                                TweenerCore<Vector3, Vector3, VectorOptions> moveTween = pieceView.Transform
                                    .DOMove(toTile.Transform.position, PieceMoveDuration)
                                    .SetEase(PieceMoveEase);
                                moveTween.Play();
                                tweeners.Add(moveTween);
                            }
                            else
                            {
                                Debug.LogError("Bug");
                            }
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
                        break;
                    case BoardChangeAction.PhaseTransition:
                    {
                        int completed = 0;
                        while (completed != routines.Count)
                        {
                            completed = 0;
                            foreach (IEnumerator routine in routines)
                            {
                                if (!routine.MoveNext())
                                {
                                    completed++;
                                }
                            }
                            yield return null;
                        }

                        bool allTweensDone = true;
                        foreach (Tweener tweener in tweeners)
                        {
                            if (tweener.IsPlaying())
                            {
                                allTweensDone = false;
                                break;
                            }
                        }

                        yield return new WaitUntil(() => allTweensDone);

                        foreach (PieceBehaviour destroyedPieceInPhase in destroyedPiecesInPhase)
                        {
                            Destroy(destroyedPieceInPhase.gameObject);
                        }
                        destroyedPiecesInPhase.Clear();

                        foreach (Tuple<PieceBehaviour, TileBehaviour> movedPieceInPhase in movedPiecesInPhase)
                        {
                            movedPieceInPhase.Item2.PutPiece(movedPieceInPhase.Item1);
                        }
                        movedPiecesInPhase.Clear();

                        break;
                    }
                }
            }
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
            tileParent.PutPiece(newPieceBehaviour);

            return newPieceBehaviour;
        }

        private void Start()
        {
            Initialize(new Board(StartBoard));
            StartCoroutine(RunBoardPhases());
        }
    }
}