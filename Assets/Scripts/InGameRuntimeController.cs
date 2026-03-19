using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum InGameCardKind { Substance, Method, Hse }
public enum InGamePhase { SubstanceSelection, MethodSelection, HseSelection, Completed }

[Serializable]
public class InGameStepData
{
    public string stepId;
    public string stepTitle;
    public string stepBrief;
    public string introBrief;
    public string introObjective;
    public string introHseFocus;
    public string substancePhaseInstruction;
    public string methodPhaseInstruction;
    public string hsePhaseInstruction;
    public string validateSubstanceLabel;
    public string validateMethodLabel;
    public string validateHseLabel;
    public int substanceSlotCount;
    public int methodSlotCount;
    public int hseSlotCount;
    public string[] substanceIds;
    public string[] methodIds;
    public string[] hseIds;
    public string[] displaySubstanceIds;
    public string[] displayMethodIds;
    public string[] displayHseIds;
    public string[] correctSubstanceIds;
    public string[] correctMethodIds;
    public string[] correctHseIds;
    public string[] expectedSubstanceIds;
    public string[] expectedMethodIds;
    public string[] expectedHseIds;
    public string[] trapSubstanceIds;
    public string[] trapMethodIds;
    public string[] trapHseIds;
    public InGameTrapErrorData[] trapSubstanceErrors;
    public InGameTrapErrorData[] trapMethodErrors;
    public InGameTrapErrorData[] trapHseErrors;
}

[Serializable]
public class InGameTrapErrorData
{
    public string cardId;
    public string message;
}

public class InGameDropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static readonly Color ColorIdle = new Color(0.06f, 0.12f, 0.22f, 0.44f);
    public static readonly Color ColorInactive = new Color(0.05f, 0.09f, 0.16f, 0.30f);
    public static readonly Color ColorHighlight = new Color(0.22f, 0.48f, 0.76f, 0.58f);
    public static readonly Color ColorFilled = new Color(0.08f, 0.16f, 0.28f, 0.46f);

    public InGameCardKind Kind { get; private set; }
    public int Index { get; private set; }
    public RectTransform RectTransform { get; private set; }
    public bool PhaseActive { get; private set; }

    private InGameRuntimeController _controller;
    private Image _background;
    private Text _label;
    private Image _labelBand;
    private bool _locked;
    private bool _filled;
    private Coroutine _flashCoroutine;

    public void Configure(InGameRuntimeController controller, InGameCardKind kind, int index, Image background, Text label, Image labelBand = null)
    {
        _controller = controller;
        Kind = kind;
        Index = index;
        _background = background;
        _label = label;
        _labelBand = labelBand;
        RectTransform = transform as RectTransform;
        _locked = false;
        _filled = false;
        PhaseActive = true;
        ApplyVisual();
    }

    public void SetPhaseActive(bool active) { PhaseActive = active; ApplyVisual(); }
    public void SetFilled(bool filled) { _filled = filled; ApplyVisual(); }
    public void SetLocked(bool locked) { _locked = locked; ApplyVisual(); }
    public void ResetVisual() { _locked = false; _filled = false; PhaseActive = true; ApplyVisual(); }

    public void SetHighlighted(bool highlighted)
    {
        if (_locked) return;
        if (!_filled && PhaseActive) _background.color = highlighted ? ColorHighlight : ColorIdle;
    }

    public void FlashReject()
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRejectRoutine());
    }

    private System.Collections.IEnumerator FlashRejectRoutine()
    {
        _background.color = new Color(0.65f, 0.2f, 0.2f, 1f);
        yield return new WaitForSecondsRealtime(0.18f);
        ApplyVisual();
        _flashCoroutine = null;
    }

    private void ApplyVisual()
    {
        if (_background == null) return;
        if (_locked || _filled) _background.color = ColorFilled;
        else if (!PhaseActive) _background.color = ColorInactive;
        else _background.color = ColorIdle;

        if (_labelBand != null)
        {
            if (_filled) _labelBand.enabled = false;
            else
            {
                _labelBand.enabled = true;
                _labelBand.color = PhaseActive ? new Color(0.02f, 0.05f, 0.10f, 0.68f) : new Color(0.02f, 0.05f, 0.10f, 0.52f);
            }
        }

        if (_label != null)
        {
            _label.enabled = !_filled;
            _label.color = PhaseActive ? Color.white : new Color(0.83f, 0.88f, 0.95f, 0.78f);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_controller == null) return;
        InGameDraggableCard card = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<InGameDraggableCard>() : null;
        if (card != null) _controller.HandleSlotDrop(this, card);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!PhaseActive || _locked) return;
        InGameDraggableCard card = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<InGameDraggableCard>() : null;
        if (card != null && card.Kind == Kind) _background.color = ColorHighlight;
    }

    public void OnPointerExit(PointerEventData eventData) { ApplyVisual(); }
}

public class InGameDraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string CardId { get; private set; }
    public InGameCardKind Kind { get; private set; }
    public bool IsTrap { get; private set; }
    public bool DropAccepted { get; set; }
    public CanvasGroup CG { get; private set; }
    public RectTransform RectTransform { get; private set; }

    private InGameRuntimeController _controller;
    private Transform _homeParent;
    private bool _locked;
    private Image _background;
    private RectTransform _imageRect;
    private RectTransform _labelBandRect;
    private RectTransform _titleRect;
    private RectTransform _subtitleRect;
    private Text _titleText;
    private Text _subtitleText;

    public void Configure(InGameRuntimeController controller, string cardId, InGameCardKind kind, bool isTrap, CanvasGroup cg, RectTransform rt)
    {
        _controller = controller;
        CardId = cardId;
        Kind = kind;
        IsTrap = isTrap;
        CG = cg;
        RectTransform = rt;
        _background = rt != null ? rt.GetComponent<Image>() : null;
        CacheVisualRefs();
        DropAccepted = false;
        _locked = false;
        ApplyHomeVisualLayout();
    }

    public void CaptureHome(Transform parent) { _homeParent = parent; }
    public void SetLocked(bool locked) { _locked = locked; if (CG != null) CG.alpha = locked ? 0.95f : 1f; }

    public void SnapToSlot(RectTransform slot)
    {
        RectTransform.SetParent(slot, false);
        RectTransform.anchorMin = new Vector2(0.05f, 0.06f);
        RectTransform.anchorMax = new Vector2(0.95f, 0.94f);
        RectTransform.pivot = new Vector2(0.5f, 0.5f);
        RectTransform.offsetMin = Vector2.zero;
        RectTransform.offsetMax = Vector2.zero;
        RectTransform.localScale = Vector3.one;
        RectTransform.SetAsLastSibling();
        if (_background != null)
        {
            Color c = _background.color;
            c.a = 0.36f;
            _background.color = c;
        }
        ApplySlotVisualLayout();
    }

    public void ReturnHome()
    {
        if (_homeParent == null) return;
        RectTransform.SetParent(_homeParent, false);
        RectTransform.anchorMin = new Vector2(0f, 0.5f);
        RectTransform.anchorMax = new Vector2(0f, 0.5f);
        RectTransform.pivot = new Vector2(0f, 0.5f);
        RectTransform.localScale = Vector3.one;
        if (_background != null)
        {
            Color c = _background.color;
            c.a = 0.45f;
            _background.color = c;
        }
        ApplyHomeVisualLayout();
    }

    private void CacheVisualRefs()
    {
        if (RectTransform == null) return;
        _imageRect = RectTransform.Find("Image") as RectTransform;
        _labelBandRect = RectTransform.Find("LabelBand") as RectTransform;
        _titleRect = RectTransform.Find("Title") as RectTransform;
        _subtitleRect = RectTransform.Find("Subtitle") as RectTransform;

        if (_titleRect != null) _titleText = _titleRect.GetComponentInChildren<Text>();
        if (_subtitleRect != null) _subtitleText = _subtitleRect.GetComponentInChildren<Text>();
    }

    private void ApplyHomeVisualLayout()
    {
        if (_imageRect != null)
        {
            _imageRect.anchorMin = new Vector2(0.08f, 0.34f);
            _imageRect.anchorMax = new Vector2(0.92f, 0.90f);
            _imageRect.offsetMin = Vector2.zero;
            _imageRect.offsetMax = Vector2.zero;
        }

        if (_labelBandRect != null)
        {
            _labelBandRect.anchorMin = new Vector2(0.06f, 0.03f);
            _labelBandRect.anchorMax = new Vector2(0.94f, 0.32f);
            _labelBandRect.offsetMin = Vector2.zero;
            _labelBandRect.offsetMax = Vector2.zero;
        }

        if (_titleRect != null)
        {
            _titleRect.anchorMin = new Vector2(0.08f, 0.16f);
            _titleRect.anchorMax = new Vector2(0.92f, 0.26f);
            _titleRect.offsetMin = Vector2.zero;
            _titleRect.offsetMax = Vector2.zero;
        }

        if (_subtitleRect != null)
        {
            _subtitleRect.anchorMin = new Vector2(0.08f, 0.06f);
            _subtitleRect.anchorMax = new Vector2(0.92f, 0.14f);
            _subtitleRect.offsetMin = Vector2.zero;
            _subtitleRect.offsetMax = Vector2.zero;
        }

        if (_titleText != null) _titleText.fontSize = 20;
        if (_subtitleText != null) _subtitleText.fontSize = 15;
    }

    private void ApplySlotVisualLayout()
    {
        if (_imageRect != null)
        {
            _imageRect.anchorMin = new Vector2(0.14f, 0.40f);
            _imageRect.anchorMax = new Vector2(0.86f, 0.90f);
            _imageRect.offsetMin = Vector2.zero;
            _imageRect.offsetMax = Vector2.zero;
        }

        if (_labelBandRect != null)
        {
            _labelBandRect.anchorMin = new Vector2(0.08f, 0.06f);
            _labelBandRect.anchorMax = new Vector2(0.92f, 0.36f);
            _labelBandRect.offsetMin = Vector2.zero;
            _labelBandRect.offsetMax = Vector2.zero;
        }

        if (_titleRect != null)
        {
            _titleRect.anchorMin = new Vector2(0.10f, 0.22f);
            _titleRect.anchorMax = new Vector2(0.90f, 0.32f);
            _titleRect.offsetMin = Vector2.zero;
            _titleRect.offsetMax = Vector2.zero;
        }

        if (_subtitleRect != null)
        {
            _subtitleRect.anchorMin = new Vector2(0.10f, 0.10f);
            _subtitleRect.anchorMax = new Vector2(0.90f, 0.20f);
            _subtitleRect.offsetMin = Vector2.zero;
            _subtitleRect.offsetMax = Vector2.zero;
        }

        if (_titleText != null) _titleText.fontSize = 16;
        if (_subtitleText != null) _subtitleText.fontSize = 12;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_controller == null || _locked) return;
        if (_controller.CanBeginDrag(this)) _controller.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_controller == null || _locked) return;
        _controller.Drag(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_controller == null || _locked) return;
        _controller.EndDrag(this);
    }
}

