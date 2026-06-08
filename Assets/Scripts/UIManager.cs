using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrimeTween;

/// <summary>
/// Builds the entire UI programmatically (no manual scene setup needed)
/// and drives all visual updates cleanly with PrimeTween.
/// Updated for Portrait layout (1080x1920) and smooth animations.
/// </summary>
namespace MillionaireGame
{
    public class UIManager : MonoBehaviour
    {
        // ══════════════════════════════════════════════
        //  PUBLIC REFERENCES
        // ══════════════════════════════════════════════
        public Canvas mainCanvas;
        public Image _bgImg;
        private Sprite _roundedSprite;

        // ── Language selection screen ──
        public GameObject languagePanel;
        public List<Button> languageButtons = new List<Button>();
        public TextMeshProUGUI languageTitle; // Fixed type

        // ── Branch selection screen (KPSS) ──
        public GameObject branchPanel;
        public TextMeshProUGUI branchTitle;
        public List<Button> branchButtons = new List<Button>();

        // ── Category selection screen ──
        public GameObject categoryPanel;
        public TextMeshProUGUI categoryTitle;
        public TextMeshProUGUI categorySubtitle;
        public List<Button> categoryButtons = new List<Button>();
        public Button btnSettings;          // persistent canvas-level settings gear

        // ── Game screen ──
        public GameObject gamePanel;
        public TextMeshProUGUI questionNumberText;
        public TextMeshProUGUI questionText;
        public Button[] answerButtons = new Button[4];
        public TextMeshProUGUI[] answerLabels = new TextMeshProUGUI[4];
        public Image[] answerBackgrounds = new Image[4];

        // Lifeline buttons
        public Button btnFiftyFifty;
        public Button btnAskAudience;
        public Button btnPhoneFriend;
        public TextMeshProUGUI lblFiftyFifty;
        public TextMeshProUGUI lblAskAudience;
        public TextMeshProUGUI lblPhoneFriend;
        public TextMeshProUGUI timerText;
        public TMP_FontAsset timerFont;

        // Walk‑away
        public Button btnWalkAway;

        // Money ladder
        public GameObject ladderArea;
        public TextMeshProUGUI[] ladderLabels;
        public Image[] ladderBackgrounds;
        private RectTransform[] _ladderRowRTs;

        // ── Audience result panel ──
        public GameObject audiencePanel;
        public Slider[] audienceSliders = new Slider[4];
        public TextMeshProUGUI[] audiencePercentLabels = new TextMeshProUGUI[4];
        public TextMeshProUGUI[] audienceLetterLabels = new TextMeshProUGUI[4];
        public Button audienceCloseButton;

        // ── Phone a Friend panel ──
        public GameObject phonePanel;
        public TextMeshProUGUI phoneFriendText;
        public Button phoneCloseButton;

        // ── Result panel (win / lose / walk away) ──
        public GameObject resultPanel;
        public TextMeshProUGUI resultTitle;
        public TextMeshProUGUI resultMessage;
        public Button resultMenuButton;

        // ── Settings panel ──
        public GameObject settingsPanel;
        public TMP_Dropdown languageDropdown;
        public Button settingsCloseButton;
        public TextMeshProUGUI settingsTitle;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public Toggle muteToggle;
        public TextMeshProUGUI musicLabel;
        public TextMeshProUGUI sfxLabel;
        public TextMeshProUGUI muteLabel;

        // ── Reminder panel ──
        public GameObject reminderPanel;
        public TextMeshProUGUI reminderTitle;
        public TextMeshProUGUI reminderText;
        public Button reminderCloseButton;

        // ── Gradient Palettes (Modernized/Lighter) ──
        private Color[] _gradTop = new Color[] {
            new Color(110/255f, 133/255f, 183/255f, 1f), // Pastel Blue
            new Color(181/255f, 114/255f, 164/255f, 1f), // Pastel Purple
            new Color(103/255f, 153/255f, 142/255f, 1f), // Soft Teal
            new Color(194/255f, 102/255f, 102/255f, 1f), // Soft Coral
            new Color(100/255f, 149/255f, 237/255f, 1f), // Cornflower
            new Color(147/255f, 112/255f, 219/255f, 1f), // Medium Purple
            new Color(205/255f, 133/255f, 162/255f, 1f), // Pale Violet
            new Color(189/255f, 165/255f, 93/255f, 1f),  // Soft Gold
            new Color(95/255f, 158/255f, 160/255f, 1f),  // Cadet Blue
            new Color(119/255f, 136/255f, 153/255f, 1f)  // Light Slate
        };
        private Color[] _gradBottom = new Color[] {
            new Color(30/255f, 40/255f, 80/255f, 1f),
            new Color(80/255f, 30/255f, 70/255f, 1f),
            new Color(20/255f, 70/255f, 60/255f, 1f),
            new Color(80/255f, 20/255f, 20/255f, 1f),
            new Color(20/255f, 40/255f, 80/255f, 1f),
            new Color(50/255f, 20/255f, 80/255f, 1f),
            new Color(80/255f, 30/255f, 60/255f, 1f),
            new Color(90/255f, 60/255f, 20/255f, 1f),
            new Color(20/255f, 70/255f, 70/255f, 1f),
            new Color(40/255f, 50/255f, 60/255f, 1f)
        };
        private GameObject _questionBgPanel;


        // ── Colors ──
        private readonly Color32 _panelBg      = new Color32(15, 15, 60, 255); // Opaque
        private readonly Color32 _accentGold   = new Color32(255, 200, 50, 255);
        private readonly Color32 _btnNormal    = new Color32(25, 45, 100, 255);
        private readonly Color32 _btnHover     = new Color32(40, 70, 140, 255);
        private readonly Color32 _btnCorrect   = new Color32(40, 190, 70, 255);
        private readonly Color32 _btnWrong     = new Color32(210, 50, 50, 255);
        private readonly Color32 _btnDisabled  = new Color32(60, 60, 80, 255);
        private readonly Color32 _ladderNormal = new Color32(20, 30, 80, 255);
        private readonly Color32 _ladderActive = new Color32(255, 180, 0, 255);
        private readonly Color32 _ladderSafe   = new Color32(80, 180, 255, 255);
        private readonly Color32 _white        = new Color32(255, 255, 255, 255);
        private readonly Color32 _borderColor  = new Color32(100, 100, 250, 180);

