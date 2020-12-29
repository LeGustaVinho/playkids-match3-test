using Sirenix.OdinInspector;

namespace Playkids.Match3
{
    public enum BoardChangeAction
    {
        PieceCreation,
        PieceMove,
        PieceDestroy,
        BoardShuffle,
        BoardShuffleLimitReached,
        PhaseTransition,
    }
    
    public class BoardChangeLogEntry
    {
        public Tile FromTile;
        public Tile ToTile;
        [ShowInInspector]
        public Piece Piece { private set; get; }
        [ShowInInspector]
        public BoardChangeAction Action { private set; get; }

        public BoardChangeLogEntry(Tile fromTile, Tile toTile, Piece piece)
        {
            FromTile = fromTile;
            ToTile = toTile;
            Piece = piece;
            Action = BoardChangeAction.PieceMove;
        }
        
        public BoardChangeLogEntry(Tile toTile, Piece piece, BoardChangeAction action)
        {
            ToTile = toTile;
            Piece = piece;
            Action = action;
        }
        
        public BoardChangeLogEntry(BoardChangeAction action)
        {
            Action = action;
        }
    }
}