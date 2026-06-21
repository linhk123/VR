using UnityEngine;

public class ForgeManager : MonoBehaviour
{
    [Header("Cấu hình Lò")]
    public ForgeTransformGlow glowEffect; // Kéo script phát sáng vào đây
    public ForgeProductRevealer revealer; // Kéo script kệ sản phẩm vào đây

    // Hàm này được gọi từ ForgeBlockZoneRelay hoặc script va chạm của bạn
    public void ProcessForgeItem(GameObject itemObj)
    {
        // 1. Kích hoạt hiệu ứng phát sáng lò
        if (glowEffect != null) glowEffect.Flare();

        // 2. Logic kiểm tra đúng/sai (Thay logic của bạn vào đây)
        bool isCorrect = CheckIfCorrect(itemObj);

        // 3. Ẩn khối đã ném vào
        itemObj.SetActive(false);

        // 4. Hiện sản phẩm lên kệ
        if (revealer != null) revealer.RevealResult(isCorrect);
    }

    bool CheckIfCorrect(GameObject obj)
    {
        // Kiểm tra Tag hoặc tên của khối để quyết định đúng/sai
        return obj.CompareTag("CorrectBlock"); 
    }
}