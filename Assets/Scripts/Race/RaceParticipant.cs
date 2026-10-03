using System;
using System.Collections.Generic;
using UnityEngine;

public class RaceParticipant : MonoBehaviour
{
    [SerializeField] private CarController car;
    [SerializeField] private string displayName = "Racer";

    private Checkpoint[] orderedCheckpoints;
    private Quaternion[] checkpointFacing;
    private int totalLaps;

    private int nextCheckpointIndex = 1;
    private int currentLap;
    private bool hasCheckpoint;
    private Vector3 lastCheckpointPosition;
    private Quaternion lastCheckpointRotation;
    private Vector3 lastCheckpointVelocity;

    private bool isTiming;
    private float lapStartTime;
    private readonly List<float> lapTimes = new List<float>();
    private float bestLapTime = float.MaxValue;

    private bool finished;
    private float finishTime;

    public CarController Car => car;
    public string DisplayName => displayName;
    public int CurrentLap => currentLap;
    public int TotalLaps => totalLaps;
    public int NextCheckpointIndex => nextCheckpointIndex;

    // Monotonic progress within the current lap. nextCheckpointIndex wraps back to 0
    // in the final segment before the finish line, which is actually the furthest
    // point in the lap, so it must rank as the highest value, not the lowest.
    public int LapProgress
    {
        get
        {
            if (orderedCheckpoints == null || orderedCheckpoints.Length == 0) return nextCheckpointIndex;
            return nextCheckpointIndex == 0 ? orderedCheckpoints.Length : nextCheckpointIndex;
        }
    }

    public float DistanceToNextCheckpoint()
    {
        if (orderedCheckpoints == null || orderedCheckpoints.Length == 0) return float.MaxValue;
        Vector3 targetPosition = orderedCheckpoints[nextCheckpointIndex % orderedCheckpoints.Length].ResetPosition;
        return Vector3.Distance(transform.position, targetPosition);
    }
    public bool Finished => finished;
    public float FinishTime => finishTime;
    public bool HasBestLap => lapTimes.Count > 0;
    public float BestLapTime => bestLapTime;
    public IReadOnlyList<float> LapTimes => lapTimes;
    public float ElapsedLapTime => isTiming ? Time.time - lapStartTime : 0f;

    public event Action OnBestLapImproved;
    public event Action<RaceParticipant> OnFinished;

    public void Initialize(Checkpoint[] checkpoints, Quaternion[] facing, int laps)
    {
        orderedCheckpoints = checkpoints;
        checkpointFacing = facing;
        totalLaps = laps;

        if (checkpoints.Length > 0)
        {
            lastCheckpointPosition = checkpoints[0].ResetPosition;
            lastCheckpointRotation = facing[0];
            hasCheckpoint = true;
        }
    }

    public void BeginTiming()
    {
        isTiming = true;
        lapStartTime = Time.time;
    }

    public void CheckpointPassed(Checkpoint checkpoint)
    {
        if (finished || orderedCheckpoints == null || orderedCheckpoints.Length == 0) return;

        int index = checkpoint.Index;

        if (index == 0)
        {
            if (nextCheckpointIndex == 0)
            {
                currentLap++;

                float lapTime = Time.time - lapStartTime;
                lapTimes.Add(lapTime);

                if (lapTime < bestLapTime)
                {
                    bestLapTime = lapTime;
                    OnBestLapImproved?.Invoke();
                }

                lapStartTime = Time.time;

                RememberCheckpoint(checkpoint);
                nextCheckpointIndex = 1;

                if (currentLap >= totalLaps)
                {
                    finished = true;
                    finishTime = Time.time;
                    OnFinished?.Invoke(this);
                }
            }

            return;
        }

        if (index == nextCheckpointIndex)
        {
            RememberCheckpoint(checkpoint);
            nextCheckpointIndex = (index + 1) % orderedCheckpoints.Length;
        }
    }

    private void RememberCheckpoint(Checkpoint checkpoint)
    {
        lastCheckpointPosition = checkpoint.ResetPosition;
        lastCheckpointRotation = checkpointFacing[checkpoint.Index];
        lastCheckpointVelocity = car != null ? car.CurrentVelocity : Vector3.zero;
        hasCheckpoint = true;
    }

    public void ResetToLastCheckpoint()
    {
        if (!hasCheckpoint || car == null) return;
        car.ResetTo(lastCheckpointPosition, lastCheckpointRotation, lastCheckpointVelocity);
    }
}
