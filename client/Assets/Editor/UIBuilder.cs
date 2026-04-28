using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class UIBuilder
{
    static TMP_FontAsset _unifiedFont;

    static TMP_FontAsset GetCJKFont()
    {
        const string assetPath = "Assets/Fonts/CJKFont.asset";
        var cjk = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

        // Delete corrupt asset (material sub-asset missing) and recreate.
        if (cjk != null && cjk.material == null)
        {
            AssetDatabase.DeleteAsset(assetPath);
            cjk = null;
        }

        if (cjk != null) return cjk;

        var ttc = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/msjh.ttc");
        if (ttc == null) { Debug.LogError("[UIBuilder] msjh.ttc not found in Assets/Fonts/"); return null; }

        cjk = TMP_FontAsset.CreateFontAsset(ttc, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        cjk.name = "CJKFont";
        AssetDatabase.CreateAsset(cjk, assetPath);

        // Save material and atlas texture as sub-assets so they survive domain reloads.
        if (cjk.material != null)
            AssetDatabase.AddObjectToAsset(cjk.material, cjk);
        foreach (var tex in cjk.atlasTextures)
            if (tex != null) AssetDatabase.AddObjectToAsset(tex, cjk);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return cjk;
    }

    // LiberationSans primary + CJKFont fallback: handles both Latin and Chinese automatically.
    static TMP_FontAsset GetUnifiedFont()
    {
        if (_unifiedFont != null) return _unifiedFont;

        var latin = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var cjk = GetCJKFont();

        if (latin != null && cjk != null)
        {
            if (!latin.fallbackFontAssetTable.Contains(cjk))
            {
                latin.fallbackFontAssetTable.Add(cjk);
                EditorUtility.SetDirty(latin);
                AssetDatabase.SaveAssets();
            }
            _unifiedFont = latin;
        }
        else
        {
            _unifiedFont = latin ?? cjk;
        }

        return _unifiedFont;
    }

    [MenuItem("Tools/Build Leaderboard UI")]
    public static void BuildUI()
    {
        _unifiedFont = null;

        var existing = GameObject.Find("Canvas");
        if (existing != null) Object.DestroyImmediate(existing);

        var existingES = Object.FindFirstObjectByType<EventSystem>();
        if (existingES != null) Object.DestroyImmediate(existingES.gameObject);

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── PlayerPanel (left third) ──────────────────────────
        var playerPanel = MakePanel(canvasGO.transform, "PlayerPanel", 0f, 0f, 0.33f, 1f, new Color(0.08f, 0.18f, 0.55f, 1f));

        var playerNameInputGO = MakeInputField(playerPanel.transform, "PlayerNameInput", "玩家名稱...", 0.05f, 0.80f, 0.95f, 0.90f);
        var scoreInputGO      = MakeInputField(playerPanel.transform, "ScoreInput",      "分數 (0-20000)", 0.05f, 0.67f, 0.95f, 0.77f);
        var submitButtonGO    = MakeButton(playerPanel.transform,    "SubmitButton",    "提交分數",    0.15f, 0.53f, 0.85f, 0.63f);
        var statusTextGO      = MakeText(playerPanel.transform,      "StatusText",      "",            0.05f, 0.38f, 0.95f, 0.51f, 16);
        MakeText(playerPanel.transform, "TitleLabel", "玩家提交", 0.05f, 0.91f, 0.95f, 0.99f, 20);

        var ppScript = playerPanel.AddComponent<PlayerPanel>();
        ppScript.playerNameInput = playerNameInputGO.GetComponent<TMP_InputField>();
        ppScript.scoreInput      = scoreInputGO.GetComponent<TMP_InputField>();
        ppScript.submitButton    = submitButtonGO.GetComponent<Button>();
        ppScript.statusText      = statusTextGO.GetComponent<TMP_Text>();

        // ── OpponentPanel (middle third) ──────────────────────
        var opponentPanel = MakePanel(canvasGO.transform, "OpponentPanel", 0.34f, 0f, 0.66f, 1f, new Color(0.05f, 0.38f, 0.15f, 1f));

        var nameDropdownGO = MakeDropdown(opponentPanel.transform, "NameDropdown", 0.05f, 0.55f, 0.95f, 0.65f);
        MakeText(opponentPanel.transform, "TitleLabel",  "AI 對手",   0.05f, 0.91f, 0.95f, 0.99f, 20);
        MakeText(opponentPanel.transform, "InfoLabel",   "每 5 秒自動送出隨機分數", 0.05f, 0.44f, 0.95f, 0.53f, 14);

        var opScript = opponentPanel.AddComponent<OpponentPanel>();
        opScript.nameDropdown = nameDropdownGO.GetComponent<TMP_Dropdown>();

        // ── LeaderboardPanel (right third) ────────────────────
        var lbPanel = MakePanel(canvasGO.transform, "LeaderboardPanel", 0.67f, 0f, 1f, 1f, new Color(0.48f, 0.06f, 0.06f, 1f));

        MakeText(lbPanel.transform, "TitleLabel", "排行榜 Top 5", 0.05f, 0.91f, 0.95f, 0.99f, 20);

        float[] yMins = { 0.78f, 0.65f, 0.52f, 0.39f, 0.26f };
        float[] yMaxs = { 0.88f, 0.75f, 0.62f, 0.49f, 0.36f };
        var rankTexts = new TMP_Text[5];
        for (int i = 0; i < 5; i++)
        {
            var rGO = MakeText(lbPanel.transform, $"Rank{i + 1}Text", $"#{i + 1} ---", 0.05f, yMins[i], 0.95f, yMaxs[i], 16);
            rankTexts[i] = rGO.GetComponent<TMP_Text>();
        }

        var lbScript = lbPanel.AddComponent<LeaderboardPanel>();
        lbScript.rankTexts = rankTexts;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[UIBuilder] Leaderboard UI built successfully!");
    }

    static GameObject MakePanel(Transform parent, string name, float xMin, float yMin, float xMax, float yMax, Color bg)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = new Vector2(4, 4);
        rt.offsetMax = new Vector2(-4, -4);
        var img = go.AddComponent<Image>();
        img.color = bg;
        return go;
    }

    static GameObject MakeText(Transform parent, string name, string content, float xMin, float yMin, float xMax, float yMax, float fontSize)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        var font = GetUnifiedFont();
        if (font != null) txt.font = font;
        return go;
    }

    static GameObject MakeButton(Transform parent, string name, string label, float xMin, float yMin, float xMax, float yMax)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.55f, 1f);
        go.AddComponent<Button>();

        var lbl = new GameObject("Text");
        lbl.layer = LayerMask.NameToLayer("UI");
        lbl.transform.SetParent(go.transform, false);
        var lrt = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var ltxt = lbl.AddComponent<TextMeshProUGUI>();
        ltxt.text = label;
        ltxt.fontSize = 18;
        ltxt.color = Color.white;
        ltxt.alignment = TextAlignmentOptions.Center;
        var font = GetUnifiedFont(); if (font != null) ltxt.font = font;
        return go;
    }

    static GameObject MakeInputField(Transform parent, string name, string placeholder, float xMin, float yMin, float xMax, float yMax)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);
        var field = go.AddComponent<TMP_InputField>();

        var area = new GameObject("Text Area");
        area.layer = LayerMask.NameToLayer("UI");
        area.transform.SetParent(go.transform, false);
        var art = area.AddComponent<RectTransform>();
        art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
        art.offsetMin = new Vector2(8, 4); art.offsetMax = new Vector2(-8, -4);
        area.AddComponent<RectMask2D>();

        var ph = new GameObject("Placeholder");
        ph.layer = LayerMask.NameToLayer("UI");
        ph.transform.SetParent(area.transform, false);
        var phrt = ph.AddComponent<RectTransform>();
        phrt.anchorMin = Vector2.zero; phrt.anchorMax = Vector2.one;
        phrt.offsetMin = phrt.offsetMax = Vector2.zero;
        var phTxt = ph.AddComponent<TextMeshProUGUI>();
        phTxt.text = placeholder;
        phTxt.fontSize = 16;
        phTxt.color = new Color(1f, 1f, 1f, 0.4f);
        phTxt.alignment = TextAlignmentOptions.MidlineLeft;
        var fi = GetUnifiedFont(); if (fi != null) phTxt.font = fi;

        var txt = new GameObject("Text");
        txt.layer = LayerMask.NameToLayer("UI");
        txt.transform.SetParent(area.transform, false);
        var trt = txt.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tTxt = txt.AddComponent<TextMeshProUGUI>();
        tTxt.fontSize = 16;
        tTxt.color = Color.white;
        tTxt.alignment = TextAlignmentOptions.MidlineLeft;
        var ft = GetUnifiedFont(); if (ft != null) tTxt.font = ft;

        field.textViewport = art;
        field.textComponent = tTxt;
        field.placeholder = phTxt;
        return go;
    }

    static GameObject MakeDropdown(Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        var dd = go.AddComponent<TMP_Dropdown>();

        var lbl = new GameObject("Label");
        lbl.layer = LayerMask.NameToLayer("UI");
        lbl.transform.SetParent(go.transform, false);
        var lrt = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(10, 4); lrt.offsetMax = new Vector2(-30, -4);
        var ltxt = lbl.AddComponent<TextMeshProUGUI>();
        ltxt.text = "Select opponent";
        ltxt.fontSize = 16;
        ltxt.color = Color.white;
        ltxt.alignment = TextAlignmentOptions.MidlineLeft;
        var fd = GetUnifiedFont(); if (fd != null) ltxt.font = fd;
        dd.captionText = ltxt;

        return go;
    }
}
