using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Synchronise le footer avec la page affichée : highlight du bouton actif et navigation au clic.
/// À attacher sur chaque Footer (un par page) ou sur un objet qui contient les 3 boutons.
/// </summary>
public class HomePagerFooterNav : MonoBehaviour
{
    [Header("Pager")]
    [SerializeField] private HomePagerSnap pager;

    [Header("Boutons (ordre: Doc, Progression, Collection)")]
    [SerializeField] private Button[] pageButtons = new Button[3];

    [Header("Highlight (optionnel)")]
    [SerializeField] private Image[] buttonBackgrounds;
    [SerializeField] private Color normalColor = new Color(0.22f, 0.25f, 0.3f, 0.9f);
    [SerializeField] private Color highlightColor = new Color(0.35f, 0.5f, 0.6f, 1f);

    [Header("Labels (optionnel, sinon utilisés par défaut)")]
    [SerializeField] private string[] labelTexts = new[]
    {
        "Documentation",
        "Progression",
        "Collection"
    };

    private void Awake()
    {
        if (!pager)
            pager = FindFirstObjectByType<HomePagerSnap>();
    }

    private void OnEnable()
    {
        if (pager != null)
            pager.OnPageChanged += RefreshHighlight;

        for (int i = 0; i < pageButtons.Length && i < 3; i++)
        {
            int index = i;
            if (pageButtons[i] != null)
            {
                pageButtons[i].onClick.RemoveAllListeners();
                pageButtons[i].onClick.AddListener(() => GoToPage(index));
            }
        }

        ApplyLabels();
        RefreshHighlight(pager != null ? pager.CurrentPage : 0);
    }

    private void OnDisable()
    {
        if (pager != null)
            pager.OnPageChanged -= RefreshHighlight;
    }

    private void ApplyLabels()
    {
        for (int i = 0; i < pageButtons.Length && i < labelTexts.Length; i++)
        {
            if (pageButtons[i] == null) continue;
            var tmp = pageButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (tmp != null && !string.IsNullOrEmpty(labelTexts[i]))
                tmp.text = labelTexts[i];
        }
    }

    private void RefreshHighlight(int currentPage)
    {
        if (buttonBackgrounds != null && buttonBackgrounds.Length >= 3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (buttonBackgrounds[i] != null)
                    buttonBackgrounds[i].color = (i == currentPage) ? highlightColor : normalColor;
            }
            return;
        }

        for (int i = 0; i < pageButtons.Length && i < 3; i++)
        {
            if (pageButtons[i] == null) continue;
            var img = pageButtons[i].targetGraphic as Image;
            if (img != null)
                img.color = (i == currentPage) ? highlightColor : normalColor;
        }
    }

    private void GoToPage(int pageIndex)
    {
        if (pager != null)
            pager.SnapToPage(pageIndex);
    }
}
