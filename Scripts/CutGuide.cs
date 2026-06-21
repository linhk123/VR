using UnityEngine;

/// <summary>
/// MẶT PHẲNG DẪN CẮT (chống cắt lệch cho học sinh).
///
/// Gắn lên khối cần cắt. Hiện 1 mặt phẳng mờ phát sáng = chỗ NÊN cắt.
/// Khi người chơi vung kiếm tới gần đúng mặt phẳng này (trong dung sai góc + vị trí):
///   → "hít" (snap) lát cắt về đúng mặt phẳng dẫn  → CẮT ĐẸP (sản phẩm tốt).
/// Lệch quá ngưỡng → KHÔNG snap, cắt theo tay người chơi → lát xiên (sản phẩm xấu).
///
/// SwordSlicer sẽ tự tìm component này trên khối và gọi Evaluate().
/// </summary>
[DisallowMultipleComponent]
public class CutGuide : MonoBehaviour
{
    [Header("Mặt phẳng dẫn (local-space của khối)")]
    [Tooltip("Điểm mặt phẳng đi qua, theo local. (0,0,0) = tâm khối.")]
    public Vector3 localPoint = Vector3.zero;
    [Tooltip("Pháp tuyến mặt phẳng dẫn, theo local. (0,1,0) = cắt ngang.")]
    public Vector3 localNormal = Vector3.up;

    [Header("Dung sai snap")]
    [Tooltip("Lệch góc tối đa vẫn cho 'hít' về mặt phẳng dẫn (độ).")]
    public float snapAngle = 22f;
    [Tooltip("Lệch vị trí tối đa vẫn cho 'hít' (mét, world).")]
    public float snapDistance = 0.06f;

    [Header("Hiển thị mặt phẳng dẫn")]
    public bool showGuide = true;
    public Color guideColor = new Color(0.3f, 1f, 0.6f, 0.28f);
    [Tooltip("Cỡ tấm dẫn so với khối (1 = bằng bao khối).")]
    public float guideScale = 1.15f;

    GameObject _guide;
    Renderer _shapeRend;
    static Material s_mat;

    public struct Result
    {
        public bool within;          // có nằm trong dung sai (được snap) không
        public Vector3 planePos;     // điểm mặt phẳng (đã snap nếu within)
        public Vector3 planeNormal;  // pháp tuyến (đã snap nếu within)
        public float quality;        // 0..1 (1 = khớp hoàn hảo)
        public float angleErr;       // độ
        public float posErr;         // mét
    }

    void Start()
    {
        _shapeRend = GetComponentInChildren<Renderer>();  // cache TRƯỚC khi dựng guide
        if (showGuide) BuildGuide();
    }

    /// <summary>Pháp tuyến mặt phẳng dẫn trong world.</summary>
    public Vector3 WorldNormal => transform.TransformDirection(localNormal).normalized;
    /// <summary>Điểm mặt phẳng dẫn trong world.</summary>
    public Vector3 WorldPoint => transform.TransformPoint(localPoint);

    /// <summary>
    /// Chấm điểm lát cắt người chơi định thực hiện (point + normal world).
    /// Nếu trong dung sai → trả mặt phẳng ĐÃ SNAP về mặt phẳng dẫn.
    /// </summary>
    public Result Evaluate(Vector3 playerPoint, Vector3 playerNormal)
    {
        Vector3 wN = WorldNormal;
        Vector3 wP = WorldPoint;

        float ang = Vector3.Angle(playerNormal.normalized, wN);
        if (ang > 90f) ang = 180f - ang;                       // n và -n cùng 1 mặt phẳng
        float pos = Mathf.Abs(Vector3.Dot(playerPoint - wP, wN));

        // Dung sai hiệu dụng: nới/thu theo độ khó (CutAssist) nếu có trong scene
        float tolA = snapAngle, tolD = snapDistance;
        if (CutAssist.Instance != null)
        {
            tolA = CutAssist.Instance.AngleTol(snapAngle);
            tolD = CutAssist.Instance.DistTol(snapDistance);
        }

        // Chặn dung sai vị trí theo CỠ KHỐI → khối nhỏ không bị "vị trí luôn trúng"
        if (_shapeRend != null)
            tolD = Mathf.Min(tolD, _shapeRend.bounds.extents.magnitude * 0.6f);

        bool within = ang <= tolA && pos <= tolD;
        float q = within
            ? Mathf.Clamp01(1f - 0.5f * (ang / tolA) - 0.5f * (pos / tolD))
            : 0f;

        return new Result
        {
            within = within,
            planePos = within ? wP : playerPoint,
            planeNormal = within ? wN : playerNormal.normalized,
            quality = q,
            angleErr = ang,
            posErr = pos
        };
    }

    void BuildGuide()
    {
        _guide = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var col = _guide.GetComponent<Collider>();
        if (col != null) Destroy(col);
        _guide.name = "CutGuidePlane";
        _guide.transform.SetParent(transform, false);
        _guide.transform.localPosition = localPoint;
        // Quad mặt (+Z) hướng theo pháp tuyến → tấm nằm trong mặt phẳng cắt
        _guide.transform.localRotation = Quaternion.LookRotation(localNormal.normalized);

        // Cỡ tấm = bao khối
        var rend = GetComponentInChildren<Renderer>();
        float size = rend != null ? rend.bounds.size.magnitude * 0.5f * guideScale : guideScale;
        _guide.transform.localScale = Vector3.one * size;

        _guide.GetComponent<Renderer>().sharedMaterial = GuideMat();
    }

    Material GuideMat()
    {
        if (s_mat == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            s_mat = new Material(sh) { name = "CutGuideMat" };
            if (s_mat.HasProperty("_Surface")) s_mat.SetFloat("_Surface", 1); // Transparent (URP)
            s_mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            s_mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            s_mat.SetInt("_ZWrite", 0);
            s_mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            s_mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        var m = s_mat;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", guideColor);
        if (m.HasProperty("_Color")) m.color = guideColor;
        return m;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.9f);
        Vector3 p = transform.TransformPoint(localPoint);
        Vector3 n = transform.TransformDirection(localNormal).normalized;
        Gizmos.DrawLine(p, p + n * 0.2f);
    }
#endif
}
