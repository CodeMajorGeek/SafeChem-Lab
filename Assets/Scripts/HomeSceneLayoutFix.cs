using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.IO;

[Serializable]
public class LevelDefinitionData
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
    public string[] substanceIds;
    public string[] methodIds;
    public string[] hseIds;
}

public class HomeSceneLayoutFix : MonoBehaviour
{
    [SerializeField] private bool fixOnStart = true;
    [SerializeField] private float headerHeight = 210f;
    [SerializeField] private float footerHeight = 184f;
    [SerializeField] private float settingsWidthRatio = 0.5f;
    [SerializeField] private float settingsAnimDuration = 0.25f;
    [SerializeField] private bool showSplash = true;
    [SerializeField] private float splashFadeInDuration = 0.4f;
    [SerializeField] private float splashHoldDuration = 2.2f;
    [SerializeField] private float splashFadeOutDuration = 0.4f;
    [SerializeField] private int levelCount = 5;
    [SerializeField] private string inGameSceneName = "InGame";
    [SerializeField] private string rightSubPageIcon1 = "icon_chemicals.png";
    [SerializeField] private string rightSubPageIcon2 = "icon_methods.png";
    [SerializeField] private string rightSubPageIcon3 = "icon_HSE.png";

    private const string SelectedLevelKey = "SelectedLevel";
    private const string SelectedLevelIdKey = "SelectedLevelId";
    private const string SelectedLevelTitleKey = "SelectedLevelTitle";
    private const string SelectedLevelSummaryKey = "SelectedLevelSummary";
    private const string SelectedLevelObjectiveKey = "SelectedLevelObjective";
    private const string SelectedLevelHseFocusKey = "SelectedLevelHseFocus";
    private const string SelectedLevelSubstancesKey = "SelectedLevelSubstances";
    private const string SelectedLevelMethodsKey = "SelectedLevelMethods";
    private const string SelectedLevelHseCardsKey = "SelectedLevelHseCards";
    private const string SelectedLevelTrapSubstanceKey = "SelectedLevelTrapSubstance";
    private const string SelectedLevelTargetProductKey = "SelectedLevelTargetProduct";
    private const string SelectedLevelTargetProductNameKey = "SelectedLevelTargetProductName";
    private const string LevelsResourcesFolder = "Levels";

    private RectTransform _canvasRt;
    private ScrollRect _scroll;
    private RectTransform _chrome;
    private RectTransform _progressBody;
    private InputField _pseudoInput;
    private RectTransform _settings;
    private RectTransform _settingsOverlay;
    private RectTransform _resetConfirmOverlay;
    private RectTransform _levelDrawerOverlay;
    private RectTransform _levelDrawer;
    private Text _levelDrawerTitle;
    private Text _levelDrawerSummary;
    private Button _levelDrawerPlay;
    private Text _uuidValueText;
    private Text _resetConfirmBody;
    private HomePagerSnap _pagerSnap;
    private Coroutine _levelDrawerAnim;
    private bool _levelDrawerVisible;
    private readonly List<LevelDefinitionData> _levels = new List<LevelDefinitionData>();
    private bool _settingsOpen;
    private Coroutine _settingsAnim;
    private Rect _lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
    private Vector2 _lastCanvasSize = new Vector2(-1f, -1f);

    private void Start()
    {
        if (!fixOnStart) return;
        MobileScreenUtility.ForcePortraitOrientation();
        RuntimeFileLogger.Log("HomeSceneLayoutFix", "Start fixOnStart=true headerHeight=" + headerHeight + " footerHeight=" + footerHeight);
        _scroll = FindFirstObjectByType<ScrollRect>();
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (_scroll == null || canvas == null)
        {
            RuntimeFileLogger.Error("HomeSceneLayoutFix", "Missing ScrollRect or Canvas");
            return;
        }
        _canvasRt = canvas.GetComponent<RectTransform>();
        if (_canvasRt == null)
        {
            RuntimeFileLogger.Error("HomeSceneLayoutFix", "Canvas RectTransform missing");
            return;
        }

        PlayerProfileStore.EnsureInitialized();

        _chrome = _canvasRt;
        NormalizeFixedBackground();
        NormalizePagerSurfaces();
        DisableDuplicatePageChrome();
        NormalizeSharedSceneChrome();
        NormalizeBodiesForOverlay();
        BuildProgression();
        BuildCollectionSubPager();
        FixTextVisibility();
        HookPagerEvents();

        if (showSplash) StartCoroutine(ShowSplash());
    }

    private void LateUpdate()
    {
        if (!fixOnStart || _canvasRt == null)
            return;

        if (!HasResponsiveMetricsChanged())
            return;

        ApplyResponsiveLayout();
    }

    private void OnDisable()
    {
        if (_pagerSnap != null)
            _pagerSnap.OnPageChanged -= OnMainPageChanged;
    }

    private void HookPagerEvents()
    {
        if (_pagerSnap == null)
            _pagerSnap = FindFirstObjectByType<HomePagerSnap>();
        if (_pagerSnap == null)
            return;

        _pagerSnap.OnPageChanged -= OnMainPageChanged;
        _pagerSnap.OnPageChanged += OnMainPageChanged;
    }

    private void OnMainPageChanged(int page)
    {
        if (page != 1)
            SetLevelDrawerVisible(false);
    }

    private void NormalizeSharedSceneChrome()
    {
        RectTransform sharedHeader = _canvasRt.Find("Header") as RectTransform;
        RectTransform sharedFooter = _canvasRt.Find("Footer") as RectTransform;
        if (sharedHeader != null)
            NormalizeHeader(sharedHeader);
        if (sharedFooter != null)
            NormalizeFooter(sharedFooter);
        ConfigureViewportForSharedChrome();
    }

    private void ConfigureViewportForSharedChrome()
    {
        if (_scroll == null) return;
        SafeAreaInsets safeInsets = MobileScreenUtility.GetSafeAreaInsets(_canvasRt);

        Image pagerImage = _scroll.GetComponent<Image>();
        if (pagerImage != null)
        {
            pagerImage.color = new Color(1f, 1f, 1f, 0f);
            pagerImage.raycastTarget = false;
        }

        RectTransform pagerRt = _scroll.GetComponent<RectTransform>();
        if (pagerRt != null)
        {
            pagerRt.anchorMin = Vector2.zero;
            pagerRt.anchorMax = Vector2.one;
            pagerRt.offsetMin = new Vector2(safeInsets.left, footerHeight + safeInsets.bottom);
            pagerRt.offsetMax = new Vector2(-safeInsets.right, -(headerHeight + safeInsets.top));
            RuntimeFileLogger.Log("HomeSceneLayoutFix", "Pager offsets min=" + pagerRt.offsetMin + " max=" + pagerRt.offsetMax);
        }

        RectTransform viewport = _scroll.viewport;
        if (viewport != null)
        {
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
        }

        if (_scroll.horizontalScrollbar != null)
        {
            _scroll.horizontalScrollbar.gameObject.SetActive(false);
            _scroll.horizontalScrollbar = null;
        }
        if (_scroll.verticalScrollbar != null)
        {
            _scroll.verticalScrollbar.gameObject.SetActive(false);
            _scroll.verticalScrollbar = null;
        }

        HomePagerPageSizer sizer = FindFirstObjectByType<HomePagerPageSizer>();
        if (sizer != null)
            sizer.Apply();

        if (_scroll.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);

        Canvas.ForceUpdateCanvases();

        if (_pagerSnap != null)
            _pagerSnap.JumpToPage(_pagerSnap.CurrentPage);
    }

    private void NormalizeHeader(RectTransform header)
    {
        SafeAreaInsets safeInsets = MobileScreenUtility.GetSafeAreaInsets(_canvasRt);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, headerHeight + safeInsets.top);
        RuntimeFileLogger.Log("HomeSceneLayoutFix", "NormalizeHeader height=" + headerHeight);

        Image bg = EnsureImage(header);
        bg.color = new Color(0.05f, 0.09f, 0.16f, 1f);

        Image tex = EnsureImage(EnsureRect(header, "HeaderTexture"));
        RectTransform texRt = tex.rectTransform;
        texRt.anchorMin = Vector2.zero;
        texRt.anchorMax = Vector2.one;
        texRt.offsetMin = Vector2.zero;
        texRt.offsetMax = Vector2.zero;
        tex.sprite = LoadSprite("Backgrounds/bg-menus-4");
        tex.color = new Color(0.12f, 0.18f, 0.28f, 0.18f);
        tex.raycastTarget = false;
        texRt.SetAsFirstSibling();

        Sprite logo = LoadSprite("Icons/logos-trans-header");
        if (logo == null) logo = LoadSprite("Icons/logos-trans");
        if (logo != null)
        {
            RectTransform logoRt = EnsureRect(header, "Logo");
            logoRt.anchorMin = logoRt.anchorMax = new Vector2(0.5f, 1f);
            logoRt.pivot = new Vector2(0.5f, 1f);
            logoRt.anchoredPosition = new Vector2(0f, -(15f + safeInsets.top));
            float targetHeight = Mathf.Min(headerHeight - 18f, _canvasRt.rect.height * 0.16f);
            float aspect = logo.rect.height > 0f ? logo.rect.width / logo.rect.height : 3f;
            logoRt.sizeDelta = new Vector2(Mathf.Min(_canvasRt.rect.width * 0.68f, targetHeight * aspect), targetHeight);
            Image logoImage = EnsureImage(logoRt);
            logoImage.sprite = logo;
            logoImage.color = Color.white;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            logoRt.SetAsLastSibling();
        }

