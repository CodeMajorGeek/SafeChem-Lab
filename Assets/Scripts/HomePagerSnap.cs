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
    [SerializeField] private int startPage = 0; // 0=left, 1=center, 2=right
    [SerializeField] private float snapDuration = 0.35f;
    [Tooltip("Snap back duration when returning to the same page.")]
    [SerializeField] private float snapBackDuration = 0.4f;

    public int CurrentPage { get; private set; }

    /// <summary>Raised when visible page changes (index 0, 1 or 2).</summary>
    public event Action<int> OnPageChanged;

    private Coroutine snapCo;

    private void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void Awake()
    {
        if (!scrollRect) scrollRect = GetComponent<ScrollRect>();
        if (scrollRect)
            scrollRect.inertia = false;
        CurrentPage = Mathf.Clamp(startPage, 0, pageCount - 1);
    }

    private void Start()
    {
        JumpToPage(CurrentPage);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || scrollRect == null)
            return;

        JumpToPage(CurrentPage);
    }

    private void OnDisable()
    {
        if (snapCo != null)
        {
            StopCoroutine(snapCo);
            snapCo = null;
        }
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!this || scrollRect == null) return;

        float pos = scrollRect.horizontalNormalizedPosition;
        int target = pageCount <= 1 ? 0 : Mathf.RoundToInt(pos * (pageCount - 1));
        target = Mathf.Clamp(target, 0, pageCount - 1);
        SnapToPage(target);
    }

    public void SnapToPage(int pageIndex)
    {
        if (!this || scrollRect == null) return;

        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        bool samePage = (CurrentPage == pageIndex);
        CurrentPage = pageIndex;
        OnPageChanged?.Invoke(CurrentPage);

        float target = GetNormalizedTarget(pageIndex);
        if (snapCo != null)
            StopCoroutine(snapCo);

        float duration = samePage ? snapBackDuration : snapDuration;
        snapCo = StartCoroutine(AnimateTo(pageIndex, target, duration));
    }

    public void JumpToPage(int pageIndex)
    {
        if (!this || scrollRect == null) return;

        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        CurrentPage = pageIndex;
        OnPageChanged?.Invoke(CurrentPage);

        float target = GetNormalizedTarget(pageIndex);
        ApplyPagePosition(pageIndex, target);
    }

    private IEnumerator AnimateTo(int pageIndex, float target, float duration)
    {
        if (!this || scrollRect == null) yield break;

        float start = scrollRect.horizontalNormalizedPosition;
        float t = 0f;

        while (t < duration)
        {
            if (!this || scrollRect == null) yield break;

            t += Time.unscaledDeltaTime;
            float x = Mathf.Clamp01(t / duration);
            float k = EaseInOutCubic(x);
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, target, k);
            yield return null;
        }

        if (!this || scrollRect == null) yield break;

        ApplyPagePosition(pageIndex, target);
        snapCo = null;
    }

    private float GetNormalizedTarget(int pageIndex)
    {
        return pageCount <= 1 ? 0f : pageIndex / (float)(pageCount - 1);
    }

    private void ApplyPagePosition(int pageIndex, float normalizedTarget)
    {
        scrollRect.horizontalNormalizedPosition = normalizedTarget;
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
