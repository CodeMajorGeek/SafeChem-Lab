using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

[Serializable]
public class SubstanceCardData
{
    public string id;
    public string displayName;
    public string formula;
    public string shortDescription;
    public string hazardSummary;
    public string handlingNotes;
    public string atlasResource;
    public string atlasTextureResource;
    public string moleculeSpriteName;
    public string cardSpriteName;
    public int atlasCellIndex;
    public int atlasColumns;
    public int atlasCellWidth;
    public int atlasCellHeight;
    public string moleculeImageResource;
    public string cardImageResource;
    public string[] tags;
}

public class HomeSubstancesPanel : MonoBehaviour
{
    [SerializeField] private string resourcesFolder = "Substances";
    [SerializeField] private string legacyResourcesFolder = "substances-datas";
    [SerializeField] private Color cardColor = new Color(0.12f, 0.18f, 0.27f, 0.92f);
    [SerializeField] private Color cardBorderColor = new Color(0.46f, 0.62f, 0.8f, 1f);
    [SerializeField] private Color selectedCardColor = new Color(0.22f, 0.45f, 0.66f, 1f);

    private readonly List<SubstanceCardData> _cards = new List<SubstanceCardData>();
    private readonly List<Button> _cardButtons = new List<Button>();
    private static readonly Dictionary<string, Sprite> TextureMapCache = new Dictionary<string, Sprite>();

    private RectTransform _root;
    private Image _previewImage;
    private Text _previewTitle;
    private Text _previewFormula;

    private RectTransform _modalOverlay;
    private Text _modalTitle;
    private Text _modalFormula;
    private Image _modalImage;
    private Text _modalBody;

    private int _selectedIndex = -1;

    private void Start()
    {
        try
        {
            LoadCards();
            BuildUi();
            if (_cards.Count > 0)
                SelectCard(0, false);
        }
        catch (Exception exception)
        {
            RuntimeFileLogger.Error("HomeSubstancesPanel", "Start failed: " + exception);
            Debug.LogException(exception, this);
        }
    }

