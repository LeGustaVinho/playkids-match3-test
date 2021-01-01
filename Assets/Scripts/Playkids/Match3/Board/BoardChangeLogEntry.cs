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
        PieceMoveShuffle,
        PieceMatch,
    }
    
    public class BoardChangeLogEntry
    {
        public Tile FromTile { private set; get; }
        public Tile ToTile { private set; get; }
        
        public Piece FromPiece { private set; get; }
        public Piece ToPiece { private set; get; }
        
        [ShowInInspector]
        public Piece Piece { private set; get; }

        public PatternFound PieceMatchPattern { private set; get; }
        
        [ShowInInspector]
        public BoardChangeAction Action { private set; get; }

        public BoardChangeLogEntry(Tile fromTile, Tile toTile, Piece piece)
        {
            FromTile = fromTile;
            ToTile = toTile;
            Piece = piece;
            Action = BoardChangeAction.PieceMove;
        }
        
        public BoardChangeLogEntry(Tile fromTile, Tile toTile, Piece fromPiece, Piece toPiece)
        {
            FromTile = fromTile;
            ToTile = toTile;
            FromPiece = fromPiece;
            ToPiece = toPiece;
            Action = BoardChangeAction.PieceMoveShuffle;
        }
        
        public BoardChangeLogEntry(Tile toTile, Piece piece, BoardChangeAction action)
        {
            ToTile = toTile;
            Piece = piece;
            Action = action;
        }
        
        public BoardChangeLogEntry(PatternFound pieceMatchPattern)
        {
            PieceMatchPattern = pieceMatchPattern;
            Action = BoardChangeAction.PieceMatch;
        }
        
        public BoardChangeLogEntry(BoardChangeAction action)
        {
            Action = action;
        }
    }
}