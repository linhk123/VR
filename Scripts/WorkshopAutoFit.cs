using UnityEngine;

public class WorkshopAutoFit : MonoBehaviour
{
    [Header("Parent chứa tường + sàn (KHÔNG chứa vật bên trong)")]
    public Transform wallsRoot;

    [Header("Kích thước xưởng tham chiếu (lúc design, khi scale=1)")]
    public Vector2 referenceSize = new Vector2(2.5f, 2.5f);

    [Header("Giới hạn scale")]
    public float minScale = 0.7f;
    public float maxScale = 1.5f;

    [Header("Padding (tường ảo nhỏ hơn Guardian một chút)")]
    public float paddingMeters = 0.2f;

    void Start()
    {
        Invoke(nameof(FitToGuardian), 1f);   // đợi OpenXR khởi tạo
    }

    void FitToGuardian()
    {
        if (!OVRManager.boundary.GetConfigured())
        {
            Debug.LogWarning("[WorkshopAutoFit] Guardian chưa thiết lập, dùng kích thước mặc định.");
            return;
        }

        Vector3 dim = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
        float availableX = dim.x - paddingMeters * 2;
        float availableZ = dim.z - paddingMeters * 2;

        float scaleX = availableX / referenceSize.x;
        float scaleZ = availableZ / referenceSize.y;
        float scale = Mathf.Clamp(Mathf.Min(scaleX, scaleZ), minScale, maxScale);

        wallsRoot.localScale = new Vector3(scale, 1f, scale);   // chỉ scale X+Z, giữ Y (chiều cao tường)
        Debug.Log($"[WorkshopAutoFit] Guardian {dim.x:F1}×{dim.z:F1}m → tường scale {scale:F2}");
    }
}