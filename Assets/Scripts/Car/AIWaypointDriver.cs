using UnityEngine;

[RequireComponent(typeof(CarController))]
public class AIWaypointDriver : MonoBehaviour
{
    [SerializeField] private RaceParticipant participant;
    [SerializeField] private float waypointReachDistance = 6f;
    [SerializeField] private float maxWaypointHeightDifference = 5f;
    [SerializeField] private float maxSteerAngle = 60f;
    [SerializeField] private float slowdownAngle = 45f;
    [SerializeField] private float minThrottle = 0.4f;

    [SerializeField] private float lineOffset = 0f;

    //adding a bit extra stuff to try to get them to be smart about becoming unstuck
    [SerializeField] private float progressCheckInterval = 1f;
    [SerializeField] private float minProgressPerInterval = 1.5f;
    [SerializeField] private int noProgressStreaksBeforeReverse = 2;
    [SerializeField] private int noProgressStreaksBeforeTeleport = 5;
    [SerializeField] private float reverseDuration = 0.6f;

    private CarController car;
    private Transform[] orderedWaypoints;
    private int currentWaypointIndex;

    private float progressCheckTimer;
    private float distanceAtLastCheck = float.MaxValue;
    private int noProgressStreak;
    private bool isReversing;
    private float reverseTimer;

    private void Awake()
    {
        car = GetComponent<CarController>();

        AIWaypoint[] found = Object.FindObjectsByType<AIWaypoint>(FindObjectsSortMode.None);
        orderedWaypoints = new Transform[found.Length];
        foreach (AIWaypoint waypoint in found)
        {
            orderedWaypoints[waypoint.Index] = waypoint.transform;
        }

        if (orderedWaypoints.Length == 0)
        {
            Debug.LogError("AIWaypointDriver: no AIWaypoints found in the scene.");
        }
    }

    private void Update()
    {
        if (participant != null && participant.Finished)
        {
            car.SetAIInput(0f, 0f);
            return;
        }

        if (orderedWaypoints == null || orderedWaypoints.Length == 0) return;
        
        if (!car.InputEnabled)
        {
            ResetUnstuckState();
            return;
        }

        if (isReversing)
        {
            DriveReverse();
            return;
        }

        Vector3 toTarget = GetVectorToCurrentWaypoint();
        TrackProgress(toTarget.magnitude);

        float heightDifference = Mathf.Abs(orderedWaypoints[currentWaypointIndex].position.y - transform.position.y);
        if (toTarget.magnitude < waypointReachDistance && heightDifference < maxWaypointHeightDifference)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % orderedWaypoints.Length;
            toTarget = GetVectorToCurrentWaypoint();
            ResetUnstuckState();
        }

        if (toTarget.sqrMagnitude < 0.01f) return;

        float angle = Vector3.SignedAngle(car.Forward, toTarget, Vector3.up);

        float steer = Mathf.Clamp(angle / maxSteerAngle, -1f, 1f);
        float throttle = Mathf.Lerp(1f, minThrottle, Mathf.Clamp01(Mathf.Abs(angle) / slowdownAngle));

        car.SetAIInput(throttle, steer);
    }
    
    private void TrackProgress(float currentDistance)
    {
        progressCheckTimer += Time.deltaTime;
        if (progressCheckTimer < progressCheckInterval) return;
        progressCheckTimer = 0f;

        if (distanceAtLastCheck - currentDistance < minProgressPerInterval)
        {
            noProgressStreak++;

            if (noProgressStreak >= noProgressStreaksBeforeTeleport)
            {
                TeleportRecover();
            }
            else if (noProgressStreak >= noProgressStreaksBeforeReverse)
            {
                StartReversing();
            }
        }
        else
        {
            noProgressStreak = 0;
        }

        distanceAtLastCheck = currentDistance;
    }

    private void StartReversing()
    {
        isReversing = true;
        reverseTimer = reverseDuration;
    }

    private void DriveReverse()
    {
        reverseTimer -= Time.deltaTime;
        if (reverseTimer <= 0f)
        {
            isReversing = false;
            progressCheckTimer = 0f;
            distanceAtLastCheck = GetVectorToCurrentWaypoint().magnitude;
            return;
        }
        
        Vector3 toTarget = GetVectorToCurrentWaypoint();
        float angle = toTarget.sqrMagnitude > 0.01f ? Vector3.SignedAngle(car.Forward, toTarget, Vector3.up) : 0f;
        float steer = Mathf.Clamp(angle / maxSteerAngle, -1f, 1f);

        car.SetAIInput(-1f, -steer);
    }
    
    private void TeleportRecover()
    {
        participant?.ResetToLastCheckpoint();
        
        SnapToNearestWaypoint();

        ResetUnstuckState();
    }

    private void ResetUnstuckState()
    {
        noProgressStreak = 0;
        progressCheckTimer = 0f;
        distanceAtLastCheck = float.MaxValue;
        isReversing = false;
        reverseTimer = 0f;
    }

    private void SnapToNearestWaypoint()
    {
        float bestDistance = float.MaxValue;
        int bestIndex = currentWaypointIndex;

        for (int i = 0; i < orderedWaypoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, orderedWaypoints[i].position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        currentWaypointIndex = bestIndex;
    }

    private Vector3 GetVectorToCurrentWaypoint()
    {
        Vector3 toTarget = GetTargetPosition(currentWaypointIndex) - transform.position;
        toTarget.y = 0f;
        return toTarget;
    }

    private Vector3 GetTargetPosition(int waypointIndex)
    {
        Vector3 basePosition = orderedWaypoints[waypointIndex].position;
        if (Mathf.Approximately(lineOffset, 0f) || orderedWaypoints.Length < 2) return basePosition;

        int nextIndex = (waypointIndex + 1) % orderedWaypoints.Length;
        Vector3 direction = orderedWaypoints[nextIndex].position - basePosition;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return basePosition;

        Vector3 sideways = Vector3.Cross(Vector3.up, direction.normalized);
        return basePosition + sideways * lineOffset;
    }
}
