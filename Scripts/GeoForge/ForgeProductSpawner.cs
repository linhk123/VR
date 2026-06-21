using System.Collections;
using UnityEngine;

public class ForgeProductSpawner : MonoBehaviour
{
    public static ForgeProductSpawner Instance;

    [Header("=== VỊ TRÍ SPAWN ===")]
    [Tooltip("Vị trí hiện sản phẩm (mặc định = vị trí lò rèn)")]
    public Transform spawnPoint;
    [Tooltip("Offset Y so với spawnPoint")]
    public float yOffset = 0.5f;

    [Header("=== HIỆU ỨNG ÁNH SÁNG LÒ RÈN ===")]
    [Tooltip("Particle System lửa/khói khi rèn")]
    public ParticleSystem forgeFireFx;
    [Tooltip("Light của lò rèn (đèn sáng lên)")]
    public Light forgeLight;
    [Tooltip("Cường độ sáng tối đa khi rèn")]
    public float maxLightIntensity = 5f;
    [Tooltip("Cường độ sáng bình thường")]
    public float idleLightIntensity = 1f;

    [Header("=== ÂM THANH ===")]
    public AudioSource forgeSfx;
    public AudioClip forgingSound;     // tiếng rèn (búa đập)
    public AudioClip successSound;     // tiếng thành công (ting!)
    public AudioClip failSound;        // tiếng thất bại (clang)

    [Header("=== THỜI GIAN ===")]
    [Tooltip("Thời gian khối tan biến (giây)")]
    public float dissolveDuration = 1.5f;
    [Tooltip("Thời gian hiển thị sản phẩm trước khi tự ẩn (giây)")]
    public float productDisplayDuration = 5f;

