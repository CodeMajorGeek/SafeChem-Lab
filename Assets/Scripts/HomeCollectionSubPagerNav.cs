using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class HomeCollectionSubPagerNav : MonoBehaviour
{
    [SerializeField] private Button[] pageButtons = new Button[3];
    [SerializeField] private RectTransform[] pages = new RectTransform[3];
    [SerializeField] private Image[] buttonIcons = new Image[3];
    [SerializeField] private string[] iconResourceNames = new string[3];
    [SerializeField] private Color normalColor = new Color(0.03f, 0.06f, 0.1f, 0.98f);
    [SerializeField] private Color highlightColor = new Color(0.22f, 0.45f, 0.66f, 1f);
    [SerializeField] private int startPage = 0;

    public int CurrentPage { get; private set; }

    private void Awake()
    {
        ResolveReferences();
        ApplyIcons();
        GoToPage(Mathf.Clamp(startPage, 0, 2));
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyIcons();
        RefreshState();
    }

    public void Configure(Button[] buttons, RectTransform[] subPages, Image[] icons)
    {
        if (buttons != null && buttons.Length >= 3)
            pageButtons = buttons;
        if (subPages != null && subPages.Length >= 3)
            pages = subPages;
        if (icons != null && icons.Length >= 3)
            buttonIcons = icons;
    }

    public void SetIconResourceNames(string first, string second, string third)
    {
        iconResourceNames[0] = first;
        iconResourceNames[1] = second;
        iconResourceNames[2] = third;
        ApplyIcons();
    }

    public void GoSubPage1() => GoToPage(0);
    public void GoSubPage2() => GoToPage(1);
    public void GoSubPage3() => GoToPage(2);

    public void GoToPage(int index)
    {
        ResolveReferences();
        CurrentPage = Mathf.Clamp(index, 0, 2);
        RefreshState();
    }

    private void RefreshState()
    {
        for (int i = 0; i < 3; i++)
        {
            if (pages != null && i < pages.Length && pages[i] != null)
                pages[i].gameObject.SetActive(i == CurrentPage);

            if (pageButtons != null && i < pageButtons.Length && pageButtons[i] != null)
            {
                Image buttonImage = pageButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                    buttonImage.color = i == CurrentPage ? highlightColor : normalColor;
            }
        }
    }

    private void ApplyIcons()
    {
        for (int i = 0; i < 3; i++)
        {
            if (buttonIcons == null || i >= buttonIcons.Length || buttonIcons[i] == null)
                continue;

            string resourcePath = iconResourceNames != null && i < iconResourceNames.Length ? iconResourceNames[i] : string.Empty;
            if (string.IsNullOrWhiteSpace(resourcePath))
                continue;

            Sprite icon = LoadSprite(resourcePath);
            if (icon != null)
            {
                buttonIcons[i].sprite = icon;
                buttonIcons[i].color = Color.white;
                buttonIcons[i].preserveAspect = true;
            }
            else
            {
                RuntimeFileLogger.Warn("HomeCollectionSubPagerNav", "Icon not found for slot " + (i + 1) + ": " + resourcePath);
            }
        }
    }

    private void ResolveReferences()
    {
        Transform navRow = transform.Find("TopNav/NavRow");
        Transform pagesRoot = transform.Find("Pages");
        if (navRow == null || pagesRoot == null)
            return;

        if (pageButtons == null || pageButtons.Length < 3)
            pageButtons = new Button[3];
        if (pages == null || pages.Length < 3)
            pages = new RectTransform[3];
        if (buttonIcons == null || buttonIcons.Length < 3)
            buttonIcons = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            int slot = i + 1;
            if (pageButtons[i] == null)
                pageButtons[i] = navRow.Find("BtnSub" + slot)?.GetComponent<Button>();
            if (pages[i] == null)
                pages[i] = pagesRoot.Find("SubPage" + slot) as RectTransform;
            if (buttonIcons[i] == null && pageButtons[i] != null)
                buttonIcons[i] = pageButtons[i].transform.Find("Icon")?.GetComponent<Image>();
        }
    }

    private static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        string trimmed = path.Trim();
        string noExt = Path.GetFileNameWithoutExtension(trimmed);

        string[] candidates;
        if (trimmed.Contains("/"))
            candidates = new[] { trimmed, noExt };
        else
            candidates = new[] { trimmed, noExt, "Icons/" + trimmed, "Icons/" + noExt };

        for (int i = 0; i < candidates.Length; i++)
        {
            string candidate = candidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            Sprite sprite = Resources.Load<Sprite>(candidate);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(candidate);
            if (sprites != null && sprites.Length > 0)
                return sprites[0];
        }

        return null;
    }
}