        RectTransform settingsRt = EnsureRect(header, "BtnSettings");
        settingsRt.anchorMin = settingsRt.anchorMax = new Vector2(0f, 1f);
        settingsRt.pivot = new Vector2(0f, 1f);
        settingsRt.anchoredPosition = new Vector2(24f + safeInsets.left, -(20f + safeInsets.top));
        settingsRt.sizeDelta = new Vector2(184f, 184f);
        Image settingsImage = EnsureImage(settingsRt);
        settingsImage.sprite = LoadSprite("Icons/param");
        settingsImage.color = Color.white;
        settingsImage.preserveAspect = true;
        Button settingsButton = EnsureButton(settingsRt);
        settingsButton.targetGraphic = settingsImage;
        settingsButton.onClick.RemoveAllListeners();
        settingsButton.onClick.AddListener(ToggleSettings);
        settingsRt.SetAsLastSibling();

        Transform title = header.Find("Title");
        if (title != null) title.gameObject.SetActive(false);

        BuildSettingsPanel();
    }

    private void NormalizeFooter(RectTransform footer)
    {
        SafeAreaInsets safeInsets = MobileScreenUtility.GetSafeAreaInsets(_canvasRt);
        footer.anchorMin = new Vector2(0f, 0f);
        footer.anchorMax = new Vector2(1f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.anchoredPosition = Vector2.zero;
        footer.sizeDelta = new Vector2(0f, footerHeight + safeInsets.bottom);

        Image bg = EnsureImage(footer);
        bg.color = new Color(0.03f, 0.06f, 0.1f, 0.98f);

        RectTransform row = footer.Find("FooterRow") as RectTransform;
        if (row == null) return;

        row.anchorMin = Vector2.zero;
        row.anchorMax = Vector2.one;
        row.offsetMin = new Vector2(24f + safeInsets.left, 16f + safeInsets.bottom);
        row.offsetMax = new Vector2(-(24f + safeInsets.right), -16f);
        row.localRotation = Quaternion.identity;
        row.localScale = Vector3.one;

        VerticalLayoutGroup vertical = row.GetComponent<VerticalLayoutGroup>();
        if (vertical != null) vertical.enabled = false;

        HorizontalLayoutGroup horizontal = row.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null) horizontal.enabled = false;

        GridLayoutGroup grid = row.GetComponent<GridLayoutGroup>();
        if (grid != null) grid.enabled = false;

        ContentSizeFitter fitter = row.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;

        for (int i = 0; i < row.childCount; i++)
        {
            RectTransform buttonRt = row.GetChild(i) as RectTransform;
            if (buttonRt == null) continue;
            EnsureImage(buttonRt);
            EnsureButton(buttonRt);
            LayoutElement le = buttonRt.GetComponent<LayoutElement>();
            if (le == null) le = buttonRt.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 140f;
            le.preferredWidth = 280f;
            le.minHeight = 78f;
            le.preferredHeight = 110f;
            le.flexibleWidth = 1f;
            le.flexibleHeight = 1f;
            buttonRt.localScale = Vector3.one;
            buttonRt.localRotation = Quaternion.identity;

            float xAnchor = i == 0 ? (1f / 6f) : (i == 1 ? 0.5f : 5f / 6f);
            float squareSize = Mathf.Max(96f, Mathf.Min(row.rect.height - 12f, (row.rect.width / 3f) - 28f));
            buttonRt.anchorMin = new Vector2(xAnchor, 0.5f);
            buttonRt.anchorMax = new Vector2(xAnchor, 0.5f);
            buttonRt.pivot = new Vector2(0.5f, 0.5f);
            buttonRt.anchoredPosition = Vector2.zero;
            buttonRt.sizeDelta = new Vector2(squareSize, squareSize);
        }

        if (footer.GetComponent<HomePagerFooterNav>() == null)
            footer.gameObject.AddComponent<HomePagerFooterNav>();

        footer.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        LayoutRebuilder.ForceRebuildLayoutImmediate(footer);
    }

    private void BuildSettingsPanel()
    {
        if (_settings != null) return;

        _settingsOverlay = EnsureRect(_chrome, "SettingsOverlay");
        _settingsOverlay.anchorMin = Vector2.zero;
        _settingsOverlay.anchorMax = Vector2.one;
        _settingsOverlay.offsetMin = Vector2.zero;
        _settingsOverlay.offsetMax = Vector2.zero;
        _settingsOverlay.SetAsLastSibling();
        Image overlayImage = EnsureImage(_settingsOverlay);
        overlayImage.color = new Color(0f, 0f, 0f, 0f);
        Button overlayButton = EnsureButton(_settingsOverlay);
        overlayButton.targetGraphic = overlayImage;
        overlayButton.onClick.RemoveAllListeners();
        overlayButton.onClick.AddListener(CloseSettings);
        _settingsOverlay.gameObject.SetActive(false);

        _settings = EnsureRect(_chrome, "SettingsSideMenu");
        _settings.SetParent(_settingsOverlay, false);
        _settings.anchorMin = new Vector2(0f, 0f);
        _settings.anchorMax = new Vector2(0f, 1f);
        _settings.pivot = new Vector2(0f, 0.5f);
        _settings.sizeDelta = new Vector2(GetSettingsWidth(), 0f);
        _settings.anchoredPosition = new Vector2(-GetSettingsWidth(), 0f);
        Image bg = EnsureImage(_settings);
        bg.color = new Color(0.04f, 0.07f, 0.12f, 0.98f);
        ClearChildren(_settings);

        VerticalLayoutGroup v = _settings.GetComponent<VerticalLayoutGroup>();
        if (v == null) v = _settings.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(20, 20, 88, 94);
        v.spacing = 14;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = false;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        RectTransform titleRt = EnsureRect(_settings, "SettingsTitle");
        LayoutElement titleLe = titleRt.GetComponent<LayoutElement>();
        if (titleLe == null) titleLe = titleRt.gameObject.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 42f;
        Text title = EnsureLabel(titleRt, "Paramètres", 28, FontStyle.Bold);
        title.alignment = TextAnchor.MiddleLeft;
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        title.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform titleTextRt = title.rectTransform;
        titleTextRt.anchorMin = Vector2.zero;
        titleTextRt.anchorMax = Vector2.one;
        titleTextRt.offsetMin = Vector2.zero;
        titleTextRt.offsetMax = Vector2.zero;

        RectTransform pseudoLabelRt = EnsureRect(_settings, "PseudoLabel");
        LayoutElement pseudoLe = pseudoLabelRt.GetComponent<LayoutElement>();
        if (pseudoLe == null) pseudoLe = pseudoLabelRt.gameObject.AddComponent<LayoutElement>();
        pseudoLe.preferredHeight = 28f;
        Text pseudoLabel = EnsureLabel(pseudoLabelRt, "Pseudo :", 22, FontStyle.Bold);
        pseudoLabel.alignment = TextAnchor.MiddleLeft;
        pseudoLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        pseudoLabel.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform pseudoTextRt = pseudoLabel.rectTransform;
        pseudoTextRt.anchorMin = Vector2.zero;
        pseudoTextRt.anchorMax = Vector2.one;
        pseudoTextRt.offsetMin = Vector2.zero;
        pseudoTextRt.offsetMax = Vector2.zero;

        RectTransform inputRt = EnsureRect(_settings, "PseudoInput");
        LayoutElement le = inputRt.GetComponent<LayoutElement>();
        if (le == null) le = inputRt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 58f;
        Image ibg = EnsureImage(inputRt);
        ibg.color = new Color(0.12f, 0.16f, 0.22f, 1f);
        _pseudoInput = inputRt.GetComponent<InputField>();
        if (_pseudoInput == null) _pseudoInput = inputRt.gameObject.AddComponent<InputField>();

        Text tx = EnsureInputText(inputRt, "Text", Color.white, FontStyle.Normal, "");
        Text ph = EnsureInputText(inputRt, "Placeholder", new Color(1f, 1f, 1f, 0.45f), FontStyle.Italic, "Entrer votre pseudo");
        _pseudoInput.textComponent = tx;
        _pseudoInput.placeholder = ph;
        _pseudoInput.onEndEdit.RemoveAllListeners();
        _pseudoInput.onEndEdit.AddListener(SavePseudo);

        RectTransform uuidRt = EnsureRect(_settings, "UuidLabel");
        LayoutElement uuidLe = uuidRt.GetComponent<LayoutElement>();
        if (uuidLe == null) uuidLe = uuidRt.gameObject.AddComponent<LayoutElement>();
        uuidLe.preferredHeight = 24f;
        _uuidValueText = EnsureLabel(uuidRt, string.Empty, 16, FontStyle.Normal);
        _uuidValueText.alignment = TextAnchor.MiddleLeft;
        _uuidValueText.color = new Color(0.78f, 0.86f, 0.96f, 0.9f);
        RectTransform uuidTextRt = _uuidValueText.rectTransform;
        uuidTextRt.anchorMin = Vector2.zero;
        uuidTextRt.anchorMax = Vector2.one;
        uuidTextRt.offsetMin = Vector2.zero;
        uuidTextRt.offsetMax = Vector2.zero;

        Button save = EnsureMenuButton(_settings, "BtnSave", "Enregistrer", new Color(0.16f, 0.36f, 0.64f, 1f));
        LayoutElement saveLe = save.GetComponent<LayoutElement>();
        if (saveLe != null) saveLe.preferredHeight = 56f;
        Text saveText = save.GetComponentInChildren<Text>();
        if (saveText != null) saveText.fontSize = 20;
        save.onClick.RemoveAllListeners();
        save.onClick.AddListener(() => SavePseudo(_pseudoInput != null ? _pseudoInput.text : string.Empty));

        RectTransform resetAnchor = EnsureRect(_settings, "ResetAnchor");
        LayoutElement resetAnchorLe = resetAnchor.GetComponent<LayoutElement>();
        if (resetAnchorLe == null) resetAnchorLe = resetAnchor.gameObject.AddComponent<LayoutElement>();
        resetAnchorLe.ignoreLayout = true;
        resetAnchor.anchorMin = new Vector2(0f, 0f);
        resetAnchor.anchorMax = new Vector2(0f, 0f);
        resetAnchor.pivot = new Vector2(0f, 0f);
        resetAnchor.anchoredPosition = new Vector2(20f, 24f);
        resetAnchor.sizeDelta = new Vector2(260f, 56f);

        Button resetButton = EnsureMenuButton(resetAnchor, "BtnResetProgress", "Reset progression", new Color(0.74f, 0.18f, 0.18f, 1f));
        RectTransform resetBtnRt = resetButton.GetComponent<RectTransform>();
        resetBtnRt.anchorMin = Vector2.zero;
        resetBtnRt.anchorMax = Vector2.one;
        resetBtnRt.offsetMin = Vector2.zero;
        resetBtnRt.offsetMax = Vector2.zero;
        Text resetTxt = resetButton.GetComponentInChildren<Text>();
        if (resetTxt != null) resetTxt.fontSize = 18;
        resetButton.onClick.RemoveAllListeners();
        resetButton.onClick.AddListener(OpenResetConfirmation);

        BuildResetConfirmModal();
        RefreshProfileUi();
    }

    private void BuildResetConfirmModal()
    {
        if (_settingsOverlay == null || _resetConfirmOverlay != null)
            return;

        _resetConfirmOverlay = EnsureRect(_settingsOverlay, "ResetConfirmOverlay");
        _resetConfirmOverlay.anchorMin = Vector2.zero;
        _resetConfirmOverlay.anchorMax = Vector2.one;
        _resetConfirmOverlay.offsetMin = Vector2.zero;
        _resetConfirmOverlay.offsetMax = Vector2.zero;
        _resetConfirmOverlay.SetAsLastSibling();

        Image overlayBg = EnsureImage(_resetConfirmOverlay);
        overlayBg.color = new Color(0f, 0f, 0f, 0.62f);
        Button overlayBtn = EnsureButton(_resetConfirmOverlay);
        overlayBtn.targetGraphic = overlayBg;
        overlayBtn.onClick.RemoveAllListeners();
        overlayBtn.onClick.AddListener(CloseResetConfirmation);

        RectTransform card = EnsureRect(_resetConfirmOverlay, "ConfirmCard");
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;
        card.sizeDelta = new Vector2(760f, 360f);
        Image cardBg = EnsureImage(card);
        cardBg.color = new Color(0.08f, 0.12f, 0.2f, 0.98f);

        Button cardBlock = EnsureButton(card);
        cardBlock.targetGraphic = cardBg;
        cardBlock.onClick.RemoveAllListeners();

        Text confirmTitle = EnsureNamedLabel(card, "ConfirmTitle", "Réinitialiser la progression ?", 34, FontStyle.Bold);
        confirmTitle.alignment = TextAnchor.UpperCenter;
        RectTransform confirmTitleRt = confirmTitle.rectTransform;
        confirmTitleRt.anchorMin = new Vector2(0f, 1f);
        confirmTitleRt.anchorMax = new Vector2(1f, 1f);
        confirmTitleRt.pivot = new Vector2(0.5f, 1f);
        confirmTitleRt.anchoredPosition = new Vector2(0f, -20f);
        confirmTitleRt.sizeDelta = new Vector2(-44f, 48f);

        _resetConfirmBody = EnsureNamedLabel(card, "ConfirmBody", string.Empty, 22, FontStyle.Normal);
        _resetConfirmBody.alignment = TextAnchor.UpperCenter;
        _resetConfirmBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        _resetConfirmBody.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform bodyRt = _resetConfirmBody.rectTransform;
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(28f, 108f);
        bodyRt.offsetMax = new Vector2(-28f, -84f);

        RectTransform buttonsRt = EnsureRect(card, "Buttons");
        buttonsRt.anchorMin = new Vector2(0.5f, 0f);
        buttonsRt.anchorMax = new Vector2(0.5f, 0f);
        buttonsRt.pivot = new Vector2(0.5f, 0f);
        buttonsRt.anchoredPosition = new Vector2(0f, 20f);
        buttonsRt.sizeDelta = new Vector2(620f, 62f);
        HorizontalLayoutGroup h = buttonsRt.GetComponent<HorizontalLayoutGroup>();
        if (h == null) h = buttonsRt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 18f;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;

        Button cancelBtn = EnsureMenuButton(buttonsRt, "BtnCancelReset", "Annuler", new Color(0.17f, 0.28f, 0.43f, 1f));
        cancelBtn.onClick.RemoveAllListeners();
        cancelBtn.onClick.AddListener(CloseResetConfirmation);

        Button confirmBtn = EnsureMenuButton(buttonsRt, "BtnConfirmReset", "Confirmer", new Color(0.74f, 0.18f, 0.18f, 1f));
        confirmBtn.onClick.RemoveAllListeners();
        confirmBtn.onClick.AddListener(ConfirmResetProgression);

        _resetConfirmOverlay.gameObject.SetActive(false);
    }

    private void OpenResetConfirmation()
    {
        BuildResetConfirmModal();
        if (_resetConfirmOverlay == null)
            return;

        if (_resetConfirmBody != null)
        {
            string pseudo = PlayerProfileStore.GetPseudo();
            _resetConfirmBody.text =
                "Cette action va créer un nouvel identifiant joueur, attribuer un nouveau pseudo aléatoire et remettre toutes les étoiles à zéro.\n\n" +
                "Profil actuel : " + pseudo;
        }

        _resetConfirmOverlay.gameObject.SetActive(true);
        _resetConfirmOverlay.SetAsLastSibling();
    }

    private void CloseResetConfirmation()
    {
        if (_resetConfirmOverlay != null)
            _resetConfirmOverlay.gameObject.SetActive(false);
    }

    private void ConfirmResetProgression()
    {
        PlayerProfileStore.ResetProfileWithNewIdentity();
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrWhiteSpace(sceneName))
            sceneName = "Home";
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void RefreshProfileUi()
    {
        PlayerProfileStore.EnsureInitialized();
        if (_pseudoInput != null)
            _pseudoInput.text = PlayerProfileStore.GetPseudo();
        if (_uuidValueText != null)
        {
            string uuid = PlayerProfileStore.GetPlayerUuid();
            string shortId = string.IsNullOrWhiteSpace(uuid) ? "n/a" : (uuid.Length > 8 ? uuid.Substring(0, 8) : uuid);
            _uuidValueText.text = "ID joueur : " + shortId;
        }
    }

    private void ToggleSettings()
    {
        if (_settings == null) return;
        if (!_settingsOpen)
            RefreshProfileUi();
        SetSettingsOpen(!_settingsOpen);
    }

    private void CloseSettings()
    {
        CloseResetConfirmation();
        if (_settingsOpen)
            SetSettingsOpen(false);
    }

    private void SetSettingsOpen(bool open)
    {
        if (_settings == null || _settingsOverlay == null)
            return;

        _settingsOpen = open;
        float width = GetSettingsWidth();
        _settings.sizeDelta = new Vector2(width, 0f);

        if (_settingsAnim != null)
            StopCoroutine(_settingsAnim);

        if (open)
            _settingsOverlay.gameObject.SetActive(true);

        _settingsOverlay.SetAsLastSibling();
        _settingsAnim = StartCoroutine(AnimateSettings(open, width));
    }

    private IEnumerator AnimateSettings(bool open, float width)
    {
        float startX = _settings.anchoredPosition.x;
        float endX = open ? 0f : -width;

        Image overlayImage = _settingsOverlay != null ? _settingsOverlay.GetComponent<Image>() : null;
        Color startColor = overlayImage != null ? overlayImage.color : new Color(0f, 0f, 0f, 0f);
        Color endColor = new Color(0f, 0f, 0f, open ? 0.35f : 0f);

        float duration = Mathf.Max(0.01f, settingsAnimDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            _settings.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, k), 0f);
            if (overlayImage != null)
                overlayImage.color = Color.Lerp(startColor, endColor, k);
            yield return null;
        }

        _settings.anchoredPosition = new Vector2(endX, 0f);
        if (overlayImage != null)
            overlayImage.color = endColor;

        if (!open && _settingsOverlay != null)
            _settingsOverlay.gameObject.SetActive(false);

        _settingsAnim = null;
    }

    private float GetSettingsWidth()
    {
        float canvasWidth = _canvasRt != null ? _canvasRt.rect.width : 1080f;
        return Mathf.Max(320f, canvasWidth * settingsWidthRatio);
    }

    private static void SavePseudo(string value)
    {
        PlayerProfileStore.SetPseudo(value);
    }

    private void DisableDuplicatePageChrome()
    {
        if (_scroll == null || _scroll.content == null) return;

        for (int i = 0; i < _scroll.content.childCount; i++)
        {
            Transform page = _scroll.content.GetChild(i);
            Transform header = page.Find("Header");
            Transform footer = page.Find("Footer");
            if (header != null)
                header.gameObject.SetActive(false);
            if (footer != null)
                footer.gameObject.SetActive(false);
        }
    }

    private void NormalizePagerSurfaces()
    {
        if (_scroll == null || _scroll.content == null)
            return;

        for (int i = 0; i < _scroll.content.childCount; i++)
        {
            Transform page = _scroll.content.GetChild(i);
            if (page == null)
                continue;

            Image pageImage = page.GetComponent<Image>();
            if (pageImage != null)
            {
                pageImage.sprite = null;
                pageImage.color = new Color(1f, 1f, 1f, 0f);
                pageImage.raycastTarget = false;
            }
        }
    }

    private void NormalizeFixedBackground()
    {
        RectTransform background = _canvasRt.Find("Background") as RectTransform;
        if (background == null)
        {
            background = EnsureRect(_canvasRt, "Background");
            background.gameObject.AddComponent<CanvasRenderer>();
        }

        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;
        background.pivot = new Vector2(0.5f, 0.5f);
        background.anchoredPosition = Vector2.zero;

        Image image = EnsureImage(background);
        image.sprite = LoadSprite("Backgrounds/bg-menus-4");
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;

        background.SetAsFirstSibling();
    }

    private void NormalizeBodiesForOverlay()
    {
        Image[] imgs = FindObjectsByType<Image>(FindObjectsSortMode.None);
        foreach (Image i in imgs)
        {
            if (i == null || i.gameObject == null || i.gameObject.name != "Body")
                continue;

            i.sprite = null;
            i.color = new Color(1f, 1f, 1f, 0f);
            i.type = Image.Type.Simple;
        }
    }

    private void BuildProgression()
    {
        if (_scroll.content == null) return;
        Transform center = _scroll.content.Find("PageCenter");
        if (center == null && _scroll.content.childCount > 1) center = _scroll.content.GetChild(1);
        if (center == null) return;
        _progressBody = center.Find("Body") as RectTransform;
        if (_progressBody == null) return;

        _levels.Clear();
        LoadLevelsFromJson(_levels);
        if (_levels.Count == 0)
        {
            int fallbackCount = Mathf.Max(1, levelCount);
            for (int i = 1; i <= fallbackCount; i++)
            {
                _levels.Add(new LevelDefinitionData
                {
                    id = "level-" + i,
                    levelIndex = i,
                    title = "Niveau " + i,
                    summary = "Objectif de niveau à définir.",
                    objective = "Résoudre le niveau en respectant les contraintes de sécurité.",
                    hseFocus = "Appliquer les protocoles de base.",
                    sceneName = inGameSceneName
                });
            }
        }
        _levels.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));

        Sprite lvl = LoadSprite("Icons/map-level");
        Sprite starSprite = LoadSprite("Icons/star");
        if (lvl == null) return;

        RectTransform map = EnsureRect(_progressBody, "LevelMap");
        map.anchorMin = Vector2.zero;
        map.anchorMax = Vector2.one;
        map.offsetMin = new Vector2(0, 210);
        map.offsetMax = new Vector2(0, -42);
        ClearChildren(map);
        VerticalLayoutGroup v = map.GetComponent<VerticalLayoutGroup>();
        if (v == null) v = map.gameObject.AddComponent<VerticalLayoutGroup>();
        float mapWidth = Mathf.Max(320f, map.rect.width);
        float rowHeight = Mathf.Clamp((_progressBody.rect.height - 240f) / Mathf.Max(1, _levels.Count), 220f, 300f);
        v.spacing = Mathf.Clamp(rowHeight * 0.12f, 16f, 30f);
        v.padding = new RectOffset(0, 0, 44, 18);
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        for (int i = 0; i < _levels.Count; i++)
        {
            LevelDefinitionData level = _levels[i];
            bool comingSoon = IsComingSoonLevel(level.levelIndex);
            bool unlocked = !comingSoon && (i == 0 || GetLevelStars(_levels[i - 1].levelIndex) >= 1);

            RectTransform row = EnsureRect(map, "LevelRow_" + level.levelIndex);
            LayoutElement le = row.GetComponent<LayoutElement>();
            if (le == null) le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight;
            le.minHeight = rowHeight;
            le.flexibleHeight = 0f;

            RectTransform btnRt = EnsureRect(row, "BtnLevel");
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            float buttonWidth = Mathf.Clamp(mapWidth * 0.39f, 248f, 352f);
            float buttonHeight = Mathf.Round(buttonWidth * 0.52f);
            float buttonY = Mathf.Clamp(rowHeight * 0.19f, 32f, 62f);
            btnRt.anchoredPosition = new Vector2(0f, buttonY);
            btnRt.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            Image bi = EnsureImage(btnRt);
            bi.sprite = lvl;
            bi.preserveAspect = true;
            bi.color = unlocked ? Color.white : (comingSoon ? new Color(0.30f, 0.30f, 0.30f, 0.90f) : new Color(0.35f, 0.35f, 0.35f, 0.95f));

            Button b = EnsureButton(btnRt);
            b.targetGraphic = bi;
            b.onClick.RemoveAllListeners();
            b.interactable = unlocked;
            if (unlocked)
            {
                int idx = i;
                b.onClick.AddListener(() => SelectLevel(_levels[idx]));
            }

            string captionValue = comingSoon ? ("Niveau " + level.levelIndex + " - Prochainement") : ("Niveau " + level.levelIndex);
            Text cap = EnsureNamedLabel(row, "LevelCaption", captionValue, 30, FontStyle.Bold);
            cap.alignment = TextAnchor.MiddleCenter;
            cap.fontSize = comingSoon ? 24 : 30;
            cap.color = unlocked ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
            cap.horizontalOverflow = HorizontalWrapMode.Overflow;
            cap.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform crt = cap.rectTransform;
            crt.anchorMin = new Vector2(0.5f, 0f);
            crt.anchorMax = new Vector2(0.5f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0f, 8f);
            crt.sizeDelta = new Vector2(Mathf.Clamp(mapWidth * 0.66f, 320f, 520f), 44f);
            Outline capOutline = cap.GetComponent<Outline>();
            if (capOutline == null) capOutline = cap.gameObject.AddComponent<Outline>();
            capOutline.effectColor = new Color(0f, 0f, 0f, 0.72f);
            capOutline.effectDistance = new Vector2(1f, -1f);

            int stars = comingSoon ? 0 : GetLevelStars(level.levelIndex);
            RectTransform starsRt = EnsureRect(row, "StarsCluster");
            starsRt.anchorMin = new Vector2(0.5f, 0f);
            starsRt.anchorMax = new Vector2(0.5f, 0f);
            starsRt.pivot = new Vector2(0.5f, 0f);
            starsRt.anchoredPosition = new Vector2(0f, buttonY + (buttonHeight * 0.76f) + 18f);
            starsRt.sizeDelta = new Vector2(230f, 108f);
            RenderStarCluster(starsRt, starSprite, stars, 38f);

            string badge = (unlocked || comingSoon) ? string.Empty : "Verrouillé";
            Text lockText = EnsureNamedLabel(btnRt, "LockBadge", badge, 20, FontStyle.Bold);
            lockText.alignment = TextAnchor.MiddleCenter;
            lockText.color = new Color(0.78f, 0.78f, 0.78f, 1f);
            RectTransform lockRt = lockText.rectTransform;
            lockRt.anchorMin = Vector2.zero;
            lockRt.anchorMax = Vector2.one;
            lockRt.offsetMin = Vector2.zero;
            lockRt.offsetMax = Vector2.zero;
            lockText.gameObject.SetActive(!string.IsNullOrWhiteSpace(badge));
        }

        BuildLevelDrawer();
    }

    private void BuildLevelDrawer()
    {
        if (_progressBody == null && _chrome == null)
            return;

        Transform drawerRoot = _chrome != null ? _chrome : _progressBody;
        _levelDrawerOverlay = EnsureRect(drawerRoot, "LevelDrawerOverlay");
        _levelDrawerOverlay.anchorMin = Vector2.zero;
        _levelDrawerOverlay.anchorMax = Vector2.one;
        _levelDrawerOverlay.offsetMin = new Vector2(0f, GetFooterReservedHeight());
        _levelDrawerOverlay.offsetMax = new Vector2(0f, -GetHeaderReservedHeight());
        _levelDrawerOverlay.SetAsLastSibling();
        Image overlayBg = EnsureImage(_levelDrawerOverlay);
        overlayBg.color = new Color(0f, 0f, 0f, 0.25f);
        Button overlayBtn = EnsureButton(_levelDrawerOverlay);
        overlayBtn.targetGraphic = overlayBg;
        overlayBtn.onClick.RemoveAllListeners();
        overlayBtn.onClick.AddListener(() => SetLevelDrawerVisible(false));

        _levelDrawer = EnsureRect(_levelDrawerOverlay, "LevelDrawer");
        _levelDrawer.anchorMin = new Vector2(0.5f, 0f);
        _levelDrawer.anchorMax = new Vector2(0.5f, 0f);
        _levelDrawer.pivot = new Vector2(0.5f, 0f);
        _levelDrawer.anchoredPosition = new Vector2(0f, GetLevelDrawerHiddenY());
        _levelDrawer.sizeDelta = new Vector2(920f, 220f);
        Image drawerBg = EnsureImage(_levelDrawer);
        drawerBg.color = new Color(0.08f, 0.14f, 0.22f, 0.95f);
        _levelDrawer.SetAsLastSibling();
        ClearChildren(_levelDrawer);

        _levelDrawerTitle = EnsureNamedLabel(_levelDrawer, "DrawerTitle", "Sélectionner un niveau", 28, FontStyle.Bold);
        _levelDrawerTitle.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRt = _levelDrawerTitle.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -14f);
        titleRt.sizeDelta = new Vector2(-40f, 40f);
        titleRt.offsetMin = new Vector2(20f, 0f);
        titleRt.offsetMax = new Vector2(-20f, 0f);

        _levelDrawerSummary = EnsureNamedLabel(_levelDrawer, "DrawerSummary", "Clique sur un niveau débloqué pour afficher son briefing.", 20, FontStyle.Normal);
        _levelDrawerSummary.alignment = TextAnchor.UpperCenter;
        _levelDrawerSummary.horizontalOverflow = HorizontalWrapMode.Wrap;
        _levelDrawerSummary.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform summaryRt = _levelDrawerSummary.rectTransform;
        summaryRt.anchorMin = new Vector2(0f, 0f);
        summaryRt.anchorMax = new Vector2(1f, 1f);
        summaryRt.offsetMin = new Vector2(24f, 88f);
        summaryRt.offsetMax = new Vector2(-24f, -62f);

        _levelDrawerPlay = EnsureMenuButton(_levelDrawer, "BtnPlayLevel", "Jouer", new Color(0.1f, 0.42f, 0.76f, 1));
        RectTransform playRt = _levelDrawerPlay.GetComponent<RectTransform>();
        playRt.anchorMin = new Vector2(0.5f, 0f);
        playRt.anchorMax = new Vector2(0.5f, 0f);
        playRt.pivot = new Vector2(0.5f, 0f);
        playRt.anchoredPosition = new Vector2(0f, 16f);
        playRt.sizeDelta = new Vector2(260f, 72f);
        _levelDrawerPlay.onClick.RemoveAllListeners();
        _levelDrawerPlay.gameObject.SetActive(false);

        _levelDrawerVisible = false;
        _levelDrawerOverlay.gameObject.SetActive(false);
    }

    private void SelectLevel(LevelDefinitionData level)
    {
        if (level == null)
            return;
        if (IsComingSoonLevel(level.levelIndex))
            return;

        PlayerPrefs.SetInt(SelectedLevelKey, level.levelIndex);
        PlayerPrefs.SetString(SelectedLevelIdKey, string.IsNullOrWhiteSpace(level.id) ? ("level-" + level.levelIndex) : level.id);
        PlayerPrefs.SetString(SelectedLevelTitleKey, string.IsNullOrWhiteSpace(level.title) ? ("Niveau " + level.levelIndex) : level.title);
        PlayerPrefs.SetString(SelectedLevelSummaryKey, level.summary ?? string.Empty);
        PlayerPrefs.SetString(SelectedLevelObjectiveKey, level.objective ?? string.Empty);
        PlayerPrefs.SetString(SelectedLevelHseFocusKey, level.hseFocus ?? string.Empty);
        PlayerPrefs.SetString(SelectedLevelSubstancesKey, JoinValues(level.substanceIds));
        PlayerPrefs.SetString(SelectedLevelMethodsKey, JoinValues(level.methodIds));
        PlayerPrefs.SetString(SelectedLevelHseCardsKey, JoinValues(level.hseIds));
        PlayerPrefs.SetString(SelectedLevelTrapSubstanceKey, level.trapSubstanceId ?? string.Empty);
        PlayerPrefs.SetString(SelectedLevelTargetProductKey, level.targetProductId ?? string.Empty);
        PlayerPrefs.SetString(SelectedLevelTargetProductNameKey, level.targetProductName ?? string.Empty);
        PlayerPrefs.Save();

        if (_levelDrawerTitle != null)
            _levelDrawerTitle.text = string.IsNullOrWhiteSpace(level.title) ? ("Niveau " + level.levelIndex) : level.title;
        if (_levelDrawerSummary != null)
            _levelDrawerSummary.text = string.IsNullOrWhiteSpace(level.levelBrief)
                ? (string.IsNullOrWhiteSpace(level.objective) ? "Objectif non défini." : level.objective.Trim())
                : level.levelBrief.Trim();
        if (_levelDrawerPlay != null)
        {
            _levelDrawerPlay.gameObject.SetActive(true);
            _levelDrawerPlay.onClick.RemoveAllListeners();
            string targetScene = string.IsNullOrWhiteSpace(level.sceneName) ? inGameSceneName : level.sceneName.Trim();
            _levelDrawerPlay.onClick.AddListener(() =>
            {
                if (Application.CanStreamedLevelBeLoaded(targetScene))
                    SceneManager.LoadScene(targetScene);
                else
                    Debug.LogError("Scene missing: " + targetScene);
            });
        }

        SetLevelDrawerVisible(true);
    }

    private static bool IsComingSoonLevel(int levelIndex)
    {
        return levelIndex == 3 || levelIndex == 4 || levelIndex == 5;
    }

    private void SetLevelDrawerVisible(bool visible)
    {
        if (_levelDrawer == null || _levelDrawerOverlay == null)
            return;
        if (visible && !IsCenterPageActive())
            visible = false;
        if (_levelDrawerVisible == visible && _levelDrawerAnim == null)
            return;

        _levelDrawerVisible = visible;
        if (visible)
        {
            _levelDrawerOverlay.gameObject.SetActive(true);
            _levelDrawerOverlay.SetAsLastSibling();
        }
        float targetY = visible ? GetLevelDrawerVisibleY() : GetLevelDrawerHiddenY();
        if (_levelDrawerAnim != null)
            StopCoroutine(_levelDrawerAnim);
        _levelDrawerAnim = StartCoroutine(AnimateLevelDrawer(targetY));
    }

    private IEnumerator AnimateLevelDrawer(float targetY)
    {
        if (_levelDrawer == null)
            yield break;

        float startY = _levelDrawer.anchoredPosition.y;
        float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            _levelDrawer.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, targetY, t));
            yield return null;
        }

        _levelDrawer.anchoredPosition = new Vector2(0f, targetY);
        if (!_levelDrawerVisible && _levelDrawerOverlay != null)
            _levelDrawerOverlay.gameObject.SetActive(false);
        _levelDrawerAnim = null;
    }

    private float GetLevelDrawerVisibleY()
    {
        return 10f;
    }

    private float GetLevelDrawerHiddenY()
    {
        return -(_levelDrawer != null ? _levelDrawer.sizeDelta.y : 220f) - 24f;
    }

    private bool IsCenterPageActive()
    {
        if (_pagerSnap != null)
            return _pagerSnap.CurrentPage == 1;
        if (_scroll == null)
            return true;
        float p = _scroll.horizontalNormalizedPosition;
        return Mathf.Abs(p - 0.5f) <= 0.25f;
    }

    private void ApplyResponsiveLayout()
    {
        NormalizeSharedSceneChrome();
        NormalizeBodiesForOverlay();

        if (_levelDrawerOverlay != null)
        {
            _levelDrawerOverlay.offsetMin = new Vector2(0f, GetFooterReservedHeight());
            _levelDrawerOverlay.offsetMax = new Vector2(0f, -GetHeaderReservedHeight());
        }
    }

    private bool HasResponsiveMetricsChanged()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 canvasSize = _canvasRt.rect.size;
        bool changed = safeArea != _lastSafeArea || canvasSize != _lastCanvasSize;
        if (changed)
        {
            _lastSafeArea = safeArea;
            _lastCanvasSize = canvasSize;
        }
        return changed;
    }

    private float GetHeaderReservedHeight()
    {
        SafeAreaInsets safeInsets = MobileScreenUtility.GetSafeAreaInsets(_canvasRt);
        return headerHeight + safeInsets.top;
    }

    private float GetFooterReservedHeight()
    {
        SafeAreaInsets safeInsets = MobileScreenUtility.GetSafeAreaInsets(_canvasRt);
        return footerHeight + safeInsets.bottom;
    }

    private void LoadLevelsFromJson(List<LevelDefinitionData> output)
    {
        if (output == null)
            return;
        output.Clear();

        TextAsset[] assets = Resources.LoadAll<TextAsset>(LevelsResourcesFolder);
        if (assets == null || assets.Length == 0)
            return;
        Array.Sort(assets, (a, b) => string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));

        for (int i = 0; i < assets.Length; i++)
        {
            TextAsset asset = assets[i];
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                continue;
            if (asset.name.IndexOf("template", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            LevelDefinitionData data = null;
            try
            {
                data = JsonUtility.FromJson<LevelDefinitionData>(asset.text);
            }
            catch (Exception exception)
            {
                RuntimeFileLogger.Error("HomeSceneLayoutFix", "Invalid level JSON " + asset.name + " - " + exception.Message);
            }
            if (data == null)
                continue;

            if (string.IsNullOrWhiteSpace(data.id))
                data.id = asset.name;
            if (data.levelIndex <= 0)
            {
                int parsed = ExtractTrailingInt(asset.name);
                data.levelIndex = parsed > 0 ? parsed : -1;
            }
            if (data.levelIndex <= 0)
                continue;
            if (string.IsNullOrWhiteSpace(data.title))
                data.title = "Niveau " + data.levelIndex;
            if (string.IsNullOrWhiteSpace(data.sceneName))
                data.sceneName = inGameSceneName;
            output.Add(data);
        }
    }

    private static int ExtractTrailingInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return -1;
        int end = value.Length - 1;
        while (end >= 0 && !char.IsDigit(value[end])) end--;
        if (end < 0) return -1;
        int start = end;
        while (start >= 0 && char.IsDigit(value[start])) start--;
        string digits = value.Substring(start + 1, end - start);
        int parsed;
        return int.TryParse(digits, out parsed) ? parsed : -1;
    }

    private static int GetLevelStars(int levelIndex)
    {
        return Mathf.Max(0, PlayerProfileStore.GetLevelStars(levelIndex));
    }

    private static string JoinValues(string[] values)
    {
        if (values == null || values.Length == 0)
            return string.Empty;
        return string.Join(",", values);
    }

    private void BuildCollectionSubPager()
    {
        if (_scroll == null || _scroll.content == null)
            return;

        Transform right = _scroll.content.Find("PageRight");
        if (right == null && _scroll.content.childCount > 2)
            right = _scroll.content.GetChild(2);
        if (right == null)
            return;

        RectTransform rightBody = right.Find("Body") as RectTransform;
        if (rightBody == null)
            return;

        RectTransform root = EnsureRect(rightBody, "CollectionSubPager");
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = new Vector2(0f, 0f);
        root.offsetMax = new Vector2(0f, 0f);
        root.SetAsLastSibling();
        LayoutElement rootLayout = root.GetComponent<LayoutElement>();
        if (rootLayout == null) rootLayout = root.gameObject.AddComponent<LayoutElement>();
        rootLayout.ignoreLayout = true;

        Image rootBg = EnsureImage(root);
        rootBg.color = new Color(1f, 1f, 1f, 0f);
        rootBg.raycastTarget = false;

        RectTransform topNav = EnsureRect(root, "TopNav");
        topNav.anchorMin = new Vector2(0f, 1f);
        topNav.anchorMax = new Vector2(1f, 1f);
        topNav.pivot = new Vector2(0.5f, 1f);
        topNav.anchoredPosition = new Vector2(0f, 8f);
        topNav.sizeDelta = new Vector2(0f, 124f);
        Image navBg = EnsureImage(topNav);
        navBg.color = new Color(0.03f, 0.06f, 0.1f, 0.82f);

        RectTransform navRow = EnsureRect(topNav, "NavRow");
        navRow.anchorMin = Vector2.zero;
        navRow.anchorMax = Vector2.one;
        navRow.offsetMin = new Vector2(18f, 14f);
        navRow.offsetMax = new Vector2(-18f, -14f);
        VerticalLayoutGroup v = navRow.GetComponent<VerticalLayoutGroup>();
        if (v != null) v.enabled = false;
        HorizontalLayoutGroup h = navRow.GetComponent<HorizontalLayoutGroup>();
        if (h != null) h.enabled = false;
        GridLayoutGroup g = navRow.GetComponent<GridLayoutGroup>();
        if (g != null) g.enabled = false;
        ContentSizeFitter c = navRow.GetComponent<ContentSizeFitter>();
        if (c != null) c.enabled = false;
        ClearChildren(navRow);

        RectTransform pagesRoot = EnsureRect(root, "Pages");
        pagesRoot.anchorMin = Vector2.zero;
        pagesRoot.anchorMax = Vector2.one;
        pagesRoot.offsetMin = Vector2.zero;
        pagesRoot.offsetMax = new Vector2(0f, -132f);
        ClearChildren(pagesRoot);

        Button[] buttons = new Button[3];
        RectTransform[] pages = new RectTransform[3];
        Image[] icons = new Image[3];
        string[] labels = { string.Empty, string.Empty, string.Empty };

        for (int i = 0; i < 3; i++)
        {
            int slot = i + 1;
            float xAnchor = i == 0 ? (1f / 6f) : (i == 1 ? 0.5f : 5f / 6f);

            RectTransform btnRt = EnsureRect(navRow, "BtnSub" + slot);
            btnRt.anchorMin = new Vector2(xAnchor, 0.5f);
            btnRt.anchorMax = new Vector2(xAnchor, 0.5f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = Vector2.zero;
            btnRt.sizeDelta = new Vector2(96f, 96f);

            Image btnImage = EnsureImage(btnRt);
            btnImage.color = new Color(0.03f, 0.06f, 0.1f, 0.98f);
            Button btn = EnsureButton(btnRt);
            btn.targetGraphic = btnImage;
            btn.onClick.RemoveAllListeners();
            buttons[i] = btn;

            RectTransform iconRt = EnsureRect(btnRt, "Icon");
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(64f, 64f);
            Image iconImage = EnsureImage(iconRt);
            iconImage.color = new Color(1f, 1f, 1f, 0f);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            icons[i] = iconImage;

            Text caption = EnsureLabel(btnRt, string.Empty, 22, FontStyle.Bold);
            caption.alignment = TextAnchor.LowerCenter;
            RectTransform captionRt = caption.rectTransform;
            captionRt.anchorMin = Vector2.zero;
            captionRt.anchorMax = Vector2.one;
            captionRt.offsetMin = new Vector2(0f, 6f);
            captionRt.offsetMax = new Vector2(0f, 0f);

            RectTransform pageRt = EnsureRect(pagesRoot, "SubPage" + slot);
            pageRt.anchorMin = Vector2.zero;
            pageRt.anchorMax = Vector2.one;
            pageRt.offsetMin = new Vector2(8f, 8f);
            pageRt.offsetMax = new Vector2(-8f, 0f);
            Image pageBg = EnsureImage(pageRt);
            pageBg.color = (i == 0 || i == 1 || i == 2) ? new Color(0.03f, 0.06f, 0.1f, 0.22f) : new Color(0.03f, 0.06f, 0.1f, 0.4f);

            HomeSubstancesPanel substancesPanel = pageRt.GetComponent<HomeSubstancesPanel>();
            HomeMethodsPanel methodsPanel = pageRt.GetComponent<HomeMethodsPanel>();
            if (i == 0)
            {
                if (substancesPanel == null)
                    substancesPanel = pageRt.gameObject.AddComponent<HomeSubstancesPanel>();
                if (methodsPanel != null)
                    Destroy(methodsPanel);
            }
            else if (i == 1)
            {
                if (substancesPanel != null)
                    Destroy(substancesPanel);
                if (methodsPanel == null)
                    methodsPanel = pageRt.gameObject.AddComponent<HomeMethodsPanel>();
                methodsPanel.Configure("Methods", "MethodsModalOverlay");
            }
            else if (i == 2)
            {
                if (substancesPanel != null)
                    Destroy(substancesPanel);
                if (methodsPanel == null)
                    methodsPanel = pageRt.gameObject.AddComponent<HomeMethodsPanel>();
                methodsPanel.Configure("HSEs", "HseModalOverlay");
            }
            else
            {
                if (substancesPanel != null)
                    Destroy(substancesPanel);
                if (methodsPanel != null)
                    Destroy(methodsPanel);

                Text placeholder = EnsureLabel(pageRt, labels[i], 28, FontStyle.Bold);
                placeholder.alignment = TextAnchor.MiddleCenter;
                RectTransform placeholderRt = placeholder.rectTransform;
                placeholderRt.anchorMin = Vector2.zero;
                placeholderRt.anchorMax = Vector2.one;
                placeholderRt.offsetMin = Vector2.zero;
                placeholderRt.offsetMax = Vector2.zero;
            }
            pages[i] = pageRt;
        }

        HomeCollectionSubPagerNav nav = root.GetComponent<HomeCollectionSubPagerNav>();
        if (nav == null)
            nav = root.gameObject.AddComponent<HomeCollectionSubPagerNav>();
        nav.Configure(buttons, pages, icons);
        nav.SetIconResourceNames(rightSubPageIcon1, rightSubPageIcon2, rightSubPageIcon3);

        buttons[0].onClick.AddListener(nav.GoSubPage1);
        buttons[1].onClick.AddListener(nav.GoSubPage2);
        buttons[2].onClick.AddListener(nav.GoSubPage3);
        nav.GoSubPage1();
    }

    private IEnumerator ShowSplash()
    {
        Sprite s = LoadSprite("Backgrounds/bg-loading"); if (s == null) yield break;

        RectTransform sharedHeader = _canvasRt != null ? (_canvasRt.Find("Header") as RectTransform) : null;
        RectTransform sharedFooter = _canvasRt != null ? (_canvasRt.Find("Footer") as RectTransform) : null;
        bool headerWasActive = sharedHeader != null && sharedHeader.gameObject.activeSelf;
        bool footerWasActive = sharedFooter != null && sharedFooter.gameObject.activeSelf;
        if (sharedHeader != null) sharedHeader.gameObject.SetActive(false);
        if (sharedFooter != null) sharedFooter.gameObject.SetActive(false);

        RectTransform rt = EnsureRect(_canvasRt, "SplashOverlay");
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.SetAsLastSibling();
        Canvas splashCanvas = rt.GetComponent<Canvas>();
        if (splashCanvas == null) splashCanvas = rt.gameObject.AddComponent<Canvas>();
        splashCanvas.overrideSorting = true;
        splashCanvas.sortingOrder = 5000;
        if (rt.GetComponent<GraphicRaycaster>() == null) rt.gameObject.AddComponent<GraphicRaycaster>();

        Image img = EnsureImage(rt); img.sprite = s; img.preserveAspect = false; img.raycastTarget = true; img.color = new Color(1, 1, 1, 0);

        float t = 0; while (t < splashFadeInDuration) { t += Time.unscaledDeltaTime; Color c = img.color; c.a = Mathf.Clamp01(t / splashFadeInDuration); img.color = c; yield return null; }
        if (splashHoldDuration > 0) yield return new WaitForSecondsRealtime(splashHoldDuration);
        t = 0; while (t < splashFadeOutDuration) { t += Time.unscaledDeltaTime; Color c = img.color; c.a = 1f - Mathf.Clamp01(t / splashFadeOutDuration); img.color = c; yield return null; }
        Destroy(rt.gameObject);

        if (sharedHeader != null) sharedHeader.gameObject.SetActive(headerWasActive);
        if (sharedFooter != null) sharedFooter.gameObject.SetActive(footerWasActive);
        NormalizeSharedSceneChrome();
        NormalizeBodiesForOverlay();
    }

    private static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null) { GameObject go = new GameObject(name); go.transform.SetParent(parent, false); return go.AddComponent<RectTransform>(); }
        RectTransform rt = t as RectTransform; return rt != null ? rt : t.gameObject.AddComponent<RectTransform>();
    }

    private static Image EnsureImage(RectTransform rt)
    {
        Image i = rt.GetComponent<Image>(); if (i == null) { rt.gameObject.AddComponent<CanvasRenderer>(); i = rt.gameObject.AddComponent<Image>(); }
        return i;
    }

    private static void RemoveLayoutComponent<T>(RectTransform rt) where T : Component
    {
        T component = rt.GetComponent<T>();
        if (component != null)
            Destroy(component);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private static Button EnsureButton(RectTransform rt)
    {
        Button b = rt.GetComponent<Button>(); if (b == null) b = rt.gameObject.AddComponent<Button>(); return b;
    }

    private static Text EnsureLabel(Transform parent, string value, int size, FontStyle style)
    {
        RectTransform rt = EnsureRect(parent, "Label");
        Text t = rt.GetComponent<Text>(); if (t == null) t = rt.gameObject.AddComponent<Text>();
        t.font = UiFontProvider.GetDefaultFont(); t.fontSize = size; t.fontStyle = style; t.color = Color.white; t.text = value; t.raycastTarget = false;
        return t;
    }

    private static Text EnsureNamedLabel(Transform parent, string name, string value, int size, FontStyle style)
    {
        RectTransform rt = EnsureRect(parent, name);
        Text t = rt.GetComponent<Text>();
        if (t == null) t = rt.gameObject.AddComponent<Text>();
        t.font = UiFontProvider.GetDefaultFont();
        t.fontSize = size;
        t.fontStyle = style;
        t.color = Color.white;
        t.text = value;
        t.raycastTarget = false;
        return t;
    }

    private static Text EnsureInputText(Transform parent, string name, Color color, FontStyle style, string value)
    {
        RectTransform rt = EnsureRect(parent, name); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(12, 6); rt.offsetMax = new Vector2(-12, -6);
        Text t = rt.GetComponent<Text>(); if (t == null) t = rt.gameObject.AddComponent<Text>();
        t.font = UiFontProvider.GetDefaultFont(); t.fontSize = 20; t.fontStyle = style; t.color = color; t.alignment = TextAnchor.MiddleLeft; t.text = value;
        return t;
    }

    private static Button EnsureMenuButton(Transform parent, string name, string caption, Color color)
    {
        RectTransform rt = EnsureRect(parent, name);
        LayoutElement le = rt.GetComponent<LayoutElement>(); if (le == null) le = rt.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 42;
        Image bg = EnsureImage(rt); bg.color = color;
        Button b = EnsureButton(rt); b.targetGraphic = bg;
        Text t = EnsureLabel(rt, caption, 18, FontStyle.Bold); t.alignment = TextAnchor.MiddleCenter;
        RectTransform tr = t.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        return b;
    }

    private static void RenderStarCluster(RectTransform root, Sprite starSprite, int stars, float size)
    {
        if (root == null) return;
        ClearChildren(root);
        if (starSprite == null || stars <= 0) return;
        stars = Mathf.Clamp(stars, 0, 3);

        List<Vector2> positions = new List<Vector2>();
        if (stars == 1)
        {
            positions.Add(new Vector2(0f, size * 0.2f));
        }
        else if (stars == 2)
        {
            positions.Add(new Vector2(-size * 0.55f, size * 0.15f));
            positions.Add(new Vector2(size * 0.55f, size * 0.15f));
        }
        else
        {
            positions.Add(new Vector2(0f, size * 0.7f));
            positions.Add(new Vector2(-size * 0.62f, -size * 0.15f));
            positions.Add(new Vector2(size * 0.62f, -size * 0.15f));
        }

        for (int i = 0; i < positions.Count; i++)
        {
            RectTransform starRt = EnsureRect(root, "Star_" + i);
            starRt.anchorMin = starRt.anchorMax = new Vector2(0.5f, 0.5f);
            starRt.pivot = new Vector2(0.5f, 0.5f);
            starRt.anchoredPosition = positions[i];
            starRt.sizeDelta = new Vector2(size, size);
            Image image = EnsureImage(starRt);
            image.sprite = starSprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }
    }

    private static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        Sprite s = Resources.Load<Sprite>(path); if (s != null) return s;
        Sprite[] all = Resources.LoadAll<Sprite>(path); if (all == null || all.Length == 0) return null;
        Sprite best = all[0]; float ba = best != null ? best.rect.width * best.rect.height : -1;
        for (int i = 1; i < all.Length; i++) { Sprite c = all[i]; if (c == null) continue; float a = c.rect.width * c.rect.height; if (a > ba) { best = c; ba = a; } }
        return best;
    }

    private static void FixTextVisibility()
    {
        Text[] ts = FindObjectsByType<Text>(FindObjectsSortMode.None);
        foreach (Text t in ts) if (t != null && t.gameObject.activeInHierarchy) { if (t.color.a < 0.1f) t.color = Color.white; if (t.fontSize <= 0 || t.fontSize > 120) t.fontSize = 18; }
        TMP_Text[] tmps = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (TMP_Text t in tmps) if (t != null && t.gameObject.activeInHierarchy) { if (t.color.a < 0.1f) t.color = Color.white; if (t.fontSize <= 0 || t.fontSize > 200) t.fontSize = 28; }
    }
}

