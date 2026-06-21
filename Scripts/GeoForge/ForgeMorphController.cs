using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LÒ RÈN "BIẾN HÌNH" — luồng hoàn chỉnh khi cho 1 KHỐI vào lò (fi_vil_forge_forge3_lit).
///
/// KỊCH BẢN:
///   1. HS nhận đơn → cầm 1 khối → phóng to/thu nhỏ theo số đo (cm) cho khớp yêu cầu đơn.
///   2. Cho khối vào lò → ĐÈN LÓE SÁNG (biến hình).
///   3. Đo khối (ProductMeasure: thể tích cm³ + kích thước cm + fill ratio → loại khối)
///      và KIỂM ĐỊNH với đơn hiện tại:
///        - ĐÚNG → khối tan biến + hiện VẬT PHẨM ĐÚNG (tag CorrectItem) CỦA ĐƠN NÀY
///                 tại VatPham_Revealer + chuyển sang đơn kế tiếp.
///        - SAI  → khối tan biến + hiện VẬT PHẨM SAI (tag WrongItem) CỦA ĐƠN NÀY + giữ đơn (làm lại).
///   4. Mỗi đơn CHỈ hiện đúng/sai CỦA ĐƠN ĐÓ — không bao giờ lẫn sang sản phẩm đơn khác
///      (vì chỉ đọc ordersProducts[đơn-hiện-tại]).
///
/// CÁCH GẮN: xem HUONG_DAN_LO_REN_BIEN_HINH.md.
/// </summary>
public class ForgeMorphController : MonoBehaviour
{
    [System.Serializable]
    public class OrderProducts
    {
        [Tooltip("Ghi chú cho dễ nhìn, vd 'Đơn 1 - Xô gỗ'. Không bắt buộc.")]
        public string note;
        [Tooltip("Vật phẩm ĐÚNG của đơn này (gắn tag CorrectItem). Đặt sẵn trong scene, để ẩn.")]
        public GameObject correctProduct;
        [Tooltip("Vật phẩm SAI của đơn này (gắn tag WrongItem). Đặt sẵn trong scene, để ẩn.")]
        public GameObject wrongProduct;
    }

    [Header("=== CÔNG TẮC TỔNG ===")]
    [Tooltip("BẬT (true) = dùng lò BIẾN HÌNH này. Nếu bật cái này thì ĐỪNG dùng ForgeForging (tránh trùng).")]
    public bool active = true;
    [Tooltip("Bỏ qua khối trong vài giây đầu (tránh vật nằm sẵn trong vùng bị rèn nhầm lúc Start → mất khối/báo sai).")]
    public float startupGrace = 1.2f;

    [Header("=== VÙNG NHẬN KHỐI TẠI LÒ ===")]
    [Tooltip("Kéo object lò / đèn lò vào. Vùng nhận khối tự sinh tại đây. Bỏ trống = tự tìm theo tên forgeAnchorName.")]
    public Transform forgeAnchor;
    [Tooltip("Tên lò để tự tìm nếu Forge Anchor trống.")]
    public string forgeAnchorName = "fi_vil_forge_forge3_lit";
    [Tooltip("Bán kính vùng nhận khối quanh lò (mét). KHÔNG để quá to kẻo bắt nhầm. Khuyến nghị 0.6–1.2.")]
    public float zoneRadius = 1.0f;
    [Tooltip("Lệch vùng so với anchor (mét).")]
    public Vector3 zoneOffset = Vector3.zero;
    [Tooltip("Layer của khối được rèn. Mặc định = Shapes (layer 6).")]
    public LayerMask blockLayers = 1 << 6;
    [Tooltip("Cũng coi là khối nếu có component Sliceable (mảnh đã cắt).")]
    public bool alsoSliceable = true;
    [Tooltip("Cũng coi là khối nếu TÊN chứa cube/cylinder/sphere/cone/pyramid/chop (phòng khi chưa đặt layer Shapes).")]
    public bool alsoByName = true;
    [Tooltip("Nghỉ tối thiểu giữa 2 lần rèn (giây).")]
    public float cooldown = 0.8f;

