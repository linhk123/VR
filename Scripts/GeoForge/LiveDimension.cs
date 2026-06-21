using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
[DisallowMultipleComponent]
public class LiveDimension : MonoBehaviour
{
    public enum Mode
    {
        EdgeBetweenPoints,   // khoảng cách 2 điểm (cho AB, BC...)
        ScaledLength         // baseValue × lossyScale (cho h, r, l)
    }

    [Header("Loại đo")]
    public Mode mode = Mode.EdgeBetweenPoints;

    [Header("Cho EdgeBetweenPoints")]
    public Transform pointA;
    public Transform pointB;

    [Header("Cho ScaledLength")]
    public Transform referenceShape;
    public float baseValue = 1f;   // giá trị khi scale = 1

    [Header("Hiển thị")]
    public string prefix = "";     // ví dụ "AB = ", "h = "
    public UnitHelper.UnitMode unitMode = UnitHelper.UnitMode.Auto;
    public int decimals = 2;

    private TextMeshPro tmp;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }

    void LateUpdate()
    {
        if (tmp == null) return;
        float meters = Compute();
        tmp.text = prefix + UnitHelper.FormatLength(meters, unitMode, decimals);
    }

    float Compute()
    {
        switch (mode)
        {
            case Mode.EdgeBetweenPoints:
                if (pointA == null || pointB == null) return 0f;
                return Vector3.Distance(pointA.position, pointB.position);

            case Mode.ScaledLength:
                if (referenceShape == null) return baseValue;
                // dùng scale.x — giả định scale uniform
                return baseValue * referenceShape.lossyScale.x;
        }
        return 0f;
    }
}