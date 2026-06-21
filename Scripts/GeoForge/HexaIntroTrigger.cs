using UnityEngine;

public class HexaIntroTrigger : MonoBehaviour
{
    [Header("Refs")]
    public Animator hexaAnimator;
    public GameObject hintCanvas;        // Canvas "Hãy chạm vào tôi" treo đầu Hexa
    public GameObject introCanvas;       // Canvas lời giới thiệu bối cảnh
    public GameObject hintCanvasOrderBoard;  // (tuỳ chọn) Canvas "Đến bảng đơn hàng" treo đầu OrderBoard

    [Header("Settings")]
    public float talkingDuration = 8f;

    private bool hasTriggered = false;

    void Start()
    {
        if (hintCanvas != null) hintCanvas.SetActive(true);
        if (introCanvas != null) introCanvas.SetActive(false);
        if (hintCanvasOrderBoard != null) hintCanvasOrderBoard.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        string n = other.gameObject.name.ToLower();
        if (n.Contains("hand") || n.Contains("finger") || n.Contains("pinch") || other.CompareTag("Hand"))
        {
            ActivateIntro();
        }
    }

    void ActivateIntro()
    {
        hasTriggered = true;
        Debug.Log("[HexaIntroTrigger] HS chạm Hexa - bắt đầu intro");

        // 1. Tắt banner trên đầu Hexa
        if (hintCanvas != null) hintCanvas.SetActive(false);

        // 2. Hexa play animation Talking
        if (hexaAnimator != null) hexaAnimator.SetTrigger("StartTalking");

        // 3. Hiện Canvas bối cảnh
        if (introCanvas != null) introCanvas.SetActive(true);

        // Đặt thời gian Hexa ngừng nói
        Invoke(nameof(StopHexaTalking), talkingDuration);
    }

    void StopHexaTalking()
    {
        if (hexaAnimator != null) hexaAnimator.SetTrigger("StopTalking");
    }

    public void OnContinueButtonPressed()
    {
        if (introCanvas != null) introCanvas.SetActive(false);
        if (hintCanvasOrderBoard != null) hintCanvasOrderBoard.SetActive(true);
        if (hexaAnimator != null) hexaAnimator.SetTrigger("StopTalking");
    }
}