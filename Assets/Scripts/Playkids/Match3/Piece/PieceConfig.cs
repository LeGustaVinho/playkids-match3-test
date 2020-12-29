using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Playkids.Match3
{
    [CreateAssetMenu(fileName = "New PieceConfig", menuName = "Match3/Create PieceConfig")]
    public class PieceConfig : SerializedScriptableObject
    {
        public PieceCategory Category;
        public PieceType Type;
        public List<PieceType> MatchingWhitelist = new List<PieceType>();
        public Sprite Image;
    }
}