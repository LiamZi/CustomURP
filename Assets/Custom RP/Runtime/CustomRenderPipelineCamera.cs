using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Camera))]

public class CustomRenderPipelineCamera : MonoBehaviour
{
    [SerializeField]
    CameraSettings _settings = default;
    public CameraSettings Setting => _settings ?? (_settings = new CameraSettings());
};