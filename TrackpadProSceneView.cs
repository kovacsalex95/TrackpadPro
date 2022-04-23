using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[EditorWindowTitle(title = "Scene View (TPP)", useTypeNameAsIconName = false)]
public class TrackpadProSceneView : SceneView
{

    // Input button modifier constants

    const int AXIS_MOUSE    =       0;
    const int AXIS_SCROLL   =       1;
    const int BN_COMMAND    =      10;
    const int BN_OPTION     =     100;
    const int BN_SHIFT      =    1000;
    const int BN_CONTROL    =   10000;
    const int MOD_INV_X     =  100000;
    const int MOD_INV_Y     = 1000000;


    // Mode controls

    [SerializeField]
    int controlRotation = AXIS_SCROLL + MOD_INV_X + MOD_INV_Y;

    [SerializeField]
    int controlPan = BN_SHIFT + AXIS_SCROLL + MOD_INV_Y;

    [SerializeField]
    int controlZoom = BN_COMMAND + AXIS_SCROLL;


    // Camera parameters

    [SerializeField]
    float pointerMultiplier = 0.1f;

    [SerializeField]
    float rotateSpeed = 5f;

    [SerializeField]
    float panSpeed = 0.1f;

    [SerializeField]
    float zoomSpeed = 0.7f;

    [SerializeField]
    float zoomMaxDelta = 10f;

    [SerializeField, Range(0.1f, 1f)]
    float zoomRatio = 0.5f;

    [SerializeField, Range(0.00001f, 0.1f)]
    float zoomMin = 0.01f;

    [SerializeField, Range(5000000f, 10000000000f)]
    float zoomMax = 1000000f;


    [MenuItem("Window/Scene View (TPP)")]
    public static void Init()
    {
        GetWindow<TrackpadProSceneView>();
    }


    protected override void OnSceneGUI()
    {
        InputEvents();

        base.OnSceneGUI();

        SettingsGUI();
    }


    int currentControlCode = 0;
    private void UpdateCurrentControlCode()
    {
        currentControlCode = 0;

        if (Event.current.control)
            currentControlCode += BN_CONTROL;

        if (Event.current.shift)
            currentControlCode += BN_SHIFT;

        if (Event.current.alt)
            currentControlCode += BN_OPTION;

        if (Event.current.command)
            currentControlCode += BN_COMMAND;

        if (Event.current.isScrollWheel)
            currentControlCode += AXIS_SCROLL;
        else if (Event.current.isMouse)
            currentControlCode += AXIS_MOUSE;
    }


    private int GetControlModifiers(int controlCode, out float invertX, out float invertY)
    {
        int result = controlCode;
        invertX = 1;
        invertY = 1;

        if (result >= MOD_INV_Y)
        {
            invertY = -1;
            result -= MOD_INV_Y;
        }

        if (result >= MOD_INV_X)
        {
            invertX = -1;
            result -= MOD_INV_X;
        }

        return result;
    }


    private bool CurrentControls(int controlCode, out float invertX, out float invertY)
    {
        return GetControlModifiers(controlCode, out invertX, out invertY) == currentControlCode;
    }


    Vector2 mouseDelta = Vector2.zero;
    private void InputEvents()
    {
        if (!Event.current.isScrollWheel && !Event.current.isMouse)
            return;

        mouseDelta = Event.current.delta * (Event.current.isScrollWheel ? 1f : pointerMultiplier);

        UpdateCurrentControlCode();

        CameraRotation();

        CameraMovement();

        CameraZoom();

        // Prevent Unity to use the scroll event
        if (Event.current.type == EventType.ScrollWheel) Event.current.Use();
        if (Event.current.type == EventType.MouseMove) Event.current.Use();
    }


    private void CameraRotation()
    {
        if (in2DMode)
            return;

        if (!CurrentControls(controlRotation, out float invertX, out float invertY))
            return;

        float rotateY = mouseDelta.x * invertX;
        float rotateX = mouseDelta.y * invertY;

        Quaternion euler = Quaternion.Euler(rotation.eulerAngles + new Vector3(0, rotateY, 0) * rotateSpeed);
        rotation = euler * Quaternion.Euler(new Vector3(rotateX, 0, 0) * rotateSpeed);
    }


    private void CameraMovement()
    {
        float invertX, invertY;
        if (!CurrentControls(controlPan, out invertX, out invertY) &&
            !(in2DMode && CurrentControls(controlRotation, out invertX, out invertY)))
            return;

        float moveX = mouseDelta.x * invertX * panSpeed;
        float moveY = mouseDelta.y * invertY * panSpeed;

        pivot += CameraSpace.TransformVector(size * new Vector3(moveX, moveY, 0));
    }


    private void CameraZoom()
    {
        if (!CurrentControls(controlZoom, out _, out float invertY))
            return;

        float zoomDelta = Mathf.Clamp(mouseDelta.y * zoomSpeed * invertY, -zoomMaxDelta, zoomMaxDelta) / zoomMaxDelta;

        float nextRatio = zoomDelta * zoomRatio + 1f;

        size = Mathf.Clamp(size * nextRatio, zoomMin, zoomMax);
    }