        // ══════════════════════════════════════════════
        //  BUILD UI 
        // ══════════════════════════════════════════════
        public void BuildUI()
        {
            // ── Canvas ──
            GameObject canvasGO = new GameObject("MainCanvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 0;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // PORTRAIT Update
            scaler.matchWidthOrHeight = 0f; // Match Width

            canvasGO.AddComponent<GraphicRaycaster>();

            // Background image as a proper child to fill the screen
            var bgGO = new GameObject("BackgroundSprite", typeof(RectTransform));
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.SetParent(canvasGO.transform, false);
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            bgGO.transform.SetAsFirstSibling();
            _bgImg = bgGO.AddComponent<Image>();
            _bgImg.color = new Color32(5, 5, 20, 255); // Solid dark background


            // Add dynamic floating shapes background effect
            canvasGO.AddComponent<FloatingShapesEffect>();

            // Generate a rounded sprite at runtime for "border radius"
            _roundedSprite = CreateRoundedSprite(80, 40); // Increased for "köşesiz" look

            // ── Event System ──
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // ── Build panels ──
            BuildLanguagePanel(canvasGO.transform);
            BuildBranchPanel(canvasGO.transform);
            BuildCategoryPanel(canvasGO.transform);
            BuildGamePanel(canvasGO.transform);
            BuildAudiencePanel(canvasGO.transform);
            BuildPhonePanel(canvasGO.transform);
            BuildResultPanel(canvasGO.transform);
            BuildSettingsPanel(canvasGO.transform);
            BuildReminderPanel(canvasGO.transform);

            // Persistent settings gear button (bottom-left corner)
            btnSettings = CreateButton(canvasGO.transform, "BtnSettings", "⚙", Vector2.zero, new Vector2(90, 90), 44);
            var btnSettingsRT = btnSettings.GetComponent<RectTransform>();
            btnSettingsRT.anchorMin = new Vector2(0f, 0f);
            btnSettingsRT.anchorMax = new Vector2(0f, 0f);
            btnSettingsRT.pivot    = new Vector2(0f, 0f);
            btnSettingsRT.anchoredPosition = new Vector2(20f, 200f);
            btnSettings.GetComponent<Image>().color = new Color32(30, 30, 100, 210);

            // Hide all initially — GameManager controls which screen to show
            languagePanel.SetActive(false);
            branchPanel.SetActive(false);
            categoryPanel.SetActive(false);
            gamePanel.SetActive(false);
            audiencePanel.SetActive(false);
            phonePanel.SetActive(false);
            resultPanel.SetActive(false);
            settingsPanel.SetActive(false);
            reminderPanel.SetActive(false);
            btnSettings.gameObject.SetActive(false); // hidden until language is chosen
        }

        public void ChangeBackgroundGradient(int index)
        {
            index = index % _gradTop.Length;
            Color panelColor = _gradTop[index];
            Color btnColor = new Color(
                Mathf.Clamp01(panelColor.r + 0.06f),
                Mathf.Clamp01(panelColor.g + 0.08f),
                Mathf.Clamp01(panelColor.b + 0.12f),
                1f
            );
            Color darkColor = _gradBottom[index];

            // Panels
            TweenPanelColor(languagePanel, panelColor);
            TweenPanelColor(branchPanel, panelColor);
            TweenPanelColor(categoryPanel, panelColor);
            TweenPanelColor(audiencePanel, panelColor);
            TweenPanelColor(phonePanel, panelColor);
            TweenPanelColor(resultPanel, panelColor);
            TweenPanelColor(settingsPanel, panelColor);
            TweenPanelColor(reminderPanel, panelColor);
            TweenPanelColor(ladderArea, darkColor);
            TweenPanelColor(_questionBgPanel, panelColor);

            // Buttons
            TweenButtonColor(btnFiftyFifty, btnColor);
            TweenButtonColor(btnAskAudience, btnColor);
            TweenButtonColor(btnPhoneFriend, btnColor);
            TweenButtonColor(btnSettings, btnColor);
            foreach (var btn in branchButtons)
                TweenButtonColor(btn, btnColor);
            foreach (var btn in categoryButtons)
                TweenButtonColor(btn, btnColor);
            for (int i = 0; i < 4; i++)
                TweenButtonColor(answerButtons[i], btnColor);

            // Ladder rows
            if (ladderBackgrounds != null)
            {
                for (int i = 0; i < ladderBackgrounds.Length; i++)
                {
                    if (i == MoneyLadder.SafeHaven1 || i == MoneyLadder.SafeHaven2)
                        continue; // keep safe haven color
                    if (ladderBackgrounds[i] != null)
                        Tween.Color(ladderBackgrounds[i], darkColor, 1.5f);
                }
            }
        }

        private void TweenPanelColor(GameObject panel, Color target)
        {
            if (panel == null) return;
            var img = panel.GetComponent<Image>();
            if (img != null && img.color.a > 0.01f) // skip transparent panels
                Tween.Color(img, target, 1.5f);
        }

        private void TweenButtonColor(Button btn, Color target)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
                Tween.Color(img, target, 1.5f);
        }


        public void UpdateBackground(Sprite bg)
        {
            if (bg != null && _bgImg != null)
            {
                _bgImg.sprite = bg;
                // Fade effect to make sprite fully visible (Color.white)
                Tween.Color(_bgImg, new Color(1f, 1f, 1f, 0f), Color.white, 1.5f);
            }
        }

        // ─────────────────────────────────────────────
        //  ANIMATION HELPERS
        // ─────────────────────────────────────────────
        private void ShowPanel(GameObject panel)
        {
            panel.SetActive(true);
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0f;
            Tween.Alpha(canvasGroup, 1f, 0.4f, Ease.OutQuad);
            
            var rt = panel.GetComponent<RectTransform>();
            rt.localScale = Vector3.one * 0.95f;
            Tween.Scale(rt, Vector3.one, 0.4f, Ease.OutBack);
        }

