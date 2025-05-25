using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OnCameraUpdate : MonoBehaviour
{
    /// <summary>
    /// Kamera g�ncellendi�inde tetiklenen event.
    /// </summary>
    public event Action OnCameraUpdated;

    // �nceki frame de�erleri
    private Vector3 _prevPosition;
    private Quaternion _prevRotation;
    private float _prevFov;
    private float _prevNearClip;
    private float _prevFarClip;

    // De�i�im alg�lama i�in float e�ik de�eri
    private const float Epsilon = 0.0001f;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();

        // Ba�lang��ta referans de�erlerini al
        _prevPosition = _cam.transform.position;
        _prevRotation = _cam.transform.rotation;
        _prevFov = _cam.fieldOfView;
        _prevNearClip = _cam.nearClipPlane;
        _prevFarClip = _cam.farClipPlane;
    }

    private void LateUpdate()
    {
        bool changed = false;

        // 1) Position kontrol�
        if ((_cam.transform.position - _prevPosition).sqrMagnitude > Epsilon * Epsilon)
        {
            changed = true;
            _prevPosition = _cam.transform.position;
        }

        // 2) Rotation kontrol�
        if (Quaternion.Angle(_cam.transform.rotation, _prevRotation) > Epsilon)
        {
            changed = true;
            _prevRotation = _cam.transform.rotation;
        }

        // 3) Field of View kontrol�
        if (Mathf.Abs(_cam.fieldOfView - _prevFov) > Epsilon)
        {
            changed = true;
            _prevFov = _cam.fieldOfView;
        }

        // 4) Near clip plane kontrol�
        if (Mathf.Abs(_cam.nearClipPlane - _prevNearClip) > Epsilon)
        {
            changed = true;
            _prevNearClip = _cam.nearClipPlane;
        }

        // 5) Far clip plane kontrol�
        if (Mathf.Abs(_cam.farClipPlane - _prevFarClip) > Epsilon)
        {
            changed = true;
            _prevFarClip = _cam.farClipPlane;
        }

        // Herhangi bir de�erde de�i�me olduysa event tetikle
        if (changed)
        {
            OnCameraUpdated?.Invoke();
        }
    }
}
