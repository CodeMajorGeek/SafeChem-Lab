using UnityEngine;

public static class UiFontProvider
{
    private static Font _cachedFont;

    public static Font GetDefaultFont()
    {
        if (_cachedFont != null)
            return _cachedFont;

        _cachedFont = Resources.Load<Font>("Fonts/LiberationSans");
        if (_cachedFont == null)
            _cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 18);
        if (_cachedFont == null)
            _cachedFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 18);

        return _cachedFont;
    }
}
