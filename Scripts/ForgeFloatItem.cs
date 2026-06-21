using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;   // XRGrabInteractable (XRI 3.x)

/// <summary>
/// Làm CHÍNH object gắn script LƠ LỬNG tại GIỮA LÒ RÈN (fi_vil_forge_forge3_lit) + offset.
///
/// Chống văng triệt để:
///   - Tắt mọi grab (XRGrabInteractable + Grabbable) → không hệ thống nào kéo vật.
///   - Ép mọi Rigidbody (self + con) kinematic + tắt gravity → không rơi.
///   - Ghim vị trí ở FixedUpdate & LateUpdate, điều khiển ĐÚNG object gắn script.
///
/// CÁCH GẮN:
///   1. Gắn script lên vật cần lơ lửng (vd Wooden_Bucket).
///   2. Kéo object lò "fi_vil_forge_forge3_lit" vào ô Forge Center.
///      (Bỏ trống thì script tự tìm theo tên.)
///   3. Chỉnh Offset (mặc định nhô lên 0.8m) để vật nổi trên miệng lò, không lọt trong mesh.
/// </summary>
public class ForgeFloatItem : MonoBehaviour
{
    [Header("Tâm lò")]
    [Tooltip("Kéo object lò 'fi_vil_forge_forge3_lit' vào. Bỏ trống = tự tìm theo tên.")]
    public Transform forgeCenter;
    [Tooltip("Tên object lò để tự tìm nếu Forge Center bỏ trống.")]
    public string forgeName = "fi_vil_forge_forge3_lit";

    [Header("Lệch so với tâm lò (mét)")]
    [Tooltip("Nâng vật lên cho nổi trên miệng lò, khỏi lọt trong mesh.")]
    public Vector3 offset = new Vector3(0f, 0.8f, 0f);

    [Header("Bồng bềnh / Xoay")]
    public float bobHeight = 0.05f;
    public float bobSpeed = 1.5f;
    public bool rotate = true;
    public float rotateSpeed = 25f;

    [Header("Tuỳ chọn")]
    public bool floating = true;
    public bool debugLog = true;

    [Header("Cho phép cầm")]
    [Tooltip("Vật lơ lửng tại lò, NHƯNG vẫn cầm được. Khi người chơi cầm, ngừng ghim vị trí; " +
             "sau khi cầm lần đầu thì nhường hẳn quyền điều khiển cho hệ cầm-nắm.")]
    public bool allowGrab = true;

    Rigidbody[] _rbs;
    float _phase;
    Grabbable _grab;          // tham chiếu Grabbable trên chính vật (nếu có)
    bool _grabbedOnce;        // đã từng được cầm → thôi ghim, để cầm/di chuyển bình thường

    void Start()
    {
        if (forgeCenter == null && !string.IsNullOrEmpty(forgeName))
        {
            var go = GameObject.Find(forgeName);
            if (go != null) forgeCenter = go.transform;
        }
        if (forgeCenter == null)
        {
            Debug.LogWarning($"[ForgeFloatItem] '{name}': KHÔNG tìm thấy lò (Forge Center trống và không thấy '{forgeName}'). " +
                             "Hãy kéo object lò vào ô Forge Center.");
            enabled = false;
            return;
        }

        _phase = Random.value * 10f;

        _grab = GetComponent<Grabbable>();
        if (!allowGrab)
        {
            // (Cũ) Chặn cầm hoàn toàn — chỉ dùng nếu muốn vật cố định.
            var xr = GetComponent<XRGrabInteractable>();
            if (xr != null) xr.enabled = false;
            if (_grab != null) _grab.enabled = false;
        }
        // allowGrab = true → GIỮ NGUYÊN Grabbable để người chơi cầm được.

        // Ép kinematic mọi Rigidbody trong vật + con (để lơ lửng, không rơi)
        _rbs = GetComponentsInChildren<Rigidbody>(true);
        SetKinematic();

        transform.position = TargetPos();

        if (debugLog)
            Debug.Log($"[ForgeFloatItem] '{name}': lò='{forgeCenter.name}' tại {forgeCenter.position:F2}, " +
                      $"vật sẽ lơ lửng tại {TargetPos():F2}. Số Rigidbody={_rbs.Length}. allowGrab={allowGrab}.");
    }

    // Đang cầm? (chỉ khi cho phép cầm và có Grabbable đang được giữ)
    bool IsHeld => allowGrab && _grab != null && _grab.isHeld;

    Vector3 TargetPos()
    {
        return forgeCenter.position + offset;
    }

    void SetKinematic()
    {
        if (_rbs == null) return;
        foreach (var rb in _rbs)
        {
            if (rb == null) continue;
            // Chỉ xoá vận tốc khi còn động — set velocity trên body KINEMATIC gây spam warning.
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    void Pin()
    {
        Vector3 p = TargetPos();
        p.y += Mathf.Sin((Time.time + _phase) * bobSpeed) * bobHeight;
        transform.position = p;
    }

    void FixedUpdate()
    {
        if (!floating || forgeCenter == null) return;
        if (_grabbedOnce) return;          // đã cầm → nhường quyền cho hệ cầm-nắm
        if (IsHeld) { _grabbedOnce = true; return; } // đang cầm → ngừng ghim ngay
        SetKinematic();
        Pin();
    }

    void LateUpdate()
    {
        if (!floating || forgeCenter == null) return;
        if (_grabbedOnce) return;          // đã cầm → không ghim/không xoay nữa
        if (IsHeld) { _grabbedOnce = true; return; }
        Pin();
        if (rotate) transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    public void Freeze() => floating = false;
    public void Resume() => floating = true;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (forgeCenter == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(forgeCenter.position + offset, 0.1f);
    }
#endif
}