    private void LoadCards()
    {
        _cards.Clear();
        string loadedFolder = resourcesFolder;
        TextAsset[] assets = Resources.LoadAll<TextAsset>(resourcesFolder);
        if ((assets == null || assets.Length == 0) && !string.IsNullOrWhiteSpace(legacyResourcesFolder))
        {
            assets = Resources.LoadAll<TextAsset>(legacyResourcesFolder);
            loadedFolder = legacyResourcesFolder;
            RuntimeFileLogger.Warn("HomeSubstancesPanel", "No JSON in folder '" + resourcesFolder + "', fallback to '" + legacyResourcesFolder + "'");
        }
        Array.Sort(assets, (a, b) => string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
        RuntimeFileLogger.Log("HomeSubstancesPanel", "LoadCards folder=" + loadedFolder + " count=" + (assets != null ? assets.Length : 0));

        foreach (TextAsset asset in assets)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                continue;

            SubstanceCardData data = null;
            try
            {
                data = JsonUtility.FromJson<SubstanceCardData>(asset.text);
            }
            catch (Exception exception)
            {
                RuntimeFileLogger.Error("HomeSubstancesPanel", "Invalid JSON " + asset.name + " - " + exception.Message);
            }

            if (data == null || string.IsNullOrWhiteSpace(data.displayName))
                continue;

            if (string.IsNullOrWhiteSpace(data.id))
                data.id = asset.name;
            if (asset.name.IndexOf("template", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(data.id, "substance-id", StringComparison.OrdinalIgnoreCase))
                continue;
            _cards.Add(data);
            RuntimeFileLogger.Log("HomeSubstancesPanel", "Loaded substance id=" + data.id + " displayName=" + data.displayName);
        }
    }

    private void BuildUi()
    {
        RectTransform host = transform as RectTransform;
        if (host == null)
            return;

        _root = EnsureRect(host, "SubstancesUI");
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = new Vector2(8f, 8f);
        _root.offsetMax = new Vector2(-8f, 0f);
        _root.SetAsLastSibling();
        ClearChildren(_root);

        LayoutElement rootLayout = _root.GetComponent<LayoutElement>();
        if (rootLayout == null) rootLayout = _root.gameObject.AddComponent<LayoutElement>();
        rootLayout.ignoreLayout = true;

        Image rootBg = EnsureImage(_root);
        rootBg.color = new Color(1f, 1f, 1f, 0f);
        rootBg.raycastTarget = false;

        BuildCardGrid();
        BuildModalOverlay();
    }

    private void BuildTopPreview()
    {
        RectTransform preview = EnsureRect(_root, "Preview");
        preview.anchorMin = new Vector2(0f, 1f);
        preview.anchorMax = new Vector2(1f, 1f);
        preview.pivot = new Vector2(0.5f, 1f);
        preview.anchoredPosition = Vector2.zero;
        preview.sizeDelta = new Vector2(0f, 198f);
        Image previewBg = EnsureImage(preview);
        previewBg.color = new Color(1f, 1f, 1f, 0f);

        RectTransform imageFrame = EnsureRect(preview, "MoleculeFrame");
        imageFrame.anchorMin = new Vector2(0.5f, 1f);
        imageFrame.anchorMax = new Vector2(0.5f, 1f);
        imageFrame.pivot = new Vector2(0.5f, 1f);
        imageFrame.anchoredPosition = new Vector2(0f, -14f);
        imageFrame.sizeDelta = new Vector2(128f, 128f);
        Image frameBg = EnsureImage(imageFrame);
        frameBg.color = new Color(0.16f, 0.23f, 0.34f, 1f);

        RectTransform imageRt = EnsureRect(imageFrame, "Image");
        imageRt.anchorMin = imageRt.anchorMax = imageRt.pivot = new Vector2(0.5f, 0.5f);
        imageRt.anchoredPosition = Vector2.zero;
        imageRt.sizeDelta = new Vector2(108f, 108f);
        _previewImage = EnsureImage(imageRt);
        _previewImage.color = Color.white;
        _previewImage.preserveAspect = true;
        _previewImage.raycastTarget = false;

        _previewTitle = EnsureText(preview, "SubstanceName", 22, FontStyle.Bold, "Substances");
        _previewTitle.alignment = TextAnchor.LowerCenter;
        RectTransform titleRt = _previewTitle.rectTransform;
        titleRt.anchorMin = Vector2.zero;
        titleRt.anchorMax = Vector2.one;
        titleRt.offsetMin = new Vector2(12f, 22f);
        titleRt.offsetMax = new Vector2(-12f, -138f);

        _previewFormula = EnsureText(preview, "Formula", 17, FontStyle.Normal, string.Empty);
        _previewFormula.alignment = TextAnchor.LowerCenter;
        _previewFormula.color = new Color(0.82f, 0.9f, 1f, 1f);
        RectTransform formulaRt = _previewFormula.rectTransform;
        formulaRt.anchorMin = Vector2.zero;
        formulaRt.anchorMax = Vector2.one;
        formulaRt.offsetMin = new Vector2(12f, 8f);
        formulaRt.offsetMax = new Vector2(-12f, -162f);
    }

    private void BuildCardGrid()
    {
        RectTransform frame = EnsureRect(_root, "CardsFrame");
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = new Vector2(0f, 0f);
        frame.offsetMax = new Vector2(0f, 0f);
        Image frameBg = EnsureImage(frame);
        frameBg.color = new Color(0.03f, 0.06f, 0.1f, 0.55f);

        RectTransform viewport = EnsureRect(frame, "Viewport");
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(8f, 8f);
        viewport.offsetMax = new Vector2(-8f, -8f);
        Image viewportBg = EnsureImage(viewport);
        viewportBg.color = new Color(1f, 1f, 1f, 0.02f);
        Mask mask = viewport.GetComponent<Mask>();
        if (mask == null) mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = EnsureRect(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(0, 0, 6, 6);
        grid.spacing = new Vector2(0f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = CalculateCardCellSize(viewport, grid);
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = frame.GetComponent<ScrollRect>();
        if (scroll == null) scroll = frame.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;

        _cardButtons.Clear();
        if (_cards.Count == 0)
        {
            RectTransform emptyRt = EnsureRect(content, "Empty");
            LayoutElement le = emptyRt.GetComponent<LayoutElement>();
            if (le == null) le = emptyRt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 480f;
            le.preferredHeight = 120f;
            Image emptyBg = EnsureImage(emptyRt);
            emptyBg.color = new Color(0.15f, 0.2f, 0.3f, 0.92f);
            Text emptyText = EnsureText(emptyRt, "Text", 20, FontStyle.Bold, "Aucune substance chargee");
            emptyText.alignment = TextAnchor.MiddleCenter;
            RectTransform rt = emptyText.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return;
        }

        for (int i = 0; i < _cards.Count; i++)
        {
            SubstanceCardData data = _cards[i];
            RectTransform card = EnsureRect(content, "Card_" + data.id);
            Image cardBg = EnsureImage(card);
            cardBg.color = cardColor;

            Button button = EnsureButton(card);
            button.targetGraphic = cardBg;
            button.onClick.RemoveAllListeners();
            int index = i;
            button.onClick.AddListener(() => SelectCard(index, true));
            _cardButtons.Add(button);

            RectTransform border = EnsureRect(card, "Border");
            border.anchorMin = Vector2.zero;
            border.anchorMax = Vector2.one;
            border.offsetMin = new Vector2(3f, 3f);
            border.offsetMax = new Vector2(-3f, -3f);
            Image borderImg = EnsureImage(border);
            borderImg.color = cardBorderColor;
            borderImg.raycastTarget = false;
            border.SetAsFirstSibling();

            RectTransform imageRt = EnsureRect(card, "Image");
            imageRt.anchorMin = new Vector2(0.05f, 0.3f);
            imageRt.anchorMax = new Vector2(0.95f, 0.96f);
            imageRt.pivot = new Vector2(0.5f, 0.5f);
            imageRt.anchoredPosition = Vector2.zero;
            imageRt.sizeDelta = Vector2.zero;
            Image img = EnsureImage(imageRt);
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            Sprite cardSprite = ResolveCardSprite(data);
            if (cardSprite != null)
            {
                img.sprite = cardSprite;
                img.color = Color.white;
            }
            else
            {
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0f);
            }

            RectTransform labelBandRt = EnsureRect(card, "LabelBand");
            labelBandRt.anchorMin = new Vector2(0.04f, 0.04f);
            labelBandRt.anchorMax = new Vector2(0.96f, 0.32f);
            labelBandRt.pivot = new Vector2(0.5f, 0f);
            labelBandRt.anchoredPosition = Vector2.zero;
            labelBandRt.sizeDelta = Vector2.zero;
            Image labelBandBg = EnsureImage(labelBandRt);
            labelBandBg.color = new Color(0.03f, 0.07f, 0.12f, 0.6f);
            labelBandBg.raycastTarget = false;

            Text nameText = EnsureText(labelBandRt, "Name", 14, FontStyle.Bold, data.displayName);
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 12;
            nameText.resizeTextMaxSize = 16;
            RectTransform nameRt = nameText.rectTransform;
            nameRt.anchorMin = new Vector2(0.03f, 0.42f);
            nameRt.anchorMax = new Vector2(0.97f, 0.95f);
            nameRt.pivot = new Vector2(0.5f, 0.5f);
            nameRt.anchoredPosition = Vector2.zero;
            nameRt.sizeDelta = Vector2.zero;

            Text formulaText = EnsureText(labelBandRt, "Formula", 12, FontStyle.Normal, data.formula);
            formulaText.alignment = TextAnchor.MiddleCenter;
            formulaText.color = new Color(0.8f, 0.9f, 1f, 1f);
            RectTransform formulaRt = formulaText.rectTransform;
            formulaRt.anchorMin = new Vector2(0.03f, 0.06f);
            formulaRt.anchorMax = new Vector2(0.97f, 0.4f);
            formulaRt.pivot = new Vector2(0.5f, 0.5f);
            formulaRt.anchoredPosition = Vector2.zero;
            formulaRt.sizeDelta = Vector2.zero;
        }
    }

    private void BuildModalOverlay()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Transform parent = parentCanvas != null ? parentCanvas.transform : _root;

        _modalOverlay = EnsureRect(parent, "SubstanceModalOverlay");
        _modalOverlay.anchorMin = Vector2.zero;
        _modalOverlay.anchorMax = Vector2.one;
        _modalOverlay.offsetMin = Vector2.zero;
        _modalOverlay.offsetMax = Vector2.zero;
        _modalOverlay.SetAsLastSibling();
        ClearChildren(_modalOverlay);

        Canvas overlayCanvas = _modalOverlay.GetComponent<Canvas>();
        if (overlayCanvas == null) overlayCanvas = _modalOverlay.gameObject.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 250;
        if (_modalOverlay.GetComponent<GraphicRaycaster>() == null)
            _modalOverlay.gameObject.AddComponent<GraphicRaycaster>();

        Image overlayBg = EnsureImage(_modalOverlay);
        overlayBg.color = new Color(0f, 0f, 0f, 0.62f);
        Button closeArea = EnsureButton(_modalOverlay);
        closeArea.targetGraphic = overlayBg;
        closeArea.onClick.RemoveAllListeners();
        closeArea.onClick.AddListener(CloseModal);

        RectTransform modal = EnsureRect(_modalOverlay, "Modal");
        modal.anchorMin = new Vector2(0.04f, 0.04f);
        modal.anchorMax = new Vector2(0.96f, 0.96f);
        modal.offsetMin = Vector2.zero;
        modal.offsetMax = Vector2.zero;
        Image modalBg = EnsureImage(modal);
        modalBg.color = new Color(0.96f, 0.92f, 0.84f, 1f);

        RectTransform top = EnsureRect(modal, "TopBar");
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = Vector2.zero;
        top.sizeDelta = new Vector2(0f, 98f);
        Image topBg = EnsureImage(top);
        topBg.color = new Color(0.36f, 0.24f, 0.11f, 1f);

        _modalTitle = EnsureText(top, "Title", 32, FontStyle.Bold, string.Empty);
        _modalTitle.alignment = TextAnchor.MiddleLeft;
        _modalTitle.color = new Color(0.98f, 0.96f, 0.92f, 1f);
        RectTransform titleRt = _modalTitle.rectTransform;
        titleRt.anchorMin = Vector2.zero;
        titleRt.anchorMax = Vector2.one;
        titleRt.offsetMin = new Vector2(20f, 12f);
        titleRt.offsetMax = new Vector2(-180f, -12f);

        RectTransform closeBtnRt = EnsureRect(top, "BtnClose");
        closeBtnRt.anchorMin = new Vector2(1f, 0.5f);
        closeBtnRt.anchorMax = new Vector2(1f, 0.5f);
        closeBtnRt.pivot = new Vector2(1f, 0.5f);
        closeBtnRt.anchoredPosition = new Vector2(-14f, 0f);
        closeBtnRt.sizeDelta = new Vector2(122f, 52f);
        Image closeBg = EnsureImage(closeBtnRt);
        closeBg.color = new Color(0.74f, 0.64f, 0.47f, 1f);
        Button closeBtn = EnsureButton(closeBtnRt);
        closeBtn.targetGraphic = closeBg;
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(CloseModal);
        Text closeText = EnsureText(closeBtnRt, "Text", 20, FontStyle.Bold, "Fermer");
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = new Color(0.2f, 0.14f, 0.08f, 1f);
        RectTransform closeTextRt = closeText.rectTransform;
        closeTextRt.anchorMin = Vector2.zero;
        closeTextRt.anchorMax = Vector2.one;
        closeTextRt.offsetMin = Vector2.zero;
        closeTextRt.offsetMax = Vector2.zero;

        _modalFormula = EnsureText(modal, "Formula", 18, FontStyle.Italic, string.Empty);
        _modalFormula.alignment = TextAnchor.UpperLeft;
        _modalFormula.color = new Color(0.36f, 0.24f, 0.11f, 1f);
        RectTransform formulaRt = _modalFormula.rectTransform;
        formulaRt.anchorMin = new Vector2(0f, 1f);
        formulaRt.anchorMax = new Vector2(1f, 1f);
        formulaRt.pivot = new Vector2(0.5f, 1f);
        formulaRt.anchoredPosition = new Vector2(0f, -106f);
        formulaRt.sizeDelta = new Vector2(0f, 30f);
        formulaRt.offsetMin = new Vector2(24f, 0f);
        formulaRt.offsetMax = new Vector2(-24f, 0f);

        RectTransform modalImgFrame = EnsureRect(modal, "MoleculeFrame");
        modalImgFrame.anchorMin = new Vector2(0.5f, 1f);
        modalImgFrame.anchorMax = new Vector2(0.5f, 1f);
        modalImgFrame.pivot = new Vector2(0.5f, 1f);
        modalImgFrame.anchoredPosition = new Vector2(0f, -146f);
        modalImgFrame.sizeDelta = new Vector2(160f, 160f);
        Image modalImgBg = EnsureImage(modalImgFrame);
        modalImgBg.color = new Color(0.86f, 0.8f, 0.66f, 1f);

        RectTransform modalImgRt = EnsureRect(modalImgFrame, "Image");
        modalImgRt.anchorMin = modalImgRt.anchorMax = modalImgRt.pivot = new Vector2(0.5f, 0.5f);
        modalImgRt.anchoredPosition = Vector2.zero;
        modalImgRt.sizeDelta = new Vector2(132f, 132f);
        _modalImage = EnsureImage(modalImgRt);
        _modalImage.color = Color.white;
        _modalImage.preserveAspect = true;
        _modalImage.raycastTarget = false;

        RectTransform viewport = EnsureRect(modal, "Viewport");
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(24f, 24f);
        viewport.offsetMax = new Vector2(-24f, -330f);
        Image viewportBg = EnsureImage(viewport);
        viewportBg.color = new Color(0.98f, 0.95f, 0.88f, 1f);
        Mask mask = viewport.GetComponent<Mask>();
        if (mask == null) mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = EnsureRect(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _modalBody = EnsureText(content, "Body", 20, FontStyle.Normal, string.Empty);
        _modalBody.color = new Color(0.17f, 0.12f, 0.08f, 1f);
        _modalBody.alignment = TextAnchor.UpperLeft;
        _modalBody.supportRichText = true;
        RectTransform bodyRt = _modalBody.rectTransform;
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = Vector2.zero;
        bodyRt.offsetMax = Vector2.zero;

        ScrollRect scroll = modal.GetComponent<ScrollRect>();
        if (scroll == null) scroll = modal.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 22f;

        _modalOverlay.gameObject.SetActive(false);
    }

    private void SelectCard(int index, bool openModal)
    {
        if (index < 0 || index >= _cards.Count)
            return;

        _selectedIndex = index;
        SubstanceCardData card = _cards[index];
        ApplyPreview(card);
        ApplyModal(card);
        RefreshButtonHighlight();
        if (openModal && _modalOverlay != null)
            _modalOverlay.gameObject.SetActive(true);
    }

    private void ApplyPreview(SubstanceCardData data)
    {
        if (_previewTitle != null)
            _previewTitle.text = data.displayName;
        if (_previewFormula != null)
            _previewFormula.text = data.formula;
        if (_previewImage != null)
        {
            Sprite sprite = ResolveMoleculeSprite(data);
            if (sprite != null)
            {
                _previewImage.sprite = sprite;
                _previewImage.color = Color.white;
            }
            else
            {
                _previewImage.sprite = null;
                _previewImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    private void ApplyModal(SubstanceCardData data)
    {
        if (_modalTitle != null)
            _modalTitle.text = data.displayName;
        if (_modalFormula != null)
            _modalFormula.text = data.formula;
        if (_modalImage != null)
        {
            Sprite sprite = ResolveMoleculeSprite(data);
            if (sprite != null)
            {
                _modalImage.sprite = sprite;
                _modalImage.color = Color.white;
            }
            else
            {
                _modalImage.sprite = null;
                _modalImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }
        if (_modalBody != null)
            _modalBody.text = BuildSubstanceBody(data);
    }

    private void CloseModal()
    {
        if (_modalOverlay != null)
            _modalOverlay.gameObject.SetActive(false);
    }

    private void RefreshButtonHighlight()
    {
        for (int i = 0; i < _cardButtons.Count; i++)
        {
            Button button = _cardButtons[i];
            if (button == null)
                continue;
            Image bg = button.GetComponent<Image>();
            if (bg != null)
                bg.color = i == _selectedIndex ? selectedCardColor : cardColor;
        }
    }

    private static string BuildSubstanceBody(SubstanceCardData data)
    {
        StringBuilder builder = new StringBuilder();
        AppendSection(builder, "Resume", data.shortDescription);
        AppendSection(builder, "Risques / HSE", data.hazardSummary);
        AppendSection(builder, "Manipulation", data.handlingNotes);
        if (data.tags != null && data.tags.Length > 0)
            AppendSection(builder, "Mots-cles", string.Join(", ", data.tags));
        return builder.ToString().Trim();
    }

    private static void AppendSection(StringBuilder builder, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return;
        builder.Append("<b><size=22>");
        builder.Append(title);
        builder.Append("</size></b>\n");
        builder.Append(body.Trim());
        builder.Append("\n\n");
    }

    private static Sprite ResolveMoleculeSprite(SubstanceCardData data)
    {
        return ResolveSprite(data, true);
    }

    private static Sprite ResolveCardSprite(SubstanceCardData data)
    {
        return ResolveSprite(data, false);
    }

    private static Sprite ResolveSprite(SubstanceCardData data, bool molecule)
    {
        if (data == null)
            return null;

        Sprite mappedSprite = LoadSpriteFromTextureMap(data);
        if (mappedSprite != null)
            return mappedSprite;

        string preferredAtlasName = molecule ? data.moleculeSpriteName : data.cardSpriteName;
        string fallbackAtlasName = molecule ? data.cardSpriteName : data.moleculeSpriteName;
        Sprite atlasSprite = LoadSpriteFromAtlas(data.atlasResource, preferredAtlasName, fallbackAtlasName);
        if (atlasSprite != null)
            return atlasSprite;

        string preferredDirectPath = molecule ? data.moleculeImageResource : data.cardImageResource;
        string fallbackDirectPath = molecule ? data.cardImageResource : data.moleculeImageResource;
        Sprite directSprite = LoadSpriteSmart(preferredDirectPath);
        if (directSprite != null)
            return directSprite;

        directSprite = LoadSpriteSmart(fallbackDirectPath);
        if (directSprite != null)
            return directSprite;

        RuntimeFileLogger.Warn("HomeSubstancesPanel", "No sprite resolved for substance: " + (string.IsNullOrWhiteSpace(data.id) ? data.displayName : data.id));
        return null;
    }

    private static Sprite LoadSpriteFromTextureMap(SubstanceCardData data)
    {
        if (data == null || data.atlasCellIndex <= 0)
            return null;

        string atlasTextureResource = !string.IsNullOrWhiteSpace(data.atlasTextureResource) ? data.atlasTextureResource : data.atlasResource;
        if (string.IsNullOrWhiteSpace(atlasTextureResource))
            return null;

        int columns = data.atlasColumns > 0 ? data.atlasColumns : 4;
        int cellWidth = data.atlasCellWidth > 0 ? data.atlasCellWidth : 78;
        int cellHeight = data.atlasCellHeight > 0 ? data.atlasCellHeight : 114;
        string cacheKey = atlasTextureResource + "|" + data.atlasCellIndex + "|" + columns + "|" + cellWidth + "|" + cellHeight;

        if (TextureMapCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            return cached;

        Texture2D texture = LoadTextureSmart(atlasTextureResource);
        if (texture == null)
        {
            RuntimeFileLogger.Warn("HomeSubstancesPanel", "Texture map not found: " + atlasTextureResource);
            return null;
        }

        int slot = data.atlasCellIndex - 1;
        int column = slot % columns;
        int row = slot / columns;
        int x = column * cellWidth;
        int y = texture.height - ((row + 1) * cellHeight);
        if (x < 0 || x >= texture.width || y + cellHeight <= 0 || y >= texture.height)
        {
            RuntimeFileLogger.Warn("HomeSubstancesPanel", "Texture map index out of range: id=" + data.id + " index=" + data.atlasCellIndex);
            return null;
        }

        int safeY = Mathf.Max(0, y);
        int width = Mathf.Min(cellWidth, texture.width - x);
        int height = Mathf.Min(cellHeight, texture.height - safeY);
        if (width <= 0 || height <= 0)
            return null;

        Rect tightRect = BuildTightRect(texture, x, safeY, width, height);
        Sprite sprite = Sprite.Create(texture, tightRect, new Vector2(0.5f, 0.5f), 100f);
        if (sprite != null)
        {
            sprite.name = "substance_cell_" + data.atlasCellIndex;
            TextureMapCache[cacheKey] = sprite;
        }
        return sprite;
    }

    private static Rect BuildTightRect(Texture2D source, int x, int y, int width, int height)
    {
        Color[] pixels = TryReadPixels(source, x, y, width, height);
        if (pixels == null || pixels.Length == 0)
            return new Rect(x, y, width, height);

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;
        const float alphaThreshold = 0.02f;

        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                Color c = pixels[(py * width) + px];
                if (c.a <= alphaThreshold)
                    continue;

                if (px < minX) minX = px;
                if (px > maxX) maxX = px;
                if (py < minY) minY = py;
                if (py > maxY) maxY = py;
            }
        }

        if (maxX < minX || maxY < minY)
            return new Rect(x, y, width, height);

        const int padding = 1;
        int tightX = Mathf.Max(0, minX - padding);
        int tightY = Mathf.Max(0, minY - padding);
        int tightW = Mathf.Min(width - tightX, (maxX - minX + 1) + (padding * 2));
        int tightH = Mathf.Min(height - tightY, (maxY - minY + 1) + (padding * 2));
        if (tightW <= 0 || tightH <= 0)
            return new Rect(x, y, width, height);

        return new Rect(x + tightX, y + tightY, tightW, tightH);
    }

    private static Color[] TryReadPixels(Texture2D source, int x, int y, int width, int height)
    {
        if (source == null)
            return null;

        try
        {
            return source.GetPixels(x, y, width, height);
        }
        catch
        {
            // Non-readable import: copy once to a readable temp texture.
            RenderTexture tmp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, tmp);
                RenderTexture.active = tmp;
                Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
                readable.Apply(false, true);
                Color[] pixels = readable.GetPixels(x, y, width, height);
                UnityEngine.Object.Destroy(readable);
                return pixels;
            }
            catch
            {
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);
            }
        }
    }

    private static Sprite LoadSpriteFromAtlas(string atlasResource, string preferredSpriteName, string fallbackSpriteName)
    {
        if (string.IsNullOrWhiteSpace(atlasResource))
            return null;

        string primaryName = NormalizeSpriteName(preferredSpriteName);
        string fallbackName = NormalizeSpriteName(fallbackSpriteName);
        string[] candidates = BuildResourceCandidates(atlasResource);
        for (int i = 0; i < candidates.Length; i++)
        {
            string candidate = candidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            SpriteAtlas atlas = Resources.Load<SpriteAtlas>(candidate);
            if (atlas != null)
            {
                Sprite atlasSprite = GetSpriteFromAtlas(atlas, primaryName);
                if (atlasSprite == null && !string.Equals(primaryName, fallbackName, StringComparison.OrdinalIgnoreCase))
                    atlasSprite = GetSpriteFromAtlas(atlas, fallbackName);
                if (atlasSprite != null)
                    return atlasSprite;
            }

            Sprite[] packed = Resources.LoadAll<Sprite>(candidate);
            if (packed != null && packed.Length > 0)
            {
                Sprite packedSprite = FindSpriteByName(packed, primaryName);
                if (packedSprite == null && !string.Equals(primaryName, fallbackName, StringComparison.OrdinalIgnoreCase))
                    packedSprite = FindSpriteByName(packed, fallbackName);
                if (packedSprite != null)
                    return packedSprite;
            }
        }

        RuntimeFileLogger.Warn("HomeSubstancesPanel", "Atlas not resolved: atlas=" + atlasResource + " sprite=" + preferredSpriteName);
        return null;
    }

    private static Texture2D LoadTextureSmart(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return null;

        string[] candidates = BuildResourceCandidates(resourcePath);
        for (int i = 0; i < candidates.Length; i++)
        {
            string candidate = candidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            Texture2D texture = Resources.Load<Texture2D>(candidate);
            if (texture != null)
                return texture;

            Sprite sprite = Resources.Load<Sprite>(candidate);
            if (sprite != null && sprite.texture != null)
                return sprite.texture;

            Sprite[] allSprites = Resources.LoadAll<Sprite>(candidate);
            if (allSprites != null)
            {
                for (int s = 0; s < allSprites.Length; s++)
                {
                    if (allSprites[s] != null && allSprites[s].texture != null)
                        return allSprites[s].texture;
                }
            }
        }

        return null;
    }

    private static Sprite GetSpriteFromAtlas(SpriteAtlas atlas, string spriteName)
    {
        if (atlas == null)
            return null;

        if (!string.IsNullOrWhiteSpace(spriteName))
        {
            Sprite sprite = atlas.GetSprite(spriteName);
            if (sprite != null)
                return sprite;
        }

        Sprite[] all = new Sprite[Mathf.Max(1, atlas.spriteCount)];
        int count = atlas.GetSprites(all);
        if (count <= 0)
            return null;

        Sprite found = FindSpriteByName(all, spriteName);
        if (found != null)
            return found;

        return all[0];
    }

    private static Sprite FindSpriteByName(Sprite[] sprites, string spriteName)
    {
        if (sprites == null || sprites.Length == 0)
            return null;

        if (string.IsNullOrWhiteSpace(spriteName))
            return sprites[0];

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
                continue;
            if (string.Equals(sprite.name, spriteName, StringComparison.OrdinalIgnoreCase))
                return sprite;
        }

        return null;
    }

    private static string NormalizeSpriteName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        string name = value.Trim().Replace("\\", "/");
        int slash = name.LastIndexOf('/');
        if (slash >= 0 && slash < name.Length - 1)
            name = name.Substring(slash + 1);
        int dot = name.LastIndexOf('.');
        if (dot > 0)
            name = name.Substring(0, dot);
        return name.Trim();
    }