[Serializable]
public class InGameLevelData
{
    public string id;
    public int levelIndex;
    public string title;
    public string summary;
    public string objective;
    public string hseFocus;
    public string sceneName;
    public string levelBrief;
    public string targetProductId;
    public string targetProductName;
    public string trapSubstanceId;
    public int substanceCardCount;
    public int methodCardCount;
    public int hseCardCount;
    public int defaultSubstanceSlotCount;
    public int defaultMethodSlotCount;
    public int defaultHseSlotCount;
    public int timeTargetSeconds;
    public string[] substanceIds;
    public string[] methodIds;
    public string[] hseIds;
    public InGameStepData[] steps;
}

[Serializable]
public class InGameSubstanceData
{
    public string id;
    public string displayName;
    public string formula;
    public string atlasTextureResource;
    public int atlasCellIndex;
    public int atlasColumns;
    public int atlasCellWidth;
    public int atlasCellHeight;
    public string moleculeImageResource;
    public string cardImageResource;
}

[Serializable]
public class InGameMethodData
{
    public string id;
    public string title;
    public string subtitle;
    public string atlasTextureResource;
    public int atlasX;
    public int atlasY;
    public int atlasWidth;
    public int atlasHeight;
    public string imageResource;
}

[Serializable]
public class InGameHseData
{
    public string id;
    public string title;
    public string subtitle;
    public string atlasTextureResource;
    public int atlasX;
    public int atlasY;
    public int atlasWidth;
    public int atlasHeight;
    public string imageResource;
}

public class InGameRuntimeController : MonoBehaviour
{
    private const string SelectedLevelKey = "SelectedLevel";
    private const string SelectedLevelIdKey = "SelectedLevelId";
    private const float ExtraCutoutTopPadding = 18f;
    private const float ExtraSideSafePadding = 14f;

    private Canvas _canvas;
    private RectTransform _root;
    private RectTransform _dragLayer;
    private RectTransform _introPanel;
    private Text _introTitle;
    private Text _introBody;
    private RectTransform _gamePanel;
    private RectTransform _slotsArea;
    private Text _timerText;
    private Text _stepTitleText;
    private Text _phaseInstructionText;
    private Text _phaseFeedbackText;
    private Button _validateButton;
    private RectTransform _trayViewport;
    private RectTransform _trayContent;
    private ScrollRect _trayScroll;
    private RectTransform _resultPanel;
    private Text _resultTitle;
    private Text _resultBody;
    private Text _resultMistakesTitle;
    private Text _resultMistakesBody;
    private ScrollRect _resultMistakesScroll;
    private RectTransform _resultStarsRoot;
    private Sprite _starSprite;

    private readonly Dictionary<InGameDropSlot, InGameDraggableCard> _assignments = new Dictionary<InGameDropSlot, InGameDraggableCard>();
    private readonly Dictionary<InGameCardKind, List<InGameDropSlot>> _slotsByKind = new Dictionary<InGameCardKind, List<InGameDropSlot>>();
    private readonly List<InGameDraggableCard> _currentTrayCards = new List<InGameDraggableCard>();
    private readonly List<InGameCardDescriptor> _phaseCards = new List<InGameCardDescriptor>();

    private InGameLevelData _level;
    private InGameStepData _step;
    private int _stepIndex;
    private InGamePhase _phase;
    private float _timerStart;
    private bool _timerRunning;
    private float _elapsedSeconds;
    private int _errorCount;
    private Vector2 _dragPointerOffset;
    private Vector2 _lastSlotsAreaSize = new Vector2(-1f, -1f);
    private readonly List<string> _mistakeLines = new List<string>();

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    private void Start()
    {
        try
        {
            RuntimeFileLogger.Log("InGameRuntimeController", "Start begin on scene=" + SceneManager.GetActiveScene().name);
            PlayerProfileStore.EnsureInitialized();
            LoadLevel();
            _errorCount = 0;
            _mistakeLines.Clear();
            BuildUi();
            PrepareStep(0);
            ShowIntroForCurrentStep();
            StartCoroutine(RefreshResponsiveLayoutNextFrame());
            RuntimeFileLogger.Log("InGameRuntimeController", "Start completed level=" + (_level != null ? _level.id : "null"));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            RuntimeFileLogger.Error("InGameRuntimeController", "Fatal Start error: " + exception);
            ShowFatalOverlay(exception);
        }
    }

    private void Update()
    {
        if (_timerRunning)
        {
            _elapsedSeconds = Mathf.Max(0f, Time.unscaledTime - _timerStart);
            if (_timerText != null) _timerText.text = BuildTimerText(_elapsedSeconds);
        }

        if (_introPanel == null || !_introPanel.gameObject.activeSelf || _slotsArea == null)
            return;

        Vector2 size = _slotsArea.rect.size;
        if (size.x <= 1f || size.y <= 1f)
            return;
        if (Mathf.Abs(size.x - _lastSlotsAreaSize.x) <= 1f && Mathf.Abs(size.y - _lastSlotsAreaSize.y) <= 1f)
            return;

        _lastSlotsAreaSize = size;
        RebuildSlotsForStep();
        UpdatePhaseUi();
    }

    private void LoadLevel()
    {
        int selectedLevel = Mathf.Max(1, PlayerPrefs.GetInt(SelectedLevelKey, 1));
        string levelId = PlayerPrefs.GetString(SelectedLevelIdKey, string.Empty);
        TextAsset asset = null;
        if (!string.IsNullOrWhiteSpace(levelId)) asset = Resources.Load<TextAsset>("Levels/" + levelId);
        if (asset == null) asset = Resources.Load<TextAsset>("Levels/level-" + selectedLevel);
        if (asset == null) asset = Resources.Load<TextAsset>("Levels/level-1");
        RuntimeFileLogger.Log("InGameRuntimeController", "LoadLevel levelId=" + levelId + " selected=" + selectedLevel + " asset=" + (asset != null ? asset.name : "null"));

        if (asset != null) _level = JsonUtility.FromJson<InGameLevelData>(asset.text);
        if (_level == null)
            _level = new InGameLevelData { id = "level-1", levelIndex = selectedLevel, title = "Niveau " + selectedLevel, objective = "Objectif non defini." };

        if (_level.defaultSubstanceSlotCount <= 0) _level.defaultSubstanceSlotCount = 3;
        if (_level.defaultMethodSlotCount <= 0) _level.defaultMethodSlotCount = 1;
        if (_level.defaultHseSlotCount <= 0) _level.defaultHseSlotCount = 2;

        if (_level.steps == null || _level.steps.Length == 0)
        {
            _level.steps = new[]
            {
                new InGameStepData
                {
                    stepId = "step-1",
                    stepTitle = "Étape 1",
                    stepBrief = _level.objective,
                    introBrief = _level.levelBrief,
                    introObjective = _level.objective,
                    introHseFocus = _level.hseFocus,
                    substanceSlotCount = _level.defaultSubstanceSlotCount,
                    methodSlotCount = _level.defaultMethodSlotCount,
                    hseSlotCount = _level.defaultHseSlotCount,
                    displaySubstanceIds = _level.substanceIds ?? Array.Empty<string>(),
                    displayMethodIds = _level.methodIds ?? Array.Empty<string>(),
                    displayHseIds = _level.hseIds ?? Array.Empty<string>(),
                    expectedSubstanceIds = _level.substanceIds ?? Array.Empty<string>(),
                    expectedMethodIds = _level.methodIds ?? Array.Empty<string>(),
                    expectedHseIds = _level.hseIds ?? Array.Empty<string>()
                }
            };
        }
    }

    private void BuildUi()
    {
        MobileScreenUtility.ForcePortraitOrientation();
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;
        gameObject.AddComponent<GraphicRaycaster>();
        _root = gameObject.GetComponent<RectTransform>();
        if (_root == null) _root = gameObject.AddComponent<RectTransform>();
        Stretch(_root, 0f, 0f, 0f, 0f);
        _starSprite = LoadSpriteSmart("Icons/star");
        BuildBackground();
        BuildGamePanel();
        BuildIntroPanel();
        BuildResultPanel();
        _dragLayer = EnsureRect(_root, "DragLayer");
        Stretch(_dragLayer, 0f, 0f, 0f, 0f);
        Image dragBg = EnsureImage(_dragLayer);
        dragBg.color = new Color(1f, 1f, 1f, 0f);
        dragBg.raycastTarget = false;
        _dragLayer.SetAsLastSibling();
    }

    private void BuildBackground()
    {
        RectTransform bgRt = EnsureRect(_root, "Background");
        Stretch(bgRt, 0f, 0f, 0f, 0f);
        Image bg = EnsureImage(bgRt);
        Sprite s = LoadSpriteSmart("Backgrounds/bg-menus-1");
        if (s == null) s = LoadSpriteSmart("Backgrounds/bg-menus-4");
        bg.sprite = s;
        bg.color = Color.white;
        bg.preserveAspect = false;
        bg.raycastTarget = false;
        bgRt.SetAsFirstSibling();
    }

    private void BuildIntroPanel()
    {
        _introPanel = EnsureRect(_root, "IntroPanel");
        Stretch(_introPanel, 0f, 0f, 0f, 0f);
        EnsureImage(_introPanel).color = new Color(0f, 0f, 0f, 0.58f);

        RectTransform card = EnsureRect(_introPanel, "IntroCard");
        card.anchorMin = new Vector2(0.08f, 0.2f);
        card.anchorMax = new Vector2(0.92f, 0.8f);
        card.offsetMin = Vector2.zero;
        card.offsetMax = Vector2.zero;
        EnsureImage(card).color = new Color(0.95f, 0.91f, 0.82f, 0.98f);

        _introTitle = EnsureText(card, "IntroTitle", 46, FontStyle.Bold, "Niveau");
        _introTitle.color = new Color(0.16f, 0.11f, 0.07f, 1f);
        _introTitle.alignment = TextAnchor.UpperCenter;
        _introTitle.lineSpacing = 1.15f;
        _introTitle.resizeTextForBestFit = false;
        RectTransform titleRt = _introTitle.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -30f);
        titleRt.sizeDelta = new Vector2(-48f, 116f);

        _introBody = EnsureText(card, "IntroBody", 30, FontStyle.Normal, string.Empty);
        _introBody.color = new Color(0.18f, 0.13f, 0.09f, 1f);
        _introBody.alignment = TextAnchor.UpperLeft;
        _introBody.lineSpacing = 1.12f;
        _introBody.supportRichText = true;
        _introBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        _introBody.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform bodyRt = _introBody.rectTransform;
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(34f, 150f);
        bodyRt.offsetMax = new Vector2(-34f, -176f);

