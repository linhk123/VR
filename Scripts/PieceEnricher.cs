using UnityEngine;

/// <summary>
/// Trao "năng lực" cho MẢNH cắt ra để nó không bị trần:
///   - Chọn được (SelectableShape) → phóng to/thu nhỏ (ScaleController) + cắt tiếp.
///   - Cầm được (Grabbable) — kế thừa cấu hình khối gốc.
///   - Hiện số đo/đơn vị (BoundingBoxDimensions), tự cập nhật theo scale.
///   - Nếu khối gốc dùng HandPinchManipulator (scale 2 tay) → copy luôn (giữ refs tay).
/// Gọi từ SetupPiece của SwordSlicer / HandChopCutter.
/// </summary>
public static class PieceEnricher
{
    [Tooltip("Có gắn nhãn số đo cho mảnh không (tắt nếu thấy rối).")]
    public static bool addDimensions = true;

    public static void Enrich(GameObject piece, Transform source)
    {
        if (piece == null) return;

        // 1) Chọn được (để scale bằng phím + cắt tiếp)
        if (piece.GetComponent<SelectableShape>() == null)
            piece.AddComponent<SelectableShape>();

        // 2) Nhãn số đo / đơn vị
        if (addDimensions && piece.GetComponent<BoundingBoxDimensions>() == null)
            piece.AddComponent<BoundingBoxDimensions>();

        // 3) Kế thừa hệ thống scale 2 tay nếu khối gốc dùng nó
        var srcPinch = source != null ? source.GetComponentInParent<HandPinchManipulator>() : null;
        if (srcPinch != null && piece.GetComponent<HandPinchManipulator>() == null)
        {
            var m = piece.AddComponent<HandPinchManipulator>();
            m.leftHand = srcPinch.leftHand;
            m.rightHand = srcPinch.rightHand;
            m.pinchLeft = srcPinch.pinchLeft;
            m.pinchRight = srcPinch.pinchRight;
            m.gripRight = srcPinch.gripRight;
            m.scaleSteps = srcPinch.scaleSteps;
            m.defaultScaleIndex = srcPinch.defaultScaleIndex;
            m.allowMove = srcPinch.allowMove;
            m.allowRotate = srcPinch.allowRotate;
            m.allowScale = srcPinch.allowScale;
        }
        else if (piece.GetComponent<Grabbable>() == null)
        {
            // 4) Không có pinch-manipulator → ít nhất cho cầm + scale bằng phím
            var g = piece.AddComponent<Grabbable>();
            var srcGrab = source != null ? source.GetComponentInParent<Grabbable>() : null;
            if (srcGrab != null)
            {
                g.dropPhysicsOnRelease = srcGrab.dropPhysicsOnRelease;
                g.maxReleaseSpeed = srcGrab.maxReleaseSpeed;
                g.maxReleaseAngular = srcGrab.maxReleaseAngular;
            }
        }
    }
}
