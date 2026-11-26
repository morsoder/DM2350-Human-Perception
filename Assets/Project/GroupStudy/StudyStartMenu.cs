using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

public sealed class StudyStartMenu : MonoBehaviour
{
    private static readonly string[] GroupLabels =
    {
        "1   Daylight",
        "2   Low-light night",
        "3   Bright night"
    };

    private static readonly Color Background = new Color32(18, 25, 33, 255);
    private static readonly Color Selected = new Color32(36, 78, 105, 255);
    private static readonly Color Unselected = new Color32(32, 44, 56, 255);
    private readonly Image[] rows = new Image[GroupLabels.Length];
    private Text status;
    private int selection;
    private bool stickHeld;
    private bool confirmHeld = true;
    private bool starting;

    public void Initialize(Transform view)
    {
        var menu = new GameObject("Study group selection", typeof(RectTransform), typeof(Canvas));
        var panel = (RectTransform)menu.transform;
        panel.SetParent(view, false);
        panel.localPosition = new Vector3(0f, -0.035f, 1.55f);
        panel.localScale = Vector3.one * 0.0016f;
        panel.sizeDelta = new Vector2(680f, 442f);
        var canvas = menu.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        menu.AddComponent<Image>().color = Background;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        AddText(panel, font, "Select a lighting group", 155f, 610f, 65f, 45, FontStyle.Bold);
        for (int i = 0; i < GroupLabels.Length; i++)
        {
            var row = CreateRect(panel, GroupLabels[i], 65f - i * 76f, 600f, 66f);
            rows[i] = row.gameObject.AddComponent<Image>();
            AddText(row, font, GroupLabels[i], 0f, 570f, 60f, 33, FontStyle.Normal);
        }

        AddText(panel, font, "Left stick  /  A to start   |   Keyboard 1-3", -153f, 620f, 44f, 25, FontStyle.Normal);
        status = AddText(panel, font, "", -191f, 620f, 28f, 21, FontStyle.Normal);
        UpdateSelection();
    }

    private void Update()
    {
        if (status == null || starting) return;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) { StartGroup(0); return; }
            if (keyboard.digit2Key.wasPressedThisFrame) { StartGroup(1); return; }
            if (keyboard.digit3Key.wasPressedThisFrame) { StartGroup(2); return; }
        }

        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out var axis))
        {
            if (Mathf.Abs(axis.y) < 0.3f) stickHeld = false;
            if (!stickHeld && Mathf.Abs(axis.y) > 0.65f)
            {
                stickHeld = true;
                selection = Mathf.Clamp(selection + (axis.y > 0f ? -1 : 1), 0, rows.Length - 1);
                UpdateSelection();
            }
        }

        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        bool pressed = right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out var value) && value;
        if (pressed && !confirmHeld) StartGroup(selection);
        confirmHeld = pressed;
    }

    private void StartGroup(int index)
    {
        selection = index;
        UpdateSelection();
        starting = SceneBucketManager.TryStartStudy(index + 1);
        status.text = starting ? "Loading the first street..." : "This group has no scenes in this build.";
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < rows.Length; i++)
            rows[i].color = i == selection ? Selected : Unselected;
    }

    private static RectTransform CreateRect(Transform parent, string label, float y, float width, float height)
    {
        var rect = (RectTransform)new GameObject(label, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(0f, y);
        return rect;
    }

    private static Text AddText(Transform parent, Font font, string label, float y, float width, float height,
        int size, FontStyle style)
    {
        var text = CreateRect(parent, label.Length > 0 ? label : "Status", y, width, height).gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.text = label;
        return text;
    }
}
