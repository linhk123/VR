using UnityEngine;

public class MagicForge : MonoBehaviour
{
    [Header("Liên kết bộ sinh khối")]
    public BlockSpawner spawner; 

    [Header("Hiệu ứng phát sáng của Lò rèn")]
    public ParticleSystem forgeGlowParticles; 
    public AudioSource audioSource;
    public AudioClip forgeSound;

    private void OnTriggerEnter(Collider other)
{
    // 1. Bộ lọc nghiêm ngặt: Chỉ loại bỏ tay và vũ khí thực sự
    if (other.CompareTag("FingerTip") || other.name.Contains("Blade") || other.name.Contains("Sword")) 
        return;

    // Tìm MeshFilter ở cả object hiện tại lẫn các cấp con của nó
    MeshFilter mf = other.GetComponentInChildren<MeshFilter>();
    if (mf == null) return; 

    Debug.Log($"<color=yellow>[Lò Rèn] Đang xử lý vật phẩm: {other.name}</color>");

    // 2. Tính toán thông số
    float currentVolumeRatio = CalculateMeshVolumeRatio(other.gameObject);
    string objectShapeName = other.gameObject.name;

    // 3. Đối chiếu kết quả với OrderManager
    bool isCorrect = false;
    if (OrderManager.Instance != null)
    {
        isCorrect = OrderManager.Instance.VerifyProduct(objectShapeName, currentVolumeRatio);
    }

    // Phát hiệu ứng bùng lửa lò rèn
    PlayForgeFeedback();

    // 4. Kích hoạt hiển thị sản phẩm mẫu (Bọc trong try-catch để tránh crash làm đứng script)
    if (ForgeProductRevealer.Instance != null)
    {
        try 
        {
            ForgeProductRevealer.Instance.RevealResult(isCorrect);
        }
        catch (System.Exception e) 
        {
            Debug.LogError($"[Lò Rèn] Lỗi hiển thị sản phẩm mẫu: {e.Message}");
        }
    }

    // 5. Thông báo giao hàng và hồi sinh khối mới
    if (OrderManager.Instance != null)
    {
        OrderManager.Instance.NotifyDelivery(isCorrect);
    }

    if (spawner != null)
    {
        spawner.TriggerRespawn();
    }

    // 6. LUÔN LUÔN HỦY KHỐI HÌNH ĐỂ TRÁNH TRƠ TRỌI TRONG LÒ
    Destroy(other.gameObject);
    Debug.Log("[Lò Rèn] Đã dọn dẹp và hủy khối hình test thành công.");
}

    private float CalculateMeshVolumeRatio(GameObject obj)
    {
        MeshFilter mf = obj.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return 1f;

        Vector3 size = mf.sharedMesh.bounds.size;
        float originalLocalVolume = size.x * size.y * size.z;
        Vector3 worldScale = obj.transform.lossyScale;
        return Mathf.Clamp01(originalLocalVolume * (worldScale.x * worldScale.y * worldScale.z));
    }

    private void PlayForgeFeedback()
    {
        if (forgeGlowParticles != null) forgeGlowParticles.Play();
        if (audioSource != null && forgeSound != null) audioSource.PlayOneShot(forgeSound);
    }
}