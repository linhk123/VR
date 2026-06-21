using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LUỒNG RÈN HOÀN CHỈNH khi cho 1 KHỐI vào lò:
///   1. Ánh sáng lò LÓE (biến hình) — gọi ForgeTransformGlow.Flare().
///   2. So loại khối với ĐƠN hiện tại:
///        - ĐÚNG  → hiện SẢN PHẨM ĐÚNG của đơn + CHUYỂN sang đơn tiếp theo.
///        - SAI   → hiện SẢN PHẨM SAI + GIỮ NGUYÊN đơn (làm lại).
///   3. Huỷ khối đã cho vào (đã rèn).
///
/// CÁCH GẮN:
///   1. Gắn script lên 1 object (vd object lò hoặc 1 manager).
///   2. Forge Zone Anchor = kéo object lò (hoặc đèn) → vùng nhận khối tại lò.
///   3. Forge Glow = kéo component ForgeTransformGlow (đèn Point light_ren) để lóe sáng.
///      (Nên tắt 'Create Own Trigger Zone' trên ForgeTransformGlow để tránh trùng vùng.)
///   4. Orders Products: mỗi đơn 1 cặp (Correct / Wrong product), thứ tự = thứ tự đơn.
/// </summary>
public class ForgeForging : MonoBehaviour
{
    [System.Serializable]
    public class OrderProducts
    {
        [Tooltip("Ghi chú, vd 'Đơn 1 - Xô gỗ'.")]
        public string note;
        public GameObject correctProduct;
        public GameObject wrongProduct;
    }

    [Header("Vùng nhận khối tại lò")]
    [Tooltip("Kéo object lò (hoặc đèn) vào để đặt vùng ngay tại lò. Bỏ trống thì dùng Zone World Position.")]
    public Transform forgeZoneAnchor;
    public Vector3 zoneWorldPosition = new Vector3(0f, 1f, -10.57f);
    public float zoneRadius = 1.0f;
    public LayerMask blockLayers = 1 << 6;   // Shapes
    public bool alsoSliceable = true;
    public float cooldown = 0.6f;

    [Header("Ánh sáng lò (biến hình)")]
    [Tooltip("Kéo component ForgeTransformGlow (điều khiển Point light_ren) vào đây.")]
    public ForgeTransformGlow forgeGlow;

    [Header("Vật phẩm")]
    [Tooltip("Kéo object 'vat_pham' vào → ẩn TẤT CẢ vật phẩm con khi bắt đầu (phủ tàng hình toàn bộ).")]
    public Transform vatPhamRoot;

    [Header("Sản phẩm theo ĐƠN (phần tử 0 = Đơn 1, ...)")]
    public List<OrderProducts> ordersProducts = new List<OrderProducts>();

    [Header("Khối đã cho vào")]
    public bool consumeBlock = true;
    public float consumeDelay = 0.15f;

    [Header("Chống tự kích lúc Start")]
    [Tooltip("Bỏ qua khối trong vài giây đầu (tránh vật nằm sẵn trong vùng bị 'rèn' nhầm → mất khối / báo sai).")]
    public float startupGrace = 1.2f;

    [Header("Debug")]
    public bool debugLog = true;

    float _lastTime = -99f;
    float _readyTime = 0f;

    void Start()
    {
        _readyTime = Time.time + startupGrace;
        HideAll();
        CreateZone();
        if (OrderManager.Instance != null)
            OrderManager.Instance.onOrderChanged.AddListener(OnOrderChanged);
        else if (debugLog)
            Debug.LogWarning("[ForgeForging] Không tìm thấy OrderManager.Instance!");
    }

    void OnOrderChanged(OrderData _) => HideAll();   // sang đơn mới → ẩn sản phẩm

    void CreateZone()
    {
        var go = new GameObject("ForgeForgingZone");
        go.transform.position = forgeZoneAnchor != null ? forgeZoneAnchor.position : zoneWorldPosition;
        var sc = go.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = zoneRadius;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        go.AddComponent<ForgeForgingRelay>().owner = this;
        if (debugLog) Debug.Log($"[ForgeForging] Vùng nhận khối tại {go.transform.position:F2} (bán kính {zoneRadius}).");
    }

