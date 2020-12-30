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

        public BoardBehaviour BoardView { private set; get; }

        public void Initialize(BoardBehaviour boardView, Tile tile)
        {
            this.BoardView = boardView;
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
                pieceView.Transform.localPosition = BoardView.PiecePrefab.transform.position;
                pieceView.RectTransform.sizeDelta = BoardView.CellSize;
                
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
                releasedPiece.Transform.SetParent(BoardView.BoardArea);

                return releasedPiece;
            }

            return null;
        }
    }
}
