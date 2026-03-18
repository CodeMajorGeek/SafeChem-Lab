using UnityEngine;

public struct SafeAreaInsets
{
    public float left;
    public float right;
    public float top;
    public float bottom;
}

public static class MobileScreenUtility
{
    public static void ForcePortraitOrientation()
    {
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.orientation = ScreenOrientation.Portrait;
    }

    public static SafeAreaInsets GetSafeAreaInsets(RectTransform referenceRect)
    {
        SafeAreaInsets insets = default;
        if (referenceRect == null)
            return insets;

        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);
        Rect safeArea = Screen.safeArea;
        Rect canvasRect = referenceRect.rect;
        if (canvasRect.width <= 0f || canvasRect.height <= 0f)
            return insets;

        insets.left = canvasRect.width * (safeArea.xMin / screenWidth);
        insets.right = canvasRect.width * ((screenWidth - safeArea.xMax) / screenWidth);
        insets.bottom = canvasRect.height * (safeArea.yMin / screenHeight);
        insets.top = canvasRect.height * ((screenHeight - safeArea.yMax) / screenHeight);
        return insets;
    }
}