[Serializable]
public class PlayerLevelResultData
{
    public string levelId;
    public int levelIndex;
    public int bestStars;
    public float bestTimeSeconds;
    public int bestErrors;
    public int completionCount;
    public string lastCompletedUtc;
}

[Serializable]
public class PlayerProfileData
{
    public string playerUuid;
    public string pseudo;
    public string createdAtUtc;
    public string updatedAtUtc;
    public PlayerLevelResultData[] levelResults;
}

[Serializable]
public class DefaultPseudoCatalog
{
    public string[] pseudos;
}

public static class PlayerProfileStore
{
    private const string ProfileFileName = "player-profile.json";
    private const string DefaultPseudoResourcePath = "Config/default-pseudos";
    private const string LegacyPseudoKey = "PlayerPseudo";
    private const string LegacyStarsKeyPrefix = "LevelStars_";
    private const int LegacyStarsMaxScan = 120;

    private static readonly string[] FallbackPseudos =
    {
        "CatalyseurX", "MoleculeNomade", "IonSerein", "AcideAlpha", "BaseBeta",
        "RefluxRider", "NitroNova", "LabPhoenix", "SigmaSolution", "OrbitalEcho",
        "PolymereZen", "ReactifRapide", "BenzeneBeat", "pHantom", "CovalentFox"
    };

