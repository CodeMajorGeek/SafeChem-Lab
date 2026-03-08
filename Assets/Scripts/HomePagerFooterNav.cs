using UnityEngine;
using UnityEngine.UI;

public class HomePagerFooterNav : MonoBehaviour
{
    [SerializeField] private HomePagerSnap pager;
    [SerializeField] private Button[] pageButtons = new Button[3];
    [SerializeField] private Color normalColor = new Color(0.03f, 0.06f, 0.1f, 0.98f);
    [SerializeField] private Color highlightColor = new Color(0.22f, 0.45f, 0.66f, 1f);

    private void Awake()
    {
        ResolveReferences();
        RefreshHighlight();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (pager != null)
            pager.OnPageChanged += OnPageChanged;
        RefreshHighlight();
    }

    private void OnDisable()
    {
        if (pager != null)
            pager.OnPageChanged -= OnPageChanged;
    }

    public void GoDoc()
    {
        GoToPage(0);
    }

    public void GoProgression()
    {
        GoToPage(1);
    }

    public void GoCollection()
    {
        GoToPage(2);
    }

    public void GoToPage(int pageIndex)
    {
        ResolveReferences();
        if (pager == null)
            return;

        pager.SnapToPage(pageIndex);
        RefreshHighlight();
    }

    private void OnPageChanged(int _)
    {
        RefreshHighlight();
    }

    private void ResolveReferences()
    {
        if (pager == null)
            pager = FindFirstObjectByType<HomePagerSnap>();

        bool needsButtons = pageButtons == null || pageButtons.Length < 3;
        if (!needsButtons)
        {
            for (int i = 0; i < 3; i++)
            {
                if (pageButtons[i] == null)
                {
                    needsButtons = true;
                    break;
                }
            }
        }

        if (!needsButtons)
            return;

        Transform footerRow = transform.Find("FooterRow");
        if (footerRow == null)
            return;

        pageButtons = new Button[3];
        pageButtons[0] = footerRow.Find("BtnDoc")?.GetComponent<Button>();
        pageButtons[1] = footerRow.Find("BtnProgression")?.GetComponent<Button>();
        pageButtons[2] = footerRow.Find("BtnCollection")?.GetComponent<Button>();
    }

    private void RefreshHighlight()
    {
        int currentPage = pager != null ? pager.CurrentPage : 1;

        if (pageButtons == null)
            return;

        for (int i = 0; i < pageButtons.Length; i++)
        {
            Button button = pageButtons[i];
            if (button == null)
                continue;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = i == currentPage ? highlightColor : normalColor;
        }
    }
}
