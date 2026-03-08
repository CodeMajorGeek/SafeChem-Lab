using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InGameBootstrap
{
    private const string SelectedLevelKey = "SelectedLevel";
    private const string PlayerPseudoKey = "PlayerPseudo";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BuildInGameUi()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "InGame", System.StringComparison.Ordinal))
            return;

        if (GameObject.Find("InGameUIRoot") != null)
            return;

        EnsureEventSystem();

        int level = Mathf.Max(1, PlayerPrefs.GetInt(SelectedLevelKey, 1));
        string pseudo = PlayerPrefs.GetString(PlayerPseudoKey, "Joueur");

        GameObject root = new GameObject("InGameUIRoot");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        root.AddComponent<GraphicRaycaster>();
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.13f, 1f);

        BuildTopBar(root.transform, level, pseudo);
        BuildObjectiveCard(root.transform, level);
        BuildPhaseArea(root.transform);
        BuildActionBar(root.transform);
    }

    private static void BuildTopBar(Transform parent, int level, string pseudo)
    {
        RectTransform bar = CreatePanel(parent, "TopBar", new Color(0.08f, 0.13f, 0.2f, 0.95f));
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = new Vector2(0f, 152f);

        CreateText(bar, "SafeChem Lab", 46, FontStyle.Bold, new Vector2(0f, -42f), new Vector2(900f, 70f));
        CreateText(bar, "Niveau " + level + " - " + pseudo, 28, FontStyle.Normal, new Vector2(0f, -102f), new Vector2(900f, 48f));
    }

    private static void BuildObjectiveCard(Transform parent, int level)
    {
        RectTransform card = CreatePanel(parent, "ObjectiveCard", new Color(0.09f, 0.16f, 0.25f, 0.95f));
        card.anchorMin = new Vector2(0.5f, 1f);
        card.anchorMax = new Vector2(0.5f, 1f);
        card.pivot = new Vector2(0.5f, 1f);
        card.anchoredPosition = new Vector2(0f, -180f);
        card.sizeDelta = new Vector2(980f, 210f);

        CreateText(card, "Objectif", 30, FontStyle.Bold, new Vector2(0f, 68f), new Vector2(900f, 44f));
        CreateText(card, "Produire un compose cible en respectant reactifs, procedes et protocoles HSE.", 24, FontStyle.Normal, new Vector2(0f, 18f), new Vector2(900f, 54f));
        CreateText(card, "Niveau actuel: " + level + " (prototype)", 22, FontStyle.Italic, new Vector2(0f, -38f), new Vector2(900f, 44f));
    }

    private static void BuildPhaseArea(Transform parent)
    {
        RectTransform area = CreatePanel(parent, "PhaseArea", new Color(0f, 0f, 0f, 0f));
        area.anchorMin = new Vector2(0.5f, 0.5f);
        area.anchorMax = new Vector2(0.5f, 0.5f);
        area.pivot = new Vector2(0.5f, 0.5f);
        area.anchoredPosition = new Vector2(0f, -80f);
        area.sizeDelta = new Vector2(980f, 980f);

        VerticalLayoutGroup vlg = area.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        BuildPhaseCard(area, "Phase 1 - Reactifs et procedes", "Selectionner les cartes reactifs et les procedes associes.");
        BuildPhaseCard(area, "Phase 2 - Protocoles HSE", "Associer les protocoles de securite pour chaque etape.");
        BuildPhaseCard(area, "Phase 3 - Simulation", "Lancer la simulation et analyser incidents / performance.");
    }

    private static void BuildPhaseCard(Transform parent, string title, string body)
    {
        RectTransform card = CreatePanel(parent, "PhaseCard", new Color(0.09f, 0.15f, 0.23f, 0.95f));
        LayoutElement le = card.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 300f;

        CreateText(card, title, 27, FontStyle.Bold, new Vector2(0f, 98f), new Vector2(900f, 44f));
        CreateText(card, body, 22, FontStyle.Normal, new Vector2(0f, 44f), new Vector2(900f, 60f));

        RectTransform slots = CreatePanel(card, "Slots", new Color(0.12f, 0.19f, 0.29f, 0.92f));
        slots.anchorMin = new Vector2(0.5f, 0f);
        slots.anchorMax = new Vector2(0.5f, 0f);
        slots.pivot = new Vector2(0.5f, 0f);
        slots.anchoredPosition = new Vector2(0f, 14f);
        slots.sizeDelta = new Vector2(900f, 132f);

        HorizontalLayoutGroup h = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.padding = new RectOffset(14, 14, 12, 12);
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;

        for (int i = 0; i < 4; i++)
        {
            RectTransform slot = CreatePanel(slots, "Slot", new Color(0.16f, 0.24f, 0.36f, 1f));
            slot.gameObject.AddComponent<LayoutElement>();
            CreateText(slot, "Carte", 20, FontStyle.Normal, Vector2.zero, new Vector2(160f, 40f));
        }
    }

    private static void BuildActionBar(Transform parent)
    {
        RectTransform bar = CreatePanel(parent, "ActionBar", new Color(0.08f, 0.13f, 0.2f, 0.95f));
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = new Vector2(0f, 170f);

        HorizontalLayoutGroup h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 16f;
        h.padding = new RectOffset(24, 24, 24, 24);
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;

        CreateActionButton(bar, "Doc", new Color(0.2f, 0.32f, 0.46f, 1f), () => { });
        CreateActionButton(bar, "Valider", new Color(0.16f, 0.45f, 0.24f, 1f), () => { });
        CreateActionButton(bar, "Simuler", new Color(0.13f, 0.48f, 0.73f, 1f), () => { });
        CreateActionButton(bar, "Home", new Color(0.36f, 0.16f, 0.16f, 1f), () => SceneManager.LoadScene("Home"));
    }

    private static void CreateActionButton(Transform parent, string caption, Color color, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rt = CreatePanel(parent, "Button", color);
        rt.gameObject.AddComponent<LayoutElement>();

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = rt.GetComponent<Image>();
        button.onClick.AddListener(onClick);

        CreateText(rt, caption, 26, FontStyle.Bold, Vector2.zero, new Vector2(190f, 46f));
    }

    private static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    private static void CreateText(Transform parent, string value, int size, FontStyle style, Vector2 pos, Vector2 sz)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sz;

        Text txt = go.AddComponent<Text>();
        txt.font = UiFontProvider.GetDefaultFont();
        txt.text = value;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}
