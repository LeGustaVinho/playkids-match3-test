using Playkids.Match3;
using UnityEngine.UI;

namespace Playkids.UI
{
    public class UIEndGameScreen : UIScreen
    {
        public Button MenuButton;
        public Button RestartButton;
        public LevelBehaviour LevelBehaviour;
        private void Start()
        {
            MenuButton.onClick.AddListener(OnClickMenu);
            RestartButton.onClick.AddListener(OnClickRestart);
        }

        private void OnDestroy()
        {
            MenuButton.onClick.RemoveListener(OnClickMenu);
            RestartButton.onClick.RemoveListener(OnClickRestart);
        }

        private void OnClickMenu()
        {
            UIController.Instance.GoTo(ScreenType.Menu);
        }

        private void OnClickRestart()
        {
            UIController.Instance.ClosePopup();
            LevelBehaviour.Restart();
        }
        
        public override void OnShow(object args)
        {
        }

        public override void OnHide(object args)
        {
        }
    }
}