    [Header("=== ĐÈN LÒ LÓE SÁNG ===")]
    [Tooltip("Đèn của lò (Point light_ren). Sẽ lóe khi rèn.")]
    public Light forgeLight;
    [Tooltip("Độ sáng nền (khi không rèn).")]
    public float baseIntensity = 0f;
    [Tooltip("Đỉnh sáng khi lóe biến hình.")]
    public float flareIntensity = 8f;
    [Tooltip("Thời gian lóe (giây).")]
    public float flareDuration = 0.9f;
    [Tooltip("Tùy chọn: nếu đã có ForgeTransformGlow, kéo vào để dùng hiệu ứng lóe của nó.")]
    public ForgeTransformGlow externalGlow;

    [Header("=== NƠI HIỆN SẢN PHẨM ===")]
    [Tooltip("Kéo 'VatPham_Revealer' vào — sản phẩm biến hình hiện ra tại đây.")]
    public Transform revealAnchor;
    [Tooltip("Lệch Y so với revealAnchor (mét).")]
    public float revealYOffset = 0f;
    [Tooltip("Cho sản phẩm bám theo revealAnchor (đặt làm con). Tắt nếu muốn giữ nguyên parent.")]
    public bool parentToAnchor = false;

    [Header("=== VẬT PHẨM THEO ĐƠN (phần tử 0 = Đơn 1, ...) ===")]
    [Tooltip("5 đơn → 5 phần tử, mỗi phần tử 1 cặp Correct/Wrong (tổng 10 vật phẩm).")]
    public List<OrderProducts> ordersProducts = new List<OrderProducts>();

    [Tooltip("Kéo 'vat_pham' vào → ẩn TẤT CẢ vật phẩm con khi bắt đầu (phủ tàng hình).")]
    public Transform vatPhamRoot;

    [Header("=== THỜI GIAN HIỆU ỨNG ===")]
    [Tooltip("Thời gian khối tan biến (giây).")]
    public float dissolveDuration = 0.8f;
    [Tooltip("Thời gian sản phẩm phóng to hiện ra (giây).")]
    public float growDuration = 0.7f;
    [Tooltip("Giữ vật phẩm SAI hiển thị bao lâu rồi ẩn để làm lại (giây). <=0 = giữ luôn.")]
    public float wrongDisplayDuration = 3f;

    [Header("Debug")]
    public bool debugLog = true;

    // --- nội bộ ---
    float _lastTime = -99f;
    float _readyTime = 0f;
    bool _busy = false;
    Coroutine _flareCo;
    readonly Dictionary<GameObject, Vector3> _baseScale = new Dictionary<GameObject, Vector3>();

    void Start()
    {
        if (!active)
        {
            // TẮT: không tạo vùng, không chấm điểm, không huỷ khối. Tránh xung đột với ForgeForging.
            enabled = false;
            return;
        }

        _readyTime = Time.time + startupGrace;
        AutoWire();
        CacheScales();
        HideAllProducts();
        CreateZone();

        if (forgeLight != null) forgeLight.intensity = baseIntensity;

        if (OrderManager.Instance != null)
            OrderManager.Instance.onOrderChanged.AddListener(OnOrderChanged);
        else if (debugLog)
            Debug.LogWarning("[ForgeMorph] Không tìm thấy OrderManager.Instance!");
    }

    void OnDestroy()
    {
        if (OrderManager.Instance != null)
            OrderManager.Instance.onOrderChanged.RemoveListener(OnOrderChanged);
    }

    void OnOrderChanged(OrderData _) => HideAllProducts(); // sang đơn mới → ẩn hết sản phẩm

    // Tự tìm các tham chiếu hay bỏ sót theo TÊN, để đỡ phải kéo-thả thủ công.
    void AutoWire()
    {
        if (forgeAnchor == null && !string.IsNullOrEmpty(forgeAnchorName))
        {
            var go = GameObject.Find(forgeAnchorName);
            if (go != null) forgeAnchor = go.transform;
        }
        if (forgeLight == null && externalGlow == null)
        {
            var go = GameObject.Find("Point light_ren");
            if (go != null) forgeLight = go.GetComponent<Light>();
        }
        if (revealAnchor == null)
        {
            var go = GameObject.Find("VatPham_Revealer");
            if (go != null) revealAnchor = go.transform;
        }
        if (vatPhamRoot == null)
        {
            var go = GameObject.Find("vat_pham");
            if (go != null) vatPhamRoot = go.transform;
        }
        if (debugLog)
            Debug.Log($"[ForgeMorph] AutoWire: forge={(forgeAnchor ? forgeAnchor.name : "NULL")}, " +
                      $"light={(forgeLight ? forgeLight.name : (externalGlow ? "glow" : "NULL"))}, " +
                      $"revealer={(revealAnchor ? revealAnchor.name : "NULL")}, vatPham={(vatPhamRoot ? vatPhamRoot.name : "NULL")}.");
    }

