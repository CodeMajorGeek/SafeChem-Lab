using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DocumentationSectionData
{
    public string heading;
    [TextArea] public string body;
}

[Serializable]
public class DocumentationEntryData
{
    public string id;
    public string title;
    public string category;
    public string shortDescription;
    public string whyItMatters;
    public DocumentationSectionData[] sections;
    public string[] keywords;
}

public class HomeDocumentationPanel : MonoBehaviour
{
    [SerializeField] private string resourcesFolder = "documentation-datas";
    [SerializeField] private Color surfaceColor = new Color(0.03f, 0.06f, 0.1f, 0.22f);
    [SerializeField] private Color cardColor = new Color(0.94f, 0.9f, 0.8f, 0.96f);
    [SerializeField] private Color buttonColor = new Color(0.92f, 0.87f, 0.76f, 0.98f);
    [SerializeField] private Color accentColor = new Color(0.49f, 0.31f, 0.14f, 1f);

    private readonly List<DocumentationEntryData> _documents = new List<DocumentationEntryData>();
    private readonly List<Button> _buttons = new List<Button>();

    private RectTransform _root;
    private RectTransform _listContent;
    private RectTransform _detailOverlay;
    private RectTransform _canvasRoot;
    private Text _detailTitle;
    private Text _detailMeta;
    private Text _detailBody;

    private void Start()
    {
        RuntimeFileLogger.Log("HomeDocumentationPanel", "Start on " + gameObject.name);
        try
        {
            LoadDocuments();
            BuildUi();
        }
        catch (Exception exception)
        {
            RuntimeFileLogger.Error("HomeDocumentationPanel", "Start failed: " + exception);
            Debug.LogException(exception, this);
        }
    }

    private void LoadDocuments()
    {
        _documents.Clear();
        TextAsset[] assets = Resources.LoadAll<TextAsset>(resourcesFolder);
        RuntimeFileLogger.Log("HomeDocumentationPanel", "LoadDocuments folder=" + resourcesFolder + " count=" + (assets != null ? assets.Length : 0));
        Array.Sort(assets, (a, b) => string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));

        foreach (TextAsset asset in assets)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                continue;

            DocumentationEntryData entry = null;
            try
            {
                entry = JsonUtility.FromJson<DocumentationEntryData>(asset.text);
            }
            catch (Exception exception)
            {
                Debug.LogError("Documentation JSON invalide: " + asset.name + " - " + exception.Message, this);
                RuntimeFileLogger.Error("HomeDocumentationPanel", "Invalid JSON " + asset.name + " - " + exception.Message);
            }

            if (entry == null || string.IsNullOrWhiteSpace(entry.title))
                continue;

            if (string.IsNullOrWhiteSpace(entry.id))
                entry.id = asset.name;