        private void HidePanel(GameObject panel)
        {
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Tween.Alpha(canvasGroup, 0f, 0.3f, Ease.InQuad).OnComplete(panel, p => p.SetActive(false));
            }
            else
            {
                panel.SetActive(false);
            }
        }

        private void AnimateButtonPress(Button btn)
        {
            var rt = btn.GetComponent<RectTransform>();
            // Explicitly stop previous scale tweens and reset scale to ensure a clean start
            Tween.StopAll(rt);
            rt.localScale = Vector3.one;
            // Use Yoyo with 2 cycles (trip and back) to guarantee it returns to its original scale
            Tween.Scale(rt, Vector3.one * 0.9f, 0.1f, cycles: 2, cycleMode: CycleMode.Yoyo);
        }

        // ─────────────────────────────────────────────
        //  PANEL BUILDERS
        // ─────────────────────────────────────────────
        private void BuildLanguagePanel(Transform parent)
        {
            languagePanel = CreatePanel(parent, "LanguagePanel", Vector2.zero, new Vector2(900, 600));

            languageTitle = CreateTMP(languagePanel.transform, "LanguageTitle", "Select Language\nDil Seçin", 56, TextAlignmentOptions.Center, new Vector2(0, 200), new Vector2(800, 150));
            languageTitle.color = _accentGold;
            languageTitle.fontStyle = FontStyles.Bold;
        }

        public void PopulateLanguageButtons(List<LanguageData> languages, System.Action<string> onClick)
        {
            // Clear old buttons
            foreach (var btn in languageButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            languageButtons.Clear();

            float startY = 20; 
            float spacing = 120;

            for (int i = 0; i < languages.Count; i++)
            {
                var lang = languages[i];
                float yPos = startY - (i * spacing);
                var btn = CreateButton(languagePanel.transform, $"BtnLang_{lang.code}", lang.name, new Vector2(0, yPos), new Vector2(500, 100), 48);
                btn.onClick.AddListener(() => {
                    AnimateButtonPress(btn);
                    onClick?.Invoke(lang.code);
                });
                languageButtons.Add(btn);
            }

            // Adjust panel height
            var rt = languagePanel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(900, 400 + languages.Count * spacing);
            languageTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, (rt.sizeDelta.y / 2) - 100);
        }

        private void BuildBranchPanel(Transform parent)
        {
            branchPanel = CreatePanel(parent, "BranchPanel", Vector2.zero, new Vector2(950, 1600));

            branchTitle = CreateTMP(branchPanel.transform, "BranchTitle", "Select Branch", 58, TextAlignmentOptions.Center, new Vector2(0, 680), new Vector2(800, 80));
            branchTitle.color = _accentGold;
            branchTitle.fontStyle = FontStyles.Bold;
        }

        public void PopulateBranchButtons(System.Action<string> onClick)
        {
            foreach (var btn in branchButtons) if (btn != null) Destroy(btn.gameObject);
            branchButtons.Clear();

            string[] branches = { "Ortaöğretim", "Önlisans", "Lisans" };
            string[] branchCodes = { "kpss_ortaogretim", "kpss_onlisans", "kpss_lisans" };

            float startY = 480f;
            float spacing = 150f;

            for (int i = 0; i < branches.Length; i++)
            {
                float yPos = startY - i * spacing;
                string code = branchCodes[i];

                var btn = CreateButton(branchPanel.transform, $"BtnBranch_{code}", branches[i], new Vector2(0, yPos), new Vector2(700, 110), 48);
                btn.onClick.AddListener(() => {
                    AnimateButtonPress(btn);
                    onClick(code);
                });
                branchButtons.Add(btn);
            }
        }

        private void BuildCategoryPanel(Transform parent)
        {
            categoryPanel = CreatePanel(parent, "CategoryPanel", Vector2.zero, new Vector2(950, 1600));

            categoryTitle = CreateTMP(categoryPanel.transform, "CategoryTitle", "Choose a Category", 58, TextAlignmentOptions.Center, new Vector2(0, 680), new Vector2(800, 80));
            categoryTitle.color = _accentGold;
            categoryTitle.fontStyle = FontStyles.Bold;

            categorySubtitle = CreateTMP(categoryPanel.transform, "Subtitle", "Religious Who Wants to Be a Millionaire?", 42, TextAlignmentOptions.Center, new Vector2(0, 600), new Vector2(800, 80));
            categorySubtitle.color = _white;
            categorySubtitle.fontStyle = FontStyles.Italic;
        }

        public void PopulateCategoryButtons(List<string> categories, System.Action<string> onClick)
        {
            foreach (var btn in categoryButtons) if (btn != null) Destroy(btn.gameObject);
            categoryButtons.Clear();

            float startY = 480f;
            float spacing = 130f;

            for (int i = 0; i < categories.Count; i++)
            {
                string cat = categories[i];
                string displayCat = cat;
                if (cat == "All") displayCat = (PlayerPrefs.GetString("SelectedLanguage", "EN") == "TR") ? "Hepsi" : "All";
                
                float yPos = startY - i * spacing;

                var btn = CreateButton(categoryPanel.transform, $"CatBtn_{cat}", displayCat, new Vector2(0, yPos), new Vector2(700, 100), 44);
                btn.onClick.AddListener(() => {
                    AnimateButtonPress(btn);
                    onClick(cat);
                });
                categoryButtons.Add(btn);
            }
        }

        private void BuildGamePanel(Transform parent)
        {
            gamePanel = CreatePanel(parent, "GamePanel", Vector2.zero, new Vector2(1080, 1920));
            gamePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0); // transparent container

            // ── Top: Lifeline buttons ──
            float lifeY = 820f + 50f;  // 50 piksel yukarı taşındı (870f oldu)
            btnFiftyFifty = CreateButton(gamePanel.transform, "Btn5050", "50:50", new Vector2(-320, lifeY), new Vector2(280, 100), 36);
            lblFiftyFifty = btnFiftyFifty.GetComponentInChildren<TextMeshProUGUI>();

            btnAskAudience = CreateButton(gamePanel.transform, "BtnAudience", "Ask Aud", new Vector2(0, lifeY), new Vector2(300, 100), 36);
            lblAskAudience = btnAskAudience.GetComponentInChildren<TextMeshProUGUI>();

