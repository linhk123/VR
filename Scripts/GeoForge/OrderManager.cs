using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("=== DANH SÁCH ĐƠN HÀNG ===")]
    [Tooltip("Kéo 5 file OrderData asset vào theo thứ tự")]
    public List<OrderData> orders = new List<OrderData>();

    [Header("=== KÍCH HOẠT ===")]
    [Tooltip("Nếu bật: Đơn 1 tự load khi Start. Tắt nếu chờ Hexa intro xong.")]
    public bool autoStartFirstOrder = false;

    [Header("=== TRẠNG THÁI ===")]
    [SerializeField] private int currentOrderIndex = 0;
    [SerializeField] private int totalGold = 0;
    [SerializeField] private int totalCompleted = 0;
    [SerializeField] private bool isActive = false;

    [Header("=== SỰ KIỆN ===")]
    public UnityEvent<OrderData> onOrderChanged;
    public UnityEvent<OrderData, float> onOrderSuccess;
    public UnityEvent<OrderData, float, string> onOrderFail;
    public UnityEvent onAllOrdersCompleted;
    public UnityEvent onSystemActivated;

    public OrderData CurrentOrder =>
        (isActive && orders != null && currentOrderIndex < orders.Count) ? orders[currentOrderIndex] : null;

    public int TotalGold => totalGold;
    public int CompletedCount => totalCompleted;
    public int CurrentOrderNumber => currentOrderIndex + 1;
    public int TotalOrders => orders.Count;
    public bool IsActive => isActive;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (autoStartFirstOrder)
            ActivateOrderSystem();
    }

    public void ActivateOrderSystem()
    {
        if (isActive) return;
        if (orders.Count == 0)
        {
            Debug.LogWarning("[OrderManager] Không có đơn nào!");
            return;
        }

        isActive = true;
        currentOrderIndex = 0;
        Debug.Log($"<color=green>[OrderManager] KÍCH HOẠT - Bắt đầu đơn 1/{orders.Count}: {orders[0].productName}</color>");

        onSystemActivated?.Invoke();
        onOrderChanged?.Invoke(CurrentOrder);
    }

    public void CheckOrder(float measuredVolume, string shapeName)
    {
        if (!isActive)
        {
            Debug.LogWarning("[OrderManager] Hệ thống chưa kích hoạt!");
            return;
        }

        if (CurrentOrder == null)
        {
            Debug.LogWarning("[OrderManager] Không có đơn hiện tại để check!");
            return;
        }

        var order = CurrentOrder;

        bool shapeMatch = string.Equals(
            shapeName?.Trim(),
            order.requiredShapeName?.Trim(),
            System.StringComparison.OrdinalIgnoreCase);

        if (!shapeMatch)
        {
            string reason = $"Sai hình! Cần {order.requiredShapeName}, bạn đem {shapeName}.";
            Debug.Log($"<color=red>[Order] THẤT BẠI: {reason}</color>");
            onOrderFail?.Invoke(order, 100f, reason);
            return;
        }

        float errorPercent = Mathf.Abs(measuredVolume - order.targetVolume) / order.targetVolume * 100f;
        Debug.Log($"[Order] Đo V = {measuredVolume:F2}, target = {order.targetVolume:F2}, sai số = {errorPercent:F1}%");

        if (errorPercent <= order.tolerancePercent)
        {
            int gold = order.goldReward;
            if (errorPercent < 1f) gold += order.goldBonus;
            totalGold += gold;
            totalCompleted++;

            Debug.Log($"<color=green>[Order] THÀNH CÔNG! Sai {errorPercent:F1}%, nhận {gold} vàng. Tổng: {totalGold}</color>");
            onOrderSuccess?.Invoke(order, errorPercent);

            Invoke(nameof(NextOrder), 3f);
        }
        else
        {
            string reason = $"Sai số {errorPercent:F1}% > {order.tolerancePercent}% cho phép. Thử lại.";
            Debug.Log($"<color=orange>[Order] THẤT BẠI: {reason}</color>");
            onOrderFail?.Invoke(order, errorPercent, reason);
        }
    }

    /// <summary>
    /// Gọi khi LÒ RÈN nhận sản phẩm. isCorrect = true → tính thắng & chuyển đơn;
    /// false → giữ nguyên đơn (báo thất bại). Dùng cho ForgeLightDetector / OrderDeliveryBridge.
    /// </summary>
    public void NotifyDelivery(bool isCorrect)
    {
        if (!isActive || CurrentOrder == null) return;
        if (_advancing) return;   // chống gọi trùng khi đang chờ chuyển đơn

        var order = CurrentOrder;

        if (isCorrect)
        {
            int gold = order.goldReward;
            totalGold += gold;
            totalCompleted++;
            _advancing = true;
            Debug.Log($"<color=green>[Order] GIAO ĐÚNG! +{gold} vàng. Chuyển đơn sau 3s.</color>");
            onOrderSuccess?.Invoke(order, 0f);
            Invoke(nameof(NextOrder), 3f);
        }
        else
        {
            Debug.Log("<color=orange>[Order] GIAO SAI! Giữ nguyên đơn.</color>");
            onOrderFail?.Invoke(order, 100f, "Sai sản phẩm — thử lại nhé!");
        }
    }

    private bool _advancing;

    void NextOrder()
    {
        _advancing = false;
        currentOrderIndex++;
        if (currentOrderIndex >= orders.Count)
        {
            Debug.Log($"<color=cyan>[OrderManager] HOÀN THÀNH TẤT CẢ ĐƠN! Tổng vàng: {totalGold}</color>");
            onAllOrdersCompleted?.Invoke();
            return;
        }

        Debug.Log($"[OrderManager] Chuyển đơn {currentOrderIndex + 1}/{orders.Count}: {CurrentOrder.productName}");
        onOrderChanged?.Invoke(CurrentOrder);
    }

    public void DebugSkipOrder()
    {
        NextOrder();
    }

    /// <summary>
    /// Kiểm tra vật phẩm đem giao có khớp đơn hiện tại không (khớp tên hình + thể tích trong sai số).
    /// Dùng bởi MagicForge.
    /// </summary>
    public bool VerifyProduct(string objectShapeName, float currentVolume)
    {
        if (CurrentOrder == null) return false;

        // 1) Khớp tên hình (vd tên chứa "Cylinder", "Cube"...)
        bool shapeMatch = !string.IsNullOrEmpty(objectShapeName) &&
                          objectShapeName.ToLower().Contains(CurrentOrder.requiredShapeName.ToLower());

        // 2) Khớp thể tích trong sai số cho phép
        float target = CurrentOrder.targetVolume;
        float tolerance = CurrentOrder.tolerancePercent / 100f;
        float minVolume = target * (1f - tolerance);
        float maxVolume = target * (1f + tolerance);
        bool volumeMatch = (currentVolume >= minVolume) && (currentVolume <= maxVolume);

        Debug.Log($"[OrderManager] VerifyProduct: Hình={shapeMatch} (cần {CurrentOrder.requiredShapeName}), " +
                  $"Thể tích={volumeMatch} (thực {currentVolume:F2}, cần {target})");

        return shapeMatch && volumeMatch;
    }
}