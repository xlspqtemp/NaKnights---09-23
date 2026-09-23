using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves a spawned food item across the digestive system NavMesh and destroys it at the end point.
/// </summary>
public class DigestiveFoodRoute : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform destination;
    [SerializeField] private float arrivalDistance = 1f;

    private bool destinationRequested;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    /// <summary>Assigns the route destination after the prefab is spawned.</summary>
    public void Configure(Transform destinationTransform)
    {
        destination = destinationTransform;
        destinationRequested = false;
        TrySetDestination();
    }

    private void Update()
    {
        if (agent == null || destination == null || !agent.isOnNavMesh)
            return;

        if (!destinationRequested)
            TrySetDestination();

        if (destinationRequested && !agent.pathPending && agent.remainingDistance <= arrivalDistance)
            Destroy(gameObject);
    }

    private void TrySetDestination()
    {
        if (agent == null || destination == null || !agent.isOnNavMesh)
            return;

        agent.SetDestination(destination.position);
        destinationRequested = true;
    }
}
