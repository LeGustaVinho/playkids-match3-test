namespace Playkids.Match3
{
    public enum BoardChangeAction
    {
        PieceCreation,
        PieceMove,
        PieceDestroy,
        BoardShuffle,
        PhaseTransition,
    }
    
    public class BoardChangeLogEntry
    {
        public Tile FromTile { private set; get; }
        public Tile ToTile { private set; get; }
        public Piece Piece { private set; get; }
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