using UnityEngine;
using UnityEngine.AI;

public class RespiratoryAirRoute : MonoBehaviour
{
    private const float DefaultArrivalDistance = 1f;

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float arrivalDistance = DefaultArrivalDistance;

    private Transform airway;
    private Transform destinationLung;
    private bool isConfigured;
    private bool isReturningToAirway;
    private bool hasIssuedDestination;

    /// <summary>
    /// Configures the air particle to travel from a lung back to the airway after reaching its assigned lung.
    /// </summary>
    public void Configure(NavMeshAgent airAgent, Transform airwayPoint, Transform lungPoint)
    {
        agent = airAgent;
        airway = airwayPoint;
        destinationLung = lungPoint;
        isReturningToAirway = false;
        hasIssuedDestination = false;
        isConfigured = airway != null && destinationLung != null;
    }

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }

    private void Update()
    {
        if (!isConfigured || agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        if (!hasIssuedDestination)
        {
            SetDestination(destinationLung);
            hasIssuedDestination = true;
            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        if (!agent.hasPath)
        {
            SetDestination(isReturningToAirway ? airway : destinationLung);
            return;
        }

        if (agent.remainingDistance <= arrivalDistance)
        {
            if (isReturningToAirway)
            {
                Destroy(gameObject);
                return;
            }

            isReturningToAirway = true;
            SetDestination(airway);
        }
    }

    private void SetDestination(Transform destination)
    {
        if (destination != null)
        {
            agent.SetDestination(destination.position);
        }
    }
}
