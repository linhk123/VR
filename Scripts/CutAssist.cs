using UnityEngine;

/// <summary>
/// ĐIỀU PHỐI ĐỘ KHÓ CẮT + TỰ HOÀN THIỆN (trợ giúp học sinh cắt đúng).
///
/// Đặt 1 object trong scene, gắn script này (singleton). `CutGuide` sẽ tự dùng nó để
/// NỚI/THU dung sai snap theo cấp độ:
///   - Practice: rất rộng (gần như chặt đâu gần cũng "hít" thành lát cắt chuẩn).
///   - Easy / Normal / Hard: hẹp dần.
/// "Tự hoàn thiện": khi cú chặt nằm trong dải hỗ trợ → LUÔN snap về đúng mặt phẳng dẫn
/// → lát cắt hoàn hảo dù tay hơi lệch.
///
/// "Tự nới khi chật vật": nếu học sinh trượt liên tiếp, dung sai tự nới rộng thêm
/// để lần sau dễ trúng; trúng 1 lần thì trở lại bình thường.
/// </summary>
[DisallowMultipleComponent]
public class CutAssist : MonoBehaviour
{
    public static CutAssist Instance { get; private set; }

    public enum Level { Practice, Easy, Normal, Hard }

    [Header("Độ khó")]
    public Level level = Level.Easy;

    [Header("Tự nới dung sai khi học sinh trượt liên tiếp")]
    public bool adaptiveAssist = true;
    [Tooltip("Trượt mấy lần liên tiếp thì bắt đầu nới rộng.")]
    public int failsBeforeBoost = 2;
    [Range(1f, 3f)] public float boostStep = 1.6f;
    [Range(1f, 5f)] public float maxBoost = 3f;

    [Header("Tự hoàn thiện")]
    [Tooltip("Trong dải hỗ trợ thì luôn snap về mặt phẳng dẫn chuẩn (lát cắt hoàn hảo).")]
    public bool autoFinishCut = true;

    [Header("Debug")]
    public bool debugLog = false;

    int _consecutiveFails;
    float _boost = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    float LevelMultiplier()
    {
        switch (level)
        {
            case Level.Practice: return 2.2f;   // ~48° góc — rộng nhưng không "hướng nào cũng trúng"
            case Level.Easy:     return 1.6f;   // ~35°
            case Level.Hard:     return 0.6f;   // ~13°
            default:             return 1.0f;   // Normal ~22°
        }
    }

    /// <summary>Dung sai GÓC hiệu dụng (đã nhân độ khó + boost thích nghi).</summary>
    public float AngleTol(float baseAngle) => baseAngle * LevelMultiplier() * _boost;
    /// <summary>Dung sai VỊ TRÍ hiệu dụng.</summary>
    public float DistTol(float baseDist) => baseDist * LevelMultiplier() * _boost;

    /// <summary>Cutter gọi sau mỗi nhát để bộ thích nghi cập nhật.</summary>
    public void Register(bool success)
    {
        if (success)
        {
            _consecutiveFails = 0;
            _boost = 1f;
        }
        else if (adaptiveAssist)
        {
            _consecutiveFails++;
            if (_consecutiveFails >= failsBeforeBoost)
                _boost = Mathf.Min(maxBoost, _boost * boostStep);
            if (debugLog)
                Debug.Log($"[CutAssist] Trượt {_consecutiveFails} lần → nới dung sai ×{_boost:F1}");
        }
    }
}