    // Ghi nhớ scale gốc của mọi sản phẩm để phục hồi sau hiệu ứng phóng to.
    void CacheScales()
    {
        foreach (var op in ordersProducts)
        {
            if (op == null) continue;
            if (op.correctProduct && !_baseScale.ContainsKey(op.correctProduct))
                _baseScale[op.correctProduct] = op.correctProduct.transform.localScale;
            if (op.wrongProduct && !_baseScale.ContainsKey(op.wrongProduct))
                _baseScale[op.wrongProduct] = op.wrongProduct.transform.localScale;
        }
    }

    void CreateZone()
    {
        Vector3 pos = (forgeAnchor != null ? forgeAnchor.position : transform.position) + zoneOffset;
        var go = new GameObject("ForgeMorphZone");
        go.transform.position = pos;
        var sc = go.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = zoneRadius;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        go.AddComponent<ForgeMorphZoneRelay>().owner = this;
        if (debugLog) Debug.Log($"[ForgeMorph] Vùng nhận khối tại {pos:F2} (bán kính {zoneRadius}).");
    }

    public void OnBlockEnter(Collider other)
    {
        if (_busy) return;
        if (Time.time < _readyTime) return;               // chống tự kích lúc Start
        if (Time.time - _lastTime < cooldown) return;
        if (!IsBlock(other)) return;

        if (OrderManager.Instance == null || !OrderManager.Instance.IsActive)
        {
            if (debugLog) Debug.LogWarning("[ForgeMorph] Hệ thống đơn hàng chưa kích hoạt!");
            return;
        }

        var order = OrderManager.Instance.CurrentOrder;
        int idx = OrderManager.Instance.CurrentOrderNumber - 1;
        if (order == null || idx < 0)
        {
            if (debugLog) Debug.LogWarning("[ForgeMorph] Không có đơn hiện tại!");
            return;
        }

        _lastTime = Time.time;

        Transform block = GetBlockRoot(other);

        // === ĐO + KIỂM ĐỊNH ===
        var print = ProductMeasure.Measure(block != null ? block.gameObject : other.gameObject);
        bool isCorrect = Validate(order, print, out string reason);

        if (debugLog)
            Debug.Log($"<color=cyan>[ForgeMorph] Đơn {idx + 1} '{order.productName}': " +
                      $"loại={print.shapeClass} V={print.volumeCm3:F0}cm³ dims=({print.dimsCm.x:F1},{print.dimsCm.y:F1},{print.dimsCm.z:F1})cm " +
                      $"fill={print.fill:F2} → {(isCorrect ? "ĐÚNG" : "SAI")} ({reason})</color>");

        StartCoroutine(MorphSequence(block, idx, isCorrect));
    }

