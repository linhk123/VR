using UnityEngine;

[CreateAssetMenu(fileName = "Order_New", menuName = "GeoForge/Order Data")]
public class OrderData : ScriptableObject
{
    [Header("=== KHÁCH HÀNG ===")]
    public string customerName = "Lão Bộ";
    public string customerTitle = "Nông Dân";
    [TextArea(3, 5)]
    public string customerQuote = "Cháu rèn cho lão cái xô đựng lúa nhé!";

    [Header("=== SẢN PHẨM ===")]
    public string productName = "Xô gỗ";
    [TextArea(2, 4)]
    public string productDescription = "Xô đựng lúa hình trụ tròn";

    [Header("=== HÌNH HỌC ===")]
    [Tooltip("Tên hình HS phải chọn: Cylinder, Cube, Sphere, Cone, Pyramid, Chop")]
    public string requiredShapeName = "Cylinder";

    [Tooltip("Thể tích yêu cầu (m³)")]
    public float targetVolume = 9.42f;

    [Tooltip("Sai số cho phép (%)")]
    [Range(0f, 30f)]
    public float tolerancePercent = 10f;

    [Header("=== KÍCH THƯỚC GỢI Ý ===")]
    [TextArea(3, 6)]
    public string sizeHints = "Cao: 3.00 m\nBán kính: 1.00 m\n→ V = π × r² × h = 9.42 m³";

    [Header("=== HÌNH ẢNH BẢN VẼ ===")]
    [Tooltip("Sprite bản vẽ kỹ thuật hiện bên cạnh đơn")]
    public Sprite blueprintSprite;

    [Header("=== PHẦN THƯỞNG ===")]
    public int goldReward = 50;
    public int goldBonus = 50;  // thưởng thêm nếu đúng <1% sai số

    [Header("=== THOẠI KHI THẮNG/THUA ===")]
    [TextArea(2, 4)] public string successQuote = "Tuyệt vời! Cảm ơn cháu!";
    [TextArea(2, 4)] public string failQuote = "Hình không đúng - cháu thử lại nhé!";

    [Header("=== SẢN PHẨM 3D HIỂN THỊ KHI RÈN XONG ===")]
    [Tooltip("Prefab sản phẩm ĐÚNG - hiện khi HS rèn đúng kích thước")]
    public GameObject correctProductPrefab;

    [Tooltip("Prefab sản phẩm SAI - hiện khi HS rèn sai (méo/vỡ)")]
    public GameObject wrongProductPrefab;

    [Tooltip("Scale của prefab khi hiện (mặc định 1)")]
    public float productScale = 1f;

    // ===================================================================
    //  (TUỲ CHỌN) Trường kiểm định cho ProductMeasure/ForgeMorphController.
    //  KHÔNG ảnh hưởng hệ lò hiện tại (ForgeForging). Chỉ giữ để 2 file morph
    //  biên dịch được. Muốn bỏ hẳn: xoá CẢ 3 — ProductMeasure.cs,
    //  ForgeMorphController.cs và khối field dưới đây.
    // ===================================================================
    [Header("=== (Tuỳ chọn) KIỂM ĐỊNH MORPH ===")]
    public bool checkShapeClass = true;
    public ShapeClass requiredShapeClass = ShapeClass.Box;
    public bool checkVolume = true;
    public float targetVolumeCm3 = 1000f;
    [Range(0f, 40f)] public float volumeTolerancePercent = 12f;
    public bool checkDims = false;
    public Vector3 targetDimsCm = new Vector3(10f, 10f, 10f);
    [Range(0f, 40f)] public float dimsTolerancePercent = 15f;
}