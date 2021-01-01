using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Playkids.Match3
{
    public class PatternSearchResult
    {
        public readonly List<PatternFound> Matchs = new List<PatternFound>();
        public readonly List<PatternFound> Hints = new List<PatternFound>();

        public int TotalCount => Matchs.Count + Hints.Count;
    }

    public class PatternFound
    {
        [ShowInInspector]
        public PiecePatternConfig PiecePatternConfig { private set; get; }
        public readonly List<Tile> Tiles;
            
        public PatternFound(PiecePatternConfig piecePatternConfig, List<Tile> tiles)
        {
            Tiles = tiles;
            PiecePatternConfig = piecePatternConfig;
        }
    }
}