            _documents.Add(entry);
            RuntimeFileLogger.Log("HomeDocumentationPanel", "Loaded doc id=" + entry.id + " title=" + entry.title);
        }

        _documents.Sort((a, b) => string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase));
        RuntimeFileLogger.Log("HomeDocumentationPanel", "Sorted documents count=" + _documents.Count);
    }

    private void BuildUi()
    {
        RectTransform body = transform as RectTransform;
        if (body == null)
        {
            RuntimeFileLogger.Error("HomeDocumentationPanel", "BuildUi aborted: transform is not RectTransform");
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        _canvasRoot = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        _root = EnsureRect(body, "DocumentationUI");
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = new Vector2(24f, 28f);
        _root.offsetMax = new Vector2(-24f, -18f);
        _root.SetAsLastSibling();
        ClearChildren(_root);
        LayoutElement rootLayout = EnsureLayoutElement(_root);
        rootLayout.ignoreLayout = true;

        Image rootBg = EnsureImage(_root);
        rootBg.color = surfaceColor;
        rootBg.raycastTarget = false;
        RuntimeFileLogger.Log(
            "HomeDocumentationPanel",
            "BuildUi bodyRect=" + body.rect.width + "x" + body.rect.height + " rootOffsets min=" + _root.offsetMin + " max=" + _root.offsetMax);

        _listContent = EnsureRect(_root, "DocButtons");
        _listContent.anchorMin = new Vector2(0f, 0f);
        _listContent.anchorMax = new Vector2(1f, 1f);
        _listContent.offsetMin = new Vector2(0f, 0f);
        _listContent.offsetMax = new Vector2(0f, 0f);
        RuntimeFileLogger.Log("HomeDocumentationPanel", "DocButtons container ready");

        BuildButtons();
        BuildDetailOverlay();
        RuntimeFileLogger.Log("HomeDocumentationPanel", "BuildUi completed");
    }

    private void BuildButtons()
    {
        _buttons.Clear();
        if (_listContent == null)
        {
            RuntimeFileLogger.Warn("HomeDocumentationPanel", "BuildButtons aborted: list content missing");
            return;
        }

        for (int i = _listContent.childCount - 1; i >= 0; i--)
            Destroy(_listContent.GetChild(i).gameObject);
        RuntimeFileLogger.Log("HomeDocumentationPanel", "Cleared previous button children");

        if (_documents.Count == 0)
        {
            RuntimeFileLogger.Warn("HomeDocumentationPanel", "No documents available for buttons");
            RectTransform emptyRt = EnsureRect(_listContent, "EmptyState");
            emptyRt.anchorMin = new Vector2(0f, 1f);
            emptyRt.anchorMax = new Vector2(1f, 1f);
            emptyRt.pivot = new Vector2(0.5f, 1f);
            emptyRt.anchoredPosition = new Vector2(0f, -8f);
            emptyRt.sizeDelta = new Vector2(0f, 120f);
            Image emptyBg = EnsureImage(emptyRt);
            emptyBg.color = cardColor;
            Text emptyText = EnsureText(emptyRt, "Text", 18, FontStyle.Normal, "Aucune fiche chargee.\nAjoute des JSON dans Resources/" + resourcesFolder + ".");
            emptyText.alignment = TextAnchor.MiddleCenter;
            Stretch(emptyText.rectTransform, 18f, 18f, 18f, 18f);
            return;
        }

        for (int i = 0; i < _documents.Count; i++)
        {
            DocumentationEntryData entry = _documents[i];
            RectTransform buttonRt = EnsureRect(_listContent, "DocButton_" + entry.id);
            buttonRt.anchorMin = new Vector2(0f, 1f);
            buttonRt.anchorMax = new Vector2(1f, 1f);
            buttonRt.pivot = new Vector2(0.5f, 1f);
            buttonRt.anchoredPosition = new Vector2(0f, -8f - (i * 88f));
            buttonRt.sizeDelta = new Vector2(0f, 72f);

            Image bg = EnsureImage(buttonRt);
            bg.color = buttonColor;

            Button button = EnsureButton(buttonRt);
            button.targetGraphic = bg;
            ConfigureButtonColors(button);
            button.onClick.RemoveAllListeners();
            int index = i;
            button.onClick.AddListener(() => OpenDocument(index));
            RuntimeFileLogger.Log("HomeDocumentationPanel", "Created button for index=" + i + " title=" + entry.title + " pos=" + buttonRt.anchoredPosition);

            Text title = EnsureText(buttonRt, "Title", 22, FontStyle.Bold, "Documentation " + entry.title);
            title.alignment = TextAnchor.MiddleLeft;
            title.color = Color.white;
            Stretch(title.rectTransform, 22f, 22f, 8f, 8f);

            _buttons.Add(button);
        }
    }

    private void BuildDetailOverlay()
    {
        Transform overlayParent = _canvasRoot != null ? _canvasRoot : _root;
        _detailOverlay = EnsureRect(overlayParent, "DocumentationOverlay");
        _detailOverlay.anchorMin = Vector2.zero;
        _detailOverlay.anchorMax = Vector2.one;
        _detailOverlay.offsetMin = Vector2.zero;
        _detailOverlay.offsetMax = Vector2.zero;
        _detailOverlay.SetAsLastSibling();
        ClearChildren(_detailOverlay);
        Canvas overlayCanvas = _detailOverlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
            overlayCanvas = _detailOverlay.gameObject.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 200;

        GraphicRaycaster overlayRaycaster = _detailOverlay.GetComponent<GraphicRaycaster>();
        if (overlayRaycaster == null)
            overlayRaycaster = _detailOverlay.gameObject.AddComponent<GraphicRaycaster>();

        Image overlayBg = EnsureImage(_detailOverlay);
        overlayBg.color = new Color(0.02f, 0.03f, 0.05f, 0.62f);

        Button overlayButton = EnsureButton(_detailOverlay);
        overlayButton.targetGraphic = overlayBg;
        overlayButton.onClick.RemoveAllListeners();
        overlayButton.onClick.AddListener(CloseDocument);

        RectTransform modal = EnsureRect(_detailOverlay, "DetailModal");
        modal.anchorMin = new Vector2(0.035f, 0.035f);
        modal.anchorMax = new Vector2(0.965f, 0.965f);
        modal.offsetMin = Vector2.zero;
        modal.offsetMax = Vector2.zero;
        Image modalBg = EnsureImage(modal);
        modalBg.color = new Color(0.96f, 0.92f, 0.84f, 0.995f);

        RectTransform topBar = EnsureRect(modal, "TopBar");
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.anchoredPosition = Vector2.zero;
        topBar.sizeDelta = new Vector2(0f, 94f);
        Image topBarBg = EnsureImage(topBar);
        topBarBg.color = new Color(0.36f, 0.24f, 0.11f, 1f);

        _detailTitle = EnsureText(topBar, "Title", 30, FontStyle.Bold, string.Empty);
        _detailTitle.alignment = TextAnchor.MiddleLeft;
        _detailTitle.color = new Color(0.98f, 0.96f, 0.91f, 1f);
        Stretch(_detailTitle.rectTransform, 16f, 120f, 12f, 10f);

        RectTransform closeRt = EnsureRect(topBar, "BtnClose");
        closeRt.anchorMin = new Vector2(1f, 0.5f);
        closeRt.anchorMax = new Vector2(1f, 0.5f);
        closeRt.pivot = new Vector2(1f, 0.5f);
        closeRt.anchoredPosition = new Vector2(-14f, 0f);
        closeRt.sizeDelta = new Vector2(122f, 52f);
        Image closeBg = EnsureImage(closeRt);
        closeBg.color = new Color(0.74f, 0.64f, 0.47f, 1f);
        Button closeButton = EnsureButton(closeRt);
        closeButton.targetGraphic = closeBg;
        ConfigureButtonColors(closeButton);
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(CloseDocument);
        Text closeText = EnsureText(closeRt, "Text", 19, FontStyle.Bold, "Fermer");
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = new Color(0.2f, 0.14f, 0.08f, 1f);
        Stretch(closeText.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform metaRt = EnsureRect(modal, "Meta");
        metaRt.anchorMin = new Vector2(0f, 1f);
        metaRt.anchorMax = new Vector2(1f, 1f);
        metaRt.pivot = new Vector2(0.5f, 1f);
        metaRt.anchoredPosition = new Vector2(0f, -108f);
        metaRt.sizeDelta = new Vector2(0f, 44f);
        _detailMeta = EnsureText(metaRt, "Text", 17, FontStyle.Italic, string.Empty);
        _detailMeta.color = new Color(0.36f, 0.24f, 0.11f, 1f);
        _detailMeta.alignment = TextAnchor.MiddleLeft;
        Stretch(_detailMeta.rectTransform, 18f, 18f, 0f, 0f);

        RectTransform viewport = EnsureRect(modal, "Viewport");
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(24f, 24f);
        viewport.offsetMax = new Vector2(-24f, -176f);
        Image viewportBg = EnsureImage(viewport);
        viewportBg.color = new Color(0.98f, 0.95f, 0.88f, 1f);
        Mask mask = viewport.GetComponent<Mask>();
        if (mask == null)
            mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = EnsureRect(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _detailBody = EnsureText(content, "Body", 20, FontStyle.Normal, string.Empty);
        _detailBody.alignment = TextAnchor.UpperLeft;
        _detailBody.color = new Color(0.17f, 0.12f, 0.08f, 1f);
        _detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detailBody.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(_detailBody.rectTransform, 0f, 0f, 0f, 0f);

        ScrollRect detailScroll = modal.GetComponent<ScrollRect>();
        if (detailScroll == null)
            detailScroll = modal.gameObject.AddComponent<ScrollRect>();
        detailScroll.horizontal = false;
        detailScroll.vertical = true;
        detailScroll.movementType = ScrollRect.MovementType.Clamped;
        detailScroll.inertia = true;
        detailScroll.scrollSensitivity = 20f;
        detailScroll.viewport = viewport;
        detailScroll.content = content;

        modalBg.raycastTarget = true;
        _detailOverlay.gameObject.SetActive(false);
        RuntimeFileLogger.Log("HomeDocumentationPanel", "Detail overlay ready");
    }

    private void OpenDocument(int index)
    {
        if (index < 0 || index >= _documents.Count)
        {
            RuntimeFileLogger.Warn("HomeDocumentationPanel", "OpenDocument ignored, invalid index=" + index);
            return;
        }

        DocumentationEntryData entry = _documents[index];
        if (_detailTitle != null)
            _detailTitle.text = entry.title;
        if (_detailMeta != null)
            _detailMeta.text = BuildMetaLine(entry);
        if (_detailBody != null)
            _detailBody.text = BuildBody(entry);
        if (_detailOverlay != null)
            _detailOverlay.gameObject.SetActive(true);

        RefreshButtonState(index);
        RuntimeFileLogger.Log("HomeDocumentationPanel", "Opened document index=" + index + " title=" + entry.title);
    }

    private void CloseDocument()
    {
        if (_detailOverlay != null)
            _detailOverlay.gameObject.SetActive(false);
        RefreshButtonState(-1);
        RuntimeFileLogger.Log("HomeDocumentationPanel", "Closed document overlay");
    }

    private void RefreshButtonState(int activeIndex)
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            Image image = _buttons[i] != null ? _buttons[i].GetComponent<Image>() : null;
            if (image != null)
                image.color = i == activeIndex ? new Color(0.82f, 0.72f, 0.54f, 1f) : buttonColor;
        }
    }

    private void ConfigureButtonColors(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.88f, 0.77f, 1f);
        colors.pressedColor = new Color(0.84f, 0.74f, 0.56f, 1f);
        colors.selectedColor = new Color(0.9f, 0.8f, 0.62f, 1f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
    }

    private static string BuildMetaLine(DocumentationEntryData entry)
    {
        StringBuilder builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(entry.category))
            builder.Append(entry.category);

        if (entry.keywords != null && entry.keywords.Length > 0)
        {
            if (builder.Length > 0)
                builder.Append("  |  ");
            builder.Append(string.Join(", ", entry.keywords));
        }

        return builder.ToString();
    }

    private static string BuildBody(DocumentationEntryData entry)
    {
        StringBuilder builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(entry.shortDescription))
        {
            builder.Append("## Resume");
            builder.Append("\n");
            builder.Append(entry.shortDescription.Trim());
            builder.Append("\n\n");
        }

        if (!string.IsNullOrWhiteSpace(entry.whyItMatters))
        {
            builder.Append("## Pourquoi c'est utile\n");
            builder.Append(entry.whyItMatters.Trim());
            builder.Append("\n\n");
        }

        if (entry.sections != null)
        {
            for (int i = 0; i < entry.sections.Length; i++)
            {
                DocumentationSectionData section = entry.sections[i];
                if (section == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(section.heading))
                {
                    builder.Append("## ");
                    builder.Append(section.heading.Trim());
                    builder.Append("\n");
                }

                if (!string.IsNullOrWhiteSpace(section.body))
                {
                    builder.Append(section.body.Trim());
                    builder.Append("\n\n");
                }
            }
        }

        return ConvertSimpleMarkdownToRichText(builder.ToString().Trim());
    }

    private static string ConvertSimpleMarkdownToRichText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string normalized = value.Replace("\r\n", "\n");
        string[] lines = normalized.Split('\n');
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.Trim();

            if (trimmed.StartsWith("### "))
            {
                builder.Append("<b><size=18>");
                builder.Append(EscapeRichText(trimmed.Substring(4)));
                builder.Append("</size></b>");
            }
            else if (trimmed.StartsWith("## "))
            {
                builder.Append("<b><size=22>");
                builder.Append(EscapeRichText(trimmed.Substring(3)));
                builder.Append("</size></b>");
            }
            else if (trimmed.StartsWith("# "))
            {
                builder.Append("<b><size=26>");
                builder.Append(EscapeRichText(trimmed.Substring(2)));
                builder.Append("</size></b>");
            }
            else
            {
                builder.Append(ApplyInlineMarkdown(EscapeRichText(line)));
            }

            if (i < lines.Length - 1)
                builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string ApplyInlineMarkdown(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string result = Regex.Replace(value, @"\*\*(.+?)\*\*", "<b>$1</b>");
        result = Regex.Replace(result, @"__(.+?)__", "<b>$1</b>");
        return result;
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

    private static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing as RectTransform ?? existing.gameObject.AddComponent<RectTransform>();

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static Image EnsureImage(RectTransform rt)
    {
        Image image = rt.GetComponent<Image>();
        if (image == null)
        {
            if (rt.GetComponent<CanvasRenderer>() == null)
                rt.gameObject.AddComponent<CanvasRenderer>();
            image = rt.gameObject.AddComponent<Image>();
        }

        return image;
    }

    private static LayoutElement EnsureLayoutElement(RectTransform rt)
    {
        LayoutElement element = rt.GetComponent<LayoutElement>();
        if (element == null)
            element = rt.gameObject.AddComponent<LayoutElement>();
        return element;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
    }

    private static Button EnsureButton(RectTransform rt)
    {
        Button button = rt.GetComponent<Button>();
        if (button == null)
            button = rt.gameObject.AddComponent<Button>();
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        return button;
    }

    private static Text EnsureText(Transform parent, string name, int fontSize, FontStyle style, string value)
    {
        RectTransform rt = EnsureRect(parent, name);
        if (rt.GetComponent<CanvasRenderer>() == null)
            rt.gameObject.AddComponent<CanvasRenderer>();
        Text text = rt.GetComponent<Text>();
        if (text == null)
            text = rt.gameObject.AddComponent<Text>();
        text.font = UiFontProvider.GetDefaultFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.text = value;
        text.supportRichText = true;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
