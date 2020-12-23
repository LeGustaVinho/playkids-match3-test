using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Playkids.Match3
{
    public class Board
    {
        [ShowInInspector]
        public BoardConfig Config { private set; get; }

        [ShowInInspector]
        private readonly Tile[][] tiles;

        public Board(BoardConfig config)
        {
            Config = config;
            tiles = config.Board;

            for (int x = 0; x < tiles.Length; x++)
            {
                for (int y = 0; y < tiles[x].Length; y++)
                {
                    tiles[x][y].Initialize(this);

                    if (!tiles[x][y].HasPiece)
                    {
                        Piece newPiece = GenerateRandomBasicPiece();
                        MovePieceTo(newPiece, tiles[x][y]);
                    }
                }
            }
        }

        public Tile GetTileAt(Vector2Int position)
        {
            if (position.x >= 0 && position.x < tiles.Length)
            {
                if (position.y >= 0 && position.y < tiles[position.x].Length)
                {
                    return tiles[position.x][position.y];
                }
            }

            return null;
        }

        public bool MovePieceTo(Piece piece, Tile tile)
        {
            if (!piece.IsPlaced && !tile.HasPiece)
            {
                piece.Tile = tile;
                tile.Piece = piece;
                return true;
            }

            return false;
        }

        private Piece GenerateRandomBasicPiece()
        {
             List<PieceConfig> basicPieces = PiecesDB.Instance.FindAll(item => item.Category == PieceCategory.Basic);
             return new Piece(basicPieces[UnityEngine.Random.Range(0, basicPieces.Count)]);
        }
    }
}
