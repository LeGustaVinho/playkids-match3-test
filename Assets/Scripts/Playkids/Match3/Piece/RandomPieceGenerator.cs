using UnityEngine;

namespace Playkids.Match3
{
    [CreateAssetMenu(fileName = "New RandomPieceGenerator", menuName = "Match3/PieceGenerator/Create RandomPieceGenerator")]
    public class RandomPieceGenerator : PieceGeneratorConfig
    {
        public override Piece GeneratePiece()
        {
            return null;
        }
    }
}