            btnPhoneFriend = CreateButton(gamePanel.transform, "BtnPhone", "Phone", new Vector2(320, lifeY), new Vector2(280, 100), 36);
            lblPhoneFriend = btnPhoneFriend.GetComponentInChildren<TextMeshProUGUI>();

            // ── Mid-Top: Money Ladder ──
            ladderArea = CreatePanel(gamePanel.transform, "LadderArea", new Vector2(0, 440), new Vector2(850, 700));
            ladderArea.GetComponent<Image>().color = new Color32(10, 10, 50, 255);

            var ladderTitle = CreateTMP(ladderArea.transform, "LadderTitle", "Prize Ladder", 40, TextAlignmentOptions.Center, new Vector2(0, 330), new Vector2(800, 45));
            ladderTitle.color = _accentGold;
            ladderTitle.fontStyle = FontStyles.Bold;

            int steps = MoneyLadder.TotalSteps;
            ladderLabels = new TextMeshProUGUI[steps];
            ladderBackgrounds = new Image[steps];
            _ladderRowRTs = new RectTransform[steps];

            for (int i = steps - 1; i >= 0; i--)
            {
                int displayRow = steps - 1 - i;
                float yPos = 265f - displayRow * 42f;

                var rowGO = new GameObject($"LadderRow_{i}", typeof(RectTransform));
                var rowRT = rowGO.GetComponent<RectTransform>();
                rowRT.SetParent(ladderArea.transform, false);
                rowRT.anchoredPosition = new Vector2(0, yPos);
                rowRT.sizeDelta = new Vector2(800, 40);
                _ladderRowRTs[i] = rowRT;

                var rowImg = rowGO.AddComponent<Image>();
                rowImg.color = _ladderNormal;
                ladderBackgrounds[i] = rowImg;

                if (i == MoneyLadder.SafeHaven1 || i == MoneyLadder.SafeHaven2)
                    rowImg.color = _ladderSafe;

                string prefix = (i + 1).ToString().PadLeft(2) + ". ";
                ladderLabels[i] = CreateTMP(rowGO.transform, $"LadderLbl_{i}", prefix + MoneyLadder.PrizeLabels[i], 28, TextAlignmentOptions.Center, Vector2.zero, new Vector2(750, 40));
                ladderLabels[i].color = _white;
            }

            // ── Middle: Question Text ──
            timerText = CreateTMP(gamePanel.transform, "TimerText", "60", 64, TextAlignmentOptions.Center, new Vector2(400, 40), new Vector2(200, 80));
            if (timerFont != null) timerText.font = timerFont;
            timerText.color = _accentGold;
            timerText.fontStyle = FontStyles.Bold;

            questionNumberText = CreateTMP(gamePanel.transform, "QuestionNumber", "Question 1 / 15", 42, TextAlignmentOptions.Center, new Vector2(0, 40), new Vector2(800, 50));
            questionNumberText.color = _accentGold;

            _questionBgPanel = CreatePanel(gamePanel.transform, "QuestionBg", new Vector2(0, -110), new Vector2(980, 220));
            _questionBgPanel.GetComponent<Image>().color = _panelBg;

            questionText = CreateTMP(_questionBgPanel.transform, "QuestionText", "Question goes here?", 48, TextAlignmentOptions.Center, Vector2.zero, new Vector2(930, 180));
            questionText.color = _white;

            // ── Bottom: Answer buttons (1x4 vertically) ──
            string[] labels = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                float aY = -310 - i * 115;
                var btn = CreateButton(gamePanel.transform, $"AnswerBtn_{labels[i]}", $"{labels[i]}: Answer", new Vector2(0, aY), new Vector2(920, 100), 42);
                
                var btnRT = btn.GetComponent<RectTransform>();
                answerBackgrounds[i] = btn.GetComponent<Image>();
                answerButtons[i] = btn;
                answerLabels[i] = btn.transform.Find("Label").GetComponent<TextMeshProUGUI>();
                answerLabels[i].alignment = TextAlignmentOptions.Left;
                // Indent text slightly
                answerLabels[i].rectTransform.anchoredPosition = new Vector2(20, 0);
                answerLabels[i].rectTransform.sizeDelta = new Vector2(880, 90);
                
