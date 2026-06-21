using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// GameManager — the central orchestrator.
/// 
/// Attach this script to an empty GameObject in your scene.
/// On Awake it creates all other managers (QuestionManager, LifelineManager, UIManager),
/// builds the UI programmatically, and wires up every button.
///
/// Game flow:
///   1. Category selection → player chooses a topic.
///   2. Questions are loaded & filtered → 15 questions picked by difficulty.
///   3. Player answers questions, uses lifelines, or walks away.
///   4. Wrong answer → game over (player keeps safe‑haven prize).
///   5. All 15 correct → player wins $1,000,000.
/// </summary>
namespace MillionaireGame
{
    public class GameManager : MonoBehaviour
    {
        // ── Manager references (created at runtime) ──
        private QuestionManager _questionMgr;
        private LifelineManager _lifelineMgr;
        private UIManager _uiMgr;

        // ── Audio & Effects (Assign in Inspector) ──
        [Header("Audio Clips")]
        public AudioClip audioGameStart;
        public AudioClip audioNewQuestion;
        public AudioClip audioCorrect;
        public AudioClip audioWrong;
        public AudioClip audioWin;
        public AudioClip audioClick;
        public AudioClip audioLifeline5050;
        public AudioClip audioLifelineAudience;
        public AudioClip audioLifelinePhone;
        public AudioClip audioBackground;

        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem _particlesCorrect;
        [SerializeField] private ParticleSystem _particlesWrong;
        [SerializeField] private ParticleSystem _particlesNewQuestion;

        [Header("Dynamic Backgrounds")]
        [SerializeField] private Sprite[] _backgroundSprites;
        [SerializeField] private float _slideshowInterval = 10f;

        [Header("Anti-Copyright Settings")]
        [SerializeField][Range(0.5f, 1.5f)] private float _audioPitch = 0.92f;

        [Header("UI Customization")]
        [SerializeField] private Sprite _settingsButtonSprite;
        public TMP_FontAsset timerFont;

        private AudioSource _audioSource;
        private AudioSource _musicSource;
        private Coroutine _slideshowCoroutine;

        private const string PREF_LANGUAGE = "SelectedLanguage";
        private const string PREF_MUSIC_VOL = "MusicVolume";
        private const string PREF_SFX_VOL = "SFXVolume";
        private const string PREF_MUTE = "MuteAll";

        // ── Game state ──
        private int _currentStep;            // 0‑based ladder step
        private string _currentCategory;
        private string _currentLanguage = "EN";
        private QuestionEntry _currentQuestion;
        private bool _waitingForAnswer;      // prevents double‑clicks
        private float _timer;
        private bool _timerActive;
        private int _consecutiveLosses = 0;
        private int _correctAnswersCount = 0;
        private int _wrongAnswersCount = 0;
        private bool _isViewingExplanation = false;

        private ReminderDatabase _reminderDB;

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            // Create manager components on this same GameObject
            _questionMgr = gameObject.AddComponent<QuestionManager>();
            _lifelineMgr = gameObject.AddComponent<LifelineManager>();
            _uiMgr = GetComponent<UIManager>();
            if (_uiMgr == null) _uiMgr = gameObject.AddComponent<UIManager>();
            _uiMgr.timerFont = timerFont;

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.pitch = _audioPitch;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
        }

        private void Start()
        {
            // Load localization data
            LocalizationManager.LoadData();

            // Build the entire UI programmatically
            _uiMgr.BuildUI();

            // Apply custom settings sprite if assigned
            if (_settingsButtonSprite != null)
                _uiMgr.SetSettingsButtonSprite(_settingsButtonSprite);

            // Wire up constant button listeners
            WireButtons();

            // Start background slideshow immediately (first image shown at once, then every 10s)
            if (_backgroundSprites != null && _backgroundSprites.Length > 0)
                _slideshowCoroutine = StartCoroutine(SlideshowRoutine());

            // Force TR language and skip language screen
            _uiMgr.SetSettingsButtonVisible(true);
            ApplyLanguageAndShowCategories("TR");

            // Also wire up branch buttons in Start
            _uiMgr.PopulateBranchButtons(OnBranchSelected);

            // Initial gradient
            _uiMgr.ChangeBackgroundGradient(Random.Range(0, 10));

            // Initialize background music
            if (audioBackground != null)
            {
                _musicSource.clip = audioBackground;
                _musicSource.Play();
            }

            // Load and apply audio settings
            LoadAudioSettings();
        }