    private static PlayerProfileData _profile;
    private static bool _loaded;

    private static string ProfilePath => Path.Combine(Application.persistentDataPath, ProfileFileName);

    public static void EnsureInitialized()
    {
        if (_loaded) return;
        _loaded = true;

        bool createdNew = false;
        _profile = LoadProfileFromDisk();
        if (!IsProfileValid(_profile))
        {
            _profile = CreateNewProfile();
            createdNew = true;
        }

        bool changed = NormalizeProfile(ref _profile);
        changed |= MergeLegacyData(ref _profile);
        if (changed)
            SaveProfileToDisk();

        if (createdNew)
            BackendApiClient.CreatePlayer(_profile.playerUuid, _profile.pseudo, DateTime.UtcNow);
    }

    public static string GetPlayerUuid()
    {
        EnsureInitialized();
        return _profile != null ? _profile.playerUuid : string.Empty;
    }

    public static string GetPseudo()
    {
        EnsureInitialized();
        return _profile != null ? _profile.pseudo : "Joueur";
    }

    public static void SetPseudo(string value)
    {
        EnsureInitialized();
        if (_profile == null) return;

        string safe = string.IsNullOrWhiteSpace(value) ? "Joueur" : value.Trim();
        if (string.Equals(_profile.pseudo, safe, StringComparison.Ordinal))
            return;

        _profile.pseudo = safe;
        _profile.updatedAtUtc = NowUtcIso();
        SaveProfileToDisk();
        SyncLegacyPseudo();
        BackendApiClient.UpdatePseudo(_profile.playerUuid, _profile.pseudo, DateTime.UtcNow);
    }

