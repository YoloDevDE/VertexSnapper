using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertexSnapper.Managers;

namespace VertexSnapper.States;

public class StateIdle : IVertexSnapperState<Components.VertexSnapper>
{
    private readonly KeyCode _vertexKey = VertexSnapperConfigManager.VertexKeyBind.Value;

    private RectTransform _activeTutorialBox;
    public Components.VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyDown[_vertexKey] += ChangeStateToSelectOriginVertex;
    }

    public void Exit()
    {
        KeyInputManager.OnKeyDown[_vertexKey] -= ChangeStateToSelectOriginVertex;
    }

    public void Update() { }

    private void ChangeStateToSelectOriginVertex()
    {
        if (VertexSnapper.LevelEditorCentral.selection.list.Count <= 0 || VertexSnapper.LevelEditorCentral.validation.amountOfBlocks < 2)
        {
            return;
        }

        RectTransform[] component = VertexSnapper.LevelEditorCentral.inspector.GetComponentsInChildren<RectTransform>();
        RectTransform inspectorContentBox = VertexSnapper.LevelEditorCentral.inspector.contentBox;
        foreach (RectTransform rectTransform in component)
        {
            if (!rectTransform.gameObject.name.Contains("Inspector"))
            {
                rectTransform.gameObject.SetActive(false);
            }
        }


        ShowTutorialInInspector(inspectorContentBox); // Call this where you want to show the tutorial (e.g., before ChangeState)
        VertexSnapper.ChangeState(new StateSetFirstVertex());
    }

    // Call when leaving the tutorial/state to restore original inspector
    private void HideTutorialAndRestore(RectTransform originalContentBox)
    {
        if (_activeTutorialBox)
        {
            Object.Destroy(_activeTutorialBox.gameObject);
            _activeTutorialBox = null;
        }

        originalContentBox.gameObject.SetActive(true);
    }

    // Call this where you want to show the tutorial (e.g., before ChangeState)
    private void ShowTutorialInInspector(RectTransform originalContentBox)
    {
        // Hide non-inspector parts (as you already do elsewhere)
        // ...

        // Clone the content box
        RectTransform tutorialBox = Object.Instantiate(originalContentBox, originalContentBox.parent);
        tutorialBox.name = originalContentBox.name + "_TutorialCopy";

        // Clear children in the clone so it's empty
        for (int i = tutorialBox.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(tutorialBox.GetChild(i).gameObject);
        }

        // Ensure it stretches like the original
        RectTransform rt = tutorialBox;
        rt.anchorMin = originalContentBox.anchorMin;
        rt.anchorMax = originalContentBox.anchorMax;
        rt.offsetMin = originalContentBox.offsetMin;
        rt.offsetMax = originalContentBox.offsetMax;
        rt.pivot = originalContentBox.pivot;

        // Optional: add a simple vertical layout to stack text blocks
        VerticalLayoutGroup layout = tutorialBox.GetComponent<VerticalLayoutGroup>() ?? tutorialBox.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 6;
        layout.padding = new RectOffset(8, 8, 8, 8);

        ContentSizeFitter fitter = tutorialBox.GetComponent<ContentSizeFitter>() ?? tutorialBox.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Add a header
        AddTMPText(tutorialBox, "Vertex Snapper Tutorial", 20, FontStyles.Bold);

        // Add body text
        AddTMPText(tutorialBox,
            "1) Select at least two blocks in the scene.\n" +
            "2) Press the Vertex key to pick the first vertex.\n" +
            "3) Pick the target vertex to snap.\n" +
            "Tip: Hold Shift to refine selection."
            , 16, FontStyles.Normal);

        // Optionally add a footer or button
        AddTMPText(tutorialBox, "Press the Vertex key again to start.", 14, FontStyles.Italic);

        // Hide original content box so only the tutorial shows
        originalContentBox.gameObject.SetActive(false);

        // Store reference so you can restore later
        _activeTutorialBox = tutorialBox;
        tutorialBox.gameObject.GetComponentInParent<Transform>().gameObject.SetActive(true);
    }

    // Utility to add a TMP text element
    private void AddTMPText(Transform parent, string text, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject("TMP_Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI textComp = go.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = fontSize;
        textComp.fontStyle = style;
        textComp.enableWordWrapping = true;
        textComp.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        // Stretch horizontally
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, 0);
    }
}