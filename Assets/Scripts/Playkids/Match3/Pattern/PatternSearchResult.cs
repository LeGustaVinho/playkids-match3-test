using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Playkids.Match3
{
    public class PatternSearchResult
    {
        public List<PatternFound> Matchs = new List<PatternFound>();
        public List<PatternFound> Hints = new List<PatternFound>();
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