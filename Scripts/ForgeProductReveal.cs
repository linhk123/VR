using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HIỆN SẢN PHẨM THEO ĐƠN HÀNG (không liên quan cho-khối-vào / vòng dịch chuyển).
///
/// Quy tắc: ĐƠN nào đang hiển thị trên bảng thì SẢN PHẨM CỦA ĐƠN ĐÓ hiện,
/// các sản phẩm của đơn khác bị ẩn (tàng hình). Tự cập nhật khi đổi đơn.
///
/// CÁCH GẮN:
///   1. Gắn script lên 1 object bất kỳ (vd vat_pham hoặc 1 manager).
///   2. Orders Products: đặt Size = số đơn. Mỗi phần tử là các sản phẩm của 1 đơn:
///        Phần tử 0 (Đơn 1): Products = [Wooden_Bucket, Bucket2_OBJ]
///        Phần tử 1 (Đơn 2): Products = [MagicSword_Ice, fi_vil_forge_broadsword4]
///        ...
/// </summary>
public class ForgeProductReveal : MonoBehaviour
{
    [System.Serializable]
    public class OrderProducts
    {
        [Tooltip("Ghi chú cho dễ nhìn, vd 'Đơn 1 - Xô gỗ'. Không bắt buộc.")]
        public string note;
        [Tooltip("Các sản phẩm CỦA ĐƠN NÀY (sẽ hiện khi đang ở đơn này).")]
        public GameObject[] products;
    }

    [Header("Sản phẩm theo ĐƠN (phần tử 0 = Đơn 1, 1 = Đơn 2, ...)")]
    public List<OrderProducts> ordersProducts = new List<OrderProducts>();

    [Header("Debug")]
    public bool debugLog = true;

    void Start()
    {
        HideAll();   // ẩn hết trước

        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.onOrderChanged.AddListener(OnOrderChanged);
            OrderManager.Instance.onSystemActivated.AddListener(OnSystemActivated);

            // Nếu đơn đã chạy sẵn → hiện luôn sản phẩm đơn hiện tại
            if (OrderManager.Instance.IsActive && OrderManager.Instance.CurrentOrder != null)
                ShowCurrent();
        }
        else if (debugLog)
        {
            Debug.LogWarning("[ForgeProductReveal] Không tìm thấy OrderManager.Instance!");
        }
    }

    void OnOrderChanged(OrderData _) => ShowCurrent();
    void OnSystemActivated() => ShowCurrent();

    [ContextMenu("Hiện sản phẩm đơn hiện tại")]
    public void ShowCurrent()
    {
        HideAll();

        int idx = (OrderManager.Instance != null) ? OrderManager.Instance.CurrentOrderNumber - 1 : -1;
        if (idx < 0 || idx >= ordersProducts.Count || ordersProducts[idx] == null)
        {
            if (debugLog) Debug.LogWarning($"[ForgeProductReveal] Chưa khai báo sản phẩm cho đơn {idx + 1}.");
            return;
        }

        var prods = ordersProducts[idx].products;
        int shown = 0;
        if (prods != null)
            foreach (var p in prods)
                if (p) { p.SetActive(true); shown++; }

        if (debugLog) Debug.Log($"[ForgeProductReveal] Đơn {idx + 1} → hiện {shown} sản phẩm của đơn này.");
    }

    void HideAll()
    {
        foreach (var op in ordersProducts)
        {
            if (op == null || op.products == null) continue;
            foreach (var p in op.products)
                if (p) p.SetActive(false);
        }
    }
}
