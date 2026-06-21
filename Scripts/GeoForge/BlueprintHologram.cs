using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BẢN THIẾT KẾ KHUNG DÂY (BLUEPRINT) — hiện khối hình của đơn hiện tại dưới dạng
/// khung dây xanh phát sáng + lớp vỏ mờ, xoay nhẹ để người chơi thấy rõ CẤU TRÚC.
///
/// Khác bản cũ (vốn dựng 1 khối đặc trông không giống bản vẽ):
///   - Vẽ các CẠNH bằng LineRenderer màu blueprint.
///   - Tự nhỏ gọn; scale/vị trí chỉnh trong Inspector để khớp bàn/bảng.
///   - Tuỳ chọn vỏ mờ bán trong suốt.
/// Tự cập nhật theo OrderManager.onOrderChanged.
/// </summary>
public class BlueprintHologram : MonoBehaviour
{
    [Header("=== NEO CỐ ĐỊNH TRÊN BẢNG ===")]
    [Tooltip("Kéo object BẢNG (OrderBoard) — hoặc 1 điểm neo trên bảng — vào đây. " +
             "Hologram sẽ KHOÁ vào điểm này, KHÔNG trôi theo người chơi. " +
             "Bỏ trống = neo vào chính object chứa script này.")]
    public Transform boardAnchor;
    [Tooltip("Lệch so với điểm neo (mét, theo LOCAL của điểm neo). " +
             "Ví dụ (0, 0.25, 0) = nhô lên trên bảng 25cm.")]
    public Vector3 anchorLocalOffset = new Vector3(0f, 0.25f, 0f);
    [Tooltip("Độ lớn khối khung dây (mét). 0.15 = nhỏ gọn để làm mẫu.")]
    public float hologramSize = 0.15f;
    [Tooltip("Bật = nét vẽ luôn quay về camera (dễ nhìn nhưng có thể trông như xoay theo người chơi). " +
             "Tắt (khuyến nghị) = nét vẽ CỐ ĐỊNH trong không gian.")]
    public bool linesFaceCamera = false;

    [Header("=== KHUNG DÂY ===")]
    public Color lineColor = new Color(0.35f, 0.8f, 1f, 1f);   // xanh blueprint
    [Tooltip("Độ dày nét vẽ (mét)")]
    public float lineWidth = 0.0035f;
    [Tooltip("Số đoạn để vẽ đường tròn (trụ/nón/cầu)")]
    public int circleSegments = 48;

    [Header("=== VỎ MỜ (tuỳ chọn) ===")]
    [Tooltip("Hiện lớp vỏ đặc mờ bên trong khung dây.")]
    public bool showShell = true;
    [Range(0f, 1f)] public float shellAlpha = 0.12f;
    public Color shellColor = new Color(0.35f, 0.8f, 1f, 1f);

    [Header("=== XOAY ===")]
    public float rotationSpeed = 25f;

    private GameObject currentHologram;
    private static Material s_lineMat;
    private static Material s_shellMat;