        private void LoadAudioSettings()
        {
            float musicVol = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.7f);
            float sfxVol = PlayerPrefs.GetFloat(PREF_SFX_VOL, 1f);
            bool isMuted = PlayerPrefs.GetInt(PREF_MUTE, 0) == 1;

            _uiMgr.musicVolumeSlider.SetValueWithoutNotify(musicVol);
            _uiMgr.sfxVolumeSlider.SetValueWithoutNotify(sfxVol);
            _uiMgr.muteToggle.SetIsOnWithoutNotify(isMuted);

            ApplyAudioSettings(musicVol, sfxVol, isMuted);
        }

        private void ApplyAudioSettings(float musicVol, float sfxVol, bool muted)
        {
            _musicSource.volume = muted ? 0 : musicVol;
            _audioSource.volume = muted ? 0 : sfxVol;
        }

        private void OnMusicVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat(PREF_MUSIC_VOL, val);
            ApplyAudioSettings(val, _uiMgr.sfxVolumeSlider.value, _uiMgr.muteToggle.isOn);
        }

        private void OnSFXVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat(PREF_SFX_VOL, val);
            ApplyAudioSettings(_uiMgr.musicVolumeSlider.value, val, _uiMgr.muteToggle.isOn);
        }

        private void OnMuteToggled(bool muted)
        {
            PlayerPrefs.SetInt(PREF_MUTE, muted ? 1 : 0);
            ApplyAudioSettings(_uiMgr.musicVolumeSlider.value, _uiMgr.sfxVolumeSlider.value, muted);
        }



        /// <summary>Loads the localized text and shows Branch screen.</summary>
        private void ApplyLanguageAndShowCategories(string language)
        {
            _currentLanguage = language;

            _reminderDB = JsonLoader.LoadReminders("Reminders/Reminders");

            // Apply localized text to UI
            _uiMgr.ApplyLanguage(language);

            // Sync dropdown to current language
            int langIndex = LocalizationManager.AvailableLanguages.FindIndex(l => l.code == language);
            _uiMgr.languageDropdown.SetValueWithoutNotify(langIndex >= 0 ? langIndex : 0);

            // Show reminder before branches
            if (_reminderDB != null && _reminderDB.items != null && _reminderDB.items.Count > 0)
            {
                int rIdx = Random.Range(0, _reminderDB.items.Count);
                string rTitle = LocalizationManager.Get("wisdomOfDay");
                string rText = _reminderDB.items[rIdx].text;
                if (!string.IsNullOrEmpty(_reminderDB.items[rIdx].source))
                    rText += $"\n\n<i>- {_reminderDB.items[rIdx].source}</i>";

                string closeLabel = LocalizationManager.Get("continue");
                _uiMgr.reminderCloseButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = closeLabel;

                _uiMgr.ShowReminderScreen(rTitle, rText, () =>
                {
                    _uiMgr.ShowBranchScreen(true);
                });
            }
            else
            {
                _uiMgr.ShowBranchScreen(true);
            }
        }

        private void OnBranchSelected(string branchCode)
        {
            PlayClickSound();
            Debug.Log($"[GameManager] Branch selected: {branchCode}");
            _questionMgr.LoadDatabase(branchCode);

            if (!_questionMgr.IsReady)
            {
                Debug.LogError($"[GameManager] QuestionManager failed to load branch {branchCode}!");
                return;
            }

            // Populate category buttons
            _uiMgr.PopulateCategoryButtons(
                _questionMgr.AvailableCategories,
                OnCategorySelected
            );

            _uiMgr.ShowBranchScreen(false);
            _uiMgr.ShowCategoryScreen(true);
        }

        private void Update()
        {
            if (_timerActive)
            {
                _timer -= Time.deltaTime;
                _uiMgr.UpdateTimerUI(_timer, true);

                if (_timer <= 0)
                {
                    OnTimeOut();
                }
            }
        }

        private void OnTimeOut()
        {
            _timerActive = false;
            _uiMgr.UpdateTimerUI(0, false);
            _uiMgr.DisableAnswerButtons();
            _uiMgr.btnWalkAway.interactable = false;

            PlayAudio(audioWrong);
            // Don't count as wrong, just unanswered, but for simplicity let's keep wrong count
            _wrongAnswersCount++;

            bool isTurk = (_currentLanguage == "TR");
            string title = isTurk ? "Süre Bitti!" : "Time's Up!";
            string msg = isTurk ? "Sınav süreniz doldu." : "Your exam time is over.";

            _uiMgr.ShowResult(title, $"{msg}" + GenerateKPSSResultReport());
        }

        private void HandleLoss(System.Action onComplete = null)
        {
            _consecutiveLosses++;
            if (_consecutiveLosses >= 3)
            {
                _consecutiveLosses = 0;
                if (GoogleAdMobController.Instance != null)
                {
                    // Reklam kapandığında (veya gösterilemezse) onComplete çağrılır
                    GoogleAdMobController.Instance.ShowInterstitialAd(onComplete);
                    return; // onComplete interstitial callback'inden çağrılacak
                }
            }
            // Reklam eşiğine ulaşılmadıysa veya AdMob yoksa hemen devam et
            onComplete?.Invoke();
        }


        //  BUTTON WIRING
        // ═══════════════════════════════════════════════

        private void WireButtons()
        {
            // Answer buttons
            for (int i = 0; i < _uiMgr.answerButtons.Length; i++)
            {
                int idx = i; // capture for closure
                _uiMgr.answerButtons[i].onClick.AddListener(() => { PlayClickSound(); OnAnswerClicked(idx); });
            }

            // Lifeline buttons
            _uiMgr.btnFiftyFifty.onClick.AddListener(() => { PlayClickSound(); OnFiftyFifty(); });
            _uiMgr.btnAskAudience.onClick.AddListener(() => { PlayClickSound(); OnAskAudience(); });
            _uiMgr.btnPhoneFriend.onClick.AddListener(() => { PlayClickSound(); OnPhoneFriend(); });

            // Audience / phone close buttons
            _uiMgr.audienceCloseButton.onClick.AddListener(() => { PlayClickSound(); _uiMgr.HideAudiencePanel(); });
            _uiMgr.phoneCloseButton.onClick.AddListener(() => { PlayClickSound(); _uiMgr.HidePhonePanel(); });

            // Walk away
            _uiMgr.btnWalkAway.onClick.AddListener(() => { PlayClickSound(); OnWalkAway(); });

            // Explanation
            _uiMgr.btnShowExplanation.onClick.AddListener(() => { PlayClickSound(); OnShowExplanationClicked(); });
            _uiMgr.explanationCloseButton.onClick.AddListener(() =>
            {
                PlayClickSound();
                _uiMgr.HideExplanationPanel();
                _isViewingExplanation = false;
            });

            // Wrong Answer Panel
            _uiMgr.btnShowCorrectAnswer.onClick.AddListener(() => { PlayClickSound(); OnShowCorrectAnswerClicked(); });
            _uiMgr.btnPassQuestion.onClick.AddListener(() => { PlayClickSound(); OnPassQuestionClicked(); });

            // Result → Main menu
            _uiMgr.resultMenuButton.onClick.AddListener(() => { PlayClickSound(); ReturnToMenu(); });

            // Persistent settings gear button (canvas-level, always visible after language chosen)
            _uiMgr.btnSettings.onClick.AddListener(() => { PlayClickSound(); _uiMgr.ShowSettingsPanel(); });
            _uiMgr.settingsCloseButton.onClick.AddListener(() => { PlayClickSound(); _uiMgr.HideSettingsPanel(); });
            // _uiMgr.languageDropdown.onValueChanged.AddListener(OnLanguageChangedFromSettings);

            _uiMgr.musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            _uiMgr.sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            _uiMgr.muteToggle.onValueChanged.AddListener(OnMuteToggled);

            _uiMgr.SetBranchBackAction(() =>
            {
                // Return to menu or do nothing since language is removed
                ReturnToMenu();
            });

            _uiMgr.SetCategoryBackAction(() =>
            {
                // Category panelden geri gelince branch paneline dön
                _uiMgr.ShowBranchScreen(true);
                _uiMgr.SetSettingsButtonVisible(false);
            });

            _uiMgr.SetGameBackAction(() =>
            {
                // Oyun panelinden geri gelince kategori seçimine dön
                // Oyunu sıfırla
                ReturnToMenu();
            });

            _uiMgr.SetGamesCloseAction(() =>
            {
                // Games panelinden çıkınca branch paneline dön
                PlayClickSound();
                _uiMgr.ShowBranchScreen(true);
            });
        }

        // ═══════════════════════════════════════════════
        //  CATEGORY SELECTION
        // ═══════════════════════════════════════════════

        private void OnCategorySelected(string category)
        {
            PlayClickSound();
            _currentCategory = category;
            Debug.Log($"[GameManager] Category selected: {category}");

            bool success = _questionMgr.PrepareQuestions(category);
            if (!success)
            {
                Debug.LogError($"[GameManager] Could not prepare questions for '{category}'.");
                return;
            }

            // Reset lifelines and answer counters
            _lifelineMgr.ResetLifelines();
            _correctAnswersCount = 0;
            _wrongAnswersCount = 0;

            // Rebuild the ladder representation in UIManager dynamically
            _uiMgr.RebuildLadderUI();

            // Switch to game panel
            _uiMgr.ShowCategoryScreen(false);

            PlayAudio(audioGameStart);

            // Start at step 0
            _currentStep = 0;

            // Set bulk timer: 60 seconds per question in the test
            _timer = MoneyLadder.TotalSteps * 60f;
            _timerActive = false; // Pause timer during the initial ladder overlay

            _uiMgr.UpdateLadder(_currentStep);
            _uiMgr.ShowLadderOverlay(() =>
            {
                _timerActive = true;
                ShowCurrentQuestion();
            });
        }

        private IEnumerator AutoDismissLadder(float delay, System.Action onDismiss)
        {
            yield return new WaitForSeconds(delay);
            _uiMgr.HideLadderOverlay(onDismiss);
        }

        // ═══════════════════════════════════════════════
        //  QUESTION DISPLAY
        // ═══════════════════════════════════════════════

        private void ShowCurrentQuestion()
        {
            _currentQuestion = _questionMgr.GetQuestion(_currentStep);
            if (_currentQuestion == null)
            {
                Debug.LogError("[GameManager] No question available for step " + _currentStep);
                return;
            }

            PlayAudio(audioNewQuestion);
            SpawnParticles(_particlesNewQuestion);

            _uiMgr.ChangeBackgroundGradient(Random.Range(0, 10));

            _uiMgr.ShowQuestion(_currentQuestion, _currentStep);
            _uiMgr.UpdateLadder(_currentStep);
            RefreshLifelineButtons();
            _uiMgr.btnWalkAway.interactable = true;

            _waitingForAnswer = true;

            // Don't reset timer, just update UI
            _uiMgr.UpdateTimerUI(_timer, true);
        }

        private void RefreshLifelineButtons()
        {
            _uiMgr.UpdateLifelineButtons(
                _lifelineMgr.FiftyFiftyAvailable,
                _lifelineMgr.AskAudienceAvailable,
                _lifelineMgr.PhoneAvailable
            );
        }

        // ═══════════════════════════════════════════════
        //  ANSWER HANDLING
        // ═══════════════════════════════════════════════

        private void OnAnswerClicked(int index)
        {
            if (!_waitingForAnswer) return;
            if (_currentQuestion == null || _currentQuestion.answers == null || index < 0 || index >= _currentQuestion.answers.Length)
            {
                Debug.LogWarning($"[GameManager] Ignoring invalid answer index {index}.");
                return;
            }

            _waitingForAnswer = false;
            // Pause timer while answering
            _timerActive = false;

            // Disable all buttons immediately
            _uiMgr.DisableAnswerButtons();
            _uiMgr.btnWalkAway.interactable = false;

            bool correct = (index == _currentQuestion.correctAnswerIndex);

            // Highlight the chosen answer
            _uiMgr.HighlightAnswer(index, correct);

            if (!correct)
            {
                _wrongAnswersCount++;
                PlayAudio(audioWrong);
                SpawnParticles(_particlesWrong);
            }
            else
            {
                _correctAnswersCount++;
                PlayAudio(audioCorrect);
                SpawnParticles(_particlesCorrect);
            }

            // Short delay before proceeding
            StartCoroutine(ProcessAnswerAfterDelay(correct, index));
        }

        private IEnumerator ProcessAnswerAfterDelay(bool correct, int chosenIndex)
        {
            if (correct && !string.IsNullOrEmpty(_currentQuestion.explanation))
            {
                _uiMgr.btnShowExplanation.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(1.5f);

            while (_isViewingExplanation)
            {
                yield return null;
            }

            _uiMgr.btnShowExplanation.gameObject.SetActive(false);

            if (!correct)
            {
                PlayAudio(audioWrong);
                _uiMgr.ShowWrongAnswerPanel();
                yield break;
            }

            GoToNextQuestionOrWin();
        }

        private void GoToNextQuestionOrWin()
        {
            _currentStep++;

            if (_currentStep >= MoneyLadder.TotalSteps)
            {
                PlayAudio(audioWin);
                bool isTurk = (_currentLanguage == "TR");
                string title = isTurk ? "Tebrikler!" : "Congratulations!";
                string prize = MoneyLadder.PrizeLabels[MoneyLadder.TotalSteps - 1];
                string text = isTurk ? $"Büyük Ödülü Kazandınız: {prize}" : $"You won the grand prize: {prize}";

                _uiMgr.ShowResult(
                    title,
                    text
                );
                _consecutiveLosses = 0; // Reset losses on win
            }
            else
            {
                _uiMgr.UpdateLadder(_currentStep);
                _uiMgr.ShowLadderOverlay(() =>
                {
                    _timerActive = true;
                    ShowCurrentQuestion();
                });
            }
        }

        // ═══════════════════════════════════════════════
        //  WALK AWAY
        // ═══════════════════════════════════════════════

        private void OnWalkAway()
        {
            if (!_waitingForAnswer) return;
            _waitingForAnswer = false;
            _timerActive = false;

            bool isTurk = (_currentLanguage == "TR");
            string title = isTurk ? "Çekildiniz" : "Walked Away";
            string prize = _currentStep > 0 ? MoneyLadder.PrizeLabels[_currentStep - 1] : "₺0";
            string text = isTurk ? $"Oyundan çekildiniz.\nKazandığınız Ödül: {prize}" : $"You walked away.\nYou won: {prize}";

            _uiMgr.ShowResult(
                title,
                text
            );
        }

        private void OnShowExplanationClicked()
        {
            if (_currentQuestion == null || string.IsNullOrEmpty(_currentQuestion.explanation)) return;

            _isViewingExplanation = true;
            _uiMgr.btnShowExplanation.gameObject.SetActive(false);

            if (GoogleAdMobController.Instance != null)
            {
                GoogleAdMobController.Instance.ShowRewardedAd((bool success) =>
                {
                    _uiMgr.ShowExplanationPanel("Açıklama", _currentQuestion.explanation);
                });
            }
            else
            {
                _uiMgr.ShowExplanationPanel("Açıklama", _currentQuestion.explanation);
            }
        }

        private void OnPassQuestionClicked()
        {
            _uiMgr.HideWrongAnswerPanel();
            // Önce HandleLoss'u çağır; reklam varsa biter bitmez sonraki soruya geç
            HandleLoss(() => GoToNextQuestionOrWin());
        }

        private void OnShowCorrectAnswerClicked()
        {
            _uiMgr.HideWrongAnswerPanel();

            // Snapshot the current question before the async ad call —
            // _currentQuestion could theoretically change while the ad is playing.
            var questionSnapshot = _currentQuestion;

            if (GoogleAdMobController.Instance != null)
            {
                GoogleAdMobController.Instance.ShowRewardedAd((bool success) =>
                {
                    StartCoroutine(ShowCorrectAnswerAndExplanationCoroutine(questionSnapshot));
                });
            }
            else
            {
                StartCoroutine(ShowCorrectAnswerAndExplanationCoroutine(questionSnapshot));
            }
        }

        private IEnumerator ShowCorrectAnswerAndExplanationCoroutine(QuestionEntry question = null)
        {
            // Use provided snapshot or fall back to current question
            var q = question ?? _currentQuestion;
            if (q == null) yield break;

            _uiMgr.ShowCorrectAnswer(q.correctAnswerIndex);

            // Construct correct answer text and explanation
            string correctOptionLetter = UIManager.AnswerLetters[q.correctAnswerIndex];
            string correctOptionText = q.answers[q.correctAnswerIndex];
            string correctMsg = $"Doğru Cevap: {correctOptionLetter}) {correctOptionText}";

            string panelText = correctMsg;
            if (!string.IsNullOrEmpty(q.explanation))
            {
                panelText += $"\n\n{q.explanation}";
            }

            _isViewingExplanation = true;
            _uiMgr.ShowExplanationPanel("Doğru Cevap", panelText);

            while (_isViewingExplanation)
            {
                yield return null;
            }

            HandleLoss(() => GoToNextQuestionOrWin());
        }

        // ═══════════════════════════════════════════════
        //  LIFELINES
        // ═══════════════════════════════════════════════

        private void OnFiftyFifty()
        {
            if (!_waitingForAnswer || !_lifelineMgr.FiftyFiftyAvailable) return;

            List<int> keepIndices = _lifelineMgr.UseFiftyFifty(_currentQuestion);
            if (keepIndices != null)
            {
                PlayAudio(audioLifeline5050);
                _uiMgr.ApplyFiftyFifty(keepIndices);
            }

            RefreshLifelineButtons();
        }

        private void OnAskAudience()
        {
            if (!_waitingForAnswer || !_lifelineMgr.AskAudienceAvailable) return;

            float[] results = _lifelineMgr.UseAskAudience(_currentQuestion);
            if (results != null)
            {
                PlayAudio(audioLifelineAudience);
                _uiMgr.ShowAudienceResults(results);
            }

            RefreshLifelineButtons();
        }

        private void OnPhoneFriend()
        {
            if (!_waitingForAnswer || !_lifelineMgr.PhoneAvailable) return;

            string friendSays = _lifelineMgr.UsePhoneFriend(_currentQuestion);
            if (friendSays != null)
            {
                PlayAudio(audioLifelinePhone);
                _uiMgr.ShowPhoneFriend(friendSays);
            }

            RefreshLifelineButtons();
        }

        // ═══════════════════════════════════════════════
        //  RETURN TO MENU / RESTART
        // ═══════════════════════════════════════════════

        private void ReturnToMenu()
        {
            _uiMgr.HideResult();
            _uiMgr.ShowBranchScreen(true);
        }
        // ═══════════════════════════════════════════════
        //  AUDIO & PARTICLES
        // ═══════════════════════════════════════════════

        private IEnumerator SlideshowRoutine()
        {
            int index = 0;
            while (true)
            {
                // Show the current slide immediately, then wait before switching
                _uiMgr.UpdateBackground(_backgroundSprites[index]);
                index = (index + 1) % _backgroundSprites.Length;
                yield return new WaitForSeconds(_slideshowInterval > 0f ? _slideshowInterval : 10f);
            }
        }

        private string GenerateKPSSResultReport()
        {
            float net = _correctAnswersCount - (_wrongAnswersCount / 4.0f);
            if (net < 0) net = 0;

            float scoreMultiplier = MoneyLadder.TotalSteps > 0 ? (50f / MoneyLadder.TotalSteps) : 0f;
            float kpssScore = 50f + (net * scoreMultiplier);
            if (kpssScore > 100f) kpssScore = 100f;

            bool isTurk = (_currentLanguage == "TR");
            if (isTurk)
            {
                return $"\n\n<b>📊 Sınav Sonuç Tablosu</b>\n" +
                       $"───────────────────────\n" +
                       $"🟢 Doğru Cevap: {_correctAnswersCount}\n" +
                       $"🔴 Yanlış Cevap: {_wrongAnswersCount}\n" +
                       $"⚖️ Net Sayısı: {net:F2}\n" +
                       $"🏆 Tahmini KPSS Puanı: <b>{kpssScore:F2}</b>\n" +
                       $"───────────────────────";
            }
            else
            {
                return $"\n\n<b>📊 Exam Results Table</b>\n" +
                       $"───────────────────────\n" +
                       $"🟢 Correct: {_correctAnswersCount}\n" +
                       $"🔴 Incorrect: {_wrongAnswersCount}\n" +
                       $"⚖️ Net Score: {net:F2}\n" +
                       $"🏆 Est. KPSS Score: <b>{kpssScore:F2}</b>\n" +
                       $"───────────────────────";
            }
        }

        private void PlayClickSound()
        {
            PlayAudio(audioClick);
        }

        private void PlayAudio(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.clip = clip;
                _audioSource.Play();
            }
        }

        private void SpawnParticles(ParticleSystem prefab)
        {
            if (prefab != null)
            {
                ParticleSystem ps = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax + 1f);
            }
        }
    }
}
