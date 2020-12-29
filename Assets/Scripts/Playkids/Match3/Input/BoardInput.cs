using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Playkids.Match3
{
    public class BoardInput : MonoBehaviour
    {
        public GraphicRaycaster GraphicRaycaster;
        public EventSystem EventSystem;

        private bool isMoving;
        private Vector3 startPosition;
        private PieceBehaviour selectedPiece;
        
        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            DesktopInput();
#elif UNITY_ANDROID || UNITY_IPHONE
            MobileInput();
#endif
        }

        private void DesktopInput()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                selectedPiece = TryGetPieceWithRaycast(Input.mousePosition);
                if (selectedPiece != null)
                {
                    isMoving = true;
                    startPosition = Input.mousePosition;
                }
            }

            if (Input.GetKeyUp(KeyCode.Mouse0) && isMoving)
            {
                ProcessSwap(selectedPiece, startPosition, Input.mousePosition);

                startPosition = Vector3.zero;
                selectedPiece = null;
                isMoving = false;
            }
        }
        
        private void MobileInput()
        {
        }

        private PieceBehaviour TryGetPieceWithRaycast(Vector3 raycastPosition)
        {
            PointerEventData pointerEventData = new PointerEventData(EventSystem) {position = Input.mousePosition};

            List<RaycastResult> results = new List<RaycastResult>();
            GraphicRaycaster.Raycast(pointerEventData, results);

            foreach (RaycastResult result in results)
            {
                PieceBehaviour pieceView = result.gameObject.GetComponent<PieceBehaviour>();
                if (pieceView != null)
                {
                    return pieceView;
                }
            }

            return null;
        }
        
        private void ProcessSwap(PieceBehaviour pieceView, Vector3 pressPosition, Vector3 releasePosition)
        {
            Vector2Int dragDirection = GetDragDirection(pressPosition, releasePosition);
            
            if(pieceView.TileView.Tile.Neighbors.TryGetValue(dragDirection, out Tile targetTile))
            {
                TileBehaviour targetTileView = pieceView.TileView.BoardView.GetTileAt(targetTile.Position);

                if (targetTileView != null)
                {
                    pieceView.TileView.BoardView.TrySwapPiece(pieceView.TileView, targetTileView);
                }
            }
        }

        private Vector2Int GetDragDirection(Vector2 pressPosition, Vector2 releasePosition)
        {
            Vector3 dragDirection = (pressPosition - releasePosition).normalized;

            if (Mathf.Abs(dragDirection.x) > Mathf.Abs(dragDirection.y))
            {
                return dragDirection.x > 0 ? Vector2Int.left : Vector2Int.right;
            }
            else
            {
                return dragDirection.y > 0 ? Vector2Int.up : Vector2Int.down;
            }
        }
    }
}