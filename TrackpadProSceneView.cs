using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[EditorWindowTitle(icon = "d_ViewToolMove", title = "Scene TPP", useTypeNameAsIconName = false)]
public class TrackpadProSceneView : SceneView
{

    // Control input buttons, axes and modifiers

    const int AXIS_MOUSE    =        0;
    const int AXIS_SCROLL   =        1;
    const int BN_COMMAND    =      100;
    const int BN_OPTION     =     1000;
    const int BN_SHIFT      =    10000;
    const int BN_CONTROL    =   100000;
    const int MOD_INV_X     =  1000000;
    const int MOD_INV_Y     = 10000000;


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

    [SerializeField]
    float zoomRatio = 0.5f;

    [SerializeField]
    float zoomMin = 0.01f;

    [SerializeField]
    float zoomMax = 1000000f;


    // Private fields

    [System.NonSerialized]
    int currentControlCode = 0;
    [System.NonSerialized]
    Vector2 mouseDelta = Vector2.zero;
    [System.NonSerialized]
    GameObject cameraDummyObject = null;
    [System.NonSerialized]
    private bool settingsOpened = false;
    [SerializeField]
    private bool settingsLoaded = false;


    [MenuItem("Window/Scene View (TPP)")]
    public static void Init()
    {
        GetWindow<TrackpadProSceneView>();
    }


    protected override void OnSceneGUI()
    {
        if (!settingsLoaded)
            LoadSettings();

        InputEvents();

        base.OnSceneGUI();

        SettingsGUI();

        if (settingsOpened)
            SaveSettings();
    }


    private void UpdateCurrentControlCode()
    {
        currentControlCode = 0;

        if (Event.current.control) currentControlCode += BN_CONTROL;

        if (Event.current.shift) currentControlCode += BN_SHIFT;

        if (Event.current.alt) currentControlCode += BN_OPTION;

        if (Event.current.command) currentControlCode += BN_COMMAND;

        if (Event.current.isScrollWheel) currentControlCode += AXIS_SCROLL;
        else if (Event.current.isMouse) currentControlCode += AXIS_MOUSE;
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


    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("TPP_controlRotation")) controlRotation = PlayerPrefs.GetInt("TPP_controlRotation");
        if (PlayerPrefs.HasKey("TPP_controlPan")) controlPan = PlayerPrefs.GetInt("TPP_controlPan");
        if (PlayerPrefs.HasKey("TPP_controlZoom")) controlZoom = PlayerPrefs.GetInt("TPP_controlZoom");
        if (PlayerPrefs.HasKey("TPP_rotateSpeed")) rotateSpeed = PlayerPrefs.GetFloat("TPP_rotateSpeed");
        if (PlayerPrefs.HasKey("TPP_panSpeed")) panSpeed = PlayerPrefs.GetFloat("TPP_panSpeed");
        if (PlayerPrefs.HasKey("TPP_zoomRatio")) zoomRatio = PlayerPrefs.GetFloat("TPP_zoomRatio");
        if (PlayerPrefs.HasKey("TPP_zoomMin")) zoomMin = PlayerPrefs.GetFloat("TPP_zoomMin");
        if (PlayerPrefs.HasKey("TPP_zoomMax")) zoomMax = PlayerPrefs.GetFloat("TPP_zoomMax");
        if (PlayerPrefs.HasKey("TPP_pointerMultiplier")) pointerMultiplier = PlayerPrefs.GetFloat("TPP_pointerMultiplier");

