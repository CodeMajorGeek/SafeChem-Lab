using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class HomePagerPageSizer : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform[] pages;

    private void OnEnable() => Apply();
    private void OnRectTransformDimensionsChange() => Apply();

    [ContextMenu("Apply")]
    public void Apply()
    {
        ResolveReferences();
        if (!viewport || pages == null) return;

        Vector2 size = viewport.rect.size;
        if (size.x <= 0f || size.y <= 0f) return;

        int activePageCount = 0;
        foreach (var p in pages)
        {
            if (!p) continue;
            activePageCount++;
            p.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            p.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        if (content)
        {
            int count = Mathf.Max(1, activePageCount);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x * count);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
    }

    private void ResolveReferences()
    {
        ScrollRect scroll = GetComponent<ScrollRect>();
        if (!scroll) scroll = GetComponentInParent<ScrollRect>();

        if (!viewport && scroll)
            viewport = scroll.viewport;

        if (!content && scroll)
            content = scroll.content;
    }
}