    public static int GetLevelStars(int levelIndex)
    {
        EnsureInitialized();
        if (_profile == null || levelIndex <= 0 || _profile.levelResults == null)
            return 0;

        for (int i = 0; i < _profile.levelResults.Length; i++)
        {
            PlayerLevelResultData entry = _profile.levelResults[i];
            if (entry == null) continue;
            if (entry.levelIndex == levelIndex)
                return Mathf.Clamp(entry.bestStars, 0, 3);
        }
        return 0;
    }

    public static void RecordLevelResult(int levelIndex, string levelId, int stars, float elapsedSeconds, int errors)
    {
        EnsureInitialized();
        if (_profile == null || levelIndex <= 0)
            return;

        stars = Mathf.Clamp(stars, 0, 3);
        List<PlayerLevelResultData> list = new List<PlayerLevelResultData>(_profile.levelResults ?? Array.Empty<PlayerLevelResultData>());

        PlayerLevelResultData entry = null;
        for (int i = 0; i < list.Count; i++)
        {
            PlayerLevelResultData candidate = list[i];
            if (candidate == null) continue;
            bool sameIndex = candidate.levelIndex == levelIndex;
            bool sameId = !string.IsNullOrWhiteSpace(levelId) &&
                          !string.IsNullOrWhiteSpace(candidate.levelId) &&
                          string.Equals(candidate.levelId, levelId, StringComparison.OrdinalIgnoreCase);
            if (sameIndex || sameId)
            {
                entry = candidate;
                break;
            }
        }

        if (entry == null)
        {
            entry = new PlayerLevelResultData
            {
                levelIndex = levelIndex,
                levelId = string.IsNullOrWhiteSpace(levelId) ? ("level-" + levelIndex) : levelId.Trim(),
                bestStars = 0,
                bestTimeSeconds = -1f,
                bestErrors = -1,
                completionCount = 0
            };
            list.Add(entry);
        }
        else if (string.IsNullOrWhiteSpace(entry.levelId) && !string.IsNullOrWhiteSpace(levelId))
        {
            entry.levelId = levelId.Trim();
        }

        if (stars > entry.bestStars)
            entry.bestStars = stars;

        if (elapsedSeconds > 0f && (entry.bestTimeSeconds <= 0f || elapsedSeconds < entry.bestTimeSeconds))
            entry.bestTimeSeconds = elapsedSeconds;

        if (errors >= 0 && (entry.bestErrors < 0 || errors < entry.bestErrors))
            entry.bestErrors = errors;

        entry.completionCount = Mathf.Max(0, entry.completionCount) + 1;
        entry.lastCompletedUtc = NowUtcIso();

        list.Sort((a, b) =>
        {
            int ai = a != null ? a.levelIndex : int.MaxValue;
            int bi = b != null ? b.levelIndex : int.MaxValue;
            return ai.CompareTo(bi);
        });

        _profile.levelResults = list.ToArray();
        _profile.updatedAtUtc = NowUtcIso();
        SaveProfileToDisk();
        SyncLegacyStars(levelIndex, entry.bestStars);
        BackendApiClient.SendLevelFinished(_profile.playerUuid, levelIndex, entry.levelId, elapsedSeconds, stars, DateTime.UtcNow);
    }

