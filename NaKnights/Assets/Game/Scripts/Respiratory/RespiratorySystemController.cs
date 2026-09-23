using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RespiratorySystemController : MonoBehaviour
{
    private const float DefaultSpawnInterval = 5f;
    private const float DefaultContaminatedAirChance = 0.05f;
    private const float DefaultNavMeshSampleRadius = 10f;

    [SerializeField] private GameObject airPrefab;
    [SerializeField] private GameObject contaminatedAirPrefab;
    [SerializeField] private Transform airway;
    [SerializeField] private Transform leftLung;
    [SerializeField] private Transform rightLung;
    [SerializeField] private float navMeshSampleRadius = DefaultNavMeshSampleRadius;

    [Tooltip("Seconds between air spawns.")]
    public float spawnInterval = DefaultSpawnInterval;

    [Range(0f, 1f)]
    [Tooltip("Chance that a spawn uses the contaminated air prefab.")]
    public float contaminatedAirChance = DefaultContaminatedAirChance;

    private Coroutine spawnRoutine;

    private void Start()
    {
        if (!HasRequiredSpawnData())
        {
            Debug.LogError("RespiratorySystemController requires both prefabs, the airway, and both lungs.", this);
            return;
        }

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
            SpawnAir();
        }
    }

    private void SpawnAir()
    {
        GameObject selectedPrefab = Random.value < Mathf.Clamp01(contaminatedAirChance)
            ? contaminatedAirPrefab
            : airPrefab;

        Vector3 spawnPosition = GetNavMeshPosition(airway.position);
        GameObject air = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity, transform);
        NavMeshAgent agent = air.GetComponent<NavMeshAgent>();
        RespiratoryAirRoute route = air.GetComponent<RespiratoryAirRoute>();

        if (agent == null)
        {
            agent = air.AddComponent<NavMeshAgent>();
        }

        if (route == null)
        {
            route = air.AddComponent<RespiratoryAirRoute>();
        }

        Transform destinationLung = Random.value < 0.5f ? leftLung : rightLung;
        route.Configure(agent, airway, destinationLung);
    }

    /// <summary>
    /// Destroys every active air particle spawned by this respiratory system.
    /// </summary>
    public void DestroyAllAir()
    {
        RespiratoryAirRoute[] activeAir = GetComponentsInChildren<RespiratoryAirRoute>(true);
        foreach (RespiratoryAirRoute air in activeAir)
        {
            if (air != null && air.gameObject != gameObject)
            {
                Destroy(air.gameObject);
            }
        }
    }

    private bool HasRequiredSpawnData()
    {
        return airPrefab != null && contaminatedAirPrefab != null && airway != null && leftLung != null && rightLung != null;
    }

    private Vector3 GetNavMeshPosition(Vector3 requestedPosition)
    {
        if (NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return requestedPosition;
    }
}
