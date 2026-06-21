using System.Collections;
using UnityEngine;

public class ForgeChecker : MonoBehaviour
{
    [Header("=== CÀI ĐẶT ===")]
    [Tooltip("Thời gian khối phải ổn định trong vùng trước khi check (giây)")]
    public float settleTime = 1.5f;

    [Tooltip("Tag của khối có thể rèn (để trống nếu dùng tên)")]
    public string forgeableTag = "Forgeable";

    [Header("=== TRẠNG THÁI ===")]
    [SerializeField] private bool isChecking = false;
    [SerializeField] private GameObject currentBlock;

    void OnTriggerEnter(Collider other)
    {
        // Đang xử lý 1 khối rồi → bỏ qua
        if (isChecking) return;

        // ForgeProductSpawner đang chạy hiệu ứng → bỏ qua
        if (ForgeProductSpawner.Instance != null && ForgeProductSpawner.Instance.IsForging()) return;

        // Kiểm tra có phải khối có thể rèn không
        if (!IsForgeable(other.gameObject)) return;

        // Kiểm tra OrderManager đã kích hoạt chưa
        if (OrderManager.Instance == null || !OrderManager.Instance.IsActive)
        {
            Debug.LogWarning("[ForgeChecker] Hệ thống đơn hàng chưa kích hoạt!");
            return;
        }

        currentBlock = other.gameObject;
        Debug.Log($"<color=cyan>[ForgeChecker] Phát hiện khối: {currentBlock.name}</color>");
        StartCoroutine(CheckAfterSettle(currentBlock));
    }

    void OnTriggerExit(Collider other)
    {
        // Nếu khối ra khỏi vùng trước khi check xong → hủy
        if (other.gameObject == currentBlock && isChecking)
        {
            Debug.Log("[ForgeChecker] Khối thoát ra ngoài, hủy check");
            StopAllCoroutines();
            isChecking = false;
            currentBlock = null;
        }
    }

    bool IsForgeable(GameObject obj)
    {
        // Cách 1: theo tag
        if (!string.IsNullOrEmpty(forgeableTag) && obj.CompareTag(forgeableTag)) return true;

        // Cách 2: theo tên (nếu chưa set tag)
        string name = obj.name.ToLower();
        return name.Contains("cube") || name.Contains("sphere") ||
               name.Contains("cylinder") || name.Contains("cone") ||
               name.Contains("pyramid") || name.Contains("chop");
    }

    IEnumerator CheckAfterSettle(GameObject block)
    {
        isChecking = true;

        // Đợi khối ổn định
        yield return new WaitForSeconds(settleTime);

        // Khối có thể đã bị destroy trong lúc đợi
        if (block == null)
        {
            isChecking = false;
            yield break;
        }

        // Đo thể tích + nhận diện hình
        string shapeName = GetShapeName(block);
        float volume = CalculateVolume(block, shapeName);

        Debug.Log($"<color=cyan>[ForgeChecker] Đo xong: {block.name} → " +
                  $"Shape={shapeName}, V={volume:F1} cm³</color>");

        // Lấy đơn hiện tại
        var order = OrderManager.Instance.CurrentOrder;
        if (order == null)
        {
            Debug.LogWarning("[ForgeChecker] Không có đơn hiện tại!");
            isChecking = false;
            yield break;
        }

        // So sánh
        bool shapeMatch = string.Equals(shapeName?.Trim(),
                                       order.requiredShapeName?.Trim(),
                                       System.StringComparison.OrdinalIgnoreCase);
        float errorPercent = Mathf.Abs(volume - order.targetVolume) / order.targetVolume * 100f;
        bool isCorrect = shapeMatch && (errorPercent <= order.tolerancePercent);

        Debug.Log($"[ForgeChecker] So sánh: Cần {order.requiredShapeName} V={order.targetVolume:F0}, " +
                  $"Sai số={errorPercent:F1}%, Tolerance={order.tolerancePercent}% " +
                  $"→ KẾT QUẢ: <b>{(isCorrect ? "ĐÚNG" : "SAI")}</b>");

        // Báo OrderManager (xử lý gold + UI + state)
        OrderManager.Instance.CheckOrder(volume, shapeName);

        // Gọi ForgeProductSpawner chạy hiệu ứng + hiện sản phẩm
        if (ForgeProductSpawner.Instance != null)
        {
            ForgeProductSpawner.Instance.StartForgeSequence(block, isCorrect, order);
        }
        else
        {
            Debug.LogWarning("[ForgeChecker] Không tìm thấy ForgeProductSpawner! Destroy khối.");
            Destroy(block);
        }

        currentBlock = null;
        isChecking = false;
    }

    // ===== NHẬN DIỆN HÌNH =====
    string GetShapeName(GameObject obj)
    {
        string n = obj.name.ToLower();

        if (n.Contains("cube") && !n.Contains("chop")) return "Cube";
        if (n.Contains("sphere")) return "Sphere";
        if (n.Contains("cylinder")) return "Cylinder";
        if (n.Contains("cone")) return "Cone";
        if (n.Contains("pyramid")) return "Pyramid";
        if (n.Contains("chop")) return "Chop";

        // Khối đã cắt từ shape khác - lấy parent name
        if (obj.transform.parent != null)
            return GetShapeName(obj.transform.parent.gameObject);

        return "Unknown";
    }

    // ===== TÍNH THỂ TÍCH (đơn vị: cm³) =====
    float CalculateVolume(GameObject obj, string shapeName)
    {
        // Lấy bounding box (Unity dùng mét → nhân 100 để ra cm)
        Renderer rend = obj.GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning($"[ForgeChecker] {obj.name} không có Renderer!");
            return 0f;
        }

        Vector3 size = rend.bounds.size * 100f; // m → cm
        float a = size.x, b = size.y, c = size.z;

        switch (shapeName)
        {
            case "Sphere":
                // V = 4/3 × π × r³
                float rSphere = Mathf.Min(a, b, c) / 2f;
                return (4f / 3f) * Mathf.PI * rSphere * rSphere * rSphere;

            case "Cylinder":
                // V = π × r² × h
                // Unity Cylinder mặc định cao theo trục Y
                float rCyl = Mathf.Min(a, c) / 2f;
                float hCyl = b;
                return Mathf.PI * rCyl * rCyl * hCyl;

            case "Cone":
                // V = 1/3 × π × r² × h
                float rCone = Mathf.Min(a, c) / 2f;
                float hCone = b;
                return (1f / 3f) * Mathf.PI * rCone * rCone * hCone;

            case "Pyramid":
                // V = 1/3 × a² × h (chóp đáy vuông)
                float aPyr = Mathf.Min(a, c);
                float hPyr = b;
                return (1f / 3f) * aPyr * aPyr * hPyr;

            case "Chop":
                // Khối cắt từ Cube - dùng 1/2 bounding box (giả định cắt đôi)
                return a * b * c * 0.5f;

            case "Cube":
            default:
                // V = a × b × c
                return a * b * c;
        }
    }
}