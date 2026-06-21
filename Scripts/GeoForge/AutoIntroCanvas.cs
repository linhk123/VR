using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class AutoIntroCanvas : MonoBehaviour
{
    [Header("=== LỒNG TIẾNG (tuỳ chọn) ===")]
[Tooltip("Audio cho từng slide (cùng số lượng với slides). Để trống nếu không lồng tiếng.")]
public List<AudioClip> slideVoiceClips = new List<AudioClip>();

[Tooltip("Nếu bật: thời gian slide = độ dài audio + extra time. Bỏ qua wordsPerSecond.")]
public bool useAudioLengthForTiming = true;

[Tooltip("Volume của giọng nói (0-1)")]
[Range(0f, 1f)]
public float voiceVolume = 1f;
    
    [Header("=== NỘI DUNG ===")]
    public string title = "GEO FORGE";
    public string subtitle = "Lò Rèn Hình Học";

    [Tooltip("Mỗi item = 1 trang. Tự động chuyển trang theo tốc độ đọc.")]
    [TextArea(4, 10)]
    public List<string> slides = new List<string>
    {
        "Chào cháu thợ rèn trẻ!\n\nLão là Hexa - thợ rèn già nhất\nvương quốc Geometria.\n\nLão đã đợi cháu lâu lắm rồi...",

        "Vương quốc Geometria đã mất\nhơn 100 năm không có thợ rèn hình.\n\nNgười dân cần silo chứa lúa.\nHiệp sĩ cần lưỡi kiếm.\nPháp sư cần tinh thể phép...\n\nNhưng không ai còn biết tính toán nữa.",

        "Cháu sẽ là thợ rèn mới\ncủa vương quốc Geometria.\n\nLão giao xưởng này cho cháu.\n\nHãy dùng tài hình học\nrèn ra những đồ vật khách cần.",

        "Khách hàng đầu tiên đã đặt đơn rồi!\n\nHãy đến BẢNG ĐƠN HÀNG\ngần cửa ra vào\nđể nhận nhiệm vụ.\n\nLão tin cháu sẽ làm được.\nChúc may mắn, thợ rèn trẻ!"
    };

    [Header("=== VỊ TRÍ CANVAS (World Space) ===")]
    [Tooltip("Cao hơn vòng ma thuật ở Forge để tránh trùng")]
    public Vector3 canvasPosition = new Vector3(-9.584f, 2.8f, 2.683f);
    public Vector3 canvasRotation = Vector3.zero;
    public Vector2 canvasSize = new Vector2(800, 500);
    public float canvasScale = 0.003f;

    [Header("=== MÀU SẮC ===")]
    public Color backgroundColor = new Color(0.17f, 0.14f, 0.09f, 0.95f);
    public Color titleColor = new Color(1f, 0.84f, 0f, 1f);
    public Color bodyColor = Color.white;
    public Color accentColor = new Color(1f, 0.84f, 0f, 1f);

    [Header("=== TỐC ĐỘ ĐỌC (chậm cho HS) ===")]
    [Tooltip("Từ/giây. 1.5 = rất chậm, 2.0 = chậm, 2.5 = vừa")]
    public float wordsPerSecond = 1.8f;
    [Tooltip("Slide ngắn vẫn đợi tối thiểu bấy nhiêu giây")]
    public float minSecondsPerSlide = 6f;
    [Tooltip("Slide dài tự nhảy sau bấy nhiêu giây tối đa")]
    public float maxSecondsPerSlide = 18f;
    [Tooltip("Thêm thời gian đệm sau mỗi slide để HS suy ngẫm (giây)")]
    public float extraReadTime = 2f;

    [Header("=== TỰ ĐỘNG ===")]
    public bool showOnStart = false;

    [Header("=== SỰ KIỆN ===")]
    public UnityEvent onIntroStarted;
    public UnityEvent onIntroCompleted;

    // Refs nội bộ
   // Refs nội bộ
    private GameObject canvasRoot;
    private TextMeshProUGUI bodyTextComp;
    private TextMeshProUGUI pageIndicator;
    private Image progressFill;
    private int currentSlideIndex;
    private float currentTimer;
    private float currentDuration;
    private AudioSource voiceAudioSource;
    private bool isShowing;

    void Awake()
    {
        BuildCanvas();
        SetupAudioSource();
        canvasRoot.SetActive(false);
    }

void SetupAudioSource()
{
    voiceAudioSource = gameObject.AddComponent<AudioSource>();
    voiceAudioSource.playOnAwake = false;
    voiceAudioSource.loop = false;            // QUAN TRỌNG: không lặp
    voiceAudioSource.spatialBlend = 0f;
    voiceAudioSource.volume = voiceVolume;
}
    void Start()
    {
        if (showOnStart) Show();
    }

    public void Show()
{
    if (slides == null || slides.Count == 0)
    {
        Debug.LogWarning("[AutoIntroCanvas] Không có slide nào!");
        return;
    }

    // DEBUG: cảnh báo nếu Show() bị gọi nhiều lần
    if (isShowing)
    {
        Debug.LogWarning("<color=red>[Intro] Show() được gọi lại trong khi đang chạy! → BỎ QUA</color>");
        return;
    }

    Debug.Log($"<color=green>[Intro] === BẮT ĐẦU - {slides.Count} slides, {slideVoiceClips.Count} audio clips ===</color>");

    canvasRoot.SetActive(true);
    currentSlideIndex = 0;
    ShowSlide(0);
    isShowing = true;
    onIntroStarted?.Invoke();
}

    public void Hide()
{
    if (canvasRoot != null) canvasRoot.SetActive(false);
    if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        voiceAudioSource.Stop();
    isShowing = false;
}

    void Update()
    {
        if (!isShowing) return;

        currentTimer += Time.deltaTime;
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(currentTimer / currentDuration);

        if (currentTimer >= currentDuration)
            NextSlide();
    }

    void NextSlide()
    {
        currentSlideIndex++;
        if (currentSlideIndex >= slides.Count)
            EndIntro();
        else
            ShowSlide(currentSlideIndex);
    }

    void ShowSlide(int index)
{
    currentTimer = 0;
    bodyTextComp.text = slides[index];
    pageIndicator.text = $"{index + 1} / {slides.Count}";

    // DEBUG: log slide đang hiện
    Debug.Log($"<color=cyan>[Intro] >>> SHOW SLIDE {index + 1}/{slides.Count}</color>");

    // Lấy audio clip cho slide này
    AudioClip clip = null;
    if (slideVoiceClips != null && index < slideVoiceClips.Count)
        clip = slideVoiceClips[index];

    // DEBUG: log audio
    if (clip != null)
        Debug.Log($"<color=yellow>[Intro] Audio: '{clip.name}', length: {clip.length:F2}s</color>");
    else
        Debug.LogWarning($"[Intro] Slot {index} KHÔNG có audio clip!");

    // STOP audio cũ trước khi play mới (an toàn)
    if (voiceAudioSource != null)
    {
        if (voiceAudioSource.isPlaying)
            voiceAudioSource.Stop();

        if (clip != null)
        {
            voiceAudioSource.clip = clip;
            voiceAudioSource.volume = voiceVolume;
            voiceAudioSource.loop = false;   // ép loop = false để chắc
            voiceAudioSource.Play();
        }
    }

    // Tính thời gian slide
    if (useAudioLengthForTiming && clip != null && clip.length > 0.1f)
    {
        currentDuration = clip.length + extraReadTime;
        Debug.Log($"[Intro] Duration (theo audio): {currentDuration:F2}s");
    }
    else
    {
        int wordCount = slides[index].Split(
            new[] { ' ', '\n', '\t', '.', ',' },
            StringSplitOptions.RemoveEmptyEntries).Length;
        float estimated = wordCount / wordsPerSecond + extraReadTime;
        currentDuration = Mathf.Clamp(estimated, minSecondsPerSlide, maxSecondsPerSlide);
        Debug.Log($"[Intro] Duration (theo từ): {currentDuration:F2}s ({wordCount} từ)");
    }
}

    void EndIntro()
    {
        Hide();
        onIntroCompleted?.Invoke();
    }

    // =============== BUILD UI BẰNG CODE ===============
    void BuildCanvas()
    {
        canvasRoot = new GameObject("AutoIntroCanvas_Generated");
        canvasRoot.transform.SetParent(transform, false);

        var canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasRoot.AddComponent<CanvasScaler>();
        canvasRoot.AddComponent<GraphicRaycaster>();

        var rect = canvasRoot.GetComponent<RectTransform>();
        rect.sizeDelta = canvasSize;
        canvasRoot.transform.position = canvasPosition;
        canvasRoot.transform.eulerAngles = canvasRotation;
        canvasRoot.transform.localScale = Vector3.one * canvasScale;

        // Background nâu trong
        var bg = CreateChild("Background", canvasRoot.transform);
        var bgImage = bg.AddComponent<Image>();
        bgImage.color = backgroundColor;
        Stretch(bg.GetComponent<RectTransform>());

        // Đường vàng phân cách title-body
        var border = CreateChild("Border", canvasRoot.transform);
        var borderImage = border.AddComponent<Image>();
        borderImage.color = accentColor;
        var borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0, 1);
        borderRect.anchorMax = new Vector2(1, 1);
        borderRect.pivot = new Vector2(0.5f, 1);
        borderRect.anchoredPosition = new Vector2(0, -150);
        borderRect.sizeDelta = new Vector2(0, 3);

        // Title vàng
        var titleGO = CreateChild("Title", canvasRoot.transform);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = title + "\n<size=65%>" + subtitle + "</size>";
        titleText.fontSize = 54;
        titleText.color = titleColor;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(0, 130);

        // Body text
        var bodyGO = CreateChild("BodyText", canvasRoot.transform);
        bodyTextComp = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTextComp.text = "";
        bodyTextComp.fontSize = 30;
        bodyTextComp.color = bodyColor;
        bodyTextComp.alignment = TextAlignmentOptions.Center;
        bodyTextComp.lineSpacing = 12;
        bodyTextComp.enableWordWrapping = true;
        var bodyRect = bodyGO.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(40, 80);
        bodyRect.offsetMax = new Vector2(-40, -170);

        // Page indicator (1/4)
        var pageGO = CreateChild("PageIndicator", canvasRoot.transform);
        pageIndicator = pageGO.AddComponent<TextMeshProUGUI>();
        pageIndicator.text = "1 / 1";
        pageIndicator.fontSize = 22;
        pageIndicator.color = new Color(1, 1, 1, 0.6f);
        pageIndicator.alignment = TextAlignmentOptions.Center;
        var pageRect = pageGO.GetComponent<RectTransform>();
        pageRect.anchorMin = new Vector2(0, 0);
        pageRect.anchorMax = new Vector2(1, 0);
        pageRect.pivot = new Vector2(0.5f, 0);
        pageRect.anchoredPosition = new Vector2(0, 15);
        pageRect.sizeDelta = new Vector2(0, 30);

        // Progress bar nền
        var progBg = CreateChild("ProgressBg", canvasRoot.transform);
        var progBgImage = progBg.AddComponent<Image>();
        progBgImage.color = new Color(1, 1, 1, 0.15f);
        var progBgRect = progBg.GetComponent<RectTransform>();
        progBgRect.anchorMin = new Vector2(0, 0);
        progBgRect.anchorMax = new Vector2(1, 0);
        progBgRect.pivot = new Vector2(0.5f, 0);
        progBgRect.anchoredPosition = new Vector2(0, 55);
        progBgRect.sizeDelta = new Vector2(-80, 8);

        // Progress bar fill vàng
        var progFillGO = CreateChild("ProgressFill", progBg.transform);
        progressFill = progFillGO.AddComponent<Image>();
        progressFill.color = accentColor;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0;
        Stretch(progFillGO.GetComponent<RectTransform>());
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
}