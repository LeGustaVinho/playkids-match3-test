using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Playkids.Match3
{
    [CreateAssetMenu(fileName = "PiecesDB", menuName = "Match3/DBs/Create PiecesDB")]
    [SingletonScriptableObject(UseAsset = true)]
    public class PiecesDB : SingletonSerializedScriptableObject<PiecesDB>
    {
        public Dictionary<PieceType, PieceConfig> AllPieces = new Dictionary<PieceType, PieceConfig>();

        public List<PieceConfig> FindAll(Predicate<PieceConfig> match)
        {
            List<PieceConfig> found = new List<PieceConfig>();
            foreach (KeyValuePair<PieceType, PieceConfig> pair in AllPieces)
            {
                if (match.Invoke(pair.Value))
                {
                    found.Add(pair.Value);
                }
            }

            return found;
        }

#if UNITY_EDITOR
        [Button]
        public void FindAllPieces()
        {
            string[] pieces = AssetDatabase.FindAssets("t:" + nameof(PieceConfig));

            foreach (string piece in pieces)
            {
                PieceConfig instance = AssetDatabase.LoadAssetAtPath<PieceConfig>(
                    AssetDatabase.GUIDToAssetPath(piece));

                if (!AllPieces.ContainsKey(instance.Type))
                {
                    AllPieces.Add(instance.Type, instance);
                }
            }
        }
#endif
    }
}