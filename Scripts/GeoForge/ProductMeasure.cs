using UnityEngine;

/// <summary>
/// BỘ ĐO "DẤU VÂN TAY SẢN PHẨM".
///
/// Đo trực tiếp từ MESH THẬT + SCALE THẬT của khối người chơi đem rèn:
///   - volumeCm3 : thể tích thật (cm³)            → bài học thể tích
///   - dimsCm    : kích thước bao 3 chiều (cm)     → dài/rộng/cao
///   - fill      : V_mesh / V_bao  (không đơn vị)  → PHÂN LOẠI khối là gì
///
/// Fill ratio là chìa khoá phân biệt loại khối rất rẻ:
///   Hộp ≈ 1.00 · Trụ ≈ 0.785 (π/4) · Cầu ≈ 0.524 (π/6)
///   Chóp đáy vuông ≈ 0.333 · Nón ≈ 0.262 (π/12)
///
/// Đơn vị: Unity tính bằng MÉT. Helper quy đổi sang CM (× 100) cho khớp đặc tả đơn hàng.
/// </summary>
public enum ShapeClass { Any, Box, Cylinder, Sphere, Cone, Pyramid }

public static class ProductMeasure
{
    public struct Print
    {
        public float volumeCm3;   // thể tích thật (cm³)
        public Vector3 dimsCm;    // kích thước bao (cm), theo trục thế giới
        public float fill;        // V_mesh / V_bao
        public ShapeClass shapeClass; // loại khối suy từ fill
        public bool valid;        // đo được hay không (có mesh + renderer)
    }

    /// <summary>Đo khối: tìm MeshFilter + Renderer ở chính nó hoặc con.</summary>
    public static Print Measure(GameObject obj)
    {
        var p = new Print { valid = false, shapeClass = ShapeClass.Any };
        if (obj == null) return p;

        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf == null) mf = obj.GetComponentInChildren<MeshFilter>();

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) rend = obj.GetComponentInChildren<Renderer>();

        if (rend == null) return p; // không đo được nếu thiếu renderer

        // 1) Kích thước bao (world) → cm
        Vector3 dimsM = rend.bounds.size;            // mét, đã gồm scale + xoay
        p.dimsCm = dimsM * 100f;

        // 2) Thể tích bao (m³) để tính fill
        float vBoundsM3 = Mathf.Max(dimsM.x * dimsM.y * dimsM.z, 1e-9f);

        // 3) Thể tích mesh thật (m³) nếu có mesh đọc được; nếu không, ước lượng từ bao
        float vMeshM3 = MeshVolumeM3(mf);
        if (vMeshM3 <= 0f)
        {
            // Fallback: không đọc được mesh → coi như đặc (fill = 1), dùng thể tích bao.
            vMeshM3 = vBoundsM3;
        }

        p.volumeCm3 = vMeshM3 * 1_000_000f;          // 1 m³ = 1.000.000 cm³
        p.fill = Mathf.Clamp01(vMeshM3 / vBoundsM3);
        p.shapeClass = ClassFromFill(p.fill);
        p.valid = true;
        return p;
    }

    /// <summary>Thể tích mesh (m³) theo tổng tứ diện, nhân lossyScale. Trả 0 nếu không đọc được.</summary>
    static float MeshVolumeM3(MeshFilter mf)
    {
        if (mf == null) return 0f;
        Mesh m = mf.sharedMesh;
        if (m == null || !m.isReadable) return 0f;

        Vector3[] v = m.vertices;
        int[] t = m.triangles;
        if (v == null || t == null || t.Length < 3) return 0f;

        double vLocal = 0.0;
        for (int i = 0; i + 2 < t.Length; i += 3)
        {
            Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
            vLocal += Vector3.Dot(a, Vector3.Cross(b, c)) / 6.0;
        }

        Vector3 s = mf.transform.lossyScale;
        float scaleVol = Mathf.Abs(s.x * s.y * s.z);
        return Mathf.Abs((float)vLocal) * scaleVol;
    }

    /// <summary>Phân loại khối từ fill ratio. Ngưỡng có thể tinh chỉnh.</summary>
    public static ShapeClass ClassFromFill(float fill)
    {
        if (fill >= 0.86f) return ShapeClass.Box;        // ~1.00
        if (fill >= 0.62f) return ShapeClass.Cylinder;   // ~0.785
        if (fill >= 0.40f) return ShapeClass.Sphere;     // ~0.524
        if (fill >= 0.30f) return ShapeClass.Pyramid;    // ~0.333 (đáy vuông)
        return ShapeClass.Cone;                          // ~0.262
    }
}
