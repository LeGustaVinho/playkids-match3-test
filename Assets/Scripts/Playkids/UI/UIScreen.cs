using System.Collections;
using LegendaryTools;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Playkids.UI
{
    [RequireComponent(typeof(Animation))]
    public abstract class UIScreen : SerializedMonoBehaviour
    {
        public ScreenType Type;
        
        public Animation Animation;
        public AnimationClip ShowAnimation;
        public AnimationClip HideAnimation;

        public bool IsPopup;
        
        public virtual void Show(object args = null)
        {
            Animation.PlayForward(ShowAnimation.name);
        }

        public virtual void Hide(object args = null)
        {
            if (HideAnimation != null)
            {
                Animation.PlayForward(HideAnimation.name);
            }
            else
            {
                Animation.PlayBackward(ShowAnimation.name);
            }
        }
        
        public IEnumerator ShowRoutine(object args = null)
        {
            Show(args);

            yield return new WaitUntil(() => !Animation.isPlaying);
        }

        public IEnumerator HideRoutine(object args = null)
        {
            Hide(args);

            yield return new WaitUntil(() => !Animation.isPlaying);
        }

        public abstract void OnShow(object args);

        public abstract void OnHide(object args);
    }
}