    public void OnBlockEnter(Collider other)
    {
        // Bỏ qua trong vài giây đầu → không 'rèn' nhầm vật nằm sẵn trong vùng lúc Start.
        if (Time.time < _readyTime) return;

        // Chỉ xử lý khi hệ thống đơn đã kích hoạt (tránh báo sai khi chưa bắt đầu).
        if (OrderManager.Instance == null || !OrderManager.Instance.IsActive)
        {
            if (debugLog) Debug.Log("[ForgeForging] Đơn chưa kích hoạt → bỏ qua khối.");
            return;
        }

        if (Time.time - _lastTime < cooldown) return;
        if (!IsBlock(other))
        {
            if (debugLog) Debug.Log($"[ForgeForging] '{other.name}' (layer {LayerMask.LayerToName(other.gameObject.layer)}) → bỏ qua.");
            return;
        }
        _lastTime = Time.time;

        // 1) ÁNH SÁNG LÓE
        if (forgeGlow != null) forgeGlow.Flare();

        // 2) ĐÚNG / SAI theo đơn
        Transform block = GetBlockRoot(other);
        bool correct = IsCorrectShape(block);
        int idx = CurrentOrderIndex();

        // 3) Hiện sản phẩm
        Reveal(idx, correct);

        // 4) Đơn hàng: đúng → chuyển đơn; sai → làm lại
        if (OrderManager.Instance != null) OrderManager.Instance.NotifyDelivery(correct);

        // 5) Khối TÀNG HÌNH ngay lập tức rồi dọn
        if (consumeBlock && block != null)
        {
            block.gameObject.SetActive(false);          // tàng hình ngay
            Destroy(block.gameObject, consumeDelay);     // dọn sau đó
        }

        if (debugLog) Debug.Log($"[ForgeForging] Đơn {idx + 1}: {(correct ? "ĐÚNG → sản phẩm đúng + chuyển đơn" : "SAI → sản phẩm sai + làm lại")}.");
    }

    bool IsCorrectShape(Transform block)
    {
        if (OrderManager.Instance == null || OrderManager.Instance.CurrentOrder == null) return false;
        string need = GuessShape(OrderManager.Instance.CurrentOrder.requiredShapeName);
        string got = GuessShape(block != null ? block.name : "");
        if (debugLog) Debug.Log($"[ForgeForging] Đơn cần '{need}', khối là '{got}'.");
        return need != "" && need == got;
    }

    int CurrentOrderIndex()
    {
        return OrderManager.Instance != null ? OrderManager.Instance.CurrentOrderNumber - 1 : -1;
    }

    void Reveal(int idx, bool correct)
    {
        HideAll();
        if (idx < 0 || idx >= ordersProducts.Count || ordersProducts[idx] == null)
        {
            if (debugLog) Debug.LogWarning($"[ForgeForging] Chưa khai báo sản phẩm cho đơn {idx + 1}.");
            return;
        }
        var op = ordersProducts[idx];
        var p = correct ? op.correctProduct : op.wrongProduct;
        if (p != null) p.SetActive(true);
        else if (debugLog) Debug.LogWarning($"[ForgeForging] Đơn {idx + 1}: chưa gán sản phẩm {(correct ? "ĐÚNG" : "SAI")}.");
    }

    void HideAll()
    {
        // Phủ TÀNG HÌNH toàn bộ vật phẩm con của vat_pham
        if (vatPhamRoot != null)
            foreach (Transform child in vatPhamRoot)
                child.gameObject.SetActive(false);

        // Ẩn thêm các sản phẩm khai báo (đề phòng nằm ngoài vat_pham)
        foreach (var op in ordersProducts)
        {
            if (op == null) continue;
            if (op.correctProduct) op.correctProduct.SetActive(false);
            if (op.wrongProduct) op.wrongProduct.SetActive(false);
        }
    }

    string GuessShape(string raw)
    {
        string s = (raw ?? "").ToLower();
        if (s.Contains("cube") || s.Contains("lap phuong") || s.Contains("hop")) return "Cube";
        if (s.Contains("cylinder") || s.Contains("tru") || s.Contains("trụ")) return "Cylinder";
        if (s.Contains("sphere") || s.Contains("cau") || s.Contains("cầu")) return "Sphere";
        if (s.Contains("cone") || s.Contains("non") || s.Contains("nón")) return "Cone";
        if (s.Contains("pyramid") || s.Contains("chop") || s.Contains("chóp")) return "Pyramid";
        return "";
    }

    bool IsBlock(Collider other)
    {
        Transform t = other.transform;
        while (t != null)
        {
            if ((blockLayers.value & (1 << t.gameObject.layer)) != 0) return true;
            t = t.parent;
        }
        if (alsoSliceable && other.GetComponentInParent<Sliceable>() != null) return true;
        return false;
    }

    Transform GetBlockRoot(Collider other)
    {
        var sl = other.GetComponentInParent<Sliceable>();
        if (sl != null) return sl.transform;
        int shapes = LayerMask.NameToLayer("Shapes");
        Transform t = other.transform, best = other.transform;
        while (t != null) { if (t.gameObject.layer == shapes) best = t; t = t.parent; }
        return best;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 c = forgeZoneAnchor != null ? forgeZoneAnchor.position : zoneWorldPosition;
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(c, zoneRadius);
    }
#endif
}

/// <summary>Chuyển sự kiện trigger của vùng về ForgeForging.</summary>
public class ForgeForgingRelay : MonoBehaviour
{
    [HideInInspector] public ForgeForging owner;
    void OnTriggerEnter(Collider other)
    {
        if (owner != null) owner.OnBlockEnter(other);
    }
}
