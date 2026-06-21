using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DimensionAutoWire : MonoBehaviour
{
    [Header("Kích thước cơ sở (khi lossyScale = 1)")]
    [Tooltip("Bán kính khi scale = 1 (cho hình tròn xoay)")]
    public float baseRadius = 0.5f;
    [Tooltip("Chiều cao khi scale = 1 (cho chóp/nón/trụ)")]
    public float baseHeight = 1f;
    [Tooltip("Đường sinh khi scale = 1 (cho nón); 0 = bỏ qua")]
    public float baseSlant = 0f;

    [Header("Hiển thị")]
    public UnitHelper.UnitMode unitMode = UnitHelper.UnitMode.Auto;
    public int decimals = 2;
    [Tooltip("Thêm tên label trước số (ví dụ 'AB = 1.50 m'). Tắt nếu chỉ muốn số.")]
    public bool includeLabelName = true;

    [Header("Đợi BuildOverlay tạo label xong rồi mới wire")]
    public int delayFrames = 2;

    private bool isWired = false;

    IEnumerator Start()
    {
        for (int i = 0; i < delayFrames; i++) yield return null;
        Wire();
    }

    [ContextMenu("Re-wire dimensions")]
    public void Wire()
    {
        var labels = GetComponentsInChildren<TextMeshPro>(true);

        // Bước 1: thu thập các label đỉnh để tham chiếu (A, B, C, ..., S, O, O1, O2)
        var vertexMap = new Dictionary<string, Transform>();
        foreach (var lbl in labels)
        {
            string txt = lbl.text.Trim();
            if (IsVertexLabel(txt))
                vertexMap[txt] = lbl.transform;
        }

        // Bước 2: gắn LiveDimension cho mỗi label theo loại
        foreach (var lbl in labels)
        {
            string txt = lbl.text.Trim();

            // Edge: 2 chữ in hoa liền nhau (AB, SC, EF...)
            if (IsEdgeLabel(txt))
            {
                string a = txt[0].ToString();
                string b = txt[1].ToString();
                if (vertexMap.ContainsKey(a) && vertexMap.ContainsKey(b))
                {
                    var ld = EnsureLiveDim(lbl);
                    ld.mode = LiveDimension.Mode.EdgeBetweenPoints;
                    ld.pointA = vertexMap[a];
                    ld.pointB = vertexMap[b];
                    ld.prefix = includeLabelName ? $"{txt} = " : "";
                    ld.unitMode = unitMode;
                    ld.decimals = decimals;
                }
            }
            // Height
            else if (txt == "h")
            {
                var ld = EnsureLiveDim(lbl);
                ld.mode = LiveDimension.Mode.ScaledLength;
                ld.referenceShape = transform;
                ld.baseValue = baseHeight;
                ld.prefix = includeLabelName ? "h = " : "";
                ld.unitMode = unitMode;
                ld.decimals = decimals;
            }
            // Radius
            else if (txt == "r")
            {
                var ld = EnsureLiveDim(lbl);
                ld.mode = LiveDimension.Mode.ScaledLength;
                ld.referenceShape = transform;
                ld.baseValue = baseRadius;
                ld.prefix = includeLabelName ? "r = " : "";
                ld.unitMode = unitMode;
                ld.decimals = decimals;
            }
            // Slant (đường sinh)
            else if (txt == "l" && baseSlant > 0f)
            {
                var ld = EnsureLiveDim(lbl);
                ld.mode = LiveDimension.Mode.ScaledLength;
                ld.referenceShape = transform;
                ld.baseValue = baseSlant;
                ld.prefix = includeLabelName ? "l = " : "";
                ld.unitMode = unitMode;
                ld.decimals = decimals;
            }
            // Các label khác (A, B, S, O, O1, O2): giữ nguyên
        }

        isWired = true;
        Debug.Log($"[DimensionAutoWire] {gameObject.name}: đã wire {CountWired()} labels");
    }

    LiveDimension EnsureLiveDim(TextMeshPro lbl)
    {
        var ld = lbl.GetComponent<LiveDimension>();
        if (ld == null) ld = lbl.gameObject.AddComponent<LiveDimension>();
        return ld;
    }

    int CountWired() => GetComponentsInChildren<LiveDimension>(true).Length;

    bool IsVertexLabel(string txt)
    {
        if (string.IsNullOrEmpty(txt)) return false;
        // 1 chữ in hoa: A, B, S, O...
        if (txt.Length == 1 && char.IsUpper(txt[0])) return true;
        // 2 ký tự: 1 chữ in hoa + 1 chữ số (O1, O2)
        if (txt.Length == 2 && char.IsUpper(txt[0]) && char.IsDigit(txt[1])) return true;
        return false;
    }

    bool IsEdgeLabel(string txt)
    {
        if (txt == null || txt.Length != 2) return false;
        return char.IsUpper(txt[0]) && char.IsUpper(txt[1]) && char.IsLetter(txt[0]) && char.IsLetter(txt[1]);
    }
}
