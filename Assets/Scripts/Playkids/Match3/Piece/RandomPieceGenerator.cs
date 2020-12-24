using System.Collections.Generic;
using UnityEngine;

namespace Playkids.Match3
{
    [CreateAssetMenu(fileName = "New RandomPieceGenerator",
        menuName = "Match3/PieceGenerator/Create RandomPieceGenerator")]
    public class RandomPieceGenerator : PieceGeneratorConfig
    {
        public override Piece GeneratePiece()
        {
            List<PieceConfig> basicPieces = PiecesDB.Instance.FindAll(item => item.Category == PieceCategory.Basic);
            return new Piece(basicPieces[Random.Range(0, basicPieces.Count)]);
        }
    }
}