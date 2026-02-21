using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HomePagerSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Refs")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Settings")]
    [SerializeField] private int pageCount = 3;
    [SerializeField] private int startPage = 1;          // 0=gauche,1=centre,2=droite
    [SerializeField] private float snapDuration = 0.20f; // animation
    [SerializeField] private float swipeThreshold = 0.12f; // 0..1 normalized

    public int CurrentPage { get; private set; }

    /// <summary>Invoqué quand la page affichée change (index 0, 1 ou 2).</summary>
    public event Action<int> OnPageChanged;

    private float dragStartPos;
    private Coroutine snapCo;

    private void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void Awake()
    {
        if (!scrollRect) scrollRect = GetComponent<ScrollRect>();
        CurrentPage = Mathf.Clamp(startPage, 0, pageCount - 1);
    }

    private void Start()
    {
        JumpToPage(CurrentPage);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPos = scrollRect.horizontalNormalizedPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float endPos = scrollRect.horizontalNormalizedPosition;
        float delta = endPos - dragStartPos;

        int target = CurrentPage;

        if (Mathf.Abs(delta) > swipeThreshold)
            target += (delta > 0f) ? 1 : -1;
        else
            target = Mathf.RoundToInt(endPos * (pageCount - 1));

        target = Mathf.Clamp(target, 0, pageCount - 1);
        SnapToPage(target);
    }

    public void SnapToPage(int pageIndex)
    {
        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        if (CurrentPage == pageIndex)
            return;
        CurrentPage = pageIndex;
        OnPageChanged?.Invoke(CurrentPage);

        float target = (pageCount <= 1) ? 0f : pageIndex / (float)(pageCount - 1);

        if (snapCo != null)
            StopCoroutine(snapCo);

        snapCo = StartCoroutine(AnimateTo(target, snapDuration));
    }

    public void JumpToPage(int pageIndex)
    {
        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        CurrentPage = pageIndex;
        OnPageChanged?.Invoke(CurrentPage);

        float target = (pageCount <= 1) ? 0f : pageIndex / (float)(pageCount - 1);
        scrollRect.horizontalNormalizedPosition = target;
    }

    private IEnumerator AnimateTo(float target, float duration)
    {
        float start = scrollRect.horizontalNormalizedPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, target, k);
            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = target;
        snapCo = null;
    }
}
