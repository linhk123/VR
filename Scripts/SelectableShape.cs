using UnityEngine;

/// <summary>
/// Cho phép 1 khối (kể cả MẢNH cắt ra) được CHỌN khi đầu ngón tay chạm,
/// để ScaleController (phím 9/0) phóng to/thu nhỏ và SliceController cắt tiếp.
/// Nhẹ hơn các script *Interactive (không dựng overlay), dùng cho mảnh cắt.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SelectableShape : MonoBehaviour
{
    [HideInInspector] public int touching = 0;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("FingerTip")) return;
        touching++;
        if (ShapeSelector.Instance != null) ShapeSelector.Instance.SetSelected(transform);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("FingerTip")) return;
        touching = Mathf.Max(0, touching - 1);
        if (ShapeSelector.Instance != null) ShapeSelector.Instance.ClearSelected(transform);
    }
}