        settingsLoaded = true;
    }


    private void SaveSettings()
    {
        PlayerPrefs.SetInt("TPP_controlRotation", controlRotation);
        PlayerPrefs.SetInt("TPP_controlPan", controlPan);
        PlayerPrefs.SetInt("TPP_controlZoom", controlZoom);
        PlayerPrefs.SetFloat("TPP_rotateSpeed", rotateSpeed);
        PlayerPrefs.SetFloat("TPP_panSpeed", panSpeed);
        PlayerPrefs.SetFloat("TPP_zoomRatio", zoomRatio);
        PlayerPrefs.SetFloat("TPP_zoomMin", zoomMin);
        PlayerPrefs.SetFloat("TPP_zoomMax", zoomMax);
        PlayerPrefs.SetFloat("TPP_pointerMultiplier", pointerMultiplier);
    }


    private void SettingsGUI()
    {
        if (GUI.Button(settingsOpened ? new Rect(position.width / 2 - 15, 10, 30, 30) : new Rect(position.width - 40, 140, 30, 30), EditorGUIUtility.IconContent(!settingsOpened ? "d_Settings Icon" : "d_winbtn_win_close")))
            settingsOpened = !settingsOpened;

        if (!settingsOpened)
            return;

        float settingsWidth = Mathf.Min(300, position.width - 100);
        float settingsHeight = Mathf.Min(500, position.height - 60);
        float settingsPadding = 20;
        float settingsSpacing = 20;

        Rect settingsPanel = new Rect(position.width / 2 - settingsWidth / 2, position.height / 2 - settingsHeight / 2, settingsWidth, settingsHeight);
        Rect settingsInner = new Rect(settingsPanel.x + settingsPadding, settingsPanel.y + settingsPadding, settingsPanel.width - settingsPadding * 2, settingsPanel.height - settingsPadding * 2);

        GUI.Box(settingsPanel, ""); GUI.Box(settingsPanel, ""); GUI.Box(settingsPanel, "");

        GUILayout.BeginArea(settingsInner);

        GUILayout.BeginVertical();

        controlRotation = ControlGUISettings(controlRotation, "Rotate", settingsInner.width - 20);
        rotateSpeed = EditorGUILayout.FloatField("Rotate speed", rotateSpeed);

        GUILayout.Space(settingsSpacing);

        controlPan = ControlGUISettings(controlPan, "Pan", settingsInner.width - 20);
        panSpeed = EditorGUILayout.FloatField("Pan speed", panSpeed);

        GUILayout.Space(settingsSpacing);

        controlZoom = ControlGUISettings(controlZoom, "Zoom", settingsInner.width - 20);
        zoomRatio = EditorGUILayout.Slider("Zoom speed", zoomRatio, 0.1f, 1f);
        zoomMin = EditorGUILayout.FloatField("Zoom min", zoomMin);
        zoomMax = EditorGUILayout.FloatField("Zoom max", zoomMax);

        GUILayout.Space(settingsSpacing);

        pointerMultiplier = EditorGUILayout.FloatField("Cursor speed multiplier", pointerMultiplier);

        GUILayout.EndVertical();

        GUILayout.EndArea();
    }


    private int ControlGUISettings(int original, string label, float width = 220)
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
        if (hasControl) combination.Add("Ctrl");
        if (hasShift) combination.Add("Shift");
        if (hasOption) combination.Add("Option");
        if (hasCommand) combination.Add("Command");
        combination.Add(mouseAxis == AXIS_MOUSE ? "Cursor" : "Scroll");


        GUILayout.Label(label.ToUpper() + ": " + string.Join(" + ", combination));


        GUILayout.BeginHorizontal();

        hasControl = ButtonCheckbox(hasControl, "Ctrl", width / 4);
        hasShift = ButtonCheckbox(hasShift, "Shift", width / 4);
        hasOption = ButtonCheckbox(hasOption, "Option", width / 4);
        hasCommand = ButtonCheckbox(hasCommand, "Command", width / 4);
        bool forcedScroll = !hasControl && !hasShift && !hasOption && !hasCommand;
        if (forcedScroll)
            mouseAxis = AXIS_SCROLL;

        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(forcedScroll);
        if (GUILayout.Button(mouseAxis == AXIS_MOUSE ? "Cursor movement" : "Scroll movement"))
            mouseAxis = mouseAxis == AXIS_MOUSE ? AXIS_SCROLL : AXIS_MOUSE;
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button(invertX ? "-X" : "+X")) invertX = !invertX;
        if (GUILayout.Button(invertY ? "-Y" : "+Y")) invertY = !invertY;

        GUILayout.EndHorizontal();


        result = 0;
        if (invertY) result += MOD_INV_Y;
        if (invertX) result += MOD_INV_X;
        if (hasControl) result += BN_CONTROL;
        if (hasShift) result += BN_SHIFT;
        if (hasOption) result += BN_OPTION;
        if (hasCommand) result += BN_COMMAND;
        result += mouseAxis;

        return result;
    }


    private bool ButtonCheckbox(bool value, string label, float width = 55)
    {
        bool result = value;

        GUILayout.BeginVertical();

        GUILayout.Label(label, GUILayout.Width(width));

        if (GUILayout.Button(EditorGUIUtility.IconContent(result ? "Button Icon" : "DotSelection"), GUILayout.Height(30)))
            result = !result;

        GUILayout.EndVertical();

        return result;
    }
}