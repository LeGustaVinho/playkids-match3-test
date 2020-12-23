using Sirenix.OdinInspector;
using UnityEngine;

namespace Playkids.Match3
{
    public class Foo
    {
        public int i = 0;
    }
    
    [CreateAssetMenu(fileName = "New MatchPatternConfig", menuName = "Match3/Create MatchPatternConfig")]
    public class MatchPatternConfig : SerializedScriptableObject
    {
        [TableMatrix]
        public Foo[,] Pattern;

        public int Score;
    }
}