    [Header("=== TRẠNG THÁI ===")]
    [SerializeField] private GameObject currentProduct;
    [SerializeField] private bool isForging = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (forgeLight != null) forgeLight.intensity = idleLightIntensity;
        if (forgeFireFx != null) forgeFireFx.Stop();
    }

    /// <summary>
    /// Gọi từ ForgeChecker khi đã đo xong khối.
    /// </summary>
    public void StartForgeSequence(GameObject blockInForge, bool isCorrect, OrderData order)
    {
        if (isForging)
        {
            Debug.LogWarning("[ForgeSpawner] Đang rèn, bỏ qua yêu cầu mới!");
            return;
        }

        if (order == null)
        {
            Debug.LogWarning("[ForgeSpawner] Không có đơn hàng hiện tại!");
            return;
        }

        StartCoroutine(ForgeSequence(blockInForge, isCorrect, order));
    }

    IEnumerator ForgeSequence(GameObject blockInForge, bool isCorrect, OrderData order)
    {
        isForging = true;
        Debug.Log($"<color=cyan>[ForgeSpawner] BẮT ĐẦU rèn - {(isCorrect ? "ĐÚNG" : "SAI")}</color>");

        // ========== STEP 1: BẬT HIỆU ỨNG SÁNG ==========
        if (forgeFireFx != null) forgeFireFx.Play();
        if (forgeSfx != null && forgingSound != null)
        {
            forgeSfx.clip = forgingSound;
            forgeSfx.loop = true;
            forgeSfx.Play();
        }

        // Tăng cường độ sáng từ idle → max trong 0.5s
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            if (forgeLight != null)
                forgeLight.intensity = Mathf.Lerp(idleLightIntensity, maxLightIntensity, t / 0.5f);
            yield return null;
        }

        // ========== STEP 2: KHỐI TAN BIẾN ==========
        if (blockInForge != null)
        {
            yield return StartCoroutine(DissolveBlock(blockInForge));
            Destroy(blockInForge);
        }

        yield return new WaitForSeconds(0.3f);

        // ========== STEP 3: SPAWN SẢN PHẨM ==========
        GameObject prefabToSpawn = isCorrect ? order.correctProductPrefab : order.wrongProductPrefab;

        if (prefabToSpawn == null)
        {
            // Fallback: tạo placeholder nếu chưa wire prefab
            currentProduct = CreatePlaceholderProduct(isCorrect, order);
        }
        else
        {
            Vector3 pos = (spawnPoint != null ? spawnPoint.position : transform.position);
            pos.y += yOffset;
            currentProduct = Instantiate(prefabToSpawn, pos, Quaternion.identity);
            currentProduct.transform.localScale *= order.productScale;
        }

        // Hiệu ứng phóng to sản phẩm từ nhỏ → bình thường
        yield return StartCoroutine(GrowInProduct(currentProduct));

        // Âm thanh kết quả
        if (forgeSfx != null)
        {
            forgeSfx.loop = false;
            forgeSfx.Stop();
            forgeSfx.PlayOneShot(isCorrect ? successSound : failSound);
        }

        // ========== STEP 4: GIẢM SÁNG XUỐNG IDLE ==========
        if (forgeFireFx != null) forgeFireFx.Stop();
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            if (forgeLight != null)
                forgeLight.intensity = Mathf.Lerp(maxLightIntensity, idleLightIntensity, t / 0.5f);
            yield return null;
        }

        // ========== STEP 5: GIỮ SẢN PHẨM HIỂN THỊ ==========
        yield return new WaitForSeconds(productDisplayDuration);

        // ========== STEP 6: SẢN PHẨM BIẾN MẤT ==========
        if (currentProduct != null)
        {
            yield return StartCoroutine(FadeOutProduct(currentProduct));
            Destroy(currentProduct);
            currentProduct = null;
        }

        isForging = false;
        Debug.Log("<color=cyan>[ForgeSpawner] KẾT THÚC chu trình rèn.</color>");
    }

    // Khối tan biến: thu nhỏ + mờ dần
    IEnumerator DissolveBlock(GameObject block)
    {
        Vector3 originalScale = block.transform.localScale;
        Renderer[] renderers = block.GetComponentsInChildren<Renderer>();

        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float p = t / dissolveDuration;
            block.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, p);
            block.transform.Rotate(0, 360f * Time.deltaTime, 0); // xoay khi tan
            yield return null;
        }
    }

    // Sản phẩm hiện ra: phóng to từ 0 → scale gốc
    IEnumerator GrowInProduct(GameObject product)
    {
        if (product == null) yield break;
        Vector3 targetScale = product.transform.localScale;
        product.transform.localScale = Vector3.zero;

        float t = 0f;
        float duration = 0.8f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            // Easing: bounce out
            float ease = 1 - Mathf.Pow(1 - p, 3);
            product.transform.localScale = targetScale * ease;
            yield return null;
        }
        product.transform.localScale = targetScale;
    }

    // Sản phẩm biến mất: thu nhỏ + mờ
    IEnumerator FadeOutProduct(GameObject product)
    {
        if (product == null) yield break;
        Vector3 originalScale = product.transform.localScale;
        float t = 0f;
        float duration = 0.5f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            product.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, p);
            yield return null;
        }
    }

    // Placeholder: tạo Cube xanh/đỏ nếu chưa có prefab
    GameObject CreatePlaceholderProduct(bool isCorrect, OrderData order)
    {
        Vector3 pos = (spawnPoint != null ? spawnPoint.position : transform.position);
        pos.y += yOffset;

        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        placeholder.name = $"Placeholder_{(isCorrect ? "Correct" : "Wrong")}_{order.productName}";
        placeholder.transform.position = pos;
        placeholder.transform.localScale = Vector3.one * 0.3f;
        Destroy(placeholder.GetComponent<Collider>()); // không cần collider

        // Chỉnh màu
        var renderer = placeholder.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = isCorrect ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 0.3f, 0.3f);
        renderer.material = mat;

        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(placeholder.transform);
        labelObj.transform.localPosition = new Vector3(0, 0.7f, 0);
        var tm = labelObj.AddComponent<TextMesh>();
        tm.text = isCorrect ? $"✓ {order.productName}" : $"✗ Lỗi!";
        tm.fontSize = 40;
        tm.characterSize = 0.05f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = isCorrect ? Color.green : Color.red;

        return placeholder;
    }

    public bool IsForging() => isForging;
}