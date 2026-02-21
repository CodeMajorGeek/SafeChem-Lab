using UnityEngine;

[ExecuteAlways]
public class HomePagerPageSizer : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform[] pages;

    private void OnEnable() => Apply();
    private void OnRectTransformDimensionsChange() => Apply();

    [ContextMenu("Apply")]
    public void Apply()
    {
        if (!viewport || pages == null) return;

        Vector2 size = viewport.rect.size;
        if (size.x <= 0f || size.y <= 0f) return;

        foreach (var p in pages)
        {
            if (!p) continue;
            p.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            p.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }
    }
}
