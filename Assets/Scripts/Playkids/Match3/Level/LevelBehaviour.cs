using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Playkids.Match3
{
    public class LevelBehaviour : SerializedMonoBehaviour
    {
        public BoardConfig BoardConfig;
        public Guid[,] beforeShuffle;
        public Guid[,] afterShuffle;
        public Board Board;
        public List<BoardChangeLogEntry> Changes = new List<BoardChangeLogEntry>();
        public PatternSearchResult matches;

        public Tile TileGetter;
        public List<Tile> availableTiles;

        private void Start()
        {
            Board = new Board(BoardConfig);
            generateGuidMap(Board, ref beforeShuffle);

            UpdatePatternDebug();
        }

        [Button]
        public void UpdatePatternDebug()
        {
            matches = Board.FindPatterns();
        }
        
        [Button]
        public void RunBoardPhases()
        {
            Changes = Board.RunBoardPhases();
            generateGuidMap(Board, ref afterShuffle);
        }

        private void generateGuidMap(Board board, ref Guid[,] guidMap)
        {
            guidMap = new Guid[board.Config.BoardSize.x,board.Config.BoardSize.y];
            
            for (int i = 0; i < board.Config.BoardSize.x; i++)
            {
                for (int j = 0; j < board.Config.BoardSize.y; j++)
                {
                    guidMap[i, j] = board.GetTileAt(i, j).Piece.GUID;
                }
            }
        }

        [Button]
        public void GetTileAt(int x, int y)
        {
            TileGetter = Board.GetTileAt(x, y);
        }

        [Button]
        public void ShufflePieces()
        {
            List<BoardChangeLogEntry> changes = new List<BoardChangeLogEntry>();

            if (availableTiles == null)
            {
                availableTiles = Board.GetAllTilesWithPieces();
            }

            if(availableTiles.Count > 1)
            {
                int rndIndex1 = UnityEngine.Random.Range(0, availableTiles.Count);
                Tile tile1 = availableTiles[rndIndex1];
                Piece piece1 = tile1.ReleasePiece();
                availableTiles.RemoveAt(rndIndex1);

                int rndIndex2 = UnityEngine.Random.Range(0, availableTiles.Count);
                Tile tile2 = availableTiles[rndIndex2];
                Piece piece2 = tile2.ReleasePiece();
                availableTiles.RemoveAt(rndIndex2);

                if (Board.MovePieceTo(piece1, tile2) && Board.MovePieceTo(piece2, tile1))
                {
                    changes.Add(new BoardChangeLogEntry(tile1, tile2, piece1));
                    changes.Add(new BoardChangeLogEntry(tile2, tile1, piece2));
                }
            }

            Changes.AddRange(changes);

            generateGuidMap(Board, ref afterShuffle);
        }
    }
}