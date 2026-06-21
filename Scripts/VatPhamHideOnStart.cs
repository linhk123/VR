using UnityEngine;

/// <summary>
/// Ẩn (tàng hình) TẤT CẢ object con của vat_pham ngay khi chạy dự án.
/// Gắn script này lên object "vat_pham".
///
/// Sau đó ForgeProductReveal sẽ bật lại đúng sản phẩm khi cho khối vào lò.
/// </summary>
public class VatPhamHideOnStart : MonoBehaviour
{
    public enum HideMode
    {
        Deactivate,    // SetActive(false) — hợp với hệ thống lộ sản phẩm (khuyến nghị)
        RenderersOff   // chỉ tắt hiển thị (Renderer), object vẫn active
    }

    [Tooltip("Deactivate = tắt hẳn object (khuyến nghị, để ForgeProductReveal bật lại). " +
             "RenderersOff = chỉ ẩn hình, vẫn giữ collider/script.")]
    public HideMode mode = HideMode.Deactivate;

    [Tooltip("Ẩn cả con-của-con (sâu) hay chỉ con trực tiếp.")]
    public bool onlyDirectChildren = true;

    public bool debugLog = true;

    void Awake()
    {
        int count = 0;

        if (mode == HideMode.Deactivate)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
                count++;
            }
        }
        else // RenderersOff
        {
            if (onlyDirectChildren)
            {
                foreach (Transform child in transform)
                {
                    foreach (var r in child.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                    count++;
                }
            }
            else
            {
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (r.transform == transform) continue;
                    r.enabled = false;
                    count++;
                }
            }
        }

        if (debugLog) Debug.Log($"[VatPhamHideOnStart] Đã ẩn {count} object con của '{name}' (chế độ {mode}).");
    }
}
