using UnityEngine;
using TMPro;

public class BoundingBoxDimensions : MonoBehaviour
{
    [Header("Cài đặt hiển thị")]
    public Color labelColor = new Color(1f, 0.85f, 0f);   // vàng
    public float labelOffsetMeters = 0.05f;
    public UnitHelper.UnitMode unitMode = UnitHelper.UnitMode.Auto;
    public int decimals = 2;

    [Header("Nhãn (tự sinh nếu để trống)")]
    public string prefixW = "Rộng: ";
    public string prefixH = "Cao: ";
    public string prefixD = "Sâu: ";

    [Header("Chỉ tính renderer có tag/ tên cụ thể (tuỳ chọn)")]
    public string mainMeshChildName = "Mesh";   // nếu rỗng: dùng all renderers

    private TextMeshPro labelW, labelH, labelD;
    private Transform cam;

    void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        CreateLabels();
    }

    void CreateLabels()
    {
        labelW = CreateLabel("DimLabel_W");
        labelH = CreateLabel("DimLabel_H");
        labelD = CreateLabel("DimLabel_D");
    }

    TextMeshPro CreateLabel(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "";
        tmp.color = labelColor;
        tmp.fontSize = 1f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.transform.localScale = Vector3.one * 0.1f;
        // Renderer 2-sided để đọc từ mọi hướng
        var mr = tmp.GetComponent<MeshRenderer>();
        if (mr) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return tmp;
    }

    void LateUpdate()
    {
        if (labelW == null) return;

        Bounds b = ComputeBounds();
        if (b.size == Vector3.zero) return;

        Vector3 c = b.center;
        Vector3 s = b.size;
        float off = labelOffsetMeters;

        // X = Rộng (width)
        labelW.transform.position = c + Vector3.right * (s.x * 0.5f + off);
        labelW.text = prefixW + UnitHelper.FormatLength(s.x, unitMode, decimals);

        // Y = Cao (height)
        labelH.transform.position = c + Vector3.up * (s.y * 0.5f + off);
        labelH.text = prefixH + UnitHelper.FormatLength(s.y, unitMode, decimals);

        // Z = Sâu (depth)
        labelD.transform.position = c + Vector3.forward * (s.z * 0.5f + off);
        labelD.text = prefixD + UnitHelper.FormatLength(s.z, unitMode, decimals);

        // Xoay 3 label về phía camera để dễ đọc
        if (cam != null)
        {
            FaceCamera(labelW.transform);
            FaceCamera(labelH.transform);
            FaceCamera(labelD.transform);
        }
    }

    void FaceCamera(Transform t)
    {
        Vector3 dir = t.position - cam.position;
        if (dir.sqrMagnitude < 0.0001f) return;
        t.rotation = Quaternion.LookRotation(dir);
    }

    Bounds ComputeBounds()
    {
        Renderer[] renderers;
        if (!string.IsNullOrEmpty(mainMeshChildName))
        {
            var t = transform.Find(mainMeshChildName);
            if (t != null)
            {
                var r = t.GetComponent<Renderer>();
                if (r != null) return r.bounds;
            }
        }
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds();

        // Bỏ qua các Renderer của TextMeshPro để không tính label vào bounds
        Bounds b = new Bounds();
        bool first = true;
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled) continue;
            if (r.GetComponent<TextMeshPro>() != null) continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }
}