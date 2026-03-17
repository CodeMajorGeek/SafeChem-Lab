using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MethodCardData
{
    public string id;
    public string title;
    public string subtitle;
    public string shortDescription;
    public string detailedDescription;
    public string safetyNotes;
    public string atlasTextureResource;
    public int atlasX;
    public int atlasY;
    public int atlasWidth;
    public int atlasHeight;
    public string imageResource;
    public string[] tags;
}

public class HomeMethodsPanel : MonoBehaviour
{
    [SerializeField] private string resourcesFolder = "Methods";
    [SerializeField] private string modalOverlayObjectName = "MethodsModalOverlay";
    [SerializeField] private Color cardColor = new Color(0.12f, 0.18f, 0.27f, 0.92f);
    [SerializeField] private Color cardBorderColor = new Color(0.46f, 0.62f, 0.8f, 1f);
    [SerializeField] private Color selectedCardColor = new Color(0.22f, 0.45f, 0.66f, 1f);

    private readonly List<MethodCardData> _methods = new List<MethodCardData>();
    private readonly List<Button> _cardButtons = new List<Button>();
    private static readonly Dictionary<string, Sprite> TextureRectCache = new Dictionary<string, Sprite>();

    private RectTransform _root;
    private RectTransform _modalOverlay;
    private Text _modalTitle;
    private Image _modalImage;
    private Text _modalBody;
    private int _selectedIndex = -1;

    public void Configure(string folder, string overlayObjectName = null)
    {
        if (!string.IsNullOrWhiteSpace(folder))
            resourcesFolder = folder.Trim();
        if (!string.IsNullOrWhiteSpace(overlayObjectName))
            modalOverlayObjectName = overlayObjectName.Trim();
    }

    private void Start()
    {
        try
        {
            LoadMethods();
            BuildUi();
            if (_methods.Count > 0)
                SelectMethod(0, false);
        }
        catch (Exception exception)
        {
            RuntimeFileLogger.Error("HomeMethodsPanel", "Start failed: " + exception);
            Debug.LogException(exception, this);
        }
    }