    GameObject cameraDummyObject = null;
    private Transform CameraSpace
    {
        get
        {
            if (cameraDummyObject == null)
            {
                cameraDummyObject = new GameObject("TrackpadPro camera dummy");
                cameraDummyObject.hideFlags = HideFlags.HideInInspector | HideFlags.HideAndDontSave;
            }

            cameraDummyObject.transform.position = pivot;
            cameraDummyObject.transform.rotation = rotation;

            return cameraDummyObject.transform;
        }
    }

    private new void OnDestroy()
    {
        if (cameraDummyObject != null)
        {
            DestroyImmediate(cameraDummyObject);
            cameraDummyObject = null;
        }
    }


    private bool settingsOpened = false;
    private void SettingsGUI()
    {
        if (GUI.Button(settingsOpened ? new Rect(position.width / 2 - 15, 10, 30, 30) : new Rect(position.width - 40, 140, 30, 30), EditorGUIUtility.IconContent(!settingsOpened ? "d_Settings Icon" : "d_winbtn_win_close")))
            settingsOpened = !settingsOpened;

        if (!settingsOpened)
            return;

        float settingsWidth = Mathf.Min(400, position.width - 100);
        float settingsHeight = Mathf.Min(460, position.height - 60);
        float settingsPadding = 20;
        float settingsSpacing = 20;

        Rect settingsPanel = new Rect(position.width / 2 - settingsWidth / 2, position.height / 2 - settingsHeight / 2, settingsWidth, settingsHeight);

        GUI.Box(settingsPanel, ""); GUI.Box(settingsPanel, ""); GUI.Box(settingsPanel, "");

        GUILayout.BeginArea(new Rect(settingsPanel.x + settingsPadding, settingsPanel.y + settingsPadding, settingsPanel.width - settingsPadding * 2, settingsPanel.height - settingsPadding * 2));

        GUILayout.BeginVertical();

        controlRotation = ControlGUISettings(controlRotation, "Rotate");
        rotateSpeed = EditorGUILayout.FloatField("Rotate speed", rotateSpeed);

        GUILayout.Space(settingsSpacing);

        controlPan = ControlGUISettings(controlPan, "Pan");
        panSpeed = EditorGUILayout.FloatField("Pan speed", panSpeed);

        GUILayout.Space(settingsSpacing);

        controlZoom = ControlGUISettings(controlZoom, "Zoom");
        zoomRatio = EditorGUILayout.Slider("Zoom speed", zoomRatio, 0.1f, 1f);
        zoomMin = EditorGUILayout.FloatField("Zoom min", zoomMin);
        zoomMax = EditorGUILayout.FloatField("Zoom max", zoomMax);

        GUILayout.Space(settingsSpacing);

        pointerMultiplier = EditorGUILayout.FloatField("Cursor speed multiplier", pointerMultiplier);

        GUILayout.EndVertical();

        GUILayout.EndArea();
    }


    private int ControlGUISettings(int original, string label)
    {
        int result = original;

        bool invertY = false; bool invertX = false; bool hasControl = false; bool hasShift = false; bool hasOption = false; bool hasCommand = false;

        if (result >= MOD_INV_Y)
        {
            invertY = true;
            result -= MOD_INV_Y;
        }
        if (result >= MOD_INV_X)
        {
            invertX = true;
            result -= MOD_INV_X;
        }
        if (result >= BN_CONTROL)
        {
            hasControl = true;
            result -= BN_CONTROL;
        }
        if (result >= BN_SHIFT)
        {
            hasShift = true;
            result -= BN_SHIFT;
        }
        if (result >= BN_OPTION)
        {
            hasOption = true;
            result -= BN_OPTION;
        }
        if (result >= BN_COMMAND)
        {
            hasCommand = true;
            result -= BN_COMMAND;
        }

        int mouseAxis = result;

        List<string> combination = new List<string>();
        if (hasControl)
            combination.Add("Ctrl");
        if (hasShift)
            combination.Add("Shift");
        if (hasOption)
            combination.Add("Option");
        if (hasCommand)
            combination.Add("Command");
        combination.Add(mouseAxis == 0 ? "Cursor" : "Scroll");

        GUILayout.Label(label.ToUpper());
        GUILayout.Label("      " + string.Join(" + ", combination));

        GUILayout.BeginHorizontal();

        hasControl = ButtonCheckbox(hasControl, "Ctrl");
        hasShift = ButtonCheckbox(hasShift, "Shift");
        hasOption = ButtonCheckbox(hasOption, "Option");
        hasCommand = ButtonCheckbox(hasCommand, "Command");

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(mouseAxis == 0 ? "Cursor movement" : "Scroll movement"))
            mouseAxis = mouseAxis == 0 ? 1 : 0;

        if (GUILayout.Button(invertX ? "-X" : "+X"))
            invertX = !invertX;
        if (GUILayout.Button(invertY ? "-Y" : "+Y"))
            invertY = !invertY;

        GUILayout.EndHorizontal();

        result = 0;

        if (invertY)
            result += MOD_INV_Y;
        if (invertX)
            result += MOD_INV_X;
        if (hasControl)
            result += BN_CONTROL;
        if (hasShift)
            result += BN_SHIFT;
        if (hasOption)
            result += BN_OPTION;
        if (hasCommand)
            result += BN_COMMAND;
        result += mouseAxis;

        return result;
    }


    private bool ButtonCheckbox(bool value, string label)
    {
        if (GUILayout.Button(string.Format("{0}{1}", value ? "[X] " : "", value ? label.ToUpper() : label.ToLower())))
            return !value;

        return value;
    }
}