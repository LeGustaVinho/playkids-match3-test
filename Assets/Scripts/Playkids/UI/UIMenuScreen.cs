using System;
using UnityEngine.UI;

namespace Playkids.UI
{
    public class UIMenuScreen : UIScreen
    {
        public Button StartButton;

        private void Start()
        {
            StartButton.onClick.AddListener(OnClickStart);
        }

        private void OnDestroy()
        {
            StartButton.onClick.RemoveListener(OnClickStart);
        }

        private void OnClickStart()
        {
            UIController.Instance.GoTo(ScreenType.SelectSelect);
        }

        public override void OnShow(object args)
        {
        }

        public override void OnHide(object args)
        {
        }
    }
}