    public static void ResetProfileWithNewIdentity()
    {
        EnsureInitialized();
        string previousUuid = _profile != null ? _profile.playerUuid : string.Empty;
        if (!string.IsNullOrWhiteSpace(previousUuid))
            BackendApiClient.DeletePlayer(previousUuid, DateTime.UtcNow);

        _profile = CreateNewProfile();
        SaveProfileToDisk();
        SyncLegacyPseudo();
        ClearLegacyStars();
        BackendApiClient.CreatePlayer(_profile.playerUuid, _profile.pseudo, DateTime.UtcNow);
        RuntimeFileLogger.Log("PlayerProfileStore", "Profile reset with new UUID and pseudo.");
    }

    private static PlayerProfileData CreateNewProfile()
    {
        string now = NowUtcIso();
        return new PlayerProfileData
        {
            playerUuid = Guid.NewGuid().ToString("N"),
            pseudo = PickRandomPseudo(),
            createdAtUtc = now,
            updatedAtUtc = now,
            levelResults = Array.Empty<PlayerLevelResultData>()
        };
    }

    private static bool NormalizeProfile(ref PlayerProfileData profile)
    {
        if (profile == null)
            return false;

        bool changed = false;
        if (string.IsNullOrWhiteSpace(profile.playerUuid))
        {
            profile.playerUuid = Guid.NewGuid().ToString("N");
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(profile.pseudo))
        {
            profile.pseudo = PickRandomPseudo();
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(profile.createdAtUtc))
        {
            profile.createdAtUtc = NowUtcIso();
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(profile.updatedAtUtc))
        {
            profile.updatedAtUtc = profile.createdAtUtc;
            changed = true;
        }
        if (profile.levelResults == null)
        {
            profile.levelResults = Array.Empty<PlayerLevelResultData>();
            changed = true;
        }
        return changed;
    }