    private void LoadMethods()
    {
        _methods.Clear();
        TextAsset[] assets = Resources.LoadAll<TextAsset>(resourcesFolder);
        Array.Sort(assets, (a, b) => string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
        RuntimeFileLogger.Log("HomeMethodsPanel", "LoadMethods folder=" + resourcesFolder + " count=" + (assets != null ? assets.Length : 0));

        foreach (TextAsset asset in assets)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                continue;

            MethodCardData data = null;
            try
            {
                data = JsonUtility.FromJson<MethodCardData>(asset.text);
            }
            catch (Exception exception)
            {
                RuntimeFileLogger.Error("HomeMethodsPanel", "Invalid JSON " + asset.name + " - " + exception.Message);
            }

            if (data == null || string.IsNullOrWhiteSpace(data.title))
                continue;

            if (string.IsNullOrWhiteSpace(data.id))
                data.id = asset.name;
            if (asset.name.IndexOf("template", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            _methods.Add(data);
        }
    }

    private void BuildUi()
    {
        RectTransform host = transform as RectTransform;
        if (host == null)
            return;

        _root = EnsureRect(host, "MethodsUI");
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

    private void BuildCardGrid()
    {
        RectTransform frame = EnsureRect(_root, "CardsFrame");
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = Vector2.zero;
        frame.offsetMax = Vector2.zero;
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
        content.sizeDelta = Vector2.zero;

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
        if (_methods.Count == 0)
        {
            RectTransform emptyRt = EnsureRect(content, "Empty");
            LayoutElement le = emptyRt.GetComponent<LayoutElement>();
            if (le == null) le = emptyRt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 480f;
            le.preferredHeight = 120f;
            Image emptyBg = EnsureImage(emptyRt);
            emptyBg.color = new Color(0.15f, 0.2f, 0.3f, 0.92f);
            Text emptyText = EnsureText(emptyRt, "Text", 20, FontStyle.Bold, "Aucune methode chargee");
            emptyText.alignment = TextAnchor.MiddleCenter;
            RectTransform rt = emptyText.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return;
        }

        for (int i = 0; i < _methods.Count; i++)
        {
            MethodCardData data = _methods[i];
            RectTransform card = EnsureRect(content, "Card_" + data.id);
            Image cardBg = EnsureImage(card);
            cardBg.color = cardColor;

            Button button = EnsureButton(card);
            button.targetGraphic = cardBg;
            button.onClick.RemoveAllListeners();
            int index = i;
            button.onClick.AddListener(() => SelectMethod(index, true));
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
            img.preserveAspect = true;
            img.raycastTarget = false;
            Sprite sprite = ResolveSprite(data);
            if (sprite != null)
            {
                img.sprite = sprite;
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

            Text title = EnsureText(labelBandRt, "Title", 14, FontStyle.Bold, data.title);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 12;
            title.resizeTextMaxSize = 16;
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.03f, 0.42f);
            titleRt.anchorMax = new Vector2(0.97f, 0.95f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = Vector2.zero;

            Text subtitle = EnsureText(labelBandRt, "Subtitle", 12, FontStyle.Normal, data.subtitle);
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.color = new Color(0.8f, 0.9f, 1f, 1f);
            RectTransform subRt = subtitle.rectTransform;
            subRt.anchorMin = new Vector2(0.03f, 0.06f);
            subRt.anchorMax = new Vector2(0.97f, 0.4f);
            subRt.pivot = new Vector2(0.5f, 0.5f);
            subRt.anchoredPosition = Vector2.zero;
            subRt.sizeDelta = Vector2.zero;
        }
    }

    private void BuildModalOverlay()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Transform parent = parentCanvas != null ? parentCanvas.transform : _root;

        _modalOverlay = EnsureRect(parent, string.IsNullOrWhiteSpace(modalOverlayObjectName) ? "MethodsModalOverlay" : modalOverlayObjectName);
        _modalOverlay.anchorMin = Vector2.zero;
        _modalOverlay.anchorMax = Vector2.one;
        _modalOverlay.offsetMin = Vector2.zero;
        _modalOverlay.offsetMax = Vector2.zero;
        _modalOverlay.SetAsLastSibling();
        ClearChildren(_modalOverlay);

        Canvas overlayCanvas = _modalOverlay.GetComponent<Canvas>();
        if (overlayCanvas == null) overlayCanvas = _modalOverlay.gameObject.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 260;
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

        _modalTitle = EnsureText(top, "Title", 30, FontStyle.Bold, string.Empty);
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
        Stretch(closeText.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform imageFrame = EnsureRect(modal, "MethodImageFrame");
        imageFrame.anchorMin = new Vector2(0.5f, 1f);
        imageFrame.anchorMax = new Vector2(0.5f, 1f);
        imageFrame.pivot = new Vector2(0.5f, 1f);
        imageFrame.anchoredPosition = new Vector2(0f, -114f);
        imageFrame.sizeDelta = new Vector2(280f, 180f);
        Image imageFrameBg = EnsureImage(imageFrame);
        imageFrameBg.color = new Color(0.86f, 0.8f, 0.66f, 1f);

        RectTransform imageRt = EnsureRect(imageFrame, "Image");
        imageRt.anchorMin = new Vector2(0.5f, 0.5f);
        imageRt.anchorMax = new Vector2(0.5f, 0.5f);
        imageRt.pivot = new Vector2(0.5f, 0.5f);
        imageRt.anchoredPosition = Vector2.zero;
        imageRt.sizeDelta = new Vector2(252f, 152f);
        _modalImage = EnsureImage(imageRt);
        _modalImage.preserveAspect = true;
        _modalImage.raycastTarget = false;

        RectTransform viewport = EnsureRect(modal, "Viewport");
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(24f, 24f);
        viewport.offsetMax = new Vector2(-24f, -312f);
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
        content.sizeDelta = Vector2.zero;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _modalBody = EnsureText(content, "Body", 20, FontStyle.Normal, string.Empty);
        _modalBody.color = new Color(0.17f, 0.12f, 0.08f, 1f);
        _modalBody.alignment = TextAnchor.UpperLeft;
        _modalBody.supportRichText = true;
        Stretch(_modalBody.rectTransform, 0f, 0f, 0f, 0f);

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

    private void SelectMethod(int index, bool openModal)
    {
        if (index < 0 || index >= _methods.Count)
            return;

        _selectedIndex = index;
        MethodCardData method = _methods[index];
        ApplyModal(method);
        RefreshButtonHighlight();
        if (openModal && _modalOverlay != null)
            _modalOverlay.gameObject.SetActive(true);
    }

    private void ApplyModal(MethodCardData method)
    {
        if (_modalTitle != null)
            _modalTitle.text = method.title;

        if (_modalImage != null)
        {
            Sprite sprite = ResolveSprite(method);
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
            _modalBody.text = BuildMethodBody(method);
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

    private void CloseModal()
    {
        if (_modalOverlay != null)
            _modalOverlay.gameObject.SetActive(false);
    }

    private static string BuildMethodBody(MethodCardData data)
    {
        StringBuilder builder = new StringBuilder();
        AppendSection(builder, "Resume", data.shortDescription);
        AppendSection(builder, "Procedure", data.detailedDescription);
        AppendSection(builder, "Securite", data.safetyNotes);
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

    private static Sprite ResolveSprite(MethodCardData data)
    {
        if (data == null)
            return null;

        Sprite fromRect = LoadSpriteFromAtlasRect(data);
        if (fromRect != null)
            return fromRect;

        return LoadSpriteSmart(data.imageResource);
    }

    private static Sprite LoadSpriteFromAtlasRect(MethodCardData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.atlasTextureResource))
            return null;
        if (data.atlasWidth <= 0 || data.atlasHeight <= 0)
            return null;

        string cacheKey = data.atlasTextureResource + "|" + data.atlasX + "|" + data.atlasY + "|" + data.atlasWidth + "|" + data.atlasHeight;
        if (TextureRectCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            return cached;

        Texture2D texture = LoadTextureSmart(data.atlasTextureResource);
        if (texture == null)
            return null;

        int x = Mathf.Clamp(data.atlasX, 0, texture.width - 1);
        int yBottom = texture.height - (data.atlasY + data.atlasHeight);
        yBottom = Mathf.Clamp(yBottom, 0, texture.height - 1);
        int width = Mathf.Min(data.atlasWidth, texture.width - x);
        int height = Mathf.Min(data.atlasHeight, texture.height - yBottom);
        if (width <= 0 || height <= 0)
            return null;

        Rect tight = BuildTightRect(texture, x, yBottom, width, height);
        Sprite sprite = Sprite.Create(texture, tight, new Vector2(0.5f, 0.5f), 100f);
        if (sprite != null)
        {
            sprite.name = data.id + "_atlas_rect";
            TextureRectCache[cacheKey] = sprite;
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
        }

        return null;
    }

    private static Sprite LoadSpriteSmart(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return null;

        string[] candidates = BuildResourceCandidates(resourcePath);
        for (int i = 0; i < candidates.Length; i++)
        {
            string candidate = candidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            Sprite sprite = Resources.Load<Sprite>(candidate);
            if (sprite != null)
                return sprite;
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

        if (trimmed.Contains("/"))
            return new[] { trimmed, noExt };

        return new[]
        {
            trimmed,
            noExt,
            "Methods/" + trimmed,
            "Methods/" + noExt,
            "Icons/" + trimmed,
            "Icons/" + noExt
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

    private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
    }
}
