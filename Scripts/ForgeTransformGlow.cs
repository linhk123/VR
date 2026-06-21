using System.Collections;
using UnityEngine;

/// <summary>
/// HIỆU ỨNG LÒ RÈN LÓE SÁNG (kiểu biến hình) khi cho KHỐI vào lò.
///
/// Đèn Point light_ren nằm TRONG lò → vùng nhận khối tự sinh NGAY TẠI VỊ TRÍ ĐÈN
/// (không cần canh toạ độ). Khi 1 khối (layer Shapes) vào vùng:
///   intensity bùng từ Base (0) → Flare (1.000.000) rồi lép lóe tắt về 0.
///
/// CÁCH GẮN:
///   1. Tạo 1 object trống (hoặc dùng object lò) → Add Component script này.
///   2. Kéo "Point light_ren" vào ô Forge Light.
///   3. Để Block Layers = Shapes. Chỉnh Zone Radius nếu cần (xem quả cầu gizmo).
/// </summary>
public class ForgeTransformGlow : MonoBehaviour
{
    [Header("Đèn lò (kéo 'Point light_ren' vào)")]
    public Light forgeLight;

    [Header("Độ sáng")]
    [Tooltip("Sáng bình thường (yêu cầu = 0 → tắt khi chưa có khối).")]
    public float baseIntensity = 0f;
    [Tooltip("Đỉnh sáng khi có khối vào.")]
    public float flareIntensity = 1000000f;

    [Header("Vùng nhận khối (tự sinh tại vị trí đèn)")]
    public bool createZoneAtLight = true;
    [Tooltip("Bán kính vùng quanh đèn (mét). To cho dễ trúng.")]
    public float zoneRadius = 1.0f;
    [Tooltip("Lệch vùng so với đèn (mét) nếu miệng lò không trùng tâm đèn.")]
    public Vector3 zoneOffset = Vector3.zero;
    [Tooltip("Layer của khối kích hoạt. Đặt = Shapes.")]
    public LayerMask blockLayers = 1 << 6;   // layer 6 = Shapes
    public bool alsoSliceable = true;
    [Tooltip("Nghỉ tối thiểu giữa 2 lần lóe (giây).")]
    public float retriggerCooldown = 0.3f;

    [Header("Hiệu ứng biến hình")]
    [Tooltip("Tổng thời gian hiệu ứng (giây).")]
    public float duration = 1.4f;
    public float flickerSpeed = 30f;
    [Range(0f, 1f)] public float flickerDepth = 0.3f;

    [Header("Test / Debug")]
    public KeyCode testKey = KeyCode.T;
    public bool debugLog = true;

    [Header("Chống lóe lúc Start")]
    [Tooltip("Bỏ qua va chạm trong vài giây đầu (tránh vật nằm sẵn trong vùng làm lóe nhầm).")]
    public float startupGrace = 1.2f;

    Coroutine _co;
    float _lastFlare = -99f;
    float _readyTime = 0f;

    void Awake()
    {
        if (forgeLight == null)
        {
            Debug.LogWarning("[ForgeTransformGlow] CHƯA kéo 'Point light_ren' vào ô Forge Light!");
            return;
        }
        // Đảm bảo đèn render được + đặt sáng nền = 0
        forgeLight.gameObject.SetActive(true);
        forgeLight.enabled = true;
        forgeLight.lightmapBakeType = LightmapBakeType.Realtime;   // Baked sẽ không đổi được lúc chạy
        forgeLight.intensity = baseIntensity;

        Debug.Log($"[ForgeTransformGlow] Sẵn sàng. Đèn '{forgeLight.name}' type={forgeLight.type}, " +
                  $"bake={forgeLight.lightmapBakeType}, pos={forgeLight.transform.position:F2}, " +
                  $"nền {baseIntensity} → bùng {flareIntensity}.");
    }

    void Start()
    {
        _readyTime = Time.time + startupGrace;
        if (createZoneAtLight) CreateZone();
    }

    void CreateZone()
    {
        if (forgeLight == null) return;
        var go = new GameObject("ForgeBlockZone");
        go.transform.position = forgeLight.transform.position + zoneOffset;
        var sc = go.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = zoneRadius;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        go.AddComponent<ForgeBlockZoneRelay>().owner = this;
        if (debugLog) Debug.Log($"[ForgeTransformGlow] Đã tạo VÙNG NHẬN KHỐI tại {go.transform.position:F2} (bán kính {zoneRadius}).");
    }

    void Update()
    {
        if (Input.GetKeyDown(testKey)) Flare();
    }

    public void OnBlockEnter(Collider other)
    {
        // Bỏ qua va chạm trong vài giây đầu (tránh lóe nhầm khi vật nằm sẵn trong vùng lúc Start).
        if (Time.time < _readyTime) return;

        if (IsBlock(other))
        {
            if (debugLog) Debug.Log($"[ForgeTransformGlow] '{other.name}' LÀ KHỐI vào lò → LÓE!");
            Flare();
        }
        else if (debugLog)
        {
            Debug.Log($"[ForgeTransformGlow] '{other.name}' (layer {LayerMask.LayerToName(other.gameObject.layer)}) → bỏ qua.");
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
        return false;
    }

    [ContextMenu("Test Flare Now")]
    public void Flare()
    {
        if (forgeLight == null) return;
        if (Time.time - _lastFlare < retriggerCooldown) return;
        _lastFlare = Time.time;

        if (debugLog) Debug.Log($"[ForgeTransformGlow] >>> LÓE BIẾN HÌNH! ({baseIntensity} → {flareIntensity})");
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(FlareRoutine());
    }

    IEnumerator FlareRoutine()
    {
        forgeLight.gameObject.SetActive(true);
        forgeLight.enabled = true;

        float seed = Random.value * 10f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;

            // Đường bao kiểu BIẾN HÌNH: bùng cực nhanh → giữ đỉnh chói → tắt dần
            float env;
            if (p < 0.08f) env = p / 0.08f;                              // bùng nhanh
            else if (p < 0.35f) env = 1f;                               // giữ đỉnh (khoảnh khắc biến hình)
            else env = Mathf.SmoothStep(1f, 0f, (p - 0.35f) / 0.65f);   // tắt dần

            float flick = 1f + flickerDepth * (Mathf.PerlinNoise(Time.time * flickerSpeed + seed, seed) * 2f - 1f);
            forgeLight.intensity = (baseIntensity + (flareIntensity - baseIntensity) * env) * flick;
            yield return null;
        }

        forgeLight.intensity = baseIntensity;   // về 0
        _co = null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!createZoneAtLight || forgeLight == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(forgeLight.transform.position + zoneOffset, zoneRadius);
    }
#endif
}

/// <summary>Chuyển sự kiện trigger của vùng nhận khối về ForgeTransformGlow.</summary>
public class ForgeBlockZoneRelay : MonoBehaviour
{
    [HideInInspector] public ForgeTransformGlow owner;
    void OnTriggerEnter(Collider other)
    {
        if (owner != null) owner.OnBlockEnter(other);
    }
}
