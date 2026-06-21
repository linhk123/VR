using UnityEngine;
using System.Collections;

public class BlockSpawner : MonoBehaviour
{
    public GameObject blockPrefab; // Kéo file Prefab Khối hình gốc vào đây
    public Transform spawnPoint;    // Vị trí trên bàn làm việc của người chơi
    public float delayTime = 2f;    // Thời gian chờ để sinh ra khối mới

    private void Start()
    {
        SpawnBlock();
    }

    public void SpawnBlock()
    {
        if (blockPrefab != null && spawnPoint != null)
        {
            Instantiate(blockPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }

    // Hàm này được gọi từ hệ thống khi khối cũ bị chém hoặc bị ném vào lò rèn
    public void TriggerRespawn()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(delayTime);
        SpawnBlock();
    }
}