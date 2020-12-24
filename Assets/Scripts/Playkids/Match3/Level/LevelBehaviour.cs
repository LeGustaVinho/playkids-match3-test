using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Playkids.Match3
{
    public class LevelBehaviour : SerializedMonoBehaviour
    {
        public BoardConfig BoardConfig;
        public Board Board;

        public PatternSearchResult matches;

        private void Start()
        {
            Board = new Board(BoardConfig);

            UpdatePatternDebug();
        }

        [Button]
        public void UpdatePatternDebug()
        {
            matches = Board.FindPatterns();
        }
    }
}