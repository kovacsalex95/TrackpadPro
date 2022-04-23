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

        // TODO: Settings GUI
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
}