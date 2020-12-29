using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Playkids.Match3
{
    public class TileBehaviour : MonoBehaviour
    {
        [ShowInInspector]
        public PieceBehaviour PieceView { private set; get; }
        public Image Foreground;
        public RectTransform PieceParent;
        public Image Background;
        
        public Tile Tile { private set; get; }
        public bool HasPiece => PieceView != null;

        public RectTransform RectTransform;
        public Transform Transform;

        private BoardBehaviour boardView;

        public void Initialize(BoardBehaviour boardView, Tile tile)
        {
            this.boardView = boardView;
            this.Tile = tile;
            
            RectTransform = GetComponent<RectTransform>();
            Transform = GetComponent<Transform>();
        }

        public bool PutPiece(PieceBehaviour pieceView)
        {
            if (!pieceView.IsPlaced && !HasPiece)
            {
                pieceView.BindTile(this);
                PieceView = pieceView;
                pieceView.Transform.SetParent(PieceParent);
                
                return true;
            }

            return false;
        }

        public PieceBehaviour ReleasePiece()
        {
            if (HasPiece)
            {
                PieceBehaviour releasedPiece = PieceView;
                PieceView.BindTile(null);
                PieceView = null;
                releasedPiece.Transform.SetParent(boardView.BoardArea);

                return releasedPiece;
            }

            return null;
        }
    }
}
