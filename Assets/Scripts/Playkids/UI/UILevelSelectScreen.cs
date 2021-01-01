using LegendaryTools.UI;
using Playkids.Match3;
using UnityEngine.UI;

namespace Playkids.UI
{
    public class UILevelSelectScreen : UIScreen
    {
        public Button CloseButton;
        public GameObjectListing<UILevel, LevelConfig> LevelListing = new GameObjectListing<UILevel, LevelConfig>();
        
        private void Start()
        {
            CloseButton.onClick.AddListener(OnClickClose);
        }

        private void OnDestroy()
        {
            CloseButton.onClick.RemoveListener(OnClickClose);
        }

        private void OnClickClose()
        {
            UIController.Instance.ClosePopup();
        }

        public override void Show(object args = null)
        {
            base.Show();
            LevelListing.GenerateList(LevelDB.Instance.AllLevels.ToArray());
        }

        public override void OnShow(object args)
        {
            
        }

        public override void OnHide(object args)
        {
            LevelListing.DestroyAll();
        }
    }
}