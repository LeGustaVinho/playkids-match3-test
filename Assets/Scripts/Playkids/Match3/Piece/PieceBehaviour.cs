using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Playkids.Match3
{
    public class PieceBehaviour : SerializedMonoBehaviour
    {
        public Image Icon;
        [ShowInInspector] public TileBehaviour TileView { private set; get; }
        public bool IsPlaced => TileView != null;
        [ShowInInspector] public Piece Piece { private set; get; }

        public Animation Animation;

        public Dictionary<BoardChangeAction, AnimationClip> AnimationClips =
            new Dictionary<BoardChangeAction, AnimationClip>();

        public RectTransform RectTransform;
        public Transform Transform;

        private bool pieceIsBeingMoved;
        private PointerEventData pointerEntryPoint;
        
        public void Initialize(Piece piece)
        {
            Piece = piece;
            Icon.sprite = piece.Config.Image;

            RectTransform = GetComponent<RectTransform>();
            Transform = GetComponent<Transform>();
        }

        public void BindTile(TileBehaviour tile)
        {
            TileView = tile;
        }

        public void PlayAnimation(BoardChangeAction action)
        {
            if (AnimationClips.TryGetValue(action, out AnimationClip clip))
            {
                Animation.Play(clip.name);
            }
        }
        
        public async Task PlayAnimationAsync(BoardChangeAction action)
        {
            PlayAnimation(action);

            while (Animation.isPlaying)
            {
                await Task.Yield();
            }
        }
        
        public IEnumerator PlayAnimationRoutine(BoardChangeAction action)
        {
            PlayAnimation(action);

            while (Animation.isPlaying)
            {
                yield return null;
            }
        }
    }
}