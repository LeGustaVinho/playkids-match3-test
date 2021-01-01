using System;
using System.Collections;
using System.Collections.Generic;
using LegendaryTools;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Playkids.UI
{
    public enum ScreenType
    {
        Menu,
        SelectSelect,
        Match3,
        EndGame,
    }
    
    public class UIController : SingletonBehaviour<UIController>
    {
        public List<UIScreen> Screens = new List<UIScreen>();
        public UIScreen StartScreen;
        
        private UIScreen baseScreen;
        private readonly Stack<UIScreen> popups = new Stack<UIScreen>();
        private readonly Dictionary<ScreenType, UIScreen> screensLookup = new Dictionary<ScreenType, UIScreen>();
        private Coroutine screenTransitionRoutine;
        
        protected override void Awake()
        {
            base.Awake();
            foreach (UIScreen screen in Screens)
            {
                screensLookup.Add(screen.Type, screen);
            }
        }

        protected override void Start()
        {
            base.Start();
            GoTo(StartScreen.Type);
        }

        [Button]
        public void GoTo(ScreenType type, object args = null)
        {
            screenTransitionRoutine = StartCoroutine(goTo(type, args));
        }

        /// <summary>
        /// Transit to a screen
        /// </summary>
        /// <param name="type"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private IEnumerator goTo(ScreenType type, object args = null)
        {
            if (!screensLookup.TryGetValue(type, out UIScreen targetScreen))
            {
                yield break; //Screen not found
            }
            
            if (baseScreen != null)
            {
                if (targetScreen.IsPopup)
                {
                    popups.Push(targetScreen);
                }
                else
                {
                    //Close all popups
                    while (popups.Count > 0)
                    {
                        UIScreen popUpScreen = popups.Pop();
                        yield return StartCoroutine(popUpScreen.HideRoutine(args));
                        popUpScreen.OnHide(args);
                        popUpScreen.gameObject.SetActive(false);
                    }
                    
                    //Close current screen
                    yield return StartCoroutine(baseScreen.HideRoutine(args));
                    baseScreen.OnHide(args);
                    baseScreen.gameObject.SetActive(false);
                    baseScreen = targetScreen;
                }
                
                //Open new screen
                targetScreen.gameObject.SetActive(true);
                yield return StartCoroutine(targetScreen.ShowRoutine(args));
                targetScreen.OnShow(args);
            }
            else
            {
                baseScreen = StartScreen;
                baseScreen.gameObject.SetActive(true);
                yield return StartCoroutine(targetScreen.ShowRoutine(args));
                targetScreen.OnShow(args);
            }
        }

        private IEnumerator ClosePopupRoutine(object args = null)
        {
            if (popups.Count > 0)
            {
                UIScreen popup = popups.Pop();
                
                yield return popup.HideRoutine();
                popup.OnHide(args);
                popup.gameObject.SetActive(false);
            }
        }
        
        public void ClosePopup(object args = null)
        {
            screenTransitionRoutine = StartCoroutine(ClosePopupRoutine());
        }
    }
}