        RectTransform btnRt = EnsureRect(card, "BtnIntroNext");
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 36f);
        btnRt.sizeDelta = new Vector2(320f, 84f);
        Image btnBg = EnsureImage(btnRt);
        btnBg.color = new Color(0.16f, 0.45f, 0.24f, 1f);
        Button next = EnsureButton(btnRt);
        next.targetGraphic = btnBg;
        next.onClick.RemoveAllListeners();
        next.onClick.AddListener(StartGameplayForStep);

        Text btnTxt = EnsureText(btnRt, "Label", 34, FontStyle.Bold, "Suivant");
        btnTxt.alignment = TextAnchor.MiddleCenter;
        Stretch(btnTxt.rectTransform, 0f, 0f, 0f, 0f);
    }

    private void BuildGamePanel()
    {
        SafeAreaInsets safeInsets = MobileScreenUtility.GetSafeAreaInsets(_root);
        float extraTopSafe = Mathf.Clamp((_root != null ? _root.rect.height : 1920f) * 0.012f, 12f, 24f) + ExtraCutoutTopPadding;
        float extraSideSafe = Mathf.Clamp((_root != null ? _root.rect.width : 1080f) * 0.014f, 10f, 22f) + ExtraSideSafePadding;

        _gamePanel = EnsureRect(_root, "GamePanel");
        Stretch(_gamePanel, 0f, 0f, 0f, 0f);

        RectTransform topBar = EnsureRect(_gamePanel, "TopBar");
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.anchoredPosition = Vector2.zero;
        topBar.sizeDelta = new Vector2(0f, 150f + safeInsets.top + extraTopSafe);
        EnsureImage(topBar).color = new Color(0.05f, 0.09f, 0.14f, 0.9f);

        RectTransform topBarContent = EnsureRect(topBar, "SafeContent");
        topBarContent.anchorMin = Vector2.zero;
        topBarContent.anchorMax = Vector2.one;
        topBarContent.offsetMin = new Vector2(safeInsets.left + extraSideSafe, 0f);
        topBarContent.offsetMax = new Vector2(-(safeInsets.right + extraSideSafe), -(safeInsets.top + extraTopSafe));
        EnsureImage(topBarContent).color = new Color(1f, 1f, 1f, 0f);

        _timerText = EnsureText(topBarContent, "Timer", 32, FontStyle.Bold, "00:00");
        _timerText.alignment = TextAnchor.MiddleLeft;
        RectTransform timerRt = _timerText.rectTransform;
        timerRt.anchorMin = new Vector2(0f, 0f);
        timerRt.anchorMax = new Vector2(0f, 1f);
        timerRt.pivot = new Vector2(0f, 0.5f);
        timerRt.anchoredPosition = new Vector2(18f, 0f);
        timerRt.sizeDelta = new Vector2(220f, 0f);

        _stepTitleText = EnsureText(topBarContent, "StepTitle", 34, FontStyle.Bold, "Étape");
        _stepTitleText.alignment = TextAnchor.UpperCenter;
        _stepTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _stepTitleText.verticalOverflow = VerticalWrapMode.Truncate;
        _stepTitleText.resizeTextForBestFit = true;
        _stepTitleText.resizeTextMinSize = 22;
        _stepTitleText.resizeTextMaxSize = 36;
        _stepTitleText.lineSpacing = 0.96f;
        RectTransform stepRt = _stepTitleText.rectTransform;
        float textCutoutShift = Mathf.Clamp((_root != null ? _root.rect.height : 1920f) * 0.012f, 16f, 28f);
        stepRt.anchorMin = new Vector2(0f, 0.42f);
        stepRt.anchorMax = new Vector2(1f, 0.88f);
        stepRt.offsetMin = new Vector2(220f, 6f);
        stepRt.offsetMax = new Vector2(-220f, -(10f + textCutoutShift));

        _phaseInstructionText = EnsureText(topBarContent, "PhaseInstruction", 24, FontStyle.Normal, string.Empty);
        _phaseInstructionText.alignment = TextAnchor.UpperCenter;
        _phaseInstructionText.color = new Color(0.84f, 0.91f, 1f, 1f);
        _phaseInstructionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _phaseInstructionText.verticalOverflow = VerticalWrapMode.Truncate;
        _phaseInstructionText.resizeTextForBestFit = true;
        _phaseInstructionText.resizeTextMinSize = 16;
        _phaseInstructionText.resizeTextMaxSize = 24;
        _phaseInstructionText.lineSpacing = 1.02f;
        RectTransform phaseRt = _phaseInstructionText.rectTransform;
        phaseRt.anchorMin = new Vector2(0f, 0.04f);
        phaseRt.anchorMax = new Vector2(1f, 0.30f);
        phaseRt.offsetMin = new Vector2(210f, 2f);
        phaseRt.offsetMax = new Vector2(-210f, -(8f + (textCutoutShift * 0.45f)));

        _slotsArea = EnsureRect(_gamePanel, "SlotsArea");
        _slotsArea.anchorMin = new Vector2(0f, 0f);
        _slotsArea.anchorMax = new Vector2(1f, 1f);
        _slotsArea.offsetMin = new Vector2(safeInsets.left + extraSideSafe, 320f + safeInsets.bottom);
        _slotsArea.offsetMax = new Vector2(-(safeInsets.right + extraSideSafe), -(170f + safeInsets.top + extraTopSafe));
        EnsureImage(_slotsArea).color = new Color(1f, 1f, 1f, 0f);

        _slotsByKind[InGameCardKind.Method] = new List<InGameDropSlot>();
        _slotsByKind[InGameCardKind.Substance] = new List<InGameDropSlot>();
        _slotsByKind[InGameCardKind.Hse] = new List<InGameDropSlot>();

        RectTransform feedbackRt = EnsureRect(_gamePanel, "Feedback");
        feedbackRt.anchorMin = new Vector2(0.5f, 0f);
        feedbackRt.anchorMax = new Vector2(0.5f, 0f);
        feedbackRt.pivot = new Vector2(0.5f, 0f);
        feedbackRt.anchoredPosition = new Vector2(0f, 392f);
        feedbackRt.sizeDelta = new Vector2(940f, 44f);
        _phaseFeedbackText = EnsureText(feedbackRt, "Text", 22, FontStyle.Bold, string.Empty);
        _phaseFeedbackText.alignment = TextAnchor.MiddleCenter;
        Stretch(_phaseFeedbackText.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform validateRt = EnsureRect(_gamePanel, "BtnValidate");
        validateRt.anchorMin = new Vector2(0.5f, 0f);
        validateRt.anchorMax = new Vector2(0.5f, 0f);
        validateRt.pivot = new Vector2(0.5f, 0f);
        validateRt.anchoredPosition = new Vector2(0f, 320f);
        validateRt.sizeDelta = new Vector2(460f, 72f);
        Image validateBg = EnsureImage(validateRt);
        validateBg.color = new Color(0.15f, 0.45f, 0.26f, 1f);
        _validateButton = EnsureButton(validateRt);
        _validateButton.targetGraphic = validateBg;
        _validateButton.onClick.RemoveAllListeners();
        _validateButton.onClick.AddListener(ValidateCurrentPhase);
        Text validateLabel = EnsureText(validateRt, "Label", 30, FontStyle.Bold, "Valider");
        validateLabel.alignment = TextAnchor.MiddleCenter;
        Stretch(validateLabel.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform tray = EnsureRect(_gamePanel, "CardTray");
        tray.anchorMin = new Vector2(0f, 0f);
        tray.anchorMax = new Vector2(1f, 0f);
        tray.pivot = new Vector2(0.5f, 0f);
        tray.anchoredPosition = Vector2.zero;
        tray.sizeDelta = new Vector2(0f, 310f + safeInsets.bottom);
        EnsureImage(tray).color = new Color(0.03f, 0.07f, 0.12f, 0.96f);

        _trayViewport = EnsureRect(tray, "Viewport");
        _trayViewport.anchorMin = Vector2.zero;
        _trayViewport.anchorMax = Vector2.one;
        _trayViewport.offsetMin = new Vector2(16f + safeInsets.left, 20f + safeInsets.bottom);
        _trayViewport.offsetMax = new Vector2(-(16f + safeInsets.right), -20f);
        EnsureImage(_trayViewport).color = new Color(1f, 1f, 1f, 0.04f);
        Mask mask = _trayViewport.GetComponent<Mask>();
        if (mask == null) mask = _trayViewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        _trayContent = EnsureRect(_trayViewport, "Content");
        _trayContent.anchorMin = new Vector2(0f, 0f);
        _trayContent.anchorMax = new Vector2(0f, 1f);
        _trayContent.pivot = new Vector2(0f, 0.5f);
        _trayContent.anchoredPosition = Vector2.zero;
        _trayContent.sizeDelta = Vector2.zero;

        _trayScroll = tray.GetComponent<ScrollRect>();
        if (_trayScroll == null) _trayScroll = tray.gameObject.AddComponent<ScrollRect>();
        _trayScroll.horizontal = true;
        _trayScroll.vertical = false;
        _trayScroll.viewport = _trayViewport;
        _trayScroll.content = _trayContent;
        _trayScroll.movementType = ScrollRect.MovementType.Clamped;
        _trayScroll.scrollSensitivity = 24f;
        if (_trayScroll.horizontalScrollbar != null)
        {
            _trayScroll.horizontalScrollbar.gameObject.SetActive(false);
            _trayScroll.horizontalScrollbar = null;
        }

        _gamePanel.gameObject.SetActive(false);
    }
    private void BuildResultPanel()
    {
        _resultPanel = EnsureRect(_root, "ResultPanel");
        Stretch(_resultPanel, 0f, 0f, 0f, 0f);
        EnsureImage(_resultPanel).color = new Color(0f, 0f, 0f, 0.70f);

        RectTransform card = EnsureRect(_resultPanel, "ResultCard");
        card.anchorMin = new Vector2(0.06f, 0.07f);
        card.anchorMax = new Vector2(0.94f, 0.93f);
        EnsureImage(card).color = new Color(0.08f, 0.12f, 0.20f, 0.96f);

        RectTransform paper = EnsureRect(card, "Paper");
        Stretch(paper, 10f, 10f, 10f, 10f);
        EnsureImage(paper).color = new Color(0.95f, 0.91f, 0.82f, 0.98f);

        _resultTitle = EnsureText(paper, "Title", 52, FontStyle.Bold, "Résultat du niveau");
        _resultTitle.color = new Color(0.17f, 0.12f, 0.08f, 1f);
        _resultTitle.alignment = TextAnchor.UpperCenter;
        RectTransform titleRt = _resultTitle.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);
        titleRt.sizeDelta = new Vector2(-50f, 64f);

        _resultStarsRoot = EnsureRect(paper, "Stars");
        _resultStarsRoot.anchorMin = new Vector2(0.5f, 1f);
        _resultStarsRoot.anchorMax = new Vector2(0.5f, 1f);
        _resultStarsRoot.pivot = new Vector2(0.5f, 1f);
        _resultStarsRoot.anchoredPosition = new Vector2(0f, -90f);
        _resultStarsRoot.sizeDelta = new Vector2(360f, 140f);

        _resultBody = EnsureText(paper, "Body", 30, FontStyle.Normal, string.Empty);
        _resultBody.color = new Color(0.20f, 0.14f, 0.10f, 1f);
        _resultBody.alignment = TextAnchor.MiddleCenter;
        _resultBody.supportRichText = true;
        RectTransform bodyRt = _resultBody.rectTransform;
        bodyRt.anchorMin = new Vector2(0.08f, 0.58f);
        bodyRt.anchorMax = new Vector2(0.92f, 0.72f);
        bodyRt.offsetMin = Vector2.zero;
        bodyRt.offsetMax = Vector2.zero;

        _resultMistakesTitle = EnsureText(paper, "MistakesTitle", 30, FontStyle.Bold, "Résumé des erreurs");
        _resultMistakesTitle.color = new Color(0.14f, 0.10f, 0.07f, 1f);
        _resultMistakesTitle.alignment = TextAnchor.MiddleLeft;
        RectTransform mistakesTitleRt = _resultMistakesTitle.rectTransform;
        mistakesTitleRt.anchorMin = new Vector2(0.08f, 0.50f);
        mistakesTitleRt.anchorMax = new Vector2(0.92f, 0.56f);
        mistakesTitleRt.offsetMin = Vector2.zero;
        mistakesTitleRt.offsetMax = Vector2.zero;

        RectTransform mistakesPanel = EnsureRect(paper, "MistakesPanel");
        mistakesPanel.anchorMin = new Vector2(0.08f, 0.19f);
        mistakesPanel.anchorMax = new Vector2(0.92f, 0.49f);
        mistakesPanel.offsetMin = Vector2.zero;
        mistakesPanel.offsetMax = Vector2.zero;
        EnsureImage(mistakesPanel).color = new Color(0.14f, 0.11f, 0.08f, 0.16f);

        RectTransform mistakesViewport = EnsureRect(mistakesPanel, "Viewport");
        Stretch(mistakesViewport, 12f, 12f, 12f, 12f);
        Image viewportImage = EnsureImage(mistakesViewport);
        viewportImage.color = new Color(1f, 1f, 1f, 0.04f);
        Mask mask = mistakesViewport.GetComponent<Mask>();
        if (mask == null) mask = mistakesViewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform mistakesContent = EnsureRect(mistakesViewport, "Content");
        mistakesContent.anchorMin = new Vector2(0f, 1f);
        mistakesContent.anchorMax = new Vector2(1f, 1f);
        mistakesContent.pivot = new Vector2(0.5f, 1f);
        mistakesContent.anchoredPosition = Vector2.zero;
        mistakesContent.sizeDelta = new Vector2(0f, 10f);

        _resultMistakesBody = EnsureText(mistakesContent, "Text", 26, FontStyle.Normal, string.Empty);
        _resultMistakesBody.color = new Color(0.19f, 0.13f, 0.09f, 1f);
        _resultMistakesBody.alignment = TextAnchor.UpperLeft;
        _resultMistakesBody.supportRichText = true;
        _resultMistakesBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        _resultMistakesBody.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform mistakesBodyRt = _resultMistakesBody.rectTransform;
        mistakesBodyRt.anchorMin = new Vector2(0f, 1f);
        mistakesBodyRt.anchorMax = new Vector2(1f, 1f);
        mistakesBodyRt.pivot = new Vector2(0.5f, 1f);
        mistakesBodyRt.anchoredPosition = Vector2.zero;
        mistakesBodyRt.sizeDelta = new Vector2(0f, 10f);

        _resultMistakesScroll = mistakesPanel.GetComponent<ScrollRect>();
        if (_resultMistakesScroll == null) _resultMistakesScroll = mistakesPanel.gameObject.AddComponent<ScrollRect>();
        _resultMistakesScroll.viewport = mistakesViewport;
        _resultMistakesScroll.content = mistakesContent;
        _resultMistakesScroll.horizontal = false;
        _resultMistakesScroll.vertical = true;
        _resultMistakesScroll.movementType = ScrollRect.MovementType.Clamped;
        _resultMistakesScroll.scrollSensitivity = 22f;
        _resultMistakesScroll.horizontalScrollbar = null;

        RectTransform homeBtnRt = EnsureRect(paper, "BtnHome");
        homeBtnRt.anchorMin = new Vector2(0.5f, 0f);
        homeBtnRt.anchorMax = new Vector2(0.5f, 0f);
        homeBtnRt.pivot = new Vector2(0.5f, 0f);
        homeBtnRt.anchoredPosition = new Vector2(0f, 34f);
        homeBtnRt.sizeDelta = new Vector2(340f, 82f);
        Image homeBg = EnsureImage(homeBtnRt);
        homeBg.color = new Color(0.14f, 0.42f, 0.69f, 1f);
        Button homeBtn = EnsureButton(homeBtnRt);
        homeBtn.targetGraphic = homeBg;
        homeBtn.onClick.RemoveAllListeners();
        homeBtn.onClick.AddListener(() => SceneManager.LoadScene("Home"));
        Text homeTxt = EnsureText(homeBtnRt, "Label", 32, FontStyle.Bold, "Menu Principal");
        homeTxt.alignment = TextAnchor.MiddleCenter;
        Stretch(homeTxt.rectTransform, 0f, 0f, 0f, 0f);

        _resultPanel.gameObject.SetActive(false);
    }

    private InGameDropSlot CreateSlot(Transform parent, string name, Vector2 anchor, Vector2 size, InGameCardKind kind, int index, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        Image bg = EnsureImage(rt);
        bg.color = InGameDropSlot.ColorIdle;
        RectTransform slotLabelBandRt = EnsureRect(rt, "LabelBand");
        slotLabelBandRt.anchorMin = new Vector2(0.06f, 0.02f);
        slotLabelBandRt.anchorMax = new Vector2(0.94f, 0.17f);
        slotLabelBandRt.offsetMin = Vector2.zero;
        slotLabelBandRt.offsetMax = Vector2.zero;
        Image slotLabelBand = EnsureImage(slotLabelBandRt);
        slotLabelBand.color = new Color(0.02f, 0.05f, 0.10f, 0.68f);
        slotLabelBand.raycastTarget = false;

        Text slotLabel = EnsureText(slotLabelBandRt, "Text", 16, FontStyle.Bold, label);
        slotLabel.alignment = TextAnchor.MiddleCenter;
        slotLabel.color = Color.white;
        RectTransform slotLabelRt = slotLabel.rectTransform;
        Stretch(slotLabelRt, 0f, 0f, 0f, 0f);
        Outline slotLabelOutline = slotLabel.GetComponent<Outline>();
        if (slotLabelOutline == null) slotLabelOutline = slotLabel.gameObject.AddComponent<Outline>();
        slotLabelOutline.effectColor = new Color(0f, 0f, 0f, 0.78f);
        slotLabelOutline.effectDistance = new Vector2(1f, -1f);
        InGameDropSlot slot = rt.GetComponent<InGameDropSlot>();
        if (slot == null) slot = rt.gameObject.AddComponent<InGameDropSlot>();
        slot.Configure(this, kind, index, bg, slotLabel, slotLabelBand);
        return slot;
    }

    private float GetSlotsScale()
    {
        if (_slotsArea == null)
            return 1f;

        float width = Mathf.Max(1f, _slotsArea.rect.width);
        float height = Mathf.Max(1f, _slotsArea.rect.height);
        float scaleW = width / 960f;
        float scaleH = height / 1120f;
        float scale = Mathf.Min(scaleW, scaleH);
        if (height < 920f)
            scale *= Mathf.Lerp(0.90f, 0.76f, Mathf.InverseLerp(920f, 640f, height));
        return Mathf.Clamp(scale, 0.46f, 1.04f);
    }

    private void RebuildSlotsForStep()
    {
        if (_slotsArea == null) return;

        for (int i = _slotsArea.childCount - 1; i >= 0; i--)
        {
            Transform child = _slotsArea.GetChild(i);
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        if (!_slotsByKind.ContainsKey(InGameCardKind.Method)) _slotsByKind[InGameCardKind.Method] = new List<InGameDropSlot>();
        if (!_slotsByKind.ContainsKey(InGameCardKind.Substance)) _slotsByKind[InGameCardKind.Substance] = new List<InGameDropSlot>();
        if (!_slotsByKind.ContainsKey(InGameCardKind.Hse)) _slotsByKind[InGameCardKind.Hse] = new List<InGameDropSlot>();
        _slotsByKind[InGameCardKind.Method].Clear();
        _slotsByKind[InGameCardKind.Substance].Clear();
        _slotsByKind[InGameCardKind.Hse].Clear();

        BuildMethodSlots(Mathf.Max(1, GetSlotCount(InGameCardKind.Method)));
        BuildSubstanceSlots(Mathf.Max(1, GetSlotCount(InGameCardKind.Substance)));
        BuildHseSlots(Mathf.Max(1, GetSlotCount(InGameCardKind.Hse)));
    }

    private void BuildMethodSlots(int count)
    {
        float slotScale = GetSlotsScale();
        const float clusterYOffset = -0.08f;
        float areaWidth = _slotsArea != null ? Mathf.Max(1f, _slotsArea.rect.width) : 1080f;
        float areaHeight = _slotsArea != null ? Mathf.Max(1f, _slotsArea.rect.height) : 1280f;
        Vector2 size = count > 1 ? new Vector2(248f, 184f) : new Vector2(276f, 198f);
        size *= slotScale;
        size.x = Mathf.Clamp(size.x, 128f, Mathf.Max(128f, areaWidth * 0.34f));
        size.y = Mathf.Clamp(size.y, 98f, Mathf.Max(98f, areaHeight * 0.23f));
        float left = count > 1 ? 0.34f : 0.5f;
        float right = count > 1 ? 0.66f : 0.5f;
        float methodY = Mathf.Lerp(0.72f, 0.78f, slotScale) + clusterYOffset;
        for (int i = 0; i < count; i++)
        {
            float x = count <= 1 ? 0.5f : Mathf.Lerp(left, right, i / (float)(count - 1));
            string label = count == 1 ? "Méthode" : "Méthode " + (i + 1);
            _slotsByKind[InGameCardKind.Method].Add(CreateSlot(_slotsArea, "MethodSlot_" + i, new Vector2(x, methodY), size, InGameCardKind.Method, i, label));
        }
    }

    private void BuildSubstanceSlots(int count)
    {
        float slotScale = GetSlotsScale();
        const float clusterYOffset = -0.08f;
        float areaWidth = _slotsArea != null ? Mathf.Max(1f, _slotsArea.rect.width) : 1080f;
        float areaHeight = _slotsArea != null ? Mathf.Max(1f, _slotsArea.rect.height) : 1280f;
        Vector2 sharedSize = new Vector2(248f, 210f) * slotScale;
        sharedSize.x = Mathf.Clamp(sharedSize.x, 120f, Mathf.Max(120f, areaWidth * 0.30f));
        sharedSize.y = Mathf.Clamp(sharedSize.y, 98f, Mathf.Max(98f, areaHeight * 0.20f));
        float subTopY = Mathf.Lerp(0.50f, 0.55f, slotScale) + clusterYOffset;
        float subBottomY = Mathf.Lerp(0.30f, 0.35f, slotScale) + clusterYOffset;

        if (count <= 1)
        {
            _slotsByKind[InGameCardKind.Substance].Add(CreateSlot(_slotsArea, "SubSlot_0", new Vector2(0.50f, subTopY), sharedSize, InGameCardKind.Substance, 0, "Substance 1"));
            return;
        }

        if (count == 2)
        {
            _slotsByKind[InGameCardKind.Substance].Add(CreateSlot(_slotsArea, "SubSlot_0", new Vector2(0.33f, subTopY), sharedSize, InGameCardKind.Substance, 0, "Substance 1"));
            _slotsByKind[InGameCardKind.Substance].Add(CreateSlot(_slotsArea, "SubSlot_1", new Vector2(0.67f, subTopY), sharedSize, InGameCardKind.Substance, 1, "Substance 2"));
            return;
        }

        if (count == 3)
        {
            _slotsByKind[InGameCardKind.Substance].Add(CreateSlot(_slotsArea, "SubSlot_0", new Vector2(0.33f, subTopY), sharedSize, InGameCardKind.Substance, 0, "Substance 1"));
            _slotsByKind[InGameCardKind.Substance].Add(CreateSlot(_slotsArea, "SubSlot_1", new Vector2(0.67f, subTopY), sharedSize, InGameCardKind.Substance, 1, "Substance 2"));
            _slotsByKind[InGameCardKind.Substance].Add(CreateSlot(_slotsArea, "SubSlot_2", new Vector2(0.50f, subBottomY), sharedSize, InGameCardKind.Substance, 2, "Substance 3"));
            return;
        }

        int columns = Mathf.Clamp(Mathf.Min(3, count), 1, 3);
        float startY = Mathf.Lerp(0.52f, 0.56f, slotScale) + clusterYOffset;
        float rowStep = Mathf.Lerp(0.16f, 0.18f, slotScale);
        Vector2 size = new Vector2(236f, 198f) * slotScale;
        size.x = Mathf.Clamp(size.x, 114f, Mathf.Max(114f, areaWidth * 0.28f));
        size.y = Mathf.Clamp(size.y, 94f, Mathf.Max(94f, areaHeight * 0.18f));
        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int col = i % columns;
            float x = columns == 1 ? 0.5f : Mathf.Lerp(0.20f, 0.80f, col / (float)(columns - 1));
            float y = startY - (row * rowStep);
            _slotsByKind[InGameCardKind.Substance].Add(CreateSlot(_slotsArea, "SubSlot_" + i, new Vector2(x, y), size, InGameCardKind.Substance, i, "Substance " + (i + 1)));
        }
    }

    private void BuildHseSlots(int count)
    {
        float slotScale = GetSlotsScale();
        const float hseYOffset = -0.08f;
        float areaWidth = _slotsArea != null ? Mathf.Max(1f, _slotsArea.rect.width) : 1080f;
        float areaHeight = _slotsArea != null ? Mathf.Max(1f, _slotsArea.rect.height) : 1280f;
        Vector2 size = new Vector2(224f, 188f) * slotScale;
        size.x = Mathf.Clamp(size.x, 92f, Mathf.Max(92f, areaWidth * 0.18f));
        size.y = Mathf.Clamp(size.y, 84f, Mathf.Max(84f, areaHeight * 0.16f));
        float hseY = Mathf.Lerp(0.24f, 0.32f, slotScale) + hseYOffset;

        if (count == 1)
        {
            _slotsByKind[InGameCardKind.Hse].Add(CreateSlot(_slotsArea, "HseSlot_0", new Vector2(0.76f, hseY), size, InGameCardKind.Hse, 0, "HSE 1"));
            return;
        }

        if (count == 2)
        {
            _slotsByKind[InGameCardKind.Hse].Add(CreateSlot(_slotsArea, "HseSlot_0", new Vector2(0.24f, hseY), size, InGameCardKind.Hse, 0, "HSE 1"));
            _slotsByKind[InGameCardKind.Hse].Add(CreateSlot(_slotsArea, "HseSlot_1", new Vector2(0.76f, hseY), size, InGameCardKind.Hse, 1, "HSE 2"));
            return;
        }

        for (int i = 0; i < count; i++)
        {
            int row = i / 2;
            int col = i % 2;
            float x = col == 0 ? 0.24f : 0.76f;
            float y = (Mathf.Lerp(0.33f, 0.41f, slotScale) + hseYOffset) - (row * Mathf.Lerp(0.12f, 0.15f, slotScale));
            _slotsByKind[InGameCardKind.Hse].Add(CreateSlot(_slotsArea, "HseSlot_" + i, new Vector2(x, y), size, InGameCardKind.Hse, i, "HSE " + (i + 1)));
        }
    }

    private void PrepareStep(int index)
    {
        _stepIndex = Mathf.Clamp(index, 0, _level.steps.Length - 1);
        _step = _level.steps[_stepIndex];
        _phase = InGamePhase.SubstanceSelection;
        _assignments.Clear();
        RebuildSlotsForStep();
        BuildTrayForCurrentPhase();
        UpdatePhaseUi();
    }

    private void ShowIntroForCurrentStep()
    {
        string levelTitle = string.IsNullOrWhiteSpace(_level.title) ? "Niveau " + _level.levelIndex : _level.title;
        _introTitle.text = levelTitle;

        StringBuilder body = new StringBuilder();
        string introBrief = FirstNonEmpty(_step.introBrief, _step.stepBrief, _level.levelBrief, _level.objective);
        string introObjective = FirstNonEmpty(_step.introObjective, _level.objective);
        string introHseFocus = BuildIntroHseFocusText();

        body.AppendLine("<b><size=34>Mission</size></b>");
        body.AppendLine(EscapeRichText(string.IsNullOrWhiteSpace(introBrief) ? "Analyse la situation et prépare un montage cohérent." : introBrief));
        body.AppendLine();

        body.AppendLine("<b><size=32>Objectif</size></b>");
        body.AppendLine(EscapeRichText(string.IsNullOrWhiteSpace(introObjective) ? "À définir." : introObjective));

        if (!string.IsNullOrWhiteSpace(introHseFocus))
        {
            body.AppendLine();
            body.AppendLine("<b><size=32>Focus HSE</size></b>");
            body.AppendLine(EscapeRichText(introHseFocus));
        }

        body.AppendLine();
        body.AppendLine("<i>Valide le briefing puis lance la phase de jeu.</i>");

        _introBody.text = body.ToString().Trim();
        _introPanel.gameObject.SetActive(true);
        _gamePanel.gameObject.SetActive(false);
        _resultPanel.gameObject.SetActive(false);
    }

    private void StartGameplayForStep()
    {
        _introPanel.gameObject.SetActive(false);
        _gamePanel.gameObject.SetActive(true);
        if (!_timerRunning)
        {
            _timerRunning = true;
            _timerStart = Time.unscaledTime;
        }
        UpdatePhaseUi();
    }

    private void UpdatePhaseUi()
    {
        _stepTitleText.text = string.IsNullOrWhiteSpace(_step.stepTitle) ? ("Étape " + (_stepIndex + 1)) : _step.stepTitle;
        Text validateLabel = _validateButton != null ? _validateButton.GetComponentInChildren<Text>() : null;
        switch (_phase)
        {
            case InGamePhase.SubstanceSelection:
                _phaseInstructionText.text = GetPhaseInstruction(InGamePhase.SubstanceSelection);
                if (validateLabel != null) validateLabel.text = GetValidateLabel(InGamePhase.SubstanceSelection);
                ActivateSlots(InGameCardKind.Substance);
                break;
            case InGamePhase.MethodSelection:
                _phaseInstructionText.text = GetPhaseInstruction(InGamePhase.MethodSelection);
                if (validateLabel != null) validateLabel.text = GetValidateLabel(InGamePhase.MethodSelection);
                ActivateSlots(InGameCardKind.Method);
                break;
            case InGamePhase.HseSelection:
                _phaseInstructionText.text = GetPhaseInstruction(InGamePhase.HseSelection);
                if (validateLabel != null) validateLabel.text = GetValidateLabel(InGamePhase.HseSelection);
                ActivateSlots(InGameCardKind.Hse);
                break;
        }
        BuildTrayForCurrentPhase();
        SetFeedback(string.Empty, false);
    }

    private void ActivateSlots(InGameCardKind activeKind)
    {
        foreach (KeyValuePair<InGameCardKind, List<InGameDropSlot>> kv in _slotsByKind)
            foreach (InGameDropSlot slot in kv.Value)
                slot.SetPhaseActive(kv.Key == activeKind);
    }

    private void BuildTrayForCurrentPhase()
    {
        _currentTrayCards.RemoveAll(card => card == null || card.RectTransform == null);
        for (int i = _currentTrayCards.Count - 1; i >= 0; i--)
        {
            InGameDraggableCard card = _currentTrayCards[i];
            if (card == null || card.RectTransform == null)
            {
                _currentTrayCards.RemoveAt(i);
                continue;
            }

            if (card.RectTransform.parent == _trayContent)
            {
                Destroy(card.gameObject);
                _currentTrayCards.RemoveAt(i);
            }
        }
        _phaseCards.Clear();
        BuildPhaseCardDescriptors(_phaseCards);
        for (int i = 0; i < _phaseCards.Count; i++)
            CreateTrayCard(_phaseCards[i]);
        LayoutTrayCardsSpaceEvenly();
        if (_trayScroll != null) _trayScroll.horizontalNormalizedPosition = 0f;
    }

    private void BuildPhaseCardDescriptors(List<InGameCardDescriptor> output)
    {
        if (output == null) return;
        output.Clear();

        if (_phase == InGamePhase.SubstanceSelection)
        {
            HashSet<string> traps = new HashSet<string>(GetTrapIds(InGameCardKind.Substance));
            foreach (string id in GetDisplayIds(InGameCardKind.Substance))
            {
                InGameSubstanceData data = LoadJson<InGameSubstanceData>("Substances", id);
                if (data == null) continue;
                output.Add(new InGameCardDescriptor
                {
                    id = data.id,
                    title = data.displayName,
                    subtitle = data.formula,
                    kind = InGameCardKind.Substance,
                    sprite = ResolveSubstanceSprite(data),
                    isTrap = traps.Contains(data.id)
                });
            }
        }
        else if (_phase == InGamePhase.MethodSelection)
        {
            HashSet<string> traps = new HashSet<string>(GetTrapIds(InGameCardKind.Method));
            foreach (string id in GetDisplayIds(InGameCardKind.Method))
            {
                InGameMethodData data = LoadJson<InGameMethodData>("Methods", id);
                if (data == null) continue;
                output.Add(new InGameCardDescriptor
                {
                    id = data.id,
                    title = data.title,
                    subtitle = data.subtitle,
                    kind = InGameCardKind.Method,
                    sprite = ResolveRectSprite(data.atlasTextureResource, data.atlasX, data.atlasY, data.atlasWidth, data.atlasHeight, data.imageResource, "Methods"),
                    isTrap = traps.Contains(data.id)
                });
            }
        }
        else if (_phase == InGamePhase.HseSelection)
        {
            HashSet<string> traps = new HashSet<string>(GetTrapIds(InGameCardKind.Hse));
            foreach (string id in GetDisplayIds(InGameCardKind.Hse))
            {
                InGameHseData data = LoadJson<InGameHseData>("HSEs", id);
                if (data == null) continue;
                output.Add(new InGameCardDescriptor
                {
                    id = data.id,
                    title = data.title,
                    subtitle = data.subtitle,
                    kind = InGameCardKind.Hse,
                    sprite = ResolveRectSprite(data.atlasTextureResource, data.atlasX, data.atlasY, data.atlasWidth, data.atlasHeight, data.imageResource, "HSEs"),
                    isTrap = traps.Contains(data.id)
                });
            }
        }
    }

    private void CreateTrayCard(InGameCardDescriptor descriptor)
    {
        RectTransform cardRt = EnsureRect(_trayContent, "Card_" + descriptor.id + "_" + descriptor.kind);
        cardRt.sizeDelta = new Vector2(210f, 232f);
        LayoutElement le = cardRt.GetComponent<LayoutElement>();
        if (le == null) le = cardRt.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 210f;
        le.preferredHeight = 232f;
        Image bg = EnsureImage(cardRt);
        bg.color = new Color(0.10f, 0.20f, 0.34f, 0.40f);
        RectTransform imageRt = EnsureRect(cardRt, "Image");
        imageRt.anchorMin = new Vector2(0.08f, 0.34f);
        imageRt.anchorMax = new Vector2(0.92f, 0.90f);
        Image icon = EnsureImage(imageRt);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        if (descriptor.sprite != null) { icon.sprite = descriptor.sprite; icon.color = Color.white; } else { icon.sprite = null; icon.color = new Color(1f, 1f, 1f, 0f); }
        RectTransform labelBandRt = EnsureRect(cardRt, "LabelBand");
        labelBandRt.anchorMin = new Vector2(0.06f, 0.03f);
        labelBandRt.anchorMax = new Vector2(0.94f, 0.32f);
        labelBandRt.offsetMin = Vector2.zero;
        labelBandRt.offsetMax = Vector2.zero;
        EnsureImage(labelBandRt).color = new Color(0.03f, 0.08f, 0.16f, 0.62f);
        RectTransform titleRt = EnsureRect(cardRt, "Title");
        titleRt.anchorMin = new Vector2(0.08f, 0.16f);
        titleRt.anchorMax = new Vector2(0.92f, 0.26f);
        Text title = EnsureText(titleRt, "Text", 20, FontStyle.Bold, descriptor.title);
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        Outline titleOutline = title.GetComponent<Outline>();
        if (titleOutline == null) titleOutline = title.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0f, 0f, 0.72f);
        titleOutline.effectDistance = new Vector2(1f, -1f);
        Stretch(title.rectTransform, 0f, 0f, 0f, 0f);
        RectTransform subRt = EnsureRect(cardRt, "Subtitle");
        subRt.anchorMin = new Vector2(0.08f, 0.06f);
        subRt.anchorMax = new Vector2(0.92f, 0.14f);
        Text subtitle = EnsureText(subRt, "Text", 15, FontStyle.Normal, descriptor.subtitle);
        subtitle.alignment = TextAnchor.MiddleCenter;
        subtitle.color = new Color(0.90f, 0.95f, 1f, 1f);
        Outline subtitleOutline = subtitle.GetComponent<Outline>();
        if (subtitleOutline == null) subtitleOutline = subtitle.gameObject.AddComponent<Outline>();
        subtitleOutline.effectColor = new Color(0f, 0f, 0f, 0.70f);
        subtitleOutline.effectDistance = new Vector2(1f, -1f);
        Stretch(subtitle.rectTransform, 0f, 0f, 0f, 0f);
        CanvasGroup cg = cardRt.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardRt.gameObject.AddComponent<CanvasGroup>();
        InGameDraggableCard draggable = cardRt.GetComponent<InGameDraggableCard>();
        if (draggable == null) draggable = cardRt.gameObject.AddComponent<InGameDraggableCard>();
        draggable.Configure(this, descriptor.id, descriptor.kind, descriptor.isTrap, cg, cardRt);
        draggable.CaptureHome(_trayContent);
        _currentTrayCards.Add(draggable);
    }

    public bool CanBeginDrag(InGameDraggableCard card)
    {
        if (card == null || _phase == InGamePhase.Completed) return false;
        if (_introPanel != null && _introPanel.gameObject.activeSelf) return false;
        if (_resultPanel != null && _resultPanel.gameObject.activeSelf) return false;
        return card.Kind == GetPhaseKind(_phase);
    }

    public void BeginDrag(InGameDraggableCard card, PointerEventData eventData)
    {
        card.DropAccepted = false;
        card.CG.blocksRaycasts = false;
        card.CG.alpha = 0.88f;
        card.transform.SetParent(_dragLayer, true);
        card.transform.SetAsLastSibling();
        card.RectTransform.localScale = Vector3.one * 1.06f;
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragLayer, eventData.position, eventData.pressEventCamera, out localPoint))
            _dragPointerOffset = card.RectTransform.anchoredPosition - localPoint;
        else
            _dragPointerOffset = Vector2.zero;
        SetSlotHighlights(card.Kind, true);
        Drag(card, eventData);
    }

    public void Drag(InGameDraggableCard card, PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragLayer, eventData.position, eventData.pressEventCamera, out localPoint))
            card.RectTransform.anchoredPosition = localPoint + _dragPointerOffset;
    }

    public void EndDrag(InGameDraggableCard card)
    {
        card.CG.blocksRaycasts = true;
        card.CG.alpha = 1f;
        card.RectTransform.localScale = Vector3.one;
        SetSlotHighlights(card.Kind, false);
        if (!card.DropAccepted) ReturnCardToHome(card);
    }

    public void HandleSlotDrop(InGameDropSlot slot, InGameDraggableCard card)
    {
        InGameCardKind expectedKind = GetPhaseKind(_phase);
        if (slot.Kind != expectedKind || card.Kind != expectedKind)
        {
            slot.FlashReject();
            SetFeedback("Mauvais type de carte pour cette phase.", true);
            card.DropAccepted = false;
            return;
        }

        RemoveCardFromSlots(card);
        InGameDraggableCard previous;
        if (_assignments.TryGetValue(slot, out previous) && previous != null && previous != card)
            ReturnCardToHome(previous);
        _assignments[slot] = card;
        card.DropAccepted = true;
        card.SnapToSlot(slot.RectTransform);
        slot.SetFilled(true);
        SetFeedback(string.Empty, false);
        LayoutTrayCardsSpaceEvenly();
    }

    private void RemoveCardFromSlots(InGameDraggableCard card)
    {
        List<InGameDropSlot> keys = new List<InGameDropSlot>(_assignments.Keys);
        foreach (InGameDropSlot key in keys)
        {
            if (_assignments[key] != card) continue;
            _assignments.Remove(key);
            key.SetFilled(false);
        }
    }

    private void ReturnCardToHome(InGameDraggableCard card)
    {
        RemoveCardFromSlots(card);
        card.ReturnHome();
        LayoutTrayCardsSpaceEvenly();
    }

    private void LayoutTrayCardsSpaceEvenly()
    {
        if (_trayContent == null || _trayViewport == null) return;

        List<InGameDraggableCard> visible = new List<InGameDraggableCard>();
        for (int i = 0; i < _currentTrayCards.Count; i++)
        {
            InGameDraggableCard card = _currentTrayCards[i];
            if (card == null || card.RectTransform == null) continue;
            if (card.RectTransform.parent != _trayContent) continue;
            visible.Add(card);
        }

        if (visible.Count == 0)
        {
            _trayContent.sizeDelta = new Vector2(Mathf.Max(1f, _trayViewport.rect.width), 0f);
            return;
        }

        float viewportWidth = Mathf.Max(1f, _trayViewport.rect.width);
        float viewportHeight = Mathf.Max(1f, _trayViewport.rect.height);
        float cardHeight = Mathf.Clamp(viewportHeight * 0.78f, 180f, 240f);
        float cardWidth = Mathf.Clamp(cardHeight * 0.86f, 160f, 208f);
        float spacing = (viewportWidth - (visible.Count * cardWidth)) / (visible.Count + 1f);
        float minSpacing = 18f;
        bool overflow = spacing < minSpacing;
        if (overflow) spacing = minSpacing;

        float contentWidth = spacing + (visible.Count * (cardWidth + spacing));
        float startX = spacing;
        if (!overflow && contentWidth < viewportWidth)
            startX += (viewportWidth - contentWidth) * 0.5f;

        _trayContent.sizeDelta = new Vector2(Mathf.Max(contentWidth, viewportWidth), 0f);
        float x = startX;
        for (int i = 0; i < visible.Count; i++)
        {
            RectTransform rt = visible[i].RectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(cardWidth, cardHeight);
            LayoutElement le = rt.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = cardWidth;
                le.preferredHeight = cardHeight;
            }
            x += cardWidth + spacing;
        }
    }

    private void SetSlotHighlights(InGameCardKind kind, bool highlighted)
    {
        foreach (KeyValuePair<InGameCardKind, List<InGameDropSlot>> kv in _slotsByKind)
            foreach (InGameDropSlot slot in kv.Value)
                if (slot != null) slot.SetHighlighted(kv.Key == kind && highlighted && slot.PhaseActive);
    }

    private void ValidateCurrentPhase()
    {
        if (_phase == InGamePhase.SubstanceSelection)
        {
            _errorCount += EvaluateSelectionMistakes(InGameCardKind.Substance, GetExpectedIds(InGameCardKind.Substance), GetTrapIds(InGameCardKind.Substance));
            LockCardsOfKind(InGameCardKind.Substance);
            _phase = InGamePhase.MethodSelection;
            UpdatePhaseUi();
            return;
        }
        if (_phase == InGamePhase.MethodSelection)
        {
            _errorCount += EvaluateSelectionMistakes(InGameCardKind.Method, GetExpectedIds(InGameCardKind.Method), GetTrapIds(InGameCardKind.Method));
            LockCardsOfKind(InGameCardKind.Method);
            _phase = InGamePhase.HseSelection;
            UpdatePhaseUi();
            return;
        }
        if (_phase == InGamePhase.HseSelection)
        {
            _errorCount += EvaluateSelectionMistakes(InGameCardKind.Hse, GetExpectedIds(InGameCardKind.Hse), GetTrapIds(InGameCardKind.Hse));
            LockCardsOfKind(InGameCardKind.Hse);
            CompleteStep();
        }
    }

    private bool AreAllSlotsFilled(InGameCardKind kind)
    {
        List<InGameDropSlot> slots = _slotsByKind.ContainsKey(kind) ? _slotsByKind[kind] : null;
        if (slots == null || slots.Count == 0) return true;
        foreach (InGameDropSlot slot in slots)
        {
            if (slot == null) continue;
            InGameDraggableCard card;
            if (!_assignments.TryGetValue(slot, out card) || card == null) return false;
        }
        return true;
    }

    private int EvaluateSelectionMistakes(InGameCardKind kind, string[] requiredIds, string[] trapIds)
    {
        List<string> placed = GetPlacedIds(kind);
        HashSet<string> placedSet = new HashSet<string>(placed);
        HashSet<string> required = new HashSet<string>(requiredIds ?? Array.Empty<string>());
        HashSet<string> traps = new HashSet<string>(trapIds ?? Array.Empty<string>());
        Dictionary<string, string> trapMessages = BuildTrapMessageLookup(kind);

        int errors = 0;
        foreach (string id in placed)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (traps.Contains(id))
            {
                errors++;
                string custom = trapMessages.ContainsKey(id) ? trapMessages[id] : string.Empty;
                string prefix = "[Pi\u00E8ge - " + GetKindLabel(kind) + "] " + GetCardLabel(kind, id) + " : ";
                _mistakeLines.Add(prefix + (string.IsNullOrWhiteSpace(custom) ? "ce choix est inadapt\u00E9 pour cette \u00E9tape." : custom));
                continue;
            }

            if (!required.Contains(id))
            {
                errors++;
                _mistakeLines.Add("[Carte en trop - " + GetKindLabel(kind) + "] " + GetCardLabel(kind, id));
            }
        }

        foreach (string requiredId in required)
        {
            if (string.IsNullOrWhiteSpace(requiredId))
                continue;
            if (placedSet.Contains(requiredId))
                continue;
            errors++;
            _mistakeLines.Add("[Carte manquante - " + GetKindLabel(kind) + "] " + GetCardLabel(kind, requiredId));
        }

        return Mathf.Max(0, errors);
    }

    private void LockCardsOfKind(InGameCardKind kind)
    {
        foreach (KeyValuePair<InGameDropSlot, InGameDraggableCard> kv in _assignments)
        {
            if (kv.Key == null || kv.Value == null || kv.Key.Kind != kind) continue;
            kv.Value.SetLocked(true);
            kv.Key.SetLocked(true);
        }
    }

    private List<string> GetPlacedIds(InGameCardKind kind)
    {
        List<string> ids = new List<string>();
        List<InGameDropSlot> slots = _slotsByKind.ContainsKey(kind) ? _slotsByKind[kind] : null;
        if (slots == null) return ids;
        foreach (InGameDropSlot slot in slots)
        {
            InGameDraggableCard card;
            if (_assignments.TryGetValue(slot, out card) && card != null) ids.Add(card.CardId);
        }
        return ids;
    }

    private void CompleteStep()
    {
        _phase = InGamePhase.Completed;
        if (_stepIndex < _level.steps.Length - 1)
        {
            PrepareStep(_stepIndex + 1);
            ShowIntroForCurrentStep();
            return;
        }
        FinishLevel();
    }

    private void FinishLevel()
    {
        _timerRunning = false;
        int stars = ComputeStars(_elapsedSeconds, _errorCount);
        PlayerProfileStore.RecordLevelResult(Mathf.Max(1, _level.levelIndex), _level.id, stars, _elapsedSeconds, _errorCount);

        _gamePanel.gameObject.SetActive(false);
        _resultPanel.gameObject.SetActive(true);
        string levelLabel = string.IsNullOrWhiteSpace(_level.title) ? ("Niveau " + Mathf.Max(1, _level.levelIndex)) : _level.title.Trim();
        _resultTitle.text = "Résultat - " + levelLabel;
        RenderStarCluster(_resultStarsRoot, _starSprite, stars, 62f);
        _resultBody.text = "Temps : <b>" + BuildTimerText(_elapsedSeconds) + "</b>   /   Erreurs : <b>" + _errorCount + "</b>\nCible : <b>" + (_level.targetProductName ?? "Produit") + "</b>";

        if (_resultMistakesTitle != null)
            _resultMistakesTitle.text = "Résumé des erreurs (" + _mistakeLines.Count + ")";
        if (_resultMistakesBody != null)
        {
            _resultMistakesBody.text = BuildMistakeSummaryText();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_resultMistakesBody.rectTransform);
            float preferred = Mathf.Max(120f, _resultMistakesBody.preferredHeight + 16f);
            RectTransform textRt = _resultMistakesBody.rectTransform;
            textRt.sizeDelta = new Vector2(textRt.sizeDelta.x, preferred);
            RectTransform contentRt = textRt.parent as RectTransform;
            if (contentRt != null)
                contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, preferred);
        }
        if (_resultMistakesScroll != null)
            _resultMistakesScroll.verticalNormalizedPosition = 1f;
    }

    private static int ComputeStars(float elapsedSeconds, int errors)
    {
        int stars = 0;
        if (elapsedSeconds < 60f)
            stars += 1;
        if (errors <= 0)
            stars += 2;
        return Mathf.Clamp(stars, 0, 3);
    }

    private string BuildMistakeSummaryText()
    {
        if (_mistakeLines == null || _mistakeLines.Count == 0)
            return "<b><size=30>Parfait ! Zéro erreur.</size></b>\nMontage validé du premier coup.";

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < _mistakeLines.Count; i++)
        {
            string line = _mistakeLines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;
            sb.Append(i + 1).Append(". ").AppendLine(line.Trim());
        }
        string value = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(value) ? "Aucune erreur d\u00E9tect\u00E9e." : value;
    }

    private static void RenderStarCluster(RectTransform root, Sprite starSprite, int stars, float size)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
        if (starSprite == null || stars <= 0) return;

        stars = Mathf.Clamp(stars, 0, 3);
        List<Vector2> positions = new List<Vector2>();
        if (stars == 1)
        {
            positions.Add(new Vector2(0f, size * 0.15f));
        }
        else if (stars == 2)
        {
            positions.Add(new Vector2(-size * 0.55f, size * 0.08f));
            positions.Add(new Vector2(size * 0.55f, size * 0.08f));
        }
        else
        {
            positions.Add(new Vector2(0f, size * 0.62f));
            positions.Add(new Vector2(-size * 0.62f, -size * 0.2f));
            positions.Add(new Vector2(size * 0.62f, -size * 0.2f));
        }

        for (int i = 0; i < positions.Count; i++)
        {
            GameObject go = new GameObject("Star_" + i, typeof(RectTransform));
            go.transform.SetParent(root, false);
            RectTransform starRt = go.GetComponent<RectTransform>();
            starRt.anchorMin = starRt.anchorMax = new Vector2(0.5f, 0.5f);
            starRt.pivot = new Vector2(0.5f, 0.5f);
            starRt.anchoredPosition = positions[i];
            starRt.sizeDelta = new Vector2(size, size);
            Image image = go.AddComponent<Image>();
            image.sprite = starSprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }
    }

    private void SetFeedback(string message, bool isError)
    {
        if (_phaseFeedbackText == null) return;
        _phaseFeedbackText.text = message;
        _phaseFeedbackText.color = isError ? new Color(1f, 0.5f, 0.5f, 1f) : new Color(0.82f, 0.95f, 0.82f, 1f);
    }

    private string[] GetDisplayIds(InGameCardKind kind)
    {
        switch (kind)
        {
            case InGameCardKind.Substance:
                return FirstNonEmptyArray(_step.displaySubstanceIds, _step.substanceIds, _level.substanceIds);
            case InGameCardKind.Method:
                return FirstNonEmptyArray(_step.displayMethodIds, _step.methodIds, _level.methodIds);
            case InGameCardKind.Hse:
                return FirstNonEmptyArray(_step.displayHseIds, _step.hseIds, _level.hseIds);
            default:
                return Array.Empty<string>();
        }
    }

    private string[] GetExpectedIds(InGameCardKind kind)
    {
        switch (kind)
        {
            case InGameCardKind.Substance:
                return FirstNonEmptyArray(_step.expectedSubstanceIds, _step.correctSubstanceIds, _step.displaySubstanceIds, _step.substanceIds, _level.substanceIds);
            case InGameCardKind.Method:
                return FirstNonEmptyArray(_step.expectedMethodIds, _step.correctMethodIds, _step.displayMethodIds, _step.methodIds, _level.methodIds);
            case InGameCardKind.Hse:
                return FirstNonEmptyArray(_step.expectedHseIds, _step.correctHseIds, _step.displayHseIds, _step.hseIds, _level.hseIds);
            default:
                return Array.Empty<string>();
        }
    }

    private string[] GetTrapIds(InGameCardKind kind)
    {
        switch (kind)
        {
            case InGameCardKind.Substance:
                return FirstNonEmptyArray(_step.trapSubstanceIds);
            case InGameCardKind.Method:
                return FirstNonEmptyArray(_step.trapMethodIds);
            case InGameCardKind.Hse:
                return FirstNonEmptyArray(_step.trapHseIds);
            default:
                return Array.Empty<string>();
        }
    }

    private string BuildIntroHseFocusText()
    {
        string focus = FirstNonEmpty(_step.introHseFocus, _level.hseFocus);
        if (string.IsNullOrWhiteSpace(focus))
            return string.Empty;

        if (!ContainsExpectedHseHint(focus))
            return focus;

        return "Analyse les dangers de l'étape et choisis les pictogrammes HSE les plus pertinents, sans te laisser piéger.";
    }

    private bool ContainsExpectedHseHint(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string[] expectedHse = GetExpectedIds(InGameCardKind.Hse);
        if (expectedHse == null || expectedHse.Length == 0)
            return false;

        for (int i = 0; i < expectedHse.Length; i++)
        {
            string id = expectedHse[i];
            if (string.IsNullOrWhiteSpace(id))
                continue;

            string normalizedId = id.Trim();
            if (ContainsIgnoreCase(text, normalizedId))
                return true;

            InGameHseData data = LoadJson<InGameHseData>("HSEs", normalizedId);
            if (data == null)
                continue;

            if (ContainsIgnoreCase(text, data.title) || ContainsIgnoreCase(text, data.subtitle))
                return true;
        }

        return false;
    }

    private Dictionary<string, string> BuildTrapMessageLookup(InGameCardKind kind)
    {
        InGameTrapErrorData[] entries = kind == InGameCardKind.Substance ? _step.trapSubstanceErrors :
            (kind == InGameCardKind.Method ? _step.trapMethodErrors : _step.trapHseErrors);

        Dictionary<string, string> lookup = new Dictionary<string, string>();
        if (entries == null)
            return lookup;

        foreach (InGameTrapErrorData entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.cardId))
                continue;

            string cardId = entry.cardId.Trim();
            if (lookup.ContainsKey(cardId))
                continue;

            lookup[cardId] = string.IsNullOrWhiteSpace(entry.message) ? string.Empty : entry.message.Trim();
        }

        return lookup;
    }

    private string GetKindLabel(InGameCardKind kind)
    {
        switch (kind)
        {
            case InGameCardKind.Substance: return "Substance";
            case InGameCardKind.Method: return "M\u00E9thode";
            case InGameCardKind.Hse: return "HSE";
            default: return "Carte";
        }
    }

    private string GetCardLabel(InGameCardKind kind, string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            return "Carte inconnue";

        string id = cardId.Trim();
        if (kind == InGameCardKind.Substance)
        {
            InGameSubstanceData data = LoadJson<InGameSubstanceData>("Substances", id);
            if (data != null && !string.IsNullOrWhiteSpace(data.displayName))
                return data.displayName.Trim();
        }
        else if (kind == InGameCardKind.Method)
        {
            InGameMethodData data = LoadJson<InGameMethodData>("Methods", id);
            if (data != null && !string.IsNullOrWhiteSpace(data.title))
                return data.title.Trim();
        }
        else
        {
            InGameHseData data = LoadJson<InGameHseData>("HSEs", id);
            if (data != null && !string.IsNullOrWhiteSpace(data.title))
                return data.title.Trim();
        }

        return id;
    }

    private int GetSlotCount(InGameCardKind kind)
    {
        int configured = kind == InGameCardKind.Substance ? _step.substanceSlotCount :
            (kind == InGameCardKind.Method ? _step.methodSlotCount : _step.hseSlotCount);
        if (configured > 0) return configured;

        string[] expected = GetExpectedIds(kind);
        if (expected.Length > 0) return expected.Length;

        int levelDefault = kind == InGameCardKind.Substance ? _level.defaultSubstanceSlotCount :
            (kind == InGameCardKind.Method ? _level.defaultMethodSlotCount : _level.defaultHseSlotCount);
        if (levelDefault > 0) return levelDefault;

        return kind == InGameCardKind.Substance ? 3 : (kind == InGameCardKind.Method ? 1 : 2);
    }

    private string GetPhaseInstruction(InGamePhase phase)
    {
        if (phase == InGamePhase.SubstanceSelection)
            return FirstNonEmpty(_step.substancePhaseInstruction, "Phase 1: place " + GetSlotCount(InGameCardKind.Substance) + " substance(s)");
        if (phase == InGamePhase.MethodSelection)
            return FirstNonEmpty(_step.methodPhaseInstruction, "Phase 2: place " + GetSlotCount(InGameCardKind.Method) + " méthode(s)");
        if (phase == InGamePhase.HseSelection)
            return FirstNonEmpty(_step.hsePhaseInstruction, "Phase 3: place " + GetSlotCount(InGameCardKind.Hse) + " carte(s) HSE");
        return string.Empty;
    }

    private string GetValidateLabel(InGamePhase phase)
    {
        return "Valider";
    }

    private static string[] FirstNonEmptyArray(params string[][] candidates)
    {
        if (candidates == null) return Array.Empty<string>();
        foreach (string[] candidate in candidates)
        {
            if (candidate == null || candidate.Length == 0) continue;
            string[] cleaned = candidate.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct().ToArray();
            if (cleaned.Length > 0) return cleaned;
        }
        return Array.Empty<string>();
    }

    private static string FirstNonEmpty(params string[] candidates)
    {
        if (candidates == null) return string.Empty;
        foreach (string candidate in candidates)
            if (!string.IsNullOrWhiteSpace(candidate)) return candidate.Trim();
        return string.Empty;
    }

    private static string EscapeRichText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static bool ContainsIgnoreCase(string text, string pattern)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(pattern))
            return false;
        return text.IndexOf(pattern.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static InGameCardKind GetPhaseKind(InGamePhase phase)
    {
        switch (phase)
        {
            case InGamePhase.MethodSelection: return InGameCardKind.Method;
            case InGamePhase.HseSelection: return InGameCardKind.Hse;
            default: return InGameCardKind.Substance;
        }
    }

    private T LoadJson<T>(string folder, string id) where T : class
    {
        if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(id)) return null;
        TextAsset asset = Resources.Load<TextAsset>(folder + "/" + id.Trim());
        if (asset == null) return null;
        try { return JsonUtility.FromJson<T>(asset.text); } catch { return null; }
    }

    private class InGameCardDescriptor
    {
        public string id;
        public string title;
        public string subtitle;
        public Sprite sprite;
        public InGameCardKind kind;
        public bool isTrap;
    }

    private static Sprite ResolveSubstanceSprite(InGameSubstanceData data)
    {
        if (data == null) return null;
        if (data.atlasCellIndex > 0)
        {
            string atlas = string.IsNullOrWhiteSpace(data.atlasTextureResource) ? "Substances/substances-1" : data.atlasTextureResource;
            int cols = data.atlasColumns > 0 ? data.atlasColumns : 4;
            int w = data.atlasCellWidth > 0 ? data.atlasCellWidth : 78;
            int h = data.atlasCellHeight > 0 ? data.atlasCellHeight : 114;
            Texture2D texture = LoadTextureSmart(atlas, "Substances");
            if (texture != null)
            {
                int slot = data.atlasCellIndex - 1;
                int col = slot % cols;
                int row = slot / cols;
                Rect rect = new Rect(col * w, texture.height - ((row + 1) * h), w, h);
                return CreateSpriteCached(atlas + "|" + data.atlasCellIndex, texture, rect);
            }
        }
        Sprite sprite = LoadSpriteSmart(data.cardImageResource);
        if (sprite == null) sprite = LoadSpriteSmart(data.moleculeImageResource);
        return sprite;
    }

    private static Sprite ResolveRectSprite(string atlasTextureResource, int atlasX, int atlasY, int atlasWidth, int atlasHeight, string fallbackImage, string fallbackFolder)
    {
        if (!string.IsNullOrWhiteSpace(atlasTextureResource) && atlasWidth > 0 && atlasHeight > 0)
        {
            Texture2D texture = LoadTextureSmart(atlasTextureResource, fallbackFolder);
            if (texture != null)
            {
                int x = Mathf.Clamp(atlasX, 0, Mathf.Max(0, texture.width - 1));
                int yBottom = Mathf.Clamp(texture.height - (atlasY + atlasHeight), 0, Mathf.Max(0, texture.height - 1));
                int width = Mathf.Min(atlasWidth, texture.width - x);
                int height = Mathf.Min(atlasHeight, texture.height - yBottom);
                if (width > 0 && height > 0)
                    return CreateSpriteCached(atlasTextureResource + "|" + atlasX + "|" + atlasY + "|" + atlasWidth + "|" + atlasHeight, texture, new Rect(x, yBottom, width, height));
            }
        }
        return LoadSpriteSmart(fallbackImage);
    }

    private static Sprite CreateSpriteCached(string key, Texture2D texture, Rect rect)
    {
        Sprite cached;
        if (SpriteCache.TryGetValue(key, out cached) && cached != null) return cached;
        Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
        if (sprite != null) SpriteCache[key] = sprite;
        return sprite;
    }

    private static Texture2D LoadTextureSmart(string resourcePath, string fallbackFolder)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return null;
        foreach (string c in BuildResourceCandidates(resourcePath, fallbackFolder))
        {
            if (string.IsNullOrWhiteSpace(c)) continue;
            Texture2D tex = Resources.Load<Texture2D>(c);
            if (tex != null) return tex;
            Sprite s = Resources.Load<Sprite>(c);
            if (s != null && s.texture != null) return s.texture;
            Sprite[] all = Resources.LoadAll<Sprite>(c);
            if (all != null) foreach (Sprite sp in all) if (sp != null && sp.texture != null) return sp.texture;
        }
        return null;
    }

    private static Sprite LoadSpriteSmart(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return null;
        foreach (string c in BuildResourceCandidates(resourcePath, string.Empty))
        {
            if (string.IsNullOrWhiteSpace(c)) continue;
            Sprite s = Resources.Load<Sprite>(c);
            if (s != null) return s;
            Sprite[] all = Resources.LoadAll<Sprite>(c);
            if (all != null && all.Length > 0) return all[0];
        }
        return null;
    }

    private static IEnumerable<string> BuildResourceCandidates(string resourcePath, string fallbackFolder)
    {
        string path = resourcePath.Trim().Replace("\\", "/");
        int dot = path.LastIndexOf('.');
        string noExt = dot > 0 ? path.Substring(0, dot) : path;
        if (path.Contains("/")) return new[] { path, noExt };
        List<string> list = new List<string> { path, noExt };
        if (!string.IsNullOrWhiteSpace(fallbackFolder))
        {
            list.Add(fallbackFolder + "/" + path);
            list.Add(fallbackFolder + "/" + noExt);
        }
        list.Add("Substances/" + path); list.Add("Substances/" + noExt);
        list.Add("Methods/" + path); list.Add("Methods/" + noExt);
        list.Add("HSEs/" + path); list.Add("HSEs/" + noExt);
        list.Add("Backgrounds/" + path); list.Add("Backgrounds/" + noExt);
        list.Add("Icons/" + path); list.Add("Icons/" + noExt);
        return list.Distinct();
    }

    private System.Collections.IEnumerator RefreshResponsiveLayoutNextFrame()
    {
        yield return null;
        if (_slotsArea == null || _level == null || _level.steps == null || _level.steps.Length == 0)
            yield break;

        Vector2 size = _slotsArea.rect.size;
        if (size.x <= 1f || size.y <= 1f)
            yield break;

        _lastSlotsAreaSize = size;
        RebuildSlotsForStep();
        UpdatePhaseUi();
    }

    private static string BuildTimerText(float seconds)
    {
        TimeSpan span = TimeSpan.FromSeconds(seconds);
        return span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00");
    }

    private static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        RectTransform rt = t as RectTransform;
        return rt != null ? rt : t.gameObject.AddComponent<RectTransform>();
    }

    private static Image EnsureImage(RectTransform rt)
    {
        Image image = rt.GetComponent<Image>();
        if (image == null)
        {
            if (rt.GetComponent<CanvasRenderer>() == null) rt.gameObject.AddComponent<CanvasRenderer>();
            image = rt.gameObject.AddComponent<Image>();
        }
        return image;
    }

    private static Button EnsureButton(RectTransform rt)
    {
        Button button = rt.GetComponent<Button>();
        if (button == null) button = rt.gameObject.AddComponent<Button>();
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
        return button;
    }

    private static Text EnsureText(Transform parent, string name, int size, FontStyle style, string value)
    {
        RectTransform rt = EnsureRect(parent, name);
        if (rt.GetComponent<CanvasRenderer>() == null) rt.gameObject.AddComponent<CanvasRenderer>();
        Text txt = rt.GetComponent<Text>();
        if (txt == null) txt = rt.gameObject.AddComponent<Text>();
        txt.font = UiFontProvider.GetDefaultFont();
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.text = value;
        txt.color = Color.white;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private void ShowFatalOverlay(Exception exception)
    {
        if (_root == null)
        {
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            _root = gameObject.GetComponent<RectTransform>();
            if (_root == null) _root = gameObject.AddComponent<RectTransform>();
            Stretch(_root, 0f, 0f, 0f, 0f);
        }

        RectTransform overlay = EnsureRect(_root, "FatalOverlay");
        Stretch(overlay, 0f, 0f, 0f, 0f);
        Image bg = EnsureImage(overlay);
        bg.color = new Color(0.03f, 0.07f, 0.13f, 0.98f);

        Text text = EnsureText(
            overlay,
            "FatalText",
            24,
            FontStyle.Bold,
            "Erreur runtime InGame.\nConsulte Logs/safechem-runtime.log\n\n" + exception.GetType().Name + ": " + exception.Message);
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(text.rectTransform, 60f, 60f, 60f, 60f);
    }
}

