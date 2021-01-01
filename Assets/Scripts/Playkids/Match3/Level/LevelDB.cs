using System.Collections.Generic;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Playkids.Match3
{
    [CreateAssetMenu(fileName = "LevelDB", menuName = "Match3/DBs/Create LevelDB")]
    [SingletonScriptableObject(UseAsset = true)]
    public class LevelDB : SingletonSerializedScriptableObject<LevelDB>
    {
        public List<LevelConfig> AllLevels = new List<LevelConfig>();

#if UNITY_EDITOR
        [Button]
        public void FindAllPieces()
        {
            string[] pieces = AssetDatabase.FindAssets("t:" + nameof(LevelConfig));

            foreach (string piece in pieces)
            {
                LevelConfig instance = AssetDatabase.LoadAssetAtPath<LevelConfig>(
                    AssetDatabase.GUIDToAssetPath(piece));

                if (!AllLevels.Contains(instance))
                {
                    AllLevels.Add(instance);
                }
            }
        }
#endif
    }
}