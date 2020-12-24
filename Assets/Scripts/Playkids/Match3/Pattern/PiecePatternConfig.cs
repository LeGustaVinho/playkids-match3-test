using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Playkids.Match3
{
    [CreateAssetMenu(fileName = "New MatchPatternConfig", menuName = "Match3/Create MatchPatternConfig")]
    public class PiecePatternConfig : SerializedScriptableObject
    {
        [TableMatrix]
        public bool[,] Pattern = new bool[3,3];

        public int Score;
        public bool IsHint;
        
        [ShowIf("IsHint"), Space]
        public Vector2Int[] HintSwapCoords = new Vector2Int[2];
        
        public List<Vector2Int> PatternCoords
        {
            get
            {
                List<Vector2Int> coords = new List<Vector2Int>();

                for (int x = 0; x < Pattern.GetLength(0); x++)
                {
                    for (int y = 0; y < Pattern.GetLength(1); y++)
                    {
                        if (Pattern[x, y])
                        {
                            coords.Add(new Vector2Int(x, y));
                        }
                    }
                }
                
                return coords;
            }
        }
    }
}