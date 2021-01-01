using UnityEngine;
using UnityEngine.EventSystems;

namespace Playkids.UI
{
    public class UIClickSound : MonoBehaviour, IPointerClickHandler
    {
        public AudioClip Clip;
        public void OnPointerClick(PointerEventData eventData)
        {
            AudioSource.PlayClipAtPoint(Clip, Camera.main.transform.position);
        }
    }
}