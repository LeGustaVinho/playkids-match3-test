using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Playkids.Match3
{
    public class Tile
    {
        private static Dictionary<GravityDirection, Vector2Int> gravityVector =
            new Dictionary<GravityDirection, Vector2Int>
            {
                {GravityDirection.Down, Vector2Int.up},
                {GravityDirection.Up, Vector2Int.down},
                {GravityDirection.Left, Vector2Int.left},
                {GravityDirection.Right, Vector2Int.right}
            };

        private static List<Vector2Int> neighborsVectors =
            new List<Vector2Int>
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left,
            };

        public int X => Position.x;
        public int Y => Position.y;
        
        [ShowInInspector]
        public Vector2Int Position { get; }

        public TileType Type { get; }
        public GravityDirection GravityDirection { get; }
        public PieceGeneratorConfig PieceGenerator;
        public Piece Piece;
        public bool HasPiece => Piece != null;

        public Tile GravitationalNeighbor;
        public readonly List<Tile> Neighbors = new List<Tile>();

        private Board board;

        public Tile(int x, int y, TileType type, GravityDirection gravityDirection, PieceGeneratorConfig pieceGenerator,
            PieceConfig filledWith = null)
        {
            Position = new Vector2Int(x, y);
            Type = type;
            GravityDirection = gravityDirection;
            PieceGenerator = pieceGenerator;
            
            if (filledWith != null)
            {
                Piece = new Piece(filledWith, this);
            }
        }

        public void Initialize(Board board)
        {
            this.board = board;

            foreach (Vector2Int neighborsVector in neighborsVectors)
            {
                Tile neighbor = board.GetTileAt(Position + neighborsVector);
                if (neighbor != null)
                {
                    Neighbors.Add(neighbor);
                }
            }

            if (Type != TileType.Blocked)
            {
                GravitationalNeighbor = board.GetTileAt(Position + gravityVector[GravityDirection]);
            }
        }

        public void Release()
        {
            if (HasPiece)
            {
                Piece.Tile = null;
                Piece = null;
            }
        }
    }
}