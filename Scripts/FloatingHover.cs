using UnityEngine;

/// <summary>
/// ĐÃ VÔ HIỆU HOÁ. Script lơ lửng không còn tác dụng (tự gỡ khi chạy).
/// Hãy XOÁ HẲN file này trong Unity: cửa sổ Project → tìm FloatingHover →
/// chuột phải → Delete. (Sau đó component "missing" trên vật thì Remove Component.)
/// </summary>
public class FloatingHover : MonoBehaviour
{
    void Awake()
    {
        // Tự gỡ bỏ chính mình → không làm gì với vật (hết lơ lửng / hết văng)
        Destroy(this);
    }
}
