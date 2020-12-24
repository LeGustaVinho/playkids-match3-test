using System;
using Sirenix.OdinInspector;

namespace Playkids.Match3
{
    public class Piece
    {
        [ShowInInspector]
        public Guid GUID { private set; get; }
        
        [ShowInInspector]
        public PieceConfig Config { private set; get; }
        
        public Tile Tile;
        public bool IsPlaced => Tile != null;
        
        public Piece(PieceConfig config)
        {
            GUID = Guid.NewGuid();
            Config = config;
        }
        
        public Piece(PieceConfig config, Tile tile) : this(config)
        {
            Tile = tile;
        }

        public virtual void OnDestroy()
        {
            
        }
    }
}