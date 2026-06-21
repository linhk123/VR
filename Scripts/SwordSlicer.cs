using UnityEngine;
using EzySlice;

[RequireComponent(typeof(BoxCollider))]
public class SwordSlicer : MonoBehaviour
{
    [Header("Material mặt cắt")]
    public Material crossSectionMaterial;

    [Header("Tham số")]
    public float separationDistance = 0.15f;

    [Header("Chế độ kích cắt")]
    [Tooltip("Bật khi test Simulator — chạm kiếm là cắt, không cần vung nhanh")]
    public bool sliceOnContact = true;
    public float minSwingVelocity = 0.5f;

    [Header("--- HỆ THỐNG ĐƠN HÀNG (MOCK) ---")]
    [Tooltip("Góc cắt mục tiêu mà NPC yêu cầu cho đơn hàng này (Ví dụ: cắt thẳng từ trên xuống là Vector3.up hoặc cắt ngang là Vector3.right)")]
    public Vector3 orderTargetNormal = Vector3.up; 
    [Tooltip("Sai số góc cho phép (độ). Cắt lệch dưới mức này thì tính là ĐÚNG")]
    public float allowedAngleError = 15f;

    [Header("Debug")]
    public bool debugLog = true;

    private Vector3 lastPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        // Khắc phục lỗi: Không cắt chính các mảnh vừa được tạo ra

        var sliceable = other.GetComponentInParent<Sliceable>();
        if (sliceable == null) return;

        if (debugLog)
            Debug.Log($"[SwordSlicer] Chạm Sliceable: {sliceable.name}, sliceOnContact={sliceOnContact}");

        float speed = currentVelocity.magnitude;

        if (!sliceOnContact && speed < minSwingVelocity)
        {
            if (debugLog) Debug.Log($"[SwordSlicer] Vung quá chậm ({speed:F2} m/s).");
            return;
        }

        TrySlice(sliceable.transform, speed);
    }

    void TrySlice(Transform shape, float speed)
    {
        if (debugLog) Debug.Log($"[SwordSlicer] Bắt đầu TrySlice {shape.name}.");

        MeshFilter mf = shape.GetComponent<MeshFilter>();
        if (mf == null)
        {
            var allMfs = shape.GetComponentsInChildren<MeshFilter>(true);
            if (allMfs.Length > 0) mf = allMfs[0];
        }

        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning($"[SwordSlicer] Không thấy MeshFilter trên {shape.name}.");
            return;
        }

        GameObject meshObj = mf.gameObject;

        // Tính toán mặt phẳng cắt giống logic cũ của bạn
        Vector3 swordAxis = transform.forward;  
        Vector3 swingDir = (currentVelocity.magnitude > 0.05f) ? currentVelocity.normalized : transform.right;

        Vector3 planeNormal = Vector3.Cross(swordAxis, swingDir).normalized;
        if (planeNormal.sqrMagnitude < 0.001f) planeNormal = transform.up;
        Vector3 planeWorldPos = transform.position;

        // === DẪN CẮT (chống lệch) ===
        // Nếu khối có CutGuide: canh gần đúng → "hít" lát cắt về mặt phẳng dẫn (cắt đẹp);
        // lệch quá ngưỡng → giữ nguyên lát xiên của người chơi (cắt xấu).
        bool isCorrectCut;
        var guide = shape.GetComponentInParent<CutGuide>();
        if (guide != null)
        {
            var r = guide.Evaluate(planeWorldPos, planeNormal);
            if (r.within)
            {
                planeNormal = r.planeNormal;     // SNAP về mặt phẳng dẫn
                planeWorldPos = r.planePos;
            }
            isCorrectCut = r.within;
            if (CutAssist.Instance != null) CutAssist.Instance.Register(r.within);
            if (debugLog)
                Debug.Log($"[DẪN CẮT] góc lệch {r.angleErr:F1}°, vị trí lệch {r.posErr*100f:F1}cm → " +
                          (r.within ? $"SNAP ĐẸP (chất lượng {r.quality:P0})" : "LỆCH → cắt xấu"));
        }
        else
        {
            // Fallback cũ: so góc chém với orderTargetNormal
            float angleError = Vector3.Angle(planeNormal, orderTargetNormal);
            if (angleError > 90f) angleError = Mathf.Abs(180f - angleError);
            isCorrectCut = (angleError <= allowedAngleError);
            if (debugLog)
                Debug.Log($"[CHẤM ĐIỂM CẮT] Độ lệch góc: {angleError:F1}° / Cho phép: {allowedAngleError}°.");
        }

        string resultTag = isCorrectCut ? "CorrectItem" : "WrongItem";

        // Tiến hành cắt bằng EzySlice (dùng mặt phẳng có thể đã được snap)
        SlicedHull hull = meshObj.Slice(planeWorldPos, planeNormal, crossSectionMaterial);
        if (hull == null)
        {
            Debug.LogWarning($"[SwordSlicer] EzySlice trả về null — plane không cắt qua mesh.");
            return;
        }

        if (debugLog) Debug.Log($"[SwordSlicer] KẾT QUẢ: {resultTag}");

        // Tạo 2 mảnh mới
        GameObject upper = hull.CreateUpperHull(meshObj, crossSectionMaterial);
        GameObject lower = hull.CreateLowerHull(meshObj, crossSectionMaterial);
        
        if (upper == null || lower == null) return;

        // Thiết lập mảnh cắt rơi tự do vật lý và dán nhãn Tag kết quả cho Lò Rèn nhận biết
        SetupPiece(upper, meshObj.transform, planeNormal * separationDistance * 0.5f, resultTag);
        SetupPiece(lower, meshObj.transform, -planeNormal * separationDistance * 0.5f, resultTag);

        // Hủy khối hình gốc (Hoặc gọi hàm hồi sinh khối mới tại đây nếu cần)
        Destroy(shape.gameObject); 
        
        // GỢI Ý: Gọi hàm hồi sinh khối hình mới trên bàn làm việc sau khi khối cũ bị phá hủy
        // ObjectSpawner.Instance.SpawnNewBlockDelayed(3f); 
    }

    void SetupPiece(GameObject piece, Transform source, Vector3 offset, string resultTag)
{
    piece.transform.position = source.position + offset;
    piece.transform.rotation = source.rotation;
    piece.transform.localScale = source.lossyScale;

    var col = piece.AddComponent<MeshCollider>();
    col.convex = true;

    var rb = piece.AddComponent<Rigidbody>();
    rb.isKinematic = false; 
    rb.useGravity = true;   

    piece.tag = resultTag;

    // 🔥 QUAN TRỌNG NHẤT: Cấp "giấy phép" Sliceable cho mảnh mới để có thể chém tiếp lần sau
    if (piece.GetComponent<Sliceable>() == null)
    {
        piece.AddComponent<Sliceable>();
    }

    // Đổi tên Object trong Hierarchy để bạn dễ Debug theo dõi
    piece.name = source.name + "_ChildPiece";

    // MẢNH KẾ THỪA NĂNG LỰC: chọn/scale/cầm + hiện số đo
    PieceEnricher.Enrich(piece, source);

    // Tăng thời gian hủy lên 60 giây (hoặc lâu hơn)
    // để tránh việc người chơi đang tập trung cắt tỉa thì vật thể biến mất
    Destroy(piece, 60f);
}
}