    // ===== KIỂM ĐỊNH ĐẦY ĐỦ =====
    bool Validate(OrderData order, ProductMeasure.Print p, out string reason)
    {
        if (!p.valid) { reason = "không đo được khối (thiếu Renderer/Mesh)"; return false; }

        // 1) Loại khối (fill ratio)
        if (order.checkShapeClass && order.requiredShapeClass != ShapeClass.Any)
        {
            if (p.shapeClass != order.requiredShapeClass)
            {
                reason = $"sai loại khối (cần {order.requiredShapeClass}, đo ra {p.shapeClass})";
                return false;
            }
        }

        // 2) Thể tích (cm³)
        if (order.checkVolume)
        {
            float target = Mathf.Max(order.targetVolumeCm3, 1e-4f);
            float err = Mathf.Abs(p.volumeCm3 - target) / target * 100f;
            if (err > order.volumeTolerancePercent)
            {
                reason = $"sai thể tích {err:F1}% > {order.volumeTolerancePercent}%";
                return false;
            }
        }

        // 3) Kích thước (cm) — so theo bộ cạnh đã sắp xếp (không phụ thuộc hướng đặt)
        if (order.checkDims)
        {
            Vector3 a = SortedAsc(p.dimsCm);
            Vector3 b = SortedAsc(order.targetDimsCm);
            if (!WithinPercent(a.x, b.x, order.dimsTolerancePercent) ||
                !WithinPercent(a.y, b.y, order.dimsTolerancePercent) ||
                !WithinPercent(a.z, b.z, order.dimsTolerancePercent))
            {
                reason = $"sai kích thước (đo {a.x:F1}/{a.y:F1}/{a.z:F1} vs cần {b.x:F1}/{b.y:F1}/{b.z:F1} cm)";
                return false;
            }
        }

        reason = "khớp đặc tả";
        return true;
    }

    static Vector3 SortedAsc(Vector3 v)
    {
        float a = v.x, b = v.y, c = v.z, t;
        if (a > b) { t = a; a = b; b = t; }
        if (b > c) { t = b; b = c; c = t; }
        if (a > b) { t = a; a = b; b = t; }
        return new Vector3(a, b, c);
    }

    static bool WithinPercent(float measured, float target, float tolPercent)
    {
        if (target <= 1e-4f) return Mathf.Abs(measured) <= 1e-3f;
        return Mathf.Abs(measured - target) / target * 100f <= tolPercent;
    }

    // ===== CHUỖI HIỆU ỨNG BIẾN HÌNH =====
    IEnumerator MorphSequence(Transform block, int idx, bool isCorrect)
    {
        _busy = true;

        // 0) AN TOÀN: chọn sản phẩm TRƯỚC. Nếu chưa gán → GIỮ khối lại, báo lỗi rõ,
        //    để không bị "khối biến mất mà chẳng hiện gì".
        GameObject product = PickProduct(idx, isCorrect);
        if (product == null)
        {
            Debug.LogError($"[ForgeMorph] Đơn {idx + 1}: CHƯA GÁN sản phẩm {(isCorrect ? "ĐÚNG (CorrectItem)" : "SAI (WrongItem)")} " +
                           $"trong 'Orders Products' (Element {idx}). → Giữ nguyên khối. Hãy kéo vật phẩm vào ô tương ứng.");
            _busy = false;
            yield break;   // KHÔNG tan khối, KHÔNG báo đơn
        }

        // 1) LÓE ĐÈN
        Flare();

        // 2) KHỐI TAN BIẾN
        if (block != null)
        {
            yield return StartCoroutine(DissolveBlock(block.gameObject));
            block.gameObject.SetActive(false);
            Destroy(block.gameObject, 0.1f);
        }

        // 3) HIỆN SẢN PHẨM CỦA ĐƠN HIỆN TẠI (chỉ đơn này)
        HideAllProducts();
        PlaceAtRevealer(product);
        yield return StartCoroutine(GrowIn(product));

        // 4) BÁO ĐƠN HÀNG: đúng → chuyển đơn (OrderManager tự Invoke NextOrder),
        //    sai → giữ nguyên đơn.
        if (OrderManager.Instance != null)
            OrderManager.Instance.NotifyDelivery(isCorrect);

        // 5) Vật phẩm SAI: hiện 1 lúc rồi ẩn để HS làm lại. Vật phẩm ĐÚNG để
        //    HideAllProducts() tự dọn khi đổi đơn.
        if (!isCorrect && product != null && wrongDisplayDuration > 0f)
        {
            yield return new WaitForSeconds(wrongDisplayDuration);
            product.SetActive(false);
        }

        _busy = false;
    }

    GameObject PickProduct(int idx, bool isCorrect)
    {
        if (idx < 0 || idx >= ordersProducts.Count || ordersProducts[idx] == null) return null;
        var op = ordersProducts[idx];
        return isCorrect ? op.correctProduct : op.wrongProduct;
    }

    void PlaceAtRevealer(GameObject product)
    {
        if (revealAnchor != null)
        {
            if (parentToAnchor) product.transform.SetParent(revealAnchor, true);
            product.transform.position = revealAnchor.position + Vector3.up * revealYOffset;
            product.transform.rotation = revealAnchor.rotation;
        }
        product.SetActive(true);
    }