    void Start()
    {
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.onOrderChanged.AddListener(OnOrderChanged);
            OrderManager.Instance.onSystemActivated.AddListener(OnSystemActivated);

            if (OrderManager.Instance.IsActive && OrderManager.Instance.CurrentOrder != null)
                OnOrderChanged(OrderManager.Instance.CurrentOrder);
        }
        else Debug.LogWarning("[BlueprintHologram] Không tìm thấy OrderManager.Instance!");
    }

    void OnSystemActivated()
    {
        if (OrderManager.Instance != null && OrderManager.Instance.CurrentOrder != null)
            OnOrderChanged(OrderManager.Instance.CurrentOrder);
    }

    void Update()
    {
        if (currentHologram != null)
            currentHologram.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
    }

    void OnOrderChanged(OrderData order)
    {
        if (currentHologram != null) Destroy(currentHologram);
        if (order == null) return;

        currentHologram = new GameObject("Blueprint_" + order.requiredShapeName);
        // Neo CỐ ĐỊNH vào bảng (hoặc object chứa script) → không trôi theo người chơi.
        Transform anchor = boardAnchor != null ? boardAnchor : transform;
        currentHologram.transform.SetParent(anchor, false);
        currentHologram.transform.localPosition = anchorLocalOffset;
        currentHologram.transform.localRotation = Quaternion.identity;
        currentHologram.transform.localScale = Vector3.one * hologramSize;

        BuildWireframe(currentHologram.transform, order.requiredShapeName);
    }

    // ============ DỰNG KHUNG DÂY THEO LOẠI HÌNH ============
    void BuildWireframe(Transform root, string shapeName)
    {
        string n = (shapeName ?? "").ToLower();

        if (n.Contains("cube"))          BuildCube(root);
        else if (n.Contains("sphere"))   BuildSphere(root);
        else if (n.Contains("cylinder")) BuildCylinder(root);
        else if (n.Contains("cone"))     BuildCone(root);
        else if (n.Contains("pyramid"))  BuildPyramid(root);
        else                             BuildCube(root); // fallback
    }

    // --- Hình lập phương: 8 đỉnh, 12 cạnh ---
    void BuildCube(Transform root)
    {
        Vector3[] v = {
            new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(0.5f,-0.5f,-0.5f), new Vector3(0.5f,-0.5f,0.5f), new Vector3(-0.5f,-0.5f,0.5f),
            new Vector3(-0.5f, 0.5f,-0.5f), new Vector3(0.5f, 0.5f,-0.5f), new Vector3(0.5f, 0.5f,0.5f), new Vector3(-0.5f, 0.5f,0.5f),
        };
        int[,] e = { {0,1},{1,2},{2,3},{3,0}, {4,5},{5,6},{6,7},{7,4}, {0,4},{1,5},{2,6},{3,7} };
        for (int i = 0; i < e.GetLength(0); i++) Edge(root, v[e[i,0]], v[e[i,1]]);
        foreach (var p in v) Vertex(root, p);
        if (showShell) Shell(root, PrimitiveType.Cube, Vector3.one);
    }

    // --- Chóp tứ giác: đáy 4 đỉnh + đỉnh S ---
    void BuildPyramid(Transform root)
    {
        Vector3 A=new Vector3(-0.5f,-0.5f,-0.5f), B=new Vector3(0.5f,-0.5f,-0.5f), C=new Vector3(0.5f,-0.5f,0.5f), D=new Vector3(-0.5f,-0.5f,0.5f);
        Vector3 S=new Vector3(0,0.5f,0);
        Edge(root,A,B); Edge(root,B,C); Edge(root,C,D); Edge(root,D,A);
        Edge(root,S,A); Edge(root,S,B); Edge(root,S,C); Edge(root,S,D);
        foreach (var p in new[]{A,B,C,D,S}) Vertex(root,p);
    }

    // --- Trụ: 2 vòng tròn + đường sinh dọc ---
    void BuildCylinder(Transform root)
    {
        Circle(root, new Vector3(0, 0.5f, 0), 0.5f, Vector3.up);
        Circle(root, new Vector3(0,-0.5f, 0), 0.5f, Vector3.up);
        int lines = 8;
        for (int i = 0; i < lines; i++)
        {
            float a = i / (float)lines * Mathf.PI * 2f;
            float x = Mathf.Cos(a)*0.5f, z = Mathf.Sin(a)*0.5f;
            Edge(root, new Vector3(x,0.5f,z), new Vector3(x,-0.5f,z));
        }
        if (showShell) Shell(root, PrimitiveType.Cylinder, new Vector3(1, 0.5f, 1));
    }

    // --- Nón: vòng đáy + đường sinh tới đỉnh ---
    void BuildCone(Transform root)
    {
        Vector3 S = new Vector3(0, 0.5f, 0);
        Circle(root, new Vector3(0,-0.5f,0), 0.5f, Vector3.up);
        int lines = 8;
        for (int i = 0; i < lines; i++)
        {
            float a = i / (float)lines * Mathf.PI * 2f;
            Edge(root, S, new Vector3(Mathf.Cos(a)*0.5f, -0.5f, Mathf.Sin(a)*0.5f));
        }
        Vertex(root, S);
    }

    // --- Cầu: 3 vòng tròn lớn ---
    void BuildSphere(Transform root)
    {
        Circle(root, Vector3.zero, 0.5f, Vector3.up);
        Circle(root, Vector3.zero, 0.5f, Vector3.right);
        Circle(root, Vector3.zero, 0.5f, Vector3.forward);
        if (showShell) Shell(root, PrimitiveType.Sphere, Vector3.one);
    }

    // ============ HELPERS ============
    void Edge(Transform parent, Vector3 a, Vector3 b)
    {
        var lr = NewLine(parent, 2, false);
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
    }

    void Circle(Transform parent, Vector3 center, float radius, Vector3 axis)
    {
        Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        Vector3 w = Vector3.Cross(axis, u).normalized;
        var lr = NewLine(parent, circleSegments, true);
        for (int i = 0; i < circleSegments; i++)
        {
            float a = i / (float)circleSegments * Mathf.PI * 2f;
            lr.SetPosition(i, center + (u * Mathf.Cos(a) + w * Mathf.Sin(a)) * radius);
        }
    }

    void Vertex(Transform parent, Vector3 pos)
    {
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(dot.GetComponent<Collider>());
        dot.name = "vtx";
        dot.transform.SetParent(parent, false);
        dot.transform.localPosition = pos;
        dot.transform.localScale = Vector3.one * (lineWidth * 3f);
        dot.GetComponent<Renderer>().sharedMaterial = LineMat();
    }

    LineRenderer NewLine(Transform parent, int count, bool loop)
    {
        var go = new GameObject("edge");
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = loop;
        lr.positionCount = count;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        lr.alignment = linesFaceCamera ? LineAlignment.View : LineAlignment.TransformZ;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.material = LineMat();
        lr.startColor = lr.endColor = lineColor;
        return lr;
    }

    void Shell(Transform parent, PrimitiveType type, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.name = "shell";
        go.transform.SetParent(parent, false);
        go.transform.localScale = scale;
        var c = shellColor; c.a = shellAlpha;
        var m = ShellMat();
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.color = c;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    Material LineMat()
    {
        if (s_lineMat == null)
        {
            var sh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            s_lineMat = new Material(sh) { name = "BlueprintLineMat" };
        }
        return s_lineMat;
    }

    Material ShellMat()
    {
        if (s_shellMat == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            s_shellMat = new Material(sh) { name = "BlueprintShellMat" };
            // Bật chế độ Transparent (URP)
            if (s_shellMat.HasProperty("_Surface")) s_shellMat.SetFloat("_Surface", 1);
            s_shellMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            s_shellMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            s_shellMat.SetInt("_ZWrite", 0);
            s_shellMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            s_shellMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        return s_shellMat;
    }
}
