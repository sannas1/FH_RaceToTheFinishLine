using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [SerializeField] private int totalLaps = 3;

    private RaceParticipant[] participants;
    private RaceParticipant player;
    private bool raceStarted;
    private bool raceEnded;
    private float raceStartTime;

    public RaceParticipant Player => player;
    public int TotalLaps => totalLaps;
    public int ParticipantCount => participants != null ? participants.Length : 0;
    public float ElapsedTotalTime => raceStarted ? Time.time - raceStartTime : 0f;

    private void Awake()
    {
        Instance = this;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;

        Checkpoint[] foundCheckpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
        int checkpointCount = foundCheckpoints.Length;
        var orderedCheckpoints = new Checkpoint[checkpointCount];

        foreach (Checkpoint checkpoint in foundCheckpoints)
        {
            orderedCheckpoints[checkpoint.Index] = checkpoint;
        }

        var checkpointFacing = new Quaternion[checkpointCount];
        for (int i = 0; i < checkpointCount; i++)
        {
            Vector3 previous = orderedCheckpoints[(i - 1 + checkpointCount) % checkpointCount].ResetPosition;
            Vector3 next = orderedCheckpoints[(i + 1) % checkpointCount].ResetPosition;
            Vector3 direction = next - previous;

            checkpointFacing[i] = direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : orderedCheckpoints[i].ResetRotation;
        }

        if (checkpointCount == 0)
        {
            Debug.LogError("RaceManager: no Checkpoints found in the scene.");
        }

        participants = Object.FindObjectsByType<RaceParticipant>(FindObjectsSortMode.None);
        if (participants.Length == 0)
        {
            Debug.LogError("RaceManager: no RaceParticipants found in the scene.");
        }

        foreach (RaceParticipant participant in participants)
        {
            participant.Initialize(orderedCheckpoints, checkpointFacing, totalLaps);
            participant.OnFinished += HandleParticipantFinished;

            if (player == null && participant.Car != null && participant.Car.CompareTag("Player"))
            {
                player = participant;
            }
        }

        if (player == null)
        {
            Debug.LogError("RaceManager: no RaceParticipant found on the 'Player' tagged car.");
        }

        LockAllCars();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            player?.ResetToLastCheckpoint();
        }
    }

    private void LockAllCars()
    {
        foreach (RaceParticipant participant in participants)
        {
            if (participant.Car != null) participant.Car.SetInputEnabled(false);
        }
    }

    public void BeginTiming()
    {
        raceStarted = true;
        raceStartTime = Time.time;

        foreach (RaceParticipant participant in participants)
        {
            participant.BeginTiming();
            if (participant.Car != null) participant.Car.SetInputEnabled(true);
        }

        if (RaceHUD.Instance != null)
        {
            RaceHUD.Instance.FadeIn();
        }
    }

    public int GetPosition(RaceParticipant participant)
    {
        int position = 1;
        foreach (RaceParticipant other in participants)
        {
            if (other != participant && IsAhead(other, participant)) position++;
        }
        return position;
    }

    public RaceParticipant[] GetStandings()
    {
        var ranked = (RaceParticipant[])participants.Clone();
        System.Array.Sort(ranked, (a, b) =>
        {
            if (IsAhead(a, b)) return -1;
            if (IsAhead(b, a)) return 1;
            return 0;
        });
        return ranked;
    }

    private static bool IsAhead(RaceParticipant a, RaceParticipant b)
    {
        if (a.CurrentLap != b.CurrentLap) return a.CurrentLap > b.CurrentLap;
        if (a.LapProgress != b.LapProgress) return a.LapProgress > b.LapProgress;
        return a.DistanceToNextCheckpoint() < b.DistanceToNextCheckpoint();
    }

    private void HandleParticipantFinished(RaceParticipant finisher)
    {
        if (raceEnded) return;
        raceEnded = true;

        FinishRace(finisher == player);
    }

    private void FinishRace(bool playerWon)
    {
        float totalTime = ElapsedTotalTime;
        int position = player != null ? GetPosition(player) : 1;

        Debug.Log(playerWon
            ? $"Victory! Total time {RaceTimeFormatter.Format(totalTime)}"
            : $"Defeat - finished P{position}/{participants.Length}");

        foreach (RaceParticipant participant in participants)
        {
            if (participant.Car != null) participant.Car.SetInputEnabled(false);
        }

        if (ResultScreen.Instance != null && player != null)
        {
            ResultScreen.Instance.Show(playerWon, position, participants.Length, totalTime, player.LapTimes, player.BestLapTime);
        }
        else
        {
            Debug.LogError("RaceManager: no ResultScreen or player found to show the results on.");
        }

        if (RaceHUD.Instance != null)
        {
            RaceHUD.Instance.FadeOut();
        }
    }
}