    private static bool MergeLegacyData(ref PlayerProfileData profile)
    {
        if (profile == null)
            return false;

        bool changed = false;

        string legacyPseudo = PlayerPrefs.GetString(LegacyPseudoKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(legacyPseudo) && string.IsNullOrWhiteSpace(profile.pseudo))
        {
            profile.pseudo = legacyPseudo.Trim();
            changed = true;
        }

        List<PlayerLevelResultData> list = new List<PlayerLevelResultData>(profile.levelResults ?? Array.Empty<PlayerLevelResultData>());
        for (int level = 1; level <= LegacyStarsMaxScan; level++)
        {
            int stars = Mathf.Clamp(PlayerPrefs.GetInt(LegacyStarsKeyPrefix + level, 0), 0, 3);
            if (stars <= 0) continue;

            PlayerLevelResultData entry = null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                if (list[i].levelIndex == level)
                {
                    entry = list[i];
                    break;
                }
            }

            if (entry == null)
            {
                entry = new PlayerLevelResultData
                {
                    levelId = "level-" + level,
                    levelIndex = level,
                    bestStars = stars,
                    bestErrors = -1,
                    bestTimeSeconds = -1f,
                    completionCount = 0,
                    lastCompletedUtc = string.Empty
                };
                list.Add(entry);
                changed = true;
            }
            else if (stars > entry.bestStars)
            {
                entry.bestStars = stars;
                changed = true;
            }
        }

