using UnityEngine;

public class BoundaryWarning : MonoBehaviour
{
    [Header("Vignette")]
    public CanvasGroup vignetteCanvas;
    public float warningDistance = 0.4f;   // mờ khi cách boundary <0.4m
    public float maxAlpha = 0.85f;
    public float fadeSpeed = 5f;

    [Header("Player")]
    public Transform playerHead;

    private float currentAlpha = 0;

    void Update()
    {
        if (playerHead == null || vignetteCanvas == null) return;

        float dist = GetBoundaryDistance();
        float target = 0f;

        if (dist >= 0 && dist < warningDistance)
        {
            target = (1f - dist / warningDistance) * maxAlpha;
        }

        currentAlpha = Mathf.MoveTowards(currentAlpha, target, fadeSpeed * Time.deltaTime);
        vignetteCanvas.alpha = currentAlpha;
    }

    float GetBoundaryDistance()
    {
        if (!OVRManager.boundary.GetConfigured()) return -1f;
        var result = OVRManager.boundary.TestPoint(
            playerHead.position,
            OVRBoundary.BoundaryType.PlayArea
        );
        return result.ClosestDistance;
    }
}