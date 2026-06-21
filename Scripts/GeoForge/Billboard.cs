using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        if (Camera.main != null)
            cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 dir = transform.position - cam.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}