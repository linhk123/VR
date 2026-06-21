using UnityEngine;
using System.Collections;

public class ForgeLightDetector : MonoBehaviour
{
    [Header("--- Thợ Rèn & Hoạt Ảnh ---")]
    public Animator blacksmithAnimator; // CHỈ GIỮ LẠI Ô NÀY - Kéo model ông lão vào đây
    public string correctTag = "CorrectItem"; // Tag của vật đúng (Cài trên Inspector vật phẩm)
    public string wrongTag = "WrongItem";     // Tag của vật sai

    [Header("--- Hiệu ứng Flash khi giao hàng ---")]
    public Light flashLight;

    [Header("--- Kéo các vòng nước ma thuật vào đây ---")]
    public GameObject waterSpell1;
    public GameObject waterSpell2;
    public GameObject waterSpell3;

    [Header("--- Bóng Đèn Của Lò Rèn ---")]
    public Light targetLight; 

    [Header("--- Cấu hình ánh sáng ---")]
    public float activeIntensity = 2000f;
    public float activeRange = 5f;

    private int colliderCount = 0;

    private void Start()
    {
        if (targetLight != null) targetLight.enabled = false;

        SetupInfiniteWaterStream(waterSpell1);
        SetupInfiniteWaterStream(waterSpell2);
        SetupInfiniteWaterStream(waterSpell3);

        Debug.Log("<color=cyan>[HỆ THỐNG VÒNG NƯỚC]</color> Đã cấu hình thành công!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(correctTag))
        {
            Debug.Log($"<color=green>[GIAO HÀNG ĐÚNG]</color> Đã nhận: {other.name}");
            // Báo hệ thống đơn hàng → CHUYỂN sang đơn tiếp theo
            if (OrderManager.Instance != null) OrderManager.Instance.NotifyDelivery(true);
            StartCoroutine(HandleReaction(true, other.gameObject));
            return;
        }
        else if (other.CompareTag(wrongTag))
        {
            Debug.Log($"<color=red>[GIAO HÀNG SAI]</color> Đã nhận: {other.name}");
            // Sai → GIỮ NGUYÊN đơn (chỉ báo thất bại)
            if (OrderManager.Instance != null) OrderManager.Instance.NotifyDelivery(false);
            StartCoroutine(HandleReaction(false, other.gameObject));
            return;
        }

        colliderCount++;
        if (targetLight != null && !targetLight.enabled)
        {
            targetLight.enabled = true;
            targetLight.intensity = activeIntensity;
            targetLight.range = activeRange;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null || other.CompareTag(correctTag) || other.CompareTag(wrongTag)) return;

        colliderCount--;
        if (colliderCount < 0) colliderCount = 0;

        if (colliderCount == 0 && targetLight != null)
        {
            targetLight.enabled = false;
        }
    }

    private IEnumerator HandleReaction(bool isCorrect, GameObject item)
    {
        if (flashLight != null)
        {
            float originalIntensity = flashLight.intensity;
            flashLight.intensity = originalIntensity * 5f; 
            
            if (item != null) item.SetActive(false);

            yield return new WaitForSeconds(0.15f);
            flashLight.intensity = originalIntensity; 
        }
        else
        {
            if (item != null) item.SetActive(false);
        }

        // Gọi hoạt ảnh cho ông lão
        if (blacksmithAnimator != null)
        {
            if (isCorrect) 
            {
                // Nhớ tạo Trigger tên chính xác "isCorrect" trong Animator của ông lão
                blacksmithAnimator.SetTrigger("isCorrect"); 
            }
            else 
            {
                // Nhớ tạo Trigger tên chính xác "isIncorrect" trong Animator của ông lão
                blacksmithAnimator.SetTrigger("isIncorrect");
            }
        }
        else
        {
            Debug.LogError("Bạn chưa kéo thả Animator của Ông Lão vào ô Blacksmith Animator trên Lò Rèn!");
        }

        yield return new WaitForSeconds(3.0f);
        if (item != null) Destroy(item);
    }

    private void SetupInfiniteWaterStream(GameObject spellObj)
    {
        if (spellObj == null) return;
        spellObj.SetActive(true);

        Animator[] animators = spellObj.GetComponentsInChildren<Animator>(true);
        foreach (var anim in animators) anim.enabled = false;

        Animation[] animations = spellObj.GetComponentsInChildren<Animation>(true);
        foreach (var anim in animations) anim.enabled = false;

        MonoBehaviour[] scripts = spellObj.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var script in scripts)
        {
            if (script != null && script != this)
            {
                string scriptName = script.GetType().Name.ToLower();
                if (scriptName.Contains("destroy") || scriptName.Contains("timer") || 
                    scriptName.Contains("life") || scriptName.Contains("fade") || 
                    scriptName.Contains("remover"))
                {
                    script.enabled = false;
                }
            }
        }

        ParticleSystem[] allParticles = spellObj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in allParticles)
        {
            var main = ps.main;
            main.loop = true;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = ps.emission;
            emission.enabled = true;
            if (emission.rateOverTime.constant == 0) emission.rateOverTime = 35f;

            if (!ps.isPlaying) ps.Play();
        }
    }
}