        if (changed)
        {
            list.Sort((a, b) =>
            {
                int ai = a != null ? a.levelIndex : int.MaxValue;
                int bi = b != null ? b.levelIndex : int.MaxValue;
                return ai.CompareTo(bi);
            });
            profile.levelResults = list.ToArray();
            profile.updatedAtUtc = NowUtcIso();
        }

        return changed;
    }

    private static PlayerProfileData LoadProfileFromDisk()
    {
        try
        {
            if (!File.Exists(ProfilePath))
                return null;

            string json = File.ReadAllText(ProfilePath);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            PlayerProfileData data = JsonUtility.FromJson<PlayerProfileData>(json);
            RuntimeFileLogger.Log("PlayerProfileStore", "Profile loaded from " + ProfilePath);
            return data;
        }
        catch (Exception exception)
        {
            RuntimeFileLogger.Error("PlayerProfileStore", "Load profile failed: " + exception.Message);
            return null;
        }
    }

    private static void SaveProfileToDisk()
    {
        if (_profile == null)
            return;

        try
        {
            _profile.updatedAtUtc = NowUtcIso();
            string dir = Path.GetDirectoryName(ProfilePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
            string json = JsonUtility.ToJson(_profile, true);
            File.WriteAllText(ProfilePath, json);
            SyncLegacyPseudo();
        }
        catch (Exception exception)
        {
            RuntimeFileLogger.Error("PlayerProfileStore", "Save profile failed: " + exception.Message);
        }
    }

    private static bool IsProfileValid(PlayerProfileData data)
    {
        return data != null && !string.IsNullOrWhiteSpace(data.playerUuid);
    }

    private static string PickRandomPseudo()
    {
        string[] candidates = LoadDefaultPseudos();
        if (candidates == null || candidates.Length == 0)
            candidates = FallbackPseudos;
        if (candidates == null || candidates.Length == 0)
            return "Joueur";

        int index = Mathf.Abs(Guid.NewGuid().GetHashCode()) % candidates.Length;
        string value = candidates[index];
        return string.IsNullOrWhiteSpace(value) ? "Joueur" : value.Trim();
    }

    private static string[] LoadDefaultPseudos()
    {
        try
        {
            TextAsset asset = Resources.Load<TextAsset>(DefaultPseudoResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return FallbackPseudos;

            DefaultPseudoCatalog catalog = JsonUtility.FromJson<DefaultPseudoCatalog>(asset.text);
            if (catalog == null || catalog.pseudos == null || catalog.pseudos.Length == 0)
                return FallbackPseudos;

            List<string> filtered = new List<string>();
            for (int i = 0; i < catalog.pseudos.Length; i++)
            {
                string candidate = catalog.pseudos[i];
                if (string.IsNullOrWhiteSpace(candidate)) continue;
                string trimmed = candidate.Trim();
                if (trimmed.Length == 0) continue;
                filtered.Add(trimmed);
            }
            return filtered.Count > 0 ? filtered.ToArray() : FallbackPseudos;
        }
        catch
        {
            return FallbackPseudos;
        }
    }

    private static void SyncLegacyPseudo()
    {
        if (_profile == null) return;
        PlayerPrefs.SetString(LegacyPseudoKey, string.IsNullOrWhiteSpace(_profile.pseudo) ? "Joueur" : _profile.pseudo);
        PlayerPrefs.Save();
    }

    private static void SyncLegacyStars(int levelIndex, int stars)
    {
        if (levelIndex <= 0) return;
        PlayerPrefs.SetInt(LegacyStarsKeyPrefix + levelIndex, Mathf.Clamp(stars, 0, 3));
        PlayerPrefs.Save();
    }

    private static void ClearLegacyStars()
    {
        for (int i = 1; i <= LegacyStarsMaxScan; i++)
            PlayerPrefs.DeleteKey(LegacyStarsKeyPrefix + i);
        PlayerPrefs.Save();
    }

    private static string NowUtcIso()
    {
        return DateTime.UtcNow.ToString("O");
    }
}


