using UnityEngine;

/// <summary>
/// CẦU NỐI GIAO HÀNG → ĐƠN HÀNG.
///
/// Vấn đề: OrderManager.CheckOrder() có sẵn logic "đúng thì đổi đơn, sai thì giữ"
/// nhưng KHÔNG nơi nào gọi nó. Script này lấp khoảng trống đó.
///
/// CÁCH DÙNG:
///  1. Gắn script này lên vùng giao hàng (lò rèn / khay nộp) — object phải có
///     Collider với Is Trigger = true và 1 Rigidbody (kinematic) để nhận OnTriggerEnter.
///  2. Mỗi KHỐI HÌNH người chơi rèn ra phải có component DeliveredShapeInfo
///     (gắn kèm dưới đây) khai báo tên hình + thể tích.
///  3. Khi người chơi thả khối đúng vào vùng này:
///        - CheckOrder() chấm điểm: đúng hình + đúng thể tích (trong sai số)
///          → OrderManager tự chuyển sang đơn kế tiếp sau 3 giây.
///        - Sai → giữ nguyên đơn, hiện thông báo thất bại trên bảng.
/// </summary>
[RequireComponent(typeof(Collider))]
public class OrderDeliveryBridge : MonoBehaviour
{
    [Tooltip("Ẩn/huỷ khối sau khi giao để tránh giao trùng (giây). <=0 = không huỷ.")]
    public float destroyDelay = 0.2f;

    [Tooltip("Bật log để theo dõi")]
    public bool debugLog = true;

    void Reset()
    {
        // Đảm bảo collider là trigger khi mới gắn
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Lấy thông tin khối được giao (tìm cả ở cha vì collider có thể nằm ở child)
        var info = other.GetComponentInParent<DeliveredShapeInfo>();
        if (info == null) return;

        if (OrderManager.Instance == null)
        {
            Debug.LogWarning("[OrderDeliveryBridge] Không tìm thấy OrderManager.Instance!");
            return;
        }

        if (debugLog)
            Debug.Log($"[OrderDeliveryBridge] Nhận khối '{info.shapeName}' V={info.volume:F2} → chấm điểm đơn.");

        // Đây chính là chỗ quyết định: đúng → đổi đơn, sai → giữ nguyên.
        OrderManager.Instance.CheckOrder(info.volume, info.shapeName);

        if (destroyDelay > 0f)
            Destroy(info.gameObject, destroyDelay);
    }
}
