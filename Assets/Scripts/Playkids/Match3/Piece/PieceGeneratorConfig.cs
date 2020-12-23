using Sirenix.OdinInspector;

namespace Playkids.Match3
{
    public abstract class PieceGeneratorConfig : SerializedScriptableObject, IPieceGenerator
    {
        public abstract Piece GeneratePiece();
    }
}