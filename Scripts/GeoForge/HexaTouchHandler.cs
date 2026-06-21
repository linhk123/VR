using UnityEngine;

public class HexaTouchHandler : MonoBehaviour
{
    [Header("Refs")]
    public Animator hexaAnimator;
    public GameObject hintCanvas;
    public AutoIntroCanvas introCanvas;

    [Header("Settings")]
    public string talkingParamName = "isTalking";
    [Tooltip("Tag của đầu ngón tay (do FingerTipPoker tạo ra).")]
    public string fingerTipTag = "FingerTip";

    private bool hasTriggered = false;

    void Start()
    {
        if (hintCanvas != null) hintCanvas.SetActive(true);

        if (introCanvas != null)
            introCanvas.onIntroCompleted.AddListener(OnIntroFinished);
    }

    void OnTriggerEnter(Collider other)
    {
        // Chỉ kích hoạt 1 lần, khi đầu ngón tay chạm vào Hexa.
        if (hasTriggered) return;
        if (!other.CompareTag(fingerTipTag)) return;

        StartIntro();
    }

    void OnIntroFinished()
{
    Debug.Log("[Hexa] Intro xong - Hexa về Idle + kích hoạt OrderManager");
    
    // 1. Hexa về Idle
    if (hexaAnimator != null) 
        hexaAnimator.SetBool(talkingParamName, false);
    
    // 2. Kích hoạt hệ thống đơn hàng → bảng đơn + hologram hiện
    if (OrderManager.Instance != null)
        OrderManager.Instance.ActivateOrderSystem();
}

    void StartIntro()
    {
        hasTriggered = true;
        Debug.Log("[Hexa] HS chạm Hexa - bắt đầu nói chuyện");

        if (hintCanvas != null) hintCanvas.SetActive(false);
        if (hexaAnimator != null) hexaAnimator.SetBool(talkingParamName, true);
        if (introCanvas != null) introCanvas.Show();
    }

    
}