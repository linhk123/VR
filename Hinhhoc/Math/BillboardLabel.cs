using UnityEngine;

/// <summary>
/// Luôn quay text/label về phía camera. Dùng chung cho VertexLabel, EdgeLabel, MeasurementTool.
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    // ★ FIX: cache Camera.main — gọi mỗi LateUpdate gây O(n) tìm tag MainCamera.
    private Transform _camCache;

    void LateUpdate()
    {
        if (_camCache == null)
        {
            Camera c = Camera.main;
            if (c == null) return;
            _camCache = c.transform;
        }

        // ★ FIX: billboard chuẩn — text "nhìn" về phía camera (không phải cùng hướng camera nhìn).
        // Việc LookAt(pos + cam.forward) khiến chữ luôn bị nghiêng khi camera nhìn cạnh.
        transform.rotation = Quaternion.LookRotation(
            transform.position - _camCache.position,
            _camCache.up);
    }
}