    // Sản phẩm phóng to từ 0 → scale gốc (bounce-out) — cảm giác "biến hình".
    IEnumerator GrowIn(GameObject product)
    {
        Vector3 target = _baseScale.TryGetValue(product, out var s) ? s : product.transform.localScale;
        if (target == Vector3.zero) target = Vector3.one;

        product.transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < growDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / growDuration);
            float ease = 1f - Mathf.Pow(1f - p, 3f); // ease-out cubic
            product.transform.localScale = target * ease;
            yield return null;
        }
        product.transform.localScale = target;
    }

    // Khối tan biến: thu nhỏ + xoay.
    IEnumerator DissolveBlock(GameObject blockGo)
    {
        Vector3 from = blockGo.transform.localScale;
        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dissolveDuration);
            blockGo.transform.localScale = Vector3.Lerp(from, Vector3.zero, p);
            blockGo.transform.Rotate(0f, 540f * Time.deltaTime, 0f);
            yield return null;
        }
    }

    // ===== ĐÈN LÓE =====
    public void Flare()
    {
        if (externalGlow != null) { externalGlow.Flare(); return; }
        if (forgeLight == null) return;
        if (_flareCo != null) StopCoroutine(_flareCo);
        _flareCo = StartCoroutine(FlareRoutine());
    }

    IEnumerator FlareRoutine()
    {
        forgeLight.enabled = true;
        float t = 0f;
        while (t < flareDuration)
        {
            t += Time.deltaTime;
            float p = t / flareDuration;
            float env;
            if (p < 0.10f) env = p / 0.10f;                                  // bùng nhanh
            else if (p < 0.35f) env = 1f;                                    // giữ đỉnh
            else env = Mathf.SmoothStep(1f, 0f, (p - 0.35f) / 0.65f);        // tắt dần
            forgeLight.intensity = Mathf.Lerp(baseIntensity, flareIntensity, env);
            yield return null;
        }
        forgeLight.intensity = baseIntensity;
        _flareCo = null;
    }

    // ===== ẨN/NHẬN DIỆN =====
    void HideAllProducts()
    {
        if (vatPhamRoot != null)
            foreach (Transform child in vatPhamRoot)
                child.gameObject.SetActive(false);

        foreach (var op in ordersProducts)
        {
            if (op == null) continue;
            if (op.correctProduct) op.correctProduct.SetActive(false);
            if (op.wrongProduct) op.wrongProduct.SetActive(false);
        }
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

        // Phòng khi chưa đặt layer Shapes: nhận diện theo TÊN khối hình học.
        if (alsoByName)
        {
            t = other.transform;
            while (t != null)
            {
                string n = t.name.ToLower();
                if (n.Contains("cube") || n.Contains("cylinder") || n.Contains("sphere") ||
                    n.Contains("cone") || n.Contains("pyramid") || n.Contains("chop") ||
                    n.Contains("non") || n.Contains("khoi"))
                    return true;
                t = t.parent;
            }
        }
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

    [ContextMenu("Test: rèn ĐÚNG đơn hiện tại")]
    void TestCorrect()
    {
        int idx = OrderManager.Instance != null ? OrderManager.Instance.CurrentOrderNumber - 1 : 0;
        StartCoroutine(MorphSequence(null, idx, true));
    }

    [ContextMenu("Test: rèn SAI đơn hiện tại")]
    void TestWrong()
    {
        int idx = OrderManager.Instance != null ? OrderManager.Instance.CurrentOrderNumber - 1 : 0;
        StartCoroutine(MorphSequence(null, idx, false));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 c = (forgeAnchor != null ? forgeAnchor.position : transform.position) + zoneOffset;
        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(c, zoneRadius);
    }
#endif
}

/// <summary>Chuyển sự kiện trigger của vùng nhận khối về ForgeMorphController.</summary>
public class ForgeMorphZoneRelay : MonoBehaviour
{
    [HideInInspector] public ForgeMorphController owner;
    void OnTriggerEnter(Collider other)
    {
        if (owner != null) owner.OnBlockEnter(other);
    }
}
