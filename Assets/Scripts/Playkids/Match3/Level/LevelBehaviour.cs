using System;
using System.Collections;
using Playkids.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Playkids.Match3
{
    public class LevelBehaviour : SerializedMonoBehaviour, IDisposable
    {
        [Required] public BoardBehaviour BoardBehaviour;

        [BoxGroup("UI")] public TextMeshProUGUI ScoreCurrentText;
        [BoxGroup("UI")] public TextMeshProUGUI ScoreGoalText;
        [BoxGroup("UI")] public TextMeshProUGUI TimerText;
        [BoxGroup("UI")] public string TimerFormat = "{0:00}:{1:00}";

        [BoxGroup("UI")] public Animation ShufflingFeedback;
        [BoxGroup("UI")] public Animation ShufflingLimitReachedFeedback;

        private LevelConfig currentLevel;
        private DateTime roundExpireDateTime;
        private Coroutine updateTimerRoutine;
        private int scoreCurrent;
        private int scoreGoal;

        public void LoadLevel(LevelConfig levelConfig)
        {
            currentLevel = levelConfig;
            BoardBehaviour.LoadBoard(levelConfig.Board);
            scoreCurrent = 0;
            scoreGoal = levelConfig.RequiredScore;
            roundExpireDateTime = DateTime.Now.AddSeconds(levelConfig.Duration);
            updateTimerRoutine = StartCoroutine(UpdateTimer());

            UpdateUI();
        }

        public void Restart()
        {
            Dispose();
            LoadLevel(currentLevel);
        }

        public void Dispose()
        {
            BoardBehaviour.Dispose();
            StopTimerRoutine();
        }

        private void Start()
        {
            BoardBehaviour.OnMatch += OnMatch;
            BoardBehaviour.OnShuffle += OnShuffle;
            BoardBehaviour.OnShuffleLimitReached += OnShuffleLimitReached;
        }

        private void OnDestroy()
        {
            BoardBehaviour.OnMatch -= OnMatch;
            BoardBehaviour.OnShuffle -= OnShuffle;
            BoardBehaviour.OnShuffleLimitReached -= OnShuffleLimitReached;
        }

        private void OnMatch(PatternFound patternFound)
        {
            scoreCurrent += patternFound.PiecePatternConfig.Score;
            if (scoreCurrent >= scoreGoal)
            {
                LevelUpRound();
            }
            else
            {
                UpdateUI();
            }
        }

        private void OnShuffleLimitReached()
        {
            StartCoroutine(ShuffleLimitReachedRoutine());
        }

        private void OnShuffle()
        {
            ShufflingFeedback.Play();
        }

        private IEnumerator ShuffleLimitReachedRoutine()
        {
            ShufflingLimitReachedFeedback.Play();

            yield return new WaitUntil(() => !ShufflingLimitReachedFeedback.isPlaying);

            StopTimerRoutine();
            UIController.Instance.GoTo(ScreenType.EndGame);
        }

        private IEnumerator UpdateTimer()
        {
            TimeSpan timeToExpire = roundExpireDateTime - DateTime.Now;

            if (timeToExpire.TotalSeconds > 0)
            {
                TimerText.text = string.Format(TimerFormat,
                    timeToExpire.Minutes,
                    timeToExpire.Seconds);

                yield return new WaitForSeconds(1);

                updateTimerRoutine = StartCoroutine(UpdateTimer());
            }
            else
            {
                UIController.Instance.GoTo(ScreenType.EndGame);
            }
        }

        private void LevelUpRound()
        {
            roundExpireDateTime = DateTime.Now.AddSeconds(currentLevel.Duration);
            scoreGoal += currentLevel.ScoreAddAfterRound;

            UpdateUI();
        }

        private void StopTimerRoutine()
        {
            if (updateTimerRoutine != null)
            {
                StopCoroutine(updateTimerRoutine);
                updateTimerRoutine = null;
            }
        }

        private void UpdateUI()
        {
            ScoreCurrentText.text = scoreCurrent.ToString();
            ScoreGoalText.text = scoreGoal.ToString();
        }
    }
}