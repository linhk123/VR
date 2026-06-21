using UnityEngine;
public class ForgeProductRevealer : MonoBehaviour
{
    public static ForgeProductRevealer Instance;
    
    public GameObject currentCorrectProduct;
    public GameObject currentWrongProduct;

    void Awake() { Instance = this; }

    public void RevealResult(bool isCorrect)
    {
        // Tắt hết trước
        if (currentCorrectProduct) currentCorrectProduct.SetActive(false);
        if (currentWrongProduct) currentWrongProduct.SetActive(false);

        // Chỉ bật cái cần thiết
        if (isCorrect && currentCorrectProduct) currentCorrectProduct.SetActive(true);
        else if (!isCorrect && currentWrongProduct) currentWrongProduct.SetActive(true);
        else Debug.LogError("Lỗi: Sản phẩm chưa được gán trong Inspector!");
    }
}