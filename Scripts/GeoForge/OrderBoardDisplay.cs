using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class OrderBoardDisplay : MonoBehaviour
{
    [Header("=== CANVAS ĐÍCH (ưu tiên) ===")]
    [Tooltip("Kéo object 'Canvas' (con của OrderBoard) vào đây để vẽ ĐÚNG kích thước & vị trí bảng gỗ. " +
             "Bỏ trống = tự tìm Canvas trong các con.")]
    public RectTransform targetCanvas;

    [Tooltip("Đẩy chữ nhô ra TRƯỚC mặt bảng (mét) để hết z-fighting (chữ ẩn/hiện). " +
             "Nếu chữ bị chìm vào bảng, tăng số này; nếu lủng ra sau, để số âm.")]
    public float surfaceOffset = 0.012f;

    [Header("=== CÀI ĐẶT BẢNG (chỉ dùng khi KHÔNG có Canvas đích) ===")]
    [Tooltip("Vị trí LOCAL so với OrderBoard (mặc định trùng mặt gỗ con 'Frame').")]
    public Vector3 boardLocalPosition = new Vector3(2.794f, 0.067f, 1.903f);
    public Vector3 boardRotation = Vector3.zero;
    public Vector2 boardSize = new Vector2(1200, 800);
    public float boardScale = 0.001f;

    [Header("=== MÀU SẮC ===")]
    public Color woodColor = new Color(0.27f, 0.18f, 0.10f, 0.95f);
    public Color titleColor = new Color(1f, 0.85f, 0.3f, 1f);
    public Color textColor = Color.white;
    public Color accentColor = new Color(1f, 0.7f, 0.2f, 1f);
    public Color successColor = new Color(0.3f, 1f, 0.4f, 1f);
    public Color failColor = new Color(1f, 0.4f, 0.4f, 1f);

    [Header("=== KIỂU PHẤN (CHALK) ===")]
    [Tooltip("Font kiểu phấn viết tay (tuỳ chọn). Kéo 1 TMP Font Asset vào. Bỏ trống = font mặc định.")]
    public TMP_FontAsset chalkFont;
    [Tooltip("Màu chữ + nét vẽ kiểu phấn (trắng ngà).")]
    public Color chalkColor = new Color(0.96f, 0.96f, 0.92f, 1f);
    [Tooltip("TRẦN phóng to cỡ chữ (chữ tự co lại nếu không vừa khung). Tăng nếu muốn chữ to hơn.")]
    public float fontScale = 1.35f;
    [Tooltip("Độ dày nét bản vẽ phác thảo (pixel trong canvas 1200×800).")]
    public float sketchThickness = 4f;

    // UI refs
    private GameObject canvasRoot;
    private GameObject sketchRoot;
    private TextMeshProUGUI customerNameText;
    private TextMeshProUGUI customerTitleText;
    private TextMeshProUGUI customerQuoteText;
    private TextMeshProUGUI productNameText;
    private TextMeshProUGUI sizeHintsText;
    private TextMeshProUGUI orderProgressText;
    private TextMeshProUGUI shapeRequiredText;
    private TextMeshProUGUI volumeTargetText;
    private TextMeshProUGUI statusText;
    private Image blueprintImage;
    private GameObject statusPanel;

    void Awake()
    {
        BuildBoard();
    }

    void Start()
{
    if (OrderManager.Instance != null)
    {
        OrderManager.Instance.onOrderChanged.AddListener(OnOrderChanged);
        OrderManager.Instance.onOrderSuccess.AddListener(OnOrderSuccess);
        OrderManager.Instance.onOrderFail.AddListener(OnOrderFail);
        OrderManager.Instance.onSystemActivated.AddListener(OnSystemActivated);

        // Ẩn bảng lúc đầu - đợi Hexa intro xong
        SetBoardVisible(false);

        // Nếu OrderManager đã active sẵn (vd debug) → hiện luôn
        if (OrderManager.Instance.IsActive && OrderManager.Instance.CurrentOrder != null)
        {
            SetBoardVisible(true);
            OnOrderChanged(OrderManager.Instance.CurrentOrder);
        }
    }
    else
    {
        Debug.LogError("[OrderBoardDisplay] Không tìm thấy OrderManager.Instance!");
    }
}

void OnSystemActivated()
{
    Debug.Log("[OrderBoard] Hệ thống đơn hàng được kích hoạt - hiện bảng");
    SetBoardVisible(true);
}

void SetBoardVisible(bool visible)
{
    if (canvasRoot != null)
        canvasRoot.SetActive(visible);
}
    void OnOrderChanged(OrderData order)
    {
        if (order == null) return;

        customerNameText.text = order.customerName;
        customerTitleText.text = "— " + order.customerTitle + " —";
        customerQuoteText.text = "\"" + order.customerQuote + "\"";
        productNameText.text = "🛠 " + order.productName;
        sizeHintsText.text = order.sizeHints;
        shapeRequiredText.text = "Hình: " + order.requiredShapeName;
        volumeTargetText.text = "V cần: " + order.targetVolume.ToString("F2") + " m³";

        // BẢN VẼ THIẾT KẾ trên bảng:
        if (order.blueprintSprite != null)
        {
            // Có sprite → dùng sprite
            ClearSketch();
            blueprintImage.sprite = order.blueprintSprite;
            blueprintImage.color = Color.white;
            blueprintImage.gameObject.SetActive(true);
        }
        else
        {
            // KHÔNG có sprite → tự VẼ phác thảo khung dây bằng phấn trắng ngay trên bảng
            blueprintImage.sprite = null;
            blueprintImage.color = new Color(0f, 0f, 0f, 0f); // ẩn nền giấy để nét trắng nổi trên gỗ
            blueprintImage.gameObject.SetActive(true);
            DrawBlueprintSketch(order.requiredShapeName);
        }

        if (OrderManager.Instance != null)
        {
            orderProgressText.text = $"Đơn {OrderManager.Instance.CurrentOrderNumber} / {OrderManager.Instance.TotalOrders}";
        }

        // Ẩn panel status
        if (statusPanel != null) statusPanel.SetActive(false);
    }

    void OnOrderSuccess(OrderData order, float errorPercent)
    {
        ShowStatus($"✓ HOÀN THÀNH!\n\n{order.successQuote}\n\nSai số: {errorPercent:F1}%\n+{order.goldReward} vàng",
                   successColor);
    }

    void OnOrderFail(OrderData order, float errorPercent, string reason)
    {
        ShowStatus($"✗ THẤT BẠI\n\n{order.failQuote}\n\n{reason}\n\nThử lại đi!",
                   failColor);

        // Tự ẩn status sau 5 giây để HS đọc rồi tiếp tục đơn cũ
        StartCoroutine(HideStatusAfter(5f));
    }

    void ShowStatus(string message, Color color)
    {
        if (statusPanel == null) return;
        statusPanel.SetActive(true);
        statusText.text = message;
        statusText.color = color;
    }

    IEnumerator HideStatusAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (statusPanel != null) statusPanel.SetActive(false);
    }

    // ===== BUILD UI BẰNG CODE =====
    void BuildBoard()
    {
        // Ưu tiên vẽ vào Canvas world-space CÓ SẴN trên bảng (con tên "Canvas")
        // → tự khớp kích thước + vị trí bảng gỗ bạn đã đặt.
        RectTransform host = targetCanvas;
        if (host == null)
        {
            var existing = GetComponentInChildren<Canvas>(true);
            if (existing != null) host = existing.transform as RectTransform;
        }

        if (host != null)
        {
            // Tạo panel nội dung phủ kín canvas có sẵn; mọi UI vẽ vào panel này.
            canvasRoot = CreateChild("OrderBoard_Content", host);
            var rt = canvasRoot.GetComponent<RectTransform>();
            Stretch(rt);

            // Đẩy nội dung nhô ra TRƯỚC mặt bảng vài mm → hết z-fighting (chữ ẩn/hiện).
            // surfaceOffset tính bằng mét; quy đổi sang local của canvas (đã scale 0.001).
            float zLocal = surfaceOffset / Mathf.Max(host.lossyScale.z, 1e-6f);
            var lp = rt.localPosition; lp.z = -zLocal; rt.localPosition = lp;
        }
        else
        {
            // Fallback: không tìm thấy Canvas → tự tạo, BÁM theo Transform bảng (local).
            canvasRoot = new GameObject("OrderBoard_Generated", typeof(RectTransform));
            canvasRoot.transform.SetParent(transform, false);

            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasRoot.AddComponent<CanvasScaler>();
            canvasRoot.AddComponent<GraphicRaycaster>();

            var rect = canvasRoot.GetComponent<RectTransform>();
            rect.sizeDelta = boardSize;
            canvasRoot.transform.localPosition = boardLocalPosition;
            canvasRoot.transform.localEulerAngles = boardRotation;
            canvasRoot.transform.localScale = Vector3.one * boardScale;
        }

        // Background gỗ
        var bg = CreateChild("WoodBackground", canvasRoot.transform);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = woodColor;
        Stretch(bg.GetComponent<RectTransform>());

        // Tiêu đề: BẢNG ĐƠN HÀNG
        var title = CreateChild("BoardTitle", canvasRoot.transform);
        var titleTxt = title.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "📜 BẢNG ĐƠN HÀNG 📜";
        titleTxt.fontSize = 60;
        titleTxt.color = titleColor;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.fontStyle = FontStyles.Bold;
        var titleR = title.GetComponent<RectTransform>();
        titleR.anchorMin = new Vector2(0, 1);
        titleR.anchorMax = new Vector2(1, 1);
        titleR.pivot = new Vector2(0.5f, 1);
        titleR.anchoredPosition = new Vector2(0, -20);
        titleR.sizeDelta = new Vector2(0, 90);

        // Số đơn (góc phải trên)
        var prog = CreateChild("OrderProgress", canvasRoot.transform);
        orderProgressText = prog.AddComponent<TextMeshProUGUI>();
        orderProgressText.text = "Đơn 1 / 5";
        orderProgressText.fontSize = 32;
        orderProgressText.color = accentColor;
        orderProgressText.alignment = TextAlignmentOptions.Right;
        var progR = prog.GetComponent<RectTransform>();
        progR.anchorMin = new Vector2(1, 1);
        progR.anchorMax = new Vector2(1, 1);
        progR.pivot = new Vector2(1, 1);
        progR.anchoredPosition = new Vector2(-30, -120);
        progR.sizeDelta = new Vector2(300, 50);

        // ====== CỘT TRÁI: THÔNG TIN KHÁCH HÀNG + SẢN PHẨM ======

        // Customer Name (to)
        var cn = CreateChild("CustomerName", canvasRoot.transform);
        customerNameText = cn.AddComponent<TextMeshProUGUI>();
        customerNameText.fontSize = 44;
        customerNameText.color = titleColor;
        customerNameText.fontStyle = FontStyles.Bold;
        customerNameText.alignment = TextAlignmentOptions.Left;
        var cnR = cn.GetComponent<RectTransform>();
        cnR.anchorMin = new Vector2(0, 1);
        cnR.anchorMax = new Vector2(0.55f, 1);
        cnR.pivot = new Vector2(0, 1);
        cnR.anchoredPosition = new Vector2(40, -150);
        cnR.sizeDelta = new Vector2(0, 62);

        // Customer Title
        var ct = CreateChild("CustomerTitle", canvasRoot.transform);
        customerTitleText = ct.AddComponent<TextMeshProUGUI>();
        customerTitleText.fontSize = 26;
        customerTitleText.color = textColor;
        customerTitleText.alignment = TextAlignmentOptions.Left;
        var ctR = ct.GetComponent<RectTransform>();
        ctR.anchorMin = new Vector2(0, 1);
        ctR.anchorMax = new Vector2(0.55f, 1);
        ctR.pivot = new Vector2(0, 1);
        ctR.anchoredPosition = new Vector2(40, -222);
        ctR.sizeDelta = new Vector2(0, 38);

        // Customer Quote
        var cq = CreateChild("CustomerQuote", canvasRoot.transform);
        customerQuoteText = cq.AddComponent<TextMeshProUGUI>();
        customerQuoteText.fontSize = 26;
        customerQuoteText.color = textColor;
        customerQuoteText.alignment = TextAlignmentOptions.TopLeft;
        customerQuoteText.fontStyle = FontStyles.Italic;
        customerQuoteText.enableWordWrapping = true;
        var cqR = cq.GetComponent<RectTransform>();
        cqR.anchorMin = new Vector2(0, 1);
        cqR.anchorMax = new Vector2(0.55f, 1);
        cqR.pivot = new Vector2(0, 1);
        cqR.anchoredPosition = new Vector2(40, -266);
        cqR.sizeDelta = new Vector2(0, 170);

        // Product Name
        var pn = CreateChild("ProductName", canvasRoot.transform);
        productNameText = pn.AddComponent<TextMeshProUGUI>();
        productNameText.fontSize = 36;
        productNameText.color = accentColor;
        productNameText.fontStyle = FontStyles.Bold;
        productNameText.alignment = TextAlignmentOptions.Left;
        var pnR = pn.GetComponent<RectTransform>();
        pnR.anchorMin = new Vector2(0, 1);
        pnR.anchorMax = new Vector2(0.55f, 1);
        pnR.pivot = new Vector2(0, 1);
        pnR.anchoredPosition = new Vector2(40, -450);
        pnR.sizeDelta = new Vector2(0, 50);

        // Shape Required
        var sr = CreateChild("ShapeRequired", canvasRoot.transform);
        shapeRequiredText = sr.AddComponent<TextMeshProUGUI>();
        shapeRequiredText.fontSize = 28;
        shapeRequiredText.color = textColor;
        shapeRequiredText.alignment = TextAlignmentOptions.Left;
        var srR = sr.GetComponent<RectTransform>();
        srR.anchorMin = new Vector2(0, 1);
        srR.anchorMax = new Vector2(0.55f, 1);
        srR.pivot = new Vector2(0, 1);
        srR.anchoredPosition = new Vector2(40, -510);
        srR.sizeDelta = new Vector2(0, 40);

        // Volume Target
        var vt = CreateChild("VolumeTarget", canvasRoot.transform);
        volumeTargetText = vt.AddComponent<TextMeshProUGUI>();
        volumeTargetText.fontSize = 28;
        volumeTargetText.color = textColor;
        volumeTargetText.alignment = TextAlignmentOptions.Left;
        var vtR = vt.GetComponent<RectTransform>();
        vtR.anchorMin = new Vector2(0, 1);
        vtR.anchorMax = new Vector2(0.55f, 1);
        vtR.pivot = new Vector2(0, 1);
        vtR.anchoredPosition = new Vector2(40, -560);
        vtR.sizeDelta = new Vector2(0, 40);

        // Size Hints
        var sh = CreateChild("SizeHints", canvasRoot.transform);
        sizeHintsText = sh.AddComponent<TextMeshProUGUI>();
        sizeHintsText.fontSize = 24;
        sizeHintsText.color = new Color(1, 0.95f, 0.7f, 1f);
        sizeHintsText.alignment = TextAlignmentOptions.TopLeft;
        sizeHintsText.enableWordWrapping = true;
        var shR = sh.GetComponent<RectTransform>();
        shR.anchorMin = new Vector2(0, 1);
        shR.anchorMax = new Vector2(0.55f, 1);
        shR.pivot = new Vector2(0, 1);
        shR.anchoredPosition = new Vector2(40, -620);
        shR.sizeDelta = new Vector2(0, 150);

        // ====== CỘT PHẢI: BẢN VẼ KỸ THUẬT ======

        // Tiêu đề Blueprint
        var bt = CreateChild("BlueprintLabel", canvasRoot.transform);
        var btTxt = bt.AddComponent<TextMeshProUGUI>();
        btTxt.text = "📐 BẢN VẼ KỸ THUẬT";
        btTxt.fontSize = 32;
        btTxt.color = accentColor;
        btTxt.alignment = TextAlignmentOptions.Center;
        btTxt.fontStyle = FontStyles.Bold;
        var btR = bt.GetComponent<RectTransform>();
        btR.anchorMin = new Vector2(0.6f, 1);
        btR.anchorMax = new Vector2(1, 1);
        btR.pivot = new Vector2(0.5f, 1);
        btR.anchoredPosition = new Vector2(0, -170);
        btR.sizeDelta = new Vector2(-30, 50);

        // Blueprint Image (placeholder nếu không có sprite)
        var bp = CreateChild("BlueprintImage", canvasRoot.transform);
        blueprintImage = bp.AddComponent<Image>();
        blueprintImage.color = new Color(0.95f, 0.9f, 0.7f, 0.9f);  // giấy ngà
        blueprintImage.preserveAspect = true;
        var bpR = bp.GetComponent<RectTransform>();
        bpR.anchorMin = new Vector2(0.6f, 0);
        bpR.anchorMax = new Vector2(1, 1);
        bpR.pivot = new Vector2(0.5f, 0.5f);
        bpR.offsetMin = new Vector2(20, 100);
        bpR.offsetMax = new Vector2(-30, -240);

        // ====== STATUS PANEL (overlay khi thắng/thua) ======
        statusPanel = CreateChild("StatusPanel", canvasRoot.transform);
        var spImg = statusPanel.AddComponent<Image>();
        spImg.color = new Color(0, 0, 0, 0.85f);
        var spR = statusPanel.GetComponent<RectTransform>();
        spR.anchorMin = Vector2.zero;
        spR.anchorMax = Vector2.one;
        spR.offsetMin = new Vector2(40, 40);
        spR.offsetMax = new Vector2(-40, -40);

        var st = CreateChild("StatusText", statusPanel.transform);
        statusText = st.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 42;
        statusText.color = successColor;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontStyle = FontStyles.Bold;
        statusText.enableWordWrapping = true;
        Stretch(st.GetComponent<RectTransform>());

        statusPanel.SetActive(false);

        // Áp kiểu PHẤN (trắng + to + font) cho mọi chữ
        ApplyChalkStyle();
    }

    GameObject CreateChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    // ===== KIỂU PHẤN =====
    void ApplyChalkStyle()
    {
        if (canvasRoot == null) return;
        foreach (var t in canvasRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            t.color = chalkColor;
            t.fontStyle |= FontStyles.Bold;        // nét phấn dày
            if (chalkFont != null) t.font = chalkFont;

            // TỰ CO chữ cho VỪA khung → không tràn/đè field bên dưới.
            // Trần cỡ chữ = cỡ gốc × fontScale; nếu không vừa, TMP tự thu nhỏ lại.
            t.enableAutoSizing = true;
            t.fontSizeMax = t.fontSize * fontScale;
            t.fontSizeMin = 6f;
            t.margin = new Vector4(6f, 2f, 6f, 2f);  // chừa lề tránh dính mép
            t.lineSpacing = -8f;                      // khít dòng trong cùng 1 field
        }
    }

    // ===== VẼ BẢN THIẾT KẾ 2D (PHẤN TRẮNG) NGAY TRÊN BẢNG =====
    void ClearSketch()
    {
        if (sketchRoot != null) Destroy(sketchRoot);
        sketchRoot = null;
    }

    void DrawBlueprintSketch(string shapeName)
    {
        ClearSketch();
        if (blueprintImage == null) return;

        // Vùng vẽ = bên trong khung "Blueprint Image"
        sketchRoot = CreateChild("Sketch", blueprintImage.transform);
        var rt = sketchRoot.GetComponent<RectTransform>();
        Stretch(rt);

        // Kích thước vùng vẽ (fallback nếu layout chưa cập nhật)
        Vector2 size = blueprintImage.rectTransform.rect.size;
        float w = size.x > 1f ? size.x : 430f;
        float h = size.y > 1f ? size.y : 460f;
        float R = Mathf.Min(w, h) * 0.30f;   // bán kính/nửa-cạnh
        float d = R * 0.45f;                  // độ sâu phối cảnh xiên

        string n = (shapeName ?? "").ToLower();
        if (n.Contains("cube"))          SketchCube(rt, R, d);
        else if (n.Contains("sphere"))   SketchSphere(rt, R);
        else if (n.Contains("cylinder")) SketchCylinder(rt, R);
        else if (n.Contains("cone"))     SketchCone(rt, R);
        else if (n.Contains("pyramid"))  SketchPyramid(rt, R, d);
        else                             SketchCube(rt, R, d);
    }

    // --- Các hình ---
    void SketchCube(RectTransform p, float R, float d)
    {
        Vector2 A = new(-R,-R), B = new(R,-R), C = new(R,R), D = new(-R,R);
        Vector2 off = new(d, d);
        Vector2 A2=A+off, B2=B+off, C2=C+off, D2=D+off;
        Line(p,A,B); Line(p,B,C); Line(p,C,D); Line(p,D,A);          // mặt trước
        Line(p,A2,B2); Line(p,B2,C2); Line(p,C2,D2); Line(p,D2,A2);  // mặt sau
        Line(p,A,A2); Line(p,B,B2); Line(p,C,C2); Line(p,D,D2);      // cạnh nối
    }

    void SketchPyramid(RectTransform p, float R, float d)
    {
        Vector2 b0=new(-R,-R), b1=new(R,-R);
        Vector2 b3=b0+new Vector2(d,d), b2=b1+new Vector2(d,d);
        Vector2 S=new(d*0.5f, R);
        Line(p,b0,b1); Line(p,b1,b2); Line(p,b2,b3); Line(p,b3,b0);  // đáy
        Line(p,S,b0); Line(p,S,b1); Line(p,S,b2); Line(p,S,b3);      // cạnh bên
    }

    void SketchCylinder(RectTransform p, float R)
    {
        float ry = R * 0.32f;
        Ellipse(p, new Vector2(0, R), R, ry);     // miệng trên
        Ellipse(p, new Vector2(0,-R), R, ry);     // đáy dưới
        Line(p, new Vector2(-R, R), new Vector2(-R,-R));
        Line(p, new Vector2( R, R), new Vector2( R,-R));
    }

    void SketchCone(RectTransform p, float R)
    {
        float ry = R * 0.32f;
        Ellipse(p, new Vector2(0,-R), R, ry);     // đáy
        Vector2 S = new(0, R);
        Line(p, S, new Vector2(-R,-R));
        Line(p, S, new Vector2( R,-R));
    }

    void SketchSphere(RectTransform p, float R)
    {
        Ellipse(p, Vector2.zero, R, R);           // đường bao
        Ellipse(p, Vector2.zero, R, R * 0.30f);   // xích đạo
        Ellipse(p, Vector2.zero, R * 0.30f, R);   // kinh tuyến
    }

    // --- Helpers vẽ UI ---
    void Line(RectTransform parent, Vector2 a, Vector2 b)
    {
        var go = CreateChild("seg", parent);
        var img = go.AddComponent<Image>();
        img.color = chalkColor;
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        Vector2 dir = b - a;
        float len = dir.magnitude;
        rt.anchoredPosition = a;
        rt.sizeDelta = new Vector2(len, sketchThickness);
        rt.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    void Ellipse(RectTransform parent, Vector2 center, float rx, float ry, int seg = 28)
    {
        Vector2 prev = center + new Vector2(rx, 0);
        for (int i = 1; i <= seg; i++)
        {
            float ang = i / (float)seg * Mathf.PI * 2f;
            Vector2 cur = center + new Vector2(Mathf.Cos(ang) * rx, Mathf.Sin(ang) * ry);
            Line(parent, prev, cur);
            prev = cur;
        }
    }
}