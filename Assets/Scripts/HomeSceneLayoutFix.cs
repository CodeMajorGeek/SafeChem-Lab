using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private int levelCount = 6;
    [SerializeField] private string inGameSceneName = "InGame";

    private const string SelectedLevelKey = "SelectedLevel";
    private const string PlayerPseudoKey = "PlayerPseudo";

    private RectTransform _canvasRt;
    private ScrollRect _scroll;
    private RectTransform _chrome;
    private RectTransform _progressBody;
    private InputField _pseudoInput;
    private RectTransform _settings;
    private RectTransform _settingsOverlay;
    private Button _play;
    private bool _settingsOpen;
    private Coroutine _settingsAnim;

    private void Start()
    {
        if (!fixOnStart) return;
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
        _chrome = _canvasRt;
        NormalizeFixedBackground();
        NormalizePagerSurfaces();
        DisableDuplicatePageChrome();
        NormalizeSharedSceneChrome();
        NormalizeBodiesForOverlay();
        BuildProgression();
        FixTextVisibility();

        if (showSplash) StartCoroutine(ShowSplash());
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
            pagerRt.offsetMin = new Vector2(0f, footerHeight);
            pagerRt.offsetMax = new Vector2(0f, -headerHeight);
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

        HomePagerPageSizer sizer = FindFirstObjectByType<HomePagerPageSizer>();
        if (sizer != null)
            sizer.Apply();

        if (_scroll.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
    }

    private void NormalizeHeader(RectTransform header)
    {
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, headerHeight);
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
            logoRt.anchoredPosition = new Vector2(0f, -15f);
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
        settingsRt.anchorMin = settingsRt.anchorMax = new Vector2(0f, 0.5f);
        settingsRt.pivot = new Vector2(0f, 0.5f);
        settingsRt.anchoredPosition = new Vector2(24f, -18f);
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
        footer.anchorMin = new Vector2(0f, 0f);
        footer.anchorMax = new Vector2(1f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.anchoredPosition = Vector2.zero;
        footer.sizeDelta = new Vector2(0f, footerHeight);

        Image bg = EnsureImage(footer);
        bg.color = new Color(0.03f, 0.06f, 0.1f, 0.98f);

        RectTransform row = footer.Find("FooterRow") as RectTransform;
        if (row == null) return;

        row.anchorMin = Vector2.zero;
        row.anchorMax = Vector2.one;
        row.offsetMin = new Vector2(24f, 16f);
        row.offsetMax = new Vector2(-24f, -16f);
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
        Image bg = EnsureImage(_settings); bg.color = new Color(0.04f, 0.07f, 0.12f, 0.98f);
        ClearChildren(_settings);
        VerticalLayoutGroup v = _settings.GetComponent<VerticalLayoutGroup>(); if (v == null) v = _settings.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(20, 20, 88, 20); v.spacing = 14; v.childAlignment = TextAnchor.UpperLeft; v.childControlWidth = true; v.childControlHeight = false; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        RectTransform titleRt = EnsureRect(_settings, "SettingsTitle");
        LayoutElement titleLe = titleRt.GetComponent<LayoutElement>(); if (titleLe == null) titleLe = titleRt.gameObject.AddComponent<LayoutElement>(); titleLe.preferredHeight = 42;
        Text title = EnsureLabel(titleRt, "Parametres", 28, FontStyle.Bold); title.alignment = TextAnchor.MiddleLeft; title.horizontalOverflow = HorizontalWrapMode.Overflow; title.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform titleTextRt = title.rectTransform; titleTextRt.anchorMin = Vector2.zero; titleTextRt.anchorMax = Vector2.one; titleTextRt.offsetMin = Vector2.zero; titleTextRt.offsetMax = Vector2.zero;

        RectTransform pseudoLabelRt = EnsureRect(_settings, "PseudoLabel");
        LayoutElement pseudoLe = pseudoLabelRt.GetComponent<LayoutElement>(); if (pseudoLe == null) pseudoLe = pseudoLabelRt.gameObject.AddComponent<LayoutElement>(); pseudoLe.preferredHeight = 28;
        Text pseudoLabel = EnsureLabel(pseudoLabelRt, "Pseudo :", 22, FontStyle.Bold); pseudoLabel.alignment = TextAnchor.MiddleLeft; pseudoLabel.horizontalOverflow = HorizontalWrapMode.Overflow; pseudoLabel.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform pseudoTextRt = pseudoLabel.rectTransform; pseudoTextRt.anchorMin = Vector2.zero; pseudoTextRt.anchorMax = Vector2.one; pseudoTextRt.offsetMin = Vector2.zero; pseudoTextRt.offsetMax = Vector2.zero;

        RectTransform inputRt = EnsureRect(_settings, "PseudoInput");
        LayoutElement le = inputRt.GetComponent<LayoutElement>(); if (le == null) le = inputRt.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 58;
        Image ibg = EnsureImage(inputRt); ibg.color = new Color(0.12f, 0.16f, 0.22f, 1);
        _pseudoInput = inputRt.GetComponent<InputField>(); if (_pseudoInput == null) _pseudoInput = inputRt.gameObject.AddComponent<InputField>();

        Text tx = EnsureInputText(inputRt, "Text", Color.white, FontStyle.Normal, "");
        Text ph = EnsureInputText(inputRt, "Placeholder", new Color(1, 1, 1, 0.45f), FontStyle.Italic, "Entrer votre pseudo");
        _pseudoInput.textComponent = tx; _pseudoInput.placeholder = ph; _pseudoInput.text = PlayerPrefs.GetString(PlayerPseudoKey, "Joueur");
        _pseudoInput.onEndEdit.RemoveAllListeners(); _pseudoInput.onEndEdit.AddListener(SavePseudo);

        Button save = EnsureMenuButton(_settings, "BtnSave", "Enregistrer", new Color(0.16f, 0.36f, 0.64f, 1));
        LayoutElement saveLe = save.GetComponent<LayoutElement>(); if (saveLe != null) saveLe.preferredHeight = 56;
        Text saveText = save.GetComponentInChildren<Text>(); if (saveText != null) saveText.fontSize = 20;
        save.onClick.RemoveAllListeners(); save.onClick.AddListener(() => SavePseudo(_pseudoInput != null ? _pseudoInput.text : string.Empty));
    }

    private void ToggleSettings()
    {
        if (_settings == null) return;
        SetSettingsOpen(!_settingsOpen);
    }

    private void CloseSettings()
    {
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
        string safe = string.IsNullOrWhiteSpace(value) ? "Joueur" : value.Trim();
        PlayerPrefs.SetString(PlayerPseudoKey, safe);
        PlayerPrefs.Save();
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

        Sprite lvl = LoadSprite("Icons/map-level"); if (lvl == null) return;
        RectTransform map = EnsureRect(_progressBody, "LevelMap");
        map.anchorMin = Vector2.zero; map.anchorMax = Vector2.one; map.offsetMin = new Vector2(0, 132); map.offsetMax = new Vector2(0, -8);
        VerticalLayoutGroup v = map.GetComponent<VerticalLayoutGroup>(); if (v == null) v = map.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8; v.padding = new RectOffset(0, 0, 8, 8); v.childAlignment = TextAnchor.UpperCenter; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true;

        int count = Mathf.Max(1, levelCount);
        for (int i = 0; i < count; i++)
        {
            RectTransform row = EnsureRect(map, "LevelRow_" + (i + 1));
            LayoutElement le = row.GetComponent<LayoutElement>(); if (le == null) le = row.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 96;
            RectTransform btnRt = EnsureRect(row, "BtnLevel");
            btnRt.anchorMin = btnRt.anchorMax = btnRt.pivot = new Vector2(0.5f, 0.5f); btnRt.sizeDelta = new Vector2(194, 98);
            Image bi = EnsureImage(btnRt); bi.sprite = lvl; bi.preserveAspect = true; bi.color = Color.white;
            Button b = EnsureButton(btnRt); b.targetGraphic = bi; b.onClick.RemoveAllListeners();
            int index = i + 1;
            b.onClick.AddListener(() => { PlayerPrefs.SetInt(SelectedLevelKey, index); PlayerPrefs.Save(); if (_play != null) { _play.gameObject.SetActive(true); Text t = _play.GetComponentInChildren<Text>(true); if (t != null) t.text = "Jouer niveau " + index; } });
            Text cap = EnsureLabel(btnRt, "Niveau " + index, 32, FontStyle.Bold);
            cap.alignment = TextAnchor.LowerCenter;
            RectTransform crt = cap.rectTransform;
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(0f, -14f);
            crt.offsetMax = new Vector2(0f, 12f);
        }

        _play = EnsureMenuButton(_progressBody, "BtnPlayLevel", "Jouer", new Color(0.1f, 0.42f, 0.76f, 1));
        RectTransform prt = _play.GetComponent<RectTransform>(); prt.anchorMin = new Vector2(0.5f, 0); prt.anchorMax = new Vector2(0.5f, 0); prt.pivot = new Vector2(0.5f, 0); prt.anchoredPosition = new Vector2(0, 20); prt.sizeDelta = new Vector2(320, 78);
        _play.onClick.RemoveAllListeners(); _play.onClick.AddListener(() => { if (Application.CanStreamedLevelBeLoaded(inGameSceneName)) SceneManager.LoadScene(inGameSceneName); else Debug.LogError("Scene missing: " + inGameSceneName); });
        _play.gameObject.SetActive(false);
    }

    private IEnumerator ShowSplash()
    {
        Sprite s = LoadSprite("Backgrounds/bg-loading"); if (s == null) yield break;
        RectTransform rt = EnsureRect(_canvasRt, "SplashOverlay");
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.SetAsLastSibling();
        Image img = EnsureImage(rt); img.sprite = s; img.preserveAspect = false; img.raycastTarget = true; img.color = new Color(1, 1, 1, 0);

        float t = 0; while (t < splashFadeInDuration) { t += Time.unscaledDeltaTime; Color c = img.color; c.a = Mathf.Clamp01(t / splashFadeInDuration); img.color = c; yield return null; }
        if (splashHoldDuration > 0) yield return new WaitForSecondsRealtime(splashHoldDuration);
        t = 0; while (t < splashFadeOutDuration) { t += Time.unscaledDeltaTime; Color c = img.color; c.a = 1f - Mathf.Clamp01(t / splashFadeOutDuration); img.color = c; yield return null; }
        Destroy(rt.gameObject);
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
