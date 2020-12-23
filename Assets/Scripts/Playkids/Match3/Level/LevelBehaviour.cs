using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Playkids.Match3
{
    public class LevelBehaviour : SerializedMonoBehaviour
    {
        public BoardConfig BoardConfig;
        public Board Board;

        private void Start()
        {
            Board = new Board(BoardConfig);
        }
    }
}