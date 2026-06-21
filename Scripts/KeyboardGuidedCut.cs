using UnityEngine;
using EzySlice;

/// <summary>
/// CẮT CHÍNH XÁC BẰNG PHÍM (để test trên máy tính, không cần canh tay).
///
/// Bấm phím → cắt khối ĐÚNG Y mặt phẳng dẫn (CutGuide) của nó → lát cắt hoàn hảo,
/// gắn tag CorrectItem, mảnh kế thừa năng lực (scale/cầm/đo) như cắt thường.
///
/// Chọn khối nào để cắt:
///   - Nếu có khối đang được chọn (chạm ngón tay) → cắt khối đó.
///   - Nếu không → cắt khối Sliceable GẦN CAMERA nhất (tiện test).
/// Nếu khối không có CutGuide → cắt theo mặt phẳng mặc định (defaultNormal).
/// </summary>
public class KeyboardGuidedCut : MonoBehaviour
{
    [Header("Phím cắt chính xác")]
    public KeyCode cutKey = KeyCode.G;

    [Header("Material mặt cắt")]
    public Material crossSectionMaterial;

    [Header("Khi khối KHÔNG có CutGuide")]
    [Tooltip("Pháp tuyến mặt phẳng cắt mặc định (local của khối). (0,1,0) = cắt ngang.")]
    public Vector3 defaultNormalLocal = Vector3.up;

    [Header("Tách mảnh")]
    public float separationDistance = 0.12f;
    public float pieceLifetime = 60f;

    [Header("Debug")]
    public bool debugLog = true;

    void Update()
    {
        if (Input.GetKeyDown(cutKey)) CutNow();
    }

    void CutNow()
    {
        Transform shape = PickShape();
        if (shape == null)
        {
            if (debugLog) Debug.Log("[GuidedCut] Không tìm thấy khối Sliceable để cắt.");
            return;
        }

        MeshFilter mf = shape.GetComponent<MeshFilter>();
        if (mf == null)
        {
            var all = shape.GetComponentsInChildren<MeshFilter>(true);
            if (all.Length > 0) mf = all[0];
        }
        if (mf == null || mf.sharedMesh == null) return;
        GameObject meshObj = mf.gameObject;

        // Mặt phẳng cắt = ĐÚNG Y mặt phẳng dẫn (chính xác tuyệt đối)
        Vector3 planeNormal, planePos;
        var guide = shape.GetComponentInParent<CutGuide>();
        bool correct;
        if (guide != null)
        {
            planeNormal = guide.WorldNormal;
            planePos = guide.WorldPoint;
            correct = true;   // cắt đúng tuyệt đối
        }
        else
        {
            planeNormal = shape.TransformDirection(defaultNormalLocal).normalized;
            planePos = mf.GetComponent<Renderer>() != null ? mf.GetComponent<Renderer>().bounds.center : shape.position;
            correct = false;  // không có guide → cắt tự do
        }

        SlicedHull hull = meshObj.Slice(planePos, planeNormal, crossSectionMaterial);
        if (hull == null)
        {
            if (debugLog) Debug.LogWarning("[GuidedCut] Plane không cắt qua mesh.");
            return;
        }

        string tag = correct ? "CorrectItem" : "WrongItem";
        GameObject upper = hull.CreateUpperHull(meshObj, crossSectionMaterial);
        GameObject lower = hull.CreateLowerHull(meshObj, crossSectionMaterial);
        if (upper == null || lower == null) return;

        SetupPiece(upper, meshObj.transform, planeNormal * separationDistance * 0.5f, tag);
        SetupPiece(lower, meshObj.transform, -planeNormal * separationDistance * 0.5f, tag);

        if (debugLog) Debug.Log($"[GuidedCut] Đã cắt CHÍNH XÁC '{shape.name}' theo guide → {tag}.");
        Destroy(shape.gameObject);
    }

    Transform PickShape()
    {
        // Ưu tiên khối đang được chọn
        var sel = ShapeSelector.Instance != null ? ShapeSelector.Instance.selectedShape : null;
        if (sel != null && sel.GetComponentInParent<Sliceable>() != null) return sel;

        // Nếu không, lấy Sliceable gần camera nhất
        Camera cam = Camera.main;
        Vector3 from = cam != null ? cam.transform.position : transform.position;
        Sliceable best = null; float bestD = float.MaxValue;
        foreach (var s in FindObjectsOfType<Sliceable>())
        {
            float d = (s.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = s; }
        }
        return best != null ? best.transform : null;
    }

    void SetupPiece(GameObject piece, Transform source, Vector3 offset, string tag)
    {
        piece.transform.position = source.position + offset;
        piece.transform.rotation = source.rotation;
        piece.transform.localScale = source.lossyScale;

        var col = piece.AddComponent<MeshCollider>();
        col.convex = true;
        var rb = piece.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        piece.tag = tag;
        if (piece.GetComponent<Sliceable>() == null) piece.AddComponent<Sliceable>();
        piece.name = source.name + "_ChildPiece";

        PieceEnricher.Enrich(piece, source);   // mảnh kế thừa năng lực
        if (pieceLifetime > 0f) Destroy(piece, pieceLifetime);
    }
}
