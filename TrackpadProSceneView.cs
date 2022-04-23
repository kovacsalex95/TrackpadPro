using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[EditorWindowTitle(title = "Scene View (TPP)", useTypeNameAsIconName = false)]
public class TrackpadProSceneView : SceneView
{

    // Input button modifier constants

    const int BN_NONE       =     0;
    const int BN_COMMAND    =    10;
    const int BN_OPTION     =   100;
    const int BN_SHIFT      =  1000;
    const int BN_CONTROL    = 10000;


    // Mode controls

    [SerializeField]
    int controlRotation = BN_NONE;

    [SerializeField]
    int controlPan = BN_COMMAND;

    [SerializeField]
    int controlZoom = BN_OPTION;


    // Camera parameters

    [SerializeField]
    float rotateSpeed = 3f;

    [SerializeField]
    float panSpeed = 0.1f;

    [SerializeField]
    float zoomSpeed = 0.7f;

    [SerializeField]
    float zoomMaxDelta = 10f;

    [SerializeField, Range(0.1f, 1f)]
    float zoomRatio = 0.3f;

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
    }


    private void InputEvents()
    {
        if (!Event.current.isScrollWheel)
            return;

        UpdateCurrentControlCode();

        CameraRotation();

        CameraMovement();

        CameraZoom();

        // Prevent Unity to use the scroll event
        if (Event.current.type == EventType.ScrollWheel) Event.current.Use();
    }


    private void CameraRotation()
    {
        if (in2DMode)
            return;

        if (currentControlCode != controlRotation)
            return;

        float rotateY = -Event.current.delta.x;
        float rotateX = -Event.current.delta.y;

        Quaternion euler = Quaternion.Euler(rotation.eulerAngles + new Vector3(0, rotateY, 0) * rotateSpeed);
        rotation = euler * Quaternion.Euler(new Vector3(rotateX, 0, 0) * rotateSpeed);
    }


    private void CameraMovement()
    {
        if (currentControlCode != controlPan && !(in2DMode && currentControlCode == controlRotation))
            return;

        float moveX = Event.current.delta.x * panSpeed;
        float moveY = -Event.current.delta.y * panSpeed;

        pivot += CameraSpace.TransformVector(size * new Vector3(moveX, moveY, 0));
    }


    private void CameraZoom()
    {
        if (currentControlCode != controlZoom)
            return;

        float zoomDelta = Mathf.Clamp(Event.current.delta.y * zoomSpeed, -zoomMaxDelta, zoomMaxDelta) / zoomMaxDelta;

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
        if (GUI.Button(new Rect(position.width - 40, 140, 30, 30), EditorGUIUtility.IconContent(!settingsOpened ? "d_Settings Icon" : "d_winbtn_win_close")))
            settingsOpened = !settingsOpened;

        if (!settingsOpened)
            return;

        Rect settingsPanel = new Rect(position.width - 310, 190, 300, 220);
        GUI.Box(settingsPanel, "");
        GUILayout.BeginArea(new Rect(settingsPanel.x + 10, settingsPanel.y + 10, settingsPanel.width - 20, settingsPanel.height - 20));

        GUILayout.BeginVertical();

        controlRotation = ControlGUISettings(controlRotation, "Rotate");

        GUILayout.Space(10);

        controlPan = ControlGUISettings(controlPan, "Pan");

        GUILayout.Space(10);

        controlZoom = ControlGUISettings(controlZoom, "Zoom");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }


    private int ControlGUISettings(int original, string label)
    {
        int result = 0;

        char[] characters = original.ToString().ToCharArray();
        char[] bits = new char[characters.Length];
        for (int i = 0; i < characters.Length; i++)
            bits[characters.Length - 1 - i] = characters[i];

        bool hasControl = original >= BN_CONTROL && bits[4] == '1';
        bool hasShift = original >= BN_SHIFT && bits[3] == '1';
        bool hasOption = original >= BN_OPTION && bits[2] == '1';
        bool hasCommand = original >= BN_COMMAND && bits[1] == '1';

        List<string> combination = new List<string>();
        if (hasControl)
            combination.Add("Ctrl");
        if (hasShift)
            combination.Add("Shift");
        if (hasOption)
            combination.Add("Option");
        if (hasCommand)
            combination.Add("Command");
        if (combination.Count == 0)
            combination.Add("-");

        GUILayout.Label(label.ToUpper());
        GUILayout.Label("      " + string.Join(" + ", combination));

        GUILayout.BeginHorizontal();

        hasControl = ButtonCheckbox(hasControl, "Ctrl");
        hasShift = ButtonCheckbox(hasShift, "Shift");
        hasOption = ButtonCheckbox(hasOption, "Option");
        hasCommand = ButtonCheckbox(hasCommand, "Command");

        GUILayout.EndHorizontal();

        if (hasControl)
            result += BN_CONTROL;
        if (hasShift)
            result += BN_SHIFT;
        if (hasOption)
            result += BN_OPTION;
        if (hasCommand)
            result += BN_COMMAND;

        return result;
    }


    private bool ButtonCheckbox(bool value, string label)
    {
        if (GUILayout.Button(string.Format("{0}{1}", value ? "[X] " : "", value ? label.ToUpper() : label.ToLower())))
            return !value;

        return value;
    }
}