using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject macrophage;
    public GameObject neutrophil;
    public float interval = 0f;

    public void SpawnMacrophage()
    {
        Vector3 spawnPos = new Vector3(Random.Range(-13, 13), 1, Random.Range(-13, 13));
        Instantiate(macrophage, spawnPos, Quaternion.identity);
    }

    public void SpawnNeutrophil()
    {
        Vector3 spawnPos = new Vector3(Random.Range(-13, 13), 1, Random.Range(-13, 13));
        Instantiate(neutrophil, spawnPos, Quaternion.identity);
    }
}