    private static Sprite LoadSpriteSmart(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        string[] candidates = BuildResourceCandidates(path);

        for (int i = 0; i < candidates.Length; i++)
        {
            string candidate = candidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;
            Sprite s = Resources.Load<Sprite>(candidate);
            if (s != null)
                return s;
            Sprite[] all = Resources.LoadAll<Sprite>(candidate);
            if (all != null && all.Length > 0)
                return FindSpriteByName(all, NormalizeSpriteName(path)) ?? all[0];
        }
        return null;
    }

    private static string[] BuildResourceCandidates(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return Array.Empty<string>();

        string trimmed = resourcePath.Trim().Replace("\\", "/");
        int dot = trimmed.LastIndexOf('.');
        string noExt = dot > 0 ? trimmed.Substring(0, dot) : trimmed;
        string fileNoExt = NormalizeSpriteName(trimmed);

        if (trimmed.Contains("/"))
        {
            return new[]
            {
                trimmed,
                noExt
            };
        }

        return new[]
        {
            trimmed,
            noExt,
            "Substances/" + trimmed,
            "Substances/" + noExt,
            "Icons/" + trimmed,
            "Icons/" + noExt,
            fileNoExt
        };
    }

    private Vector2 CalculateCardCellSize(RectTransform viewport, GridLayoutGroup grid)
    {
        float viewportWidth = viewport != null ? viewport.rect.width : 0f;
        if (viewportWidth <= 1f)
        {
            Canvas.ForceUpdateCanvases();
            if (viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
            viewportWidth = viewport != null ? viewport.rect.width : 0f;
        }
        if (viewportWidth <= 1f)
            viewportWidth = _root != null ? _root.rect.width - 12f : 720f;

        int columns = grid != null ? Mathf.Max(1, grid.constraintCount) : 3;
        float gap = Mathf.Clamp(Mathf.Round(viewportWidth * 0.03f), 8f, 22f);
        float minCellWidth = 92f;
        float cellWidth = Mathf.Floor((viewportWidth - (gap * (columns + 1))) / columns);
        if (cellWidth < minCellWidth)
        {
            gap = Mathf.Max(4f, Mathf.Floor((viewportWidth - (minCellWidth * columns)) / (columns + 1)));
            cellWidth = Mathf.Floor((viewportWidth - (gap * (columns + 1))) / columns);
        }
        cellWidth = Mathf.Clamp(cellWidth, 82f, 280f);

        if (grid != null)
        {
            grid.spacing = new Vector2(gap, Mathf.Max(8f, gap * 0.9f));
            float usedWidth = (cellWidth * columns) + (grid.spacing.x * Mathf.Max(0, columns - 1));
            float freeWidth = Mathf.Max(0f, viewportWidth - usedWidth);
            int left = Mathf.FloorToInt(freeWidth * 0.5f);
            int right = Mathf.CeilToInt(freeWidth * 0.5f);
            grid.padding = new RectOffset(left, right, 6, 6);
        }

        float cellHeight = Mathf.Round(cellWidth * 1.06f);
        return new Vector2(cellWidth, cellHeight);
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
        Image i = rt.GetComponent<Image>();
        if (i == null)
        {
            if (rt.GetComponent<CanvasRenderer>() == null)
                rt.gameObject.AddComponent<CanvasRenderer>();
            i = rt.gameObject.AddComponent<Image>();
        }
        return i;
    }

    private static Button EnsureButton(RectTransform rt)
    {
        Button b = rt.GetComponent<Button>();
        if (b == null) b = rt.gameObject.AddComponent<Button>();
        Navigation nav = b.navigation;
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;
        return b;
    }

    private static Text EnsureText(Transform parent, string name, int size, FontStyle style, string value)
    {
        RectTransform rt = EnsureRect(parent, name);
        if (rt.GetComponent<CanvasRenderer>() == null)
            rt.gameObject.AddComponent<CanvasRenderer>();
        Text t = rt.GetComponent<Text>();
        if (t == null) t = rt.gameObject.AddComponent<Text>();
        t.font = UiFontProvider.GetDefaultFont();
        t.fontSize = size;
        t.fontStyle = style;
        t.text = value;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
    }
}