                int index = i;
                btn.onClick.AddListener(() => AnimateButtonPress(answerButtons[index]));
            }

            // Walk away button
            btnWalkAway = CreateButton(gamePanel.transform, "BtnWalkAway", "Walk Away", new Vector2(0, -800), new Vector2(350, 80), 40);
            btnWalkAway.GetComponent<Image>().color = new Color32(180, 50, 50, 255);
        }

        private void BuildAudiencePanel(Transform parent)
        {
            audiencePanel = CreatePanel(parent, "AudiencePanel", Vector2.zero, new Vector2(800, 700));
            
            CreateTMP(audiencePanel.transform, "AudTitle", "Audience Results", 50, TextAlignmentOptions.Center, new Vector2(0, 280), new Vector2(700, 60)).color = _accentGold;

            string[] letters = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                float xPos = -240 + i * 160;

                audienceLetterLabels[i] = CreateTMP(audiencePanel.transform, $"AudLetter_{i}", letters[i], 46, TextAlignmentOptions.Center, new Vector2(xPos, -240), new Vector2(100, 50));
                
                var sliderGO = new GameObject($"AudSlider_{i}", typeof(RectTransform));
                var sliderRT = sliderGO.GetComponent<RectTransform>();
                sliderRT.SetParent(audiencePanel.transform, false);
                sliderRT.anchoredPosition = new Vector2(xPos, 0);
                sliderRT.sizeDelta = new Vector2(100, 380);

                var bgGO = new GameObject("Background", typeof(RectTransform));
                var bgRT = bgGO.GetComponent<RectTransform>();
                bgRT.SetParent(sliderGO.transform, false);
                bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
                bgRT.sizeDelta = Vector2.zero; bgGO.AddComponent<Image>().color = new Color32(40, 40, 80, 255);

                var fillAreaGO = new GameObject("FillArea", typeof(RectTransform));
                var fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
                fillAreaRT.SetParent(sliderGO.transform, false);
                fillAreaRT.anchorMin = Vector2.zero; fillAreaRT.anchorMax = Vector2.one;
                fillAreaRT.sizeDelta = Vector2.zero; 

                var fillGO = new GameObject("Fill", typeof(RectTransform));
                var fillRT = fillGO.GetComponent<RectTransform>();
                fillRT.SetParent(fillAreaGO.transform, false);
                fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
                fillRT.sizeDelta = Vector2.zero; fillGO.AddComponent<Image>().color = _accentGold;

                var slider = sliderGO.AddComponent<Slider>();
                slider.direction = Slider.Direction.BottomToTop;
                slider.minValue = 0; slider.maxValue = 100;
                slider.interactable = false;
                slider.fillRect = fillRT;
                audienceSliders[i] = slider;

                audiencePercentLabels[i] = CreateTMP(audiencePanel.transform, $"AudPct_{i}", "0%", 38, TextAlignmentOptions.Center, new Vector2(xPos, 220), new Vector2(120, 40));
            }

            audienceCloseButton = CreateButton(audiencePanel.transform, "AudClose", "OK", new Vector2(0, -300), new Vector2(250, 70), 42);
        }

        private void BuildPhonePanel(Transform parent)
        {
            phonePanel = CreatePanel(parent, "PhonePanel", Vector2.zero, new Vector2(800, 500));

            CreateTMP(phonePanel.transform, "PhoneTitle", "📞 Phone a Friend", 50, TextAlignmentOptions.Center, new Vector2(0, 180), new Vector2(700, 60)).color = _accentGold;

            phoneFriendText = CreateTMP(phonePanel.transform, "PhoneFriendText", "", 44, TextAlignmentOptions.Center, new Vector2(0, 20), new Vector2(750, 200));
            
            phoneCloseButton = CreateButton(phonePanel.transform, "PhoneClose", "OK", new Vector2(0, -180), new Vector2(250, 70), 42);
        }

        private void BuildResultPanel(Transform parent)
        {
            // Full-screen overlay
            resultPanel = new GameObject("ResultOverlay", typeof(RectTransform));
            var overlayRT = resultPanel.GetComponent<RectTransform>();
            overlayRT.SetParent(parent, false);
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.sizeDelta = Vector2.zero;
            
            var overlayImg = resultPanel.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.7f);

            // Dialog box inside overlay
            var dialog = CreatePanel(resultPanel.transform, "ResultDialog", Vector2.zero, new Vector2(900, 700));
            dialog.GetComponent<Image>().color = _panelBg;

            resultTitle = CreateTMP(dialog.transform, "ResultTitle", "Game Over", 65, TextAlignmentOptions.Center, new Vector2(0, 250), new Vector2(800, 80));
            resultTitle.color = _accentGold;
            resultTitle.fontStyle = FontStyles.Bold;

            resultMessage = CreateTMP(dialog.transform, "ResultMsg", "", 50, TextAlignmentOptions.Center, new Vector2(0, 60), new Vector2(800, 250));
            
            resultMenuButton = CreateButton(dialog.transform, "ResultMenuBtn", "Main Menu", new Vector2(0, -220), new Vector2(400, 100), 46);
        }

        private void BuildReminderPanel(Transform parent)
        {
            reminderPanel = CreatePanel(parent, "ReminderPanel", Vector2.zero, new Vector2(900, 600));

            reminderTitle = CreateTMP(reminderPanel.transform, "ReminderTitle", "Did You Know?", 56, TextAlignmentOptions.Center, new Vector2(0, 200), new Vector2(800, 80));
            reminderTitle.color = _accentGold;
            reminderTitle.fontStyle = FontStyles.Bold;

            reminderText = CreateTMP(reminderPanel.transform, "ReminderText", "Reminder goes here", 46, TextAlignmentOptions.Center, new Vector2(0, 0), new Vector2(800, 300));
            reminderText.color = _white;

            reminderCloseButton = CreateButton(reminderPanel.transform, "ReminderClose", "Start", new Vector2(0, -200), new Vector2(300, 80), 42);
        }

        private void BuildSettingsPanel(Transform parent)
        {
            settingsPanel = CreatePanel(parent, "SettingsPanel", Vector2.zero, new Vector2(850, 950));

            settingsTitle = CreateTMP(settingsPanel.transform, "SettingsTitle", "Settings", 56, TextAlignmentOptions.Center, new Vector2(0, 420), new Vector2(700, 70));
            settingsTitle.color = _accentGold;
            settingsTitle.fontStyle = FontStyles.Bold;

            // Language label
            CreateTMP(settingsPanel.transform, "LangLabel", "Language / Dil", 42, TextAlignmentOptions.Center, new Vector2(0, 320), new Vector2(600, 50)).color = _white;

            // TMP_Dropdown for language
            var dropdownGO = new GameObject("LanguageDropdown", typeof(RectTransform));
            var dropdownRT = dropdownGO.GetComponent<RectTransform>();
            dropdownRT.SetParent(settingsPanel.transform, false);
            dropdownRT.anchorMin = new Vector2(0.5f, 0.5f);
            dropdownRT.anchorMax = new Vector2(0.5f, 0.5f);
            dropdownRT.anchoredPosition = new Vector2(0, 240);
            dropdownRT.sizeDelta = new Vector2(500, 80);

            // Background image for dropdown
            var dropBgImg = dropdownGO.AddComponent<Image>();
            dropBgImg.color = _btnNormal;
            dropBgImg.sprite = _roundedSprite;
            dropBgImg.type = Image.Type.Sliced;

            // Create dropdown component
            languageDropdown = dropdownGO.AddComponent<TMP_Dropdown>();

            // Caption text
            var captionTMP = CreateTMP(dropdownGO.transform, "CaptionText", "", 40, TextAlignmentOptions.Center, Vector2.zero, new Vector2(460, 70));
            captionTMP.color = _white;
            languageDropdown.captionText = captionTMP;

            // Template (dropdown list)
            var templateGO = new GameObject("Template", typeof(RectTransform));
            var templateRT = templateGO.GetComponent<RectTransform>();
            templateRT.SetParent(dropdownGO.transform, false);
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.pivot = new Vector2(0.5f, 1f);
            templateRT.anchoredPosition = Vector2.zero;
            templateRT.sizeDelta = new Vector2(0, 200);
            var templateImg = templateGO.AddComponent<Image>();
            templateImg.color = new Color32(20, 20, 70, 255);
            templateGO.AddComponent<ScrollRect>();

            // Viewport
            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.SetParent(templateRT, false);
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = Color.white;
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGO = new GameObject("Content", typeof(RectTransform));
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.SetParent(viewportRT, false);
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = new Vector2(0, 80);

            // Item template
            var itemGO = new GameObject("Item", typeof(RectTransform));
            var itemRT = itemGO.GetComponent<RectTransform>();
            itemRT.SetParent(contentRT, false);
            itemRT.anchorMin = new Vector2(0, 0.5f);
            itemRT.anchorMax = new Vector2(1, 0.5f);
            itemRT.sizeDelta = new Vector2(0, 80);
            var itemToggle = itemGO.AddComponent<Toggle>();
            var itemBg = itemGO.AddComponent<Image>();
            itemBg.color = new Color32(30, 30, 90, 255);

            // Item label
            var itemLabelTMP = CreateTMP(itemGO.transform, "ItemLabel", "", 38, TextAlignmentOptions.Center, Vector2.zero, new Vector2(460, 70));
            itemLabelTMP.color = _white;
            languageDropdown.itemText = itemLabelTMP;
            languageDropdown.template = templateRT;

            // Populate options from LocalizationManager
            languageDropdown.options.Clear();
            foreach (var lang in LocalizationManager.AvailableLanguages)
            {
                languageDropdown.options.Add(new TMP_Dropdown.OptionData(lang.name));
            }

            // Wire scroll rect
            var scrollRect = templateGO.GetComponent<ScrollRect>();
            scrollRect.content = contentRT;
            scrollRect.viewport = viewportRT;

            // Wire template
            languageDropdown.template = templateRT;
            templateGO.SetActive(false);

            // ── Audio Settings ──
            float startY = 120f;
            float spacingY = 140f;

            // Music Volume
            musicLabel = CreateTMP(settingsPanel.transform, "MusicLabel", "Music Volume", 36, TextAlignmentOptions.Left, new Vector2(-150, startY), new Vector2(300, 40));
            musicVolumeSlider = CreateSlider(settingsPanel.transform, "MusicSlider", new Vector2(0, startY - 50), new Vector2(600, 40));

            // SFX Volume
            sfxLabel = CreateTMP(settingsPanel.transform, "SFXLabel", "SFX Volume", 36, TextAlignmentOptions.Left, new Vector2(-150, startY - spacingY), new Vector2(300, 40));
            sfxVolumeSlider = CreateSlider(settingsPanel.transform, "SFXSlider", new Vector2(0, startY - spacingY - 50), new Vector2(600, 40));

            // Mute Toggle
            muteLabel = CreateTMP(settingsPanel.transform, "MuteLabel", "Mute All", 40, TextAlignmentOptions.Left, new Vector2(-80, startY - spacingY * 2 - 20), new Vector2(300, 50));
            muteToggle = CreateToggle(settingsPanel.transform, "MuteToggle", new Vector2(150, startY - spacingY * 2 - 20), new Vector2(60, 60));

            // Close button
            settingsCloseButton = CreateButton(settingsPanel.transform, "SettingsClose", "Close", new Vector2(0, -400), new Vector2(300, 80), 42);
        }

        private Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var sliderGO = new GameObject(name, typeof(RectTransform));
            var sliderRT = sliderGO.GetComponent<RectTransform>();
            sliderRT.SetParent(parent, false);
            sliderRT.anchoredPosition = pos;
            sliderRT.sizeDelta = size;

            var bg = new GameObject("Background", typeof(RectTransform)).AddComponent<Image>();
            bg.rectTransform.SetParent(sliderRT, false);
            bg.rectTransform.anchorMin = new Vector2(0, 0.25f);
            bg.rectTransform.anchorMax = new Vector2(1, 0.75f);
            bg.rectTransform.sizeDelta = Vector2.zero;
            bg.color = new Color32(40, 40, 100, 255);
            bg.sprite = _roundedSprite;
            bg.type = Image.Type.Sliced;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(sliderRT, false);
            fillArea.anchorMin = new Vector2(0, 0.25f);
            fillArea.anchorMax = new Vector2(1, 0.75f);
            fillArea.sizeDelta = new Vector2(-20, 0);

            var fill = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            fill.rectTransform.SetParent(fillArea, false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.sizeDelta = Vector2.zero;
            fill.color = _accentGold;
            fill.sprite = _roundedSprite;
            fill.type = Image.Type.Sliced;

            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(sliderRT, false);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.sizeDelta = new Vector2(-20, 0);

            var handle = new GameObject("Handle", typeof(RectTransform)).AddComponent<Image>();
            handle.rectTransform.SetParent(handleArea, false);
            handle.rectTransform.sizeDelta = new Vector2(40, 40);
            handle.color = Color.white;
            handle.sprite = _roundedSprite;

            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private Toggle CreateToggle(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var toggleGO = new GameObject(name, typeof(RectTransform));
            var toggleRT = toggleGO.GetComponent<RectTransform>();
            toggleRT.SetParent(parent, false);
            toggleRT.anchoredPosition = pos;
            toggleRT.sizeDelta = size;

            var bg = new GameObject("Background", typeof(RectTransform)).AddComponent<Image>();
            bg.rectTransform.SetParent(toggleRT, false);
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
            bg.color = _btnNormal;
            bg.sprite = _roundedSprite;

            var checkmark = new GameObject("Checkmark", typeof(RectTransform)).AddComponent<Image>();
            checkmark.rectTransform.SetParent(bg.rectTransform, false);
            checkmark.rectTransform.anchorMin = new Vector2(0.2f, 0.2f);
            checkmark.rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
            checkmark.rectTransform.sizeDelta = Vector2.zero;
            checkmark.color = _accentGold;
            checkmark.sprite = _roundedSprite;

            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.graphic = checkmark;
            toggle.isOn = false;

            return toggle;
        }

        public void ShowSettingsPanel() { ShowPanel(settingsPanel); }
        public void HideSettingsPanel() { AnimateButtonPress(settingsCloseButton); HidePanel(settingsPanel); }

        /// <summary>Show or hide the persistent settings gear button.</summary>
        public void SetSettingsButtonVisible(bool visible)
        {
            if (btnSettings != null)
                btnSettings.gameObject.SetActive(visible);
        }

        public void SetSettingsButtonSprite(Sprite s)
        {
            if (btnSettings != null && s != null)
            {
                var img = btnSettings.GetComponent<Image>();
                img.sprite = s;
                btnSettings.GetComponentInChildren<TextMeshProUGUI>().text = ""; // clear gear icon text
            }
        }

        public void UpdateTimerUI(float seconds, bool active)
        {
            timerText.gameObject.SetActive(active);
            if (active)
            {
                int totalSeconds = Mathf.CeilToInt(seconds);
                int m = totalSeconds / 60;
                int s = totalSeconds % 60;
                timerText.text = string.Format("{0:00}:{1:00}", m, s);

                if (seconds <= 10f)
                {
                    timerText.color = Color.red;
                    timerText.transform.localScale = Vector3.one * (1f + Mathf.PingPong(Time.time * 2, 0.2f));
                }
                else
                {
                    timerText.color = _accentGold;
                    timerText.transform.localScale = Vector3.one;
                }
            }
        }

        // ══════════════════════════════════════════════
        //  UI UPDATE METHODS
        // ══════════════════════════════════════════════
        public void ApplyLanguage(string language)
        {
            LocalizationManager.SetLanguage(language);

            categoryTitle.text = LocalizationManager.Get("categoryTitle");
            categorySubtitle.text = LocalizationManager.Get("categorySubtitle");

            lblFiftyFifty.text = LocalizationManager.Get("fiftyFifty");
            lblAskAudience.text = LocalizationManager.Get("askAudience");
            lblPhoneFriend.text = LocalizationManager.Get("phoneFriend");
            btnWalkAway.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Get("walkAway");

            audienceCloseButton.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Get("ok");
            phoneCloseButton.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Get("ok");
            resultMenuButton.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Get("mainMenu");

            // Settings panel
            if (settingsTitle != null)
                settingsTitle.text = LocalizationManager.Get("settings");
            if (settingsCloseButton != null)
                settingsCloseButton.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Get("close");

            if (musicLabel != null) musicLabel.text = LocalizationManager.Get("musicVolume");
            if (sfxLabel != null) sfxLabel.text = LocalizationManager.Get("sfxVolume");
            if (muteLabel != null) muteLabel.text = LocalizationManager.Get("muteAll");

            // Reminder panel
            if (reminderCloseButton != null)
                reminderCloseButton.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Get("start");
        }

        public void ShowLanguageScreen(bool show)
        {
            if(show) ShowPanel(languagePanel); else HidePanel(languagePanel);
        }

        public void ShowBranchScreen(bool show)
        {
            if(show) 
            {
                HidePanel(languagePanel);
                ShowPanel(branchPanel);
            }
            else 
            {
                HidePanel(branchPanel);
            }
        }

        public void ShowCategoryScreen(bool show)
        {
            if(show) 
            {
                HidePanel(languagePanel);
                ShowPanel(categoryPanel);
            }
            else 
            {
                HidePanel(categoryPanel);
            }

            if(show) HidePanel(gamePanel); else ShowPanel(gamePanel);
            
            if(show)
            {
                HidePanel(resultPanel);
                HidePanel(audiencePanel);
                HidePanel(phonePanel);
                HidePanel(reminderPanel);
            }
        }

        public void ShowReminderScreen(string title, string text, System.Action onClose)
        {
            reminderTitle.text = title;
            reminderText.text = text;
            
            reminderCloseButton.onClick.RemoveAllListeners();
            reminderCloseButton.onClick.AddListener(() => {
                AnimateButtonPress(reminderCloseButton);
                HidePanel(reminderPanel);
                onClose?.Invoke();
            });

            ShowPanel(reminderPanel);
        }

        public void ShowQuestion(QuestionEntry q, int stepIndex)
        {
            string questionLabel = (LocalizationManager.AvailableLanguages.Count > 0) ? LocalizationManager.Get("question") : "Question";
            //// If "question" key doesn't exist yet in JSON, handle it:
            //if (questionLabel == "question") questionLabel = (_currentLanguage == "TR" ? "Soru" : "Question");

            questionNumberText.text = $"{questionLabel} {stepIndex + 1} / {MoneyLadder.TotalSteps}";
            questionText.text = q.questionText;

            // Fade in Question text
            Tween.Alpha(questionText, 0f, 1f, 0.5f);

            string[] prefixes = { "A: ", "B: ", "C: ", "D: " };
            for (int i = 0; i < 4; i++)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].interactable = true;
                answerLabels[i].text = prefixes[i] + q.answers[i];
                answerBackgrounds[i].color = _btnNormal;
                
                // Animation for Answer Buttons sliding in
                var rt = answerButtons[i].GetComponent<RectTransform>();
                var origPos = rt.anchoredPosition;
                rt.anchoredPosition = origPos + new Vector2(0, -60);
                var cg = answerButtons[i].gameObject.GetComponent<CanvasGroup>();
                if(cg == null) cg = answerButtons[i].gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                
                Tween.UIAnchoredPosition(rt, origPos, 0.3f, Ease.OutBack, startDelay: i * 0.1f);
                Tween.Alpha(cg, 1f, 0.3f, Ease.Linear, startDelay: i * 0.1f);
            }
        }

        public void HighlightAnswer(int index, bool correct)
        {
            var targetColor = correct ? _btnCorrect : _btnWrong;
            Tween.Color(answerBackgrounds[index], (Color)targetColor, 0.3f);
            
            // Ensure the button is at its base scale/position before starting the final highlight animation
            var trans = answerButtons[index].transform;
            Tween.StopAll(trans);
            trans.localScale = Vector3.one;

            if (correct)
            {
                // Pulsate 2 times (4 trips: out, in, out, in) to return to 1.0 scale
                Tween.Scale(trans, Vector3.one * 1.05f, 0.2f, cycles: 4, cycleMode: CycleMode.Yoyo);
            }
            else
            {
                // Shake 3 times (6 trips) to return to original position
                Tween.LocalPositionX(trans, endValue: 15f, duration: 0.05f, cycles: 6, cycleMode: CycleMode.Yoyo);
            }
        }

        public void ShowCorrectAnswer(int correctIndex)
        {
            Tween.Color(answerBackgrounds[correctIndex], (Color)_btnCorrect, 0.3f, startDelay: 0.5f);
        }

        public void DisableAnswerButtons()
        {
            for (int i = 0; i < 4; i++) answerButtons[i].interactable = false;
        }

        public void ApplyFiftyFifty(List<int> keepIndices)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!keepIndices.Contains(i))
                {
                    var btnCG = answerButtons[i].GetComponent<CanvasGroup>();
                    Tween.Alpha(btnCG, 0f, 0.4f).OnComplete(answerButtons[i], b => b.gameObject.SetActive(false));
                }
            }
        }

        public void ShowAudienceResults(float[] percentages)
        {
            ShowPanel(audiencePanel);
            for (int i = 0; i < 4; i++)
            {
                int idx = i; // local copy for closure
                Tween.Custom(0f, percentages[idx], 1.5f, onValueChange: (val) => {
                    audienceSliders[idx].value = val;
                    audiencePercentLabels[idx].text = $"{Mathf.RoundToInt(val)}%";
                }, Ease.OutCubic);
            }
        }

        public void HideAudiencePanel() { AnimateButtonPress(audienceCloseButton); HidePanel(audiencePanel); }

        public void ShowPhoneFriend(string friendSays)
        {
            ShowPanel(phonePanel);
            phoneFriendText.text = friendSays;
            Tween.Alpha(phoneFriendText, 0f, 1f, 0.5f);
        }

        public void HidePhonePanel() { AnimateButtonPress(phoneCloseButton); HidePanel(phonePanel); }

        public void UpdateLifelineButtons(bool fifty, bool audience, bool phone)
        {
            btnFiftyFifty.interactable = fifty;
            btnAskAudience.interactable = audience;
            btnPhoneFriend.interactable = phone;

            SetButtonUsedVisual(btnFiftyFifty, lblFiftyFifty, fifty);
            SetButtonUsedVisual(btnAskAudience, lblAskAudience, audience);
            SetButtonUsedVisual(btnPhoneFriend, lblPhoneFriend, phone);
        }

        private void SetButtonUsedVisual(Button btn, TextMeshProUGUI label, bool available)
        {
            var img = btn.GetComponent<Image>();
            Tween.Color(img, available ? (Color)_btnNormal : (Color)_btnDisabled, 0.3f);
            Tween.Alpha(label, available ? 1f : 0.4f, 0.3f);
        }

        public void UpdateLadder(int currentStep)
        {
            for (int i = 0; i < MoneyLadder.TotalSteps; i++)
            {
                if (i == currentStep)
                {
                    Tween.Color(ladderBackgrounds[i], (Color)_ladderActive, 0.4f);
                    ladderLabels[i].fontStyle = FontStyles.Bold;
                    Tween.Scale(_ladderRowRTs[i], Vector3.one * 1.05f, 0.5f, cycles: -1, cycleMode: CycleMode.Yoyo);
                }
                else if (i == MoneyLadder.SafeHaven1 || i == MoneyLadder.SafeHaven2)
                {
                    Tween.StopAll(_ladderRowRTs[i]);
                    _ladderRowRTs[i].localScale = Vector3.one;
                    ladderBackgrounds[i].color = _ladderSafe;
                    ladderLabels[i].fontStyle = FontStyles.Normal;
                    ladderLabels[i].color = _white;
                }
                else
                {
                    Tween.StopAll(_ladderRowRTs[i]);
                    _ladderRowRTs[i].localScale = Vector3.one;
                    ladderBackgrounds[i].color = i < currentStep ? new Color32(40, 80, 40, 150) : _ladderNormal; // slight green for passed
                    ladderLabels[i].fontStyle = FontStyles.Normal;
                    ladderLabels[i].color = _white;
                }
            }
        }

        public void ShowResult(string title, string message)
        {
            ShowPanel(resultPanel);
            resultTitle.text = title;
            resultMessage.text = message;

            DisableAnswerButtons();
            btnFiftyFifty.interactable = false;
            btnAskAudience.interactable = false;
            btnPhoneFriend.interactable = false;
            btnWalkAway.interactable = false;
        }

        public void HideResult() { AnimateButtonPress(resultMenuButton); HidePanel(resultPanel); }

        // ══════════════════════════════════════════════
        //  HELPER FACTORY METHODS
        // ══════════════════════════════════════════════
        private GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = _panelBg;
            img.sprite = _roundedSprite;
            img.type = Image.Type.Sliced;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = _borderColor;
            outline.effectDistance = new Vector2(2, -2);
            
            return go;
        }

        private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.color = _white;
            return tmp;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, float fontSize = 28)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = _btnNormal;
            img.sprite = _roundedSprite;
            img.type = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.7f, 0.85f, 1f);
            colors.pressedColor = new Color(0.5f, 0.65f, 0.9f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.55f);
            btn.colors = colors;
            btn.targetGraphic = img;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            var tmp = CreateTMP(go.transform, "Label", label, fontSize, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(20, 20));
            tmp.raycastTarget = false;

            return btn;
        }

        /// <summary>
        /// Generates a simple rounded square sprite programmatically to provide "border radius".
        /// </summary>
        private Sprite CreateRoundedSprite(int size, int radius)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Min(x, size - x - 1);
                    float dy = Mathf.Min(y, size - y - 1);
                    bool inside = true;

                    if (dx < radius && dy < radius)
                    {
                        float d = Mathf.Sqrt(Mathf.Pow(radius - dx, 2) + Mathf.Pow(radius - dy, 2));
                        if (d > radius) inside = false;
                    }

                    cols[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
