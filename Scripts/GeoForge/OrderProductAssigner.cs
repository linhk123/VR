using UnityEngine;

public class OrderProductAssigner : MonoBehaviour
{
    public GameObject myCorrectProduct;
    public GameObject myWrongProduct;

    // Giả sử hàm nạp của nhóm tên là TryAssign hoặc AssignProducts
    public void TryAssign()
    {
        // Ẩn 2 sản phẩm này đi lúc khởi tạo để tránh lộ diện sớm
        if (myCorrectProduct != null) myCorrectProduct.SetActive(false);
        if (myWrongProduct != null) myWrongProduct.SetActive(false);

        // Đẩy tham chiếu sản phẩm của Đơn này sang cho Kệ trưng bày quản lý
        if (ForgeProductRevealer.Instance != null)
        {
            ForgeProductRevealer.Instance.currentCorrectProduct = myCorrectProduct;
            ForgeProductRevealer.Instance.currentWrongProduct = myWrongProduct;
            
            Debug.Log($"<color=cyan>[Assigner] Đã nạp thành công bộ sản phẩm của '{gameObject.name}' vào hệ thống hiển thị.</color>");
        }
    }
}