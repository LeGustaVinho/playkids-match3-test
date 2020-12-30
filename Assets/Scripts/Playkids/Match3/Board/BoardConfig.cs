using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Playkids.Match3
{
    [CreateAssetMenu(fileName = "New BoardConfig", menuName = "Match3/Create BoardConfig")]
    public class BoardConfig : SerializedScriptableObject
    {
        public enum BoardViewType
        {
            Tile,
            Gravity,
            Piece,
            PieceGenerator,
        }

        [OnValueChanged("EditorOnBoardSizeChange")]
        public Vector2Int BoardSize = new Vector2Int(3, 3);

        [HideLabel, EnumToggleButtons] 
        public BoardViewType ViewMode = BoardViewType.Tile;

        [TableMatrix, ShowIf("ViewMode", BoardViewType.Tile)]
        public TileType[,] BoardTileTypeMatrix = new TileType[3, 3];

        [TableMatrix, ShowIf("ViewMode", BoardViewType.Gravity)]
        public GravityDirection[,] BoardGravityDirectionMatrix = new GravityDirection[3, 3];

        [TableMatrix, ShowIf("ViewMode", BoardViewType.Piece)]
        public PieceType[,] BoardPieceTypesMatrix = new PieceType[3, 3];
        
        [TableMatrix, ShowIf("ViewMode", BoardViewType.PieceGenerator)]
        public PieceGeneratorConfig[,] BoardPieceGeneratorMatrix = new PieceGeneratorConfig[3, 3];

        public bool AutoGeneratePieceInEmpty = true;
        
        public Tile[][] Board
        {
            get
            {
                Tile[][] board = new Tile[BoardSize.x][];

                for (int x = 0; x < BoardSize.x; x++)
                {
                    board[x] = new Tile[BoardSize.y];
                    for (int y = 0; y < BoardSize.y; y++)
                    {
                        if (BoardPieceTypesMatrix[x, y] != PieceType.Empty)
                        {
                            if (PiecesDB.Instance.AllPieces.TryGetValue(BoardPieceTypesMatrix[x, y],
                                out PieceConfig pieceConfig))
                            {
                                board[x][y] = new Tile(x, y, BoardTileTypeMatrix[x, y],
                                    BoardGravityDirectionMatrix[x, y], BoardPieceGeneratorMatrix[x,y], pieceConfig);
                            }
                            else
                            {
                                Debug.LogError(
                                    $"Could not find the config of the piece of type {BoardPieceTypesMatrix[x, y]}");
                            }
                        }
                        else
                        {
                            board[x][y] = new Tile(x, y, BoardTileTypeMatrix[x, y],
                                BoardGravityDirectionMatrix[x, y], BoardPieceGeneratorMatrix[x,y]);
                        }
                    }
                }

                return board;
            }
        }


#if UNITY_EDITOR
        private void EditorOnBoardSizeChange()
        {
            ResizeBidimArrayWithElements(ref BoardTileTypeMatrix, BoardSize);
            ResizeBidimArrayWithElements(ref BoardGravityDirectionMatrix, BoardSize);
            ResizeBidimArrayWithElements(ref BoardPieceTypesMatrix, BoardSize);
            ResizeBidimArrayWithElements(ref BoardPieceGeneratorMatrix, BoardSize);
        }

        private static void ResizeBidimArrayWithElements<T>(ref T[,] original, Vector2Int size)
        {
            T[,] newArray = new T[size.x, size.y];
            int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
            int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

            for (int i = 0; i < minX; ++i)
            {
                Array.Copy(original, i * original.GetLength(1),
                    newArray, i * newArray.GetLength(1), minY);
            }

            original = newArray;
        }
#endif
    }
}