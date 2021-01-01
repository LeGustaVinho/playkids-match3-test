using LegendaryTools.UI;
using Playkids.Match3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Playkids.UI
{
    public class UILevel : MonoBehaviour, GameObjectListing<UILevel, LevelConfig>.IListingItem
    {
        public TextMeshProUGUI LevelName;
        public Button LevelButton;

        private LevelConfig levelConfig;
        
        public void Init(LevelConfig item)
        {
            levelConfig = item;
            LevelName.text = item.Name;
            LevelButton.onClick.AddListener(OnClickLevel);
        }

        private void OnClickLevel()
        {
            UIController.Instance.GoTo(ScreenType.Match3, levelConfig);
        }
    }
}