using System.Collections;
using UnityEngine;
using EzySlice;

/// <summary>
/// CẮT BẰNG CÚ CHẶT CỦA TAY — bản dành cho HAND TRACKING / Building Blocks.
///
/// Gắn TRỰC TIẾP lên object "[BuildingBlock] Hand Tracking right" (object có OVRSkeleton + OVRHand).
/// KHÔNG cần kéo thả object ngón út: script TỰ chờ skeleton khởi tạo rồi tạo 1 collider "lưỡi tay"
/// bám vào bone cạnh bàn tay (mặc định Hand_Pinky3) lúc chạy.
///
/// Chỉ cắt khi:
///   1. Tay VUNG đủ nhanh (>= minChopSpeed) — bỏ "chạm nhẹ là cắt".
///   2. Hết cooldown.
///   3. Tay đang MỞ (không pinch/nắm).
/// Vẫn dùng CutGuide để snap chống lệch + chấm CorrectItem/WrongItem.
/// </summary>
public class HandChopCutter : MonoBehaviour
{
    [Header("Tham chiếu (tự lấy nếu để trống)")]
    public OVRSkeleton skeleton;
    public OVRHand hand;
    public Material crossSectionMaterial;

    [Header("Bone làm 'lưỡi tay'")]
    [Tooltip("Bone bám collider chặt. Mặc định cạnh bàn tay (ngón út).")]
    public OVRSkeleton.BoneId chopBone = OVRSkeleton.BoneId.Hand_Pinky3;
    [Tooltip("Kích thước hộp va chạm của lưỡi tay (mét).")]
    public Vector3 colliderSize = new Vector3(0.02f, 0.045f, 0.10f);

    [Header("Điều kiện 'chặt dứt khoát'")]
    [Tooltip("Tốc độ tay tối thiểu để tính là cú chặt (m/s).")]
    public float minChopSpeed = 1.3f;
    [Tooltip("Nghỉ giữa 2 nhát (giây).")]
    public float cooldown = 0.6f;
    [Tooltip("Pinch mạnh hơn mức này = đang nắm → không chặt.")]
    [Range(0f, 1f)] public float pinchBlock = 0.5f;

    [Header("Tách mảnh")]
    public float separationDistance = 0.12f;
    public float pieceLifetime = 60f;

    [Header("Debug")]
    public bool debugLog = true;

    Transform _blade;          // object collider bám bone
    Vector3 _lastPos;
    Vector3 _velocity;
    float _lastCutTime = -999f;

    IEnumerator Start()
    {
        if (skeleton == null) skeleton = GetComponent<OVRSkeleton>() ?? GetComponentInChildren<OVRSkeleton>(true);
        if (hand == null) hand = GetComponent<OVRHand>() ?? GetComponentInChildren<OVRHand>(true);

        // Chờ skeleton sinh bone
        while (skeleton == null || !skeleton.IsInitialized || skeleton.Bones.Count == 0)
            yield return null;

        Transform bone = null;
        foreach (var b in skeleton.Bones)
            if (b.Id == chopBone) { bone = b.Transform; break; }

        if (bone == null)
        {
            Debug.LogWarning($"[HandChop] Không tìm thấy bone {chopBone} trên skeleton.");
            yield break;
        }

        // Tạo "lưỡi tay" bám vào bone
        var go = new GameObject("HandChopBlade");
        go.transform.SetParent(bone, false);

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = colliderSize;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var relay = go.AddComponent<ChopColliderRelay>();
        relay.owner = this;

        _blade = go.transform;
        _lastPos = _blade.position;

        if (debugLog) Debug.Log($"[HandChop] Đã gắn lưỡi tay vào bone {chopBone}.");
    }

    void Update()
    {
        if (_blade == null) return;
        _velocity = (_blade.position - _lastPos) / Mathf.Max(Time.deltaTime, 1e-5f);
        _lastPos = _blade.position;
    }

    // Gọi từ ChopColliderRelay
    public void OnBladeTrigger(Collider other)
    {
        var sliceable = other.GetComponentInParent<Sliceable>();
        if (sliceable == null) return;

        if (Time.time - _lastCutTime < cooldown) return;

        if (hand != null && hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > pinchBlock)
            return;   // đang nắm → không chặt

        float speed = _velocity.magnitude;
        if (speed < minChopSpeed)
        {
            if (debugLog) Debug.Log($"[HandChop] Chạm nhẹ ({speed:F2} m/s) → KHÔNG cắt. Hãy chặt dứt khoát.");
            return;
        }

        TrySlice(sliceable.transform, other);
    }

    void TrySlice(Transform shape, Collider hitCol)
    {
        MeshFilter mf = shape.GetComponent<MeshFilter>();
        if (mf == null)
        {
            var all = shape.GetComponentsInChildren<MeshFilter>(true);
            if (all.Length > 0) mf = all[0];
        }
        if (mf == null || mf.sharedMesh == null) return;
        GameObject meshObj = mf.gameObject;

        Vector3 bladeAxis = _blade.forward;                 // dọc theo bone tay
        Vector3 swingDir = _velocity.normalized;
        Vector3 planeNormal = Vector3.Cross(bladeAxis, swingDir).normalized;
        if (planeNormal.sqrMagnitude < 0.001f) planeNormal = _blade.up;
        Vector3 planeWorldPos = hitCol != null ? hitCol.ClosestPoint(_blade.position) : _blade.position;

        bool isCorrectCut;
        var guide = shape.GetComponentInParent<CutGuide>();
        if (guide != null)
        {
            var r = guide.Evaluate(planeWorldPos, planeNormal);
            if (r.within) { planeNormal = r.planeNormal; planeWorldPos = r.planePos; }
            isCorrectCut = r.within;
            if (CutAssist.Instance != null) CutAssist.Instance.Register(r.within);
            if (debugLog)
                Debug.Log($"[HandChop] {_velocity.magnitude:F2} m/s · lệch {r.angleErr:F1}°/{r.posErr*100f:F1}cm → " +
                          (r.within ? $"CHẶT ĐẸP ({r.quality:P0})" : "LỆCH → mảnh xấu"));
        }
        else isCorrectCut = true;

        SlicedHull hull = meshObj.Slice(planeWorldPos, planeNormal, crossSectionMaterial);
        if (hull == null)
        {
            if (debugLog) Debug.LogWarning("[HandChop] Plane không cắt qua mesh.");
            return;
        }

        string resultTag = isCorrectCut ? "CorrectItem" : "WrongItem";
        GameObject upper = hull.CreateUpperHull(meshObj, crossSectionMaterial);
        GameObject lower = hull.CreateLowerHull(meshObj, crossSectionMaterial);
        if (upper == null || lower == null) return;

        SetupPiece(upper, meshObj.transform, planeNormal * separationDistance * 0.5f, resultTag);
        SetupPiece(lower, meshObj.transform, -planeNormal * separationDistance * 0.5f, resultTag);

        Destroy(shape.gameObject);
        _lastCutTime = Time.time;
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
        if (piece.GetComponent<Sliceable>() == null) piece.AddComponent<Sliceable>();
        piece.name = source.name + "_ChildPiece";

        // MẢNH KẾ THỪA NĂNG LỰC: chọn/scale/cầm + hiện số đo
        PieceEnricher.Enrich(piece, source);

        if (pieceLifetime > 0f) Destroy(piece, pieceLifetime);
    }
}

/// <summary>Chuyển sự kiện trigger của 'lưỡi tay' về HandChopCutter.</summary>
public class ChopColliderRelay : MonoBehaviour
{
    [HideInInspector] public HandChopCutter owner;
    void OnTriggerEnter(Collider other)
    {
        if (owner != null) owner.OnBladeTrigger(other);
    }
}
