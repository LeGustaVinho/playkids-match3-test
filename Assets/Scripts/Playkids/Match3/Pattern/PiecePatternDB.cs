using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Playkids.Match3
{
    [CreateAssetMenu(fileName = "MatchPatternDB", menuName = "Match3/DBs/Create MatchPatternDB")]
    [SingletonScriptableObject(UseAsset = true)]
    public class PiecePatternDB : SingletonSerializedScriptableObject<PiecePatternDB>
    {
        public List<PiecePatternConfig> MatchPatterns = new List<PiecePatternConfig>();
        public List<PiecePatternConfig> HintPatterns = new List<PiecePatternConfig>();
        
#if UNITY_EDITOR
        [Button]
        public void FindAllPatterns()
        {
            string[] pieces = AssetDatabase.FindAssets("t:" + nameof(PiecePatternConfig));

            foreach (string piece in pieces)
            {
                PiecePatternConfig instance = AssetDatabase.LoadAssetAtPath<PiecePatternConfig>(
                    AssetDatabase.GUIDToAssetPath(piece));

                if (instance.IsHint)
                {
                    if (!HintPatterns.Contains(instance))
                    {
                        HintPatterns.Add(instance);
                    }
                }
                else
                {
                    if (!MatchPatterns.Contains(instance))
                    {
                        MatchPatterns.Add(instance);
                    }
                }
                
            }
        }
#endif
    }
}