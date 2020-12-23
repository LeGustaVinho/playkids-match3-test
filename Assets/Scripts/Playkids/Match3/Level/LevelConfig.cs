using Sirenix.OdinInspector;
using UnityEngine;

namespace Playkids.Match3
{
    [CreateAssetMenu(fileName = "New LevelConfig", menuName = "Match3/Create LevelConfig")]
    public class LevelConfig : SerializedScriptableObject
    {
        public string ID;
        public string Name;

        [SuffixLabel("seconds")]
        public float Duration;
        public int RequiredScore;
        public int ScoreAddAfterRound;

        public BoardConfig Board;
    }
}