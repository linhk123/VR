using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    [Header("Vị trí spawn")]
    public Transform spawnPoint;

    [Header("XR Rig (kéo Camera Rig vào)")]
    public Transform xrRig;
    public Transform headCamera;

    void Start()
    {
        TeleportToSpawn();
    }

    public void TeleportToSpawn()
    {
        if (spawnPoint == null || xrRig == null || headCamera == null)
        {
            Debug.LogWarning("[PlayerSpawn] Thiếu reference!");
            return;
        }

        // Offset từ rig đến head (trong XZ)
        Vector3 headOffset = headCamera.position - xrRig.position;
        headOffset.y = 0;

        // Đặt rig sao cho head trùng spawnPoint
        Vector3 targetPos = spawnPoint.position - headOffset;
        targetPos.y = spawnPoint.position.y;
        xrRig.position = targetPos;

        Debug.Log($"[PlayerSpawn] HS spawn tại {headCamera.position}");
    }
}