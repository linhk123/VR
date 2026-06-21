using UnityEngine;

/// <summary>
/// Gắn lên mỗi KHỐI HÌNH người chơi có thể giao cho lò rèn.
/// Khai báo tên hình (khớp với OrderData.requiredShapeName: Cube/Cylinder/Sphere/Cone/Pyramid)
/// và thể tích hiện tại (m³) để OrderDeliveryBridge chấm điểm đơn hàng.
///
/// GỢI Ý tính thể tích:
///  - Nếu khối được scale đồng đều từ kích thước gốc, có thể cập nhật volume theo lossyScale.
///  - Hoặc gán cứng đúng thể tích đáp án nếu bạn chỉ cần "đúng hình là đạt".
/// </summary>
[DisallowMultipleComponent]
public class DeliveredShapeInfo : MonoBehaviour
{
    [Tooltip("Tên hình — phải khớp requiredShapeName trong OrderData (Cube/Cylinder/Sphere/Cone/Pyramid)")]
    public string shapeName = "Cube";

    [Tooltip("Thể tích hiện tại của khối (m³)")]
    public float volume = 1f;

    [Header("Tự tính thể tích theo scale (tuỳ chọn)")]
    [Tooltip("Nếu bật: volume = baseVolume × (lossyScale.x³). Phù hợp khi scale đồng đều.")]
    public bool autoComputeFromScale = false;

    [Tooltip("Thể tích khi lossyScale = 1")]
    public float baseVolume = 1f;

    void Awake()
    {
        if (autoComputeFromScale) RecomputeVolume();
    }

    /// <summary>Gọi lại sau khi người chơi phóng to/thu nhỏ khối.</summary>
    public void RecomputeVolume()
    {
        float s = transform.lossyScale.x;
        volume = baseVolume * s * s * s;
    }
}
