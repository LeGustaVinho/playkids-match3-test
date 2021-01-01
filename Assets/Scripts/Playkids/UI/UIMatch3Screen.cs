using Playkids.Match3;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Playkids.UI
{
    public class UIMatch3Screen : UIScreen
    {
        [Required]
        public LevelBehaviour LevelBehaviour;

        public Button MenuButton;
        
        public AudioSource BGM;

        private void Start()
        {
            MenuButton.onClick.AddListener(OnClickMenu);
        }

        private void OnDestroy()
        {
            MenuButton.onClick.RemoveListener(OnClickMenu);
        }

        private void OnClickMenu()
        {
            UIController.Instance.GoTo(ScreenType.Menu);
        }

        public override void Show(object args = null)
        {
            base.Show(args);
            
            if (args is LevelConfig levelConfig)
            {
                LevelBehaviour.LoadLevel(levelConfig);
            }
        }

        public override void OnShow(object args)
        {
            BGM.Play();
        }

        public override void OnHide(object args)
        {
            LevelBehaviour.Dispose();
            BGM.Stop();
        }
    }
}