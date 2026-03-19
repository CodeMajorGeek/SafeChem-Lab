using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class HomePagerPageSizer : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform[] pages;
    [SerializeField] private bool autoDiscoverPages = true;

    private Vector2 _lastViewportSize = new Vector2(-1f, -1f);

    private void OnEnable() => Apply();
    private void OnRectTransformDimensionsChange() => Apply();

    private void LateUpdate()
    {
        ResolveReferences();
        if (viewport == null)
            return;

        Vector2 size = viewport.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            return;
        if (Mathf.Abs(size.x - _lastViewportSize.x) < 0.5f && Mathf.Abs(size.y - _lastViewportSize.y) < 0.5f)
            return;

        Apply();
    }

    [ContextMenu("Apply")]
    public void Apply()
    {
        ResolveReferences();
        if (viewport == null || pages == null)
            return;

        Vector2 size = viewport.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            return;
        _lastViewportSize = size;

        int activePageCount = 0;
        for (int i = 0; i < pages.Length; i++)
        {
            RectTransform page = pages[i];
            if (page == null)
                continue;

            activePageCount++;
            page.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            page.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            LayoutElement pageLayout = page.GetComponent<LayoutElement>();
            if (pageLayout == null)
                pageLayout = page.gameObject.AddComponent<LayoutElement>();
            pageLayout.minWidth = size.x;
            pageLayout.preferredWidth = size.x;
            pageLayout.flexibleWidth = 0f;
            pageLayout.minHeight = size.y;
            pageLayout.preferredHeight = size.y;
            pageLayout.flexibleHeight = 0f;
        }

        if (content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
    }

    private void ResolveReferences()
    {
        ScrollRect scroll = GetComponent<ScrollRect>();
        if (scroll == null)
            scroll = GetComponentInParent<ScrollRect>();

        if (viewport == null && scroll != null)
            viewport = scroll.viewport;
        if (content == null && scroll != null)
            content = scroll.content;

        if ((pages == null || pages.Length == 0) && autoDiscoverPages && content != null)
        {
            pages = new RectTransform[content.childCount];
            for (int i = 0; i < content.childCount; i++)
                pages[i] = content.GetChild(i) as RectTransform;
        }
    }
}
