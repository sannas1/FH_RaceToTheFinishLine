using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RaceHUD : MonoBehaviour
{
    public static RaceHUD Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.4f;

    [SerializeField] private Image speedFillImage;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Gradient speedColorGradient;

    [SerializeField] private TMP_Text lapText;
    [SerializeField] private TMP_Text lapTimeText;
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private TMP_Text bestLapText;
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private Color bestLapNormalColor = Color.white;
    [SerializeField] private Color bestLapHighlightColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private float bestLapPunchScale = 1.3f;
    [SerializeField] private float bestLapPunchDuration = 0.5f;

    private CarController playerCar;
    private Coroutine fadeCoroutine;
    private Coroutine punchCoroutine;

    private void Awake()
    {
        Instance = this;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerCar = player.GetComponentInParent<CarController>() ?? player.GetComponent<CarController>();
        }

        if (bestLapText != null)
        {
            bestLapText.color = bestLapNormalColor;
        }
    }

    private void Start()
    {
        RaceParticipant player = RaceManager.Instance != null ? RaceManager.Instance.Player : null;
        if (player != null)
        {
            player.OnBestLapImproved += HandleBestLapImproved;
        }
    }

    private void OnDestroy()
    {
        RaceParticipant player = RaceManager.Instance != null ? RaceManager.Instance.Player : null;
        if (player != null)
        {
            player.OnBestLapImproved -= HandleBestLapImproved;
        }
    }

    private void Update()
    {
        UpdateSpeed();
        UpdateTimers();
    }

    private void UpdateSpeed()
    {
        if (playerCar == null) return;

        float speedKmh = playerCar.CurrentVelocity.magnitude * 3.6f;
        float maxSpeedKmh = playerCar.MaxSpeed * 3.6f;
        float ratio = maxSpeedKmh > 0f ? Mathf.Clamp01(speedKmh / maxSpeedKmh) : 0f;

        if (speedFillImage != null)
        {
            speedFillImage.fillAmount = ratio * 0.5f;
            speedFillImage.color = speedColorGradient.Evaluate(ratio);
        }

        if (speedText != null)
        {
            speedText.text = $"{Mathf.RoundToInt(speedKmh)} km/h";
        }
    }

    private void UpdateTimers()
    {
        RaceManager race = RaceManager.Instance;
        RaceParticipant player = race != null ? race.Player : null;
        if (race == null || player == null) return;

        if (lapText != null)
        {
            int displayLap = Mathf.Min(player.CurrentLap + 1, player.TotalLaps);
            lapText.text = $"Lap {displayLap} / {player.TotalLaps}";
        }

        if (lapTimeText != null)
        {
            lapTimeText.text = RaceTimeFormatter.Format(player.ElapsedLapTime);
        }

        if (totalTimeText != null)
        {
            totalTimeText.text = RaceTimeFormatter.Format(race.ElapsedTotalTime);
        }

        if (bestLapText != null)
        {
            bestLapText.text = player.HasBestLap ? RaceTimeFormatter.Format(player.BestLapTime) : "--:--.---";
        }

        if (positionText != null)
        {
            RaceParticipant[] standings = race.GetStandings();
            var builder = new System.Text.StringBuilder();

            for (int i = 0; i < standings.Length; i++)
            {
                string name = standings[i] == player ? "YOU" : standings[i].DisplayName;
                builder.AppendLine($"P{i + 1}: {name}");
            }

            positionText.text = builder.ToString();
        }
    }

    private void HandleBestLapImproved()
    {
        if (bestLapText == null) return;

        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchBestLap());
    }

    private IEnumerator PunchBestLap()
    {
        Transform textTransform = bestLapText.transform;
        float elapsed = 0f;

        while (elapsed < bestLapPunchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bestLapPunchDuration;

            float scaleT = Mathf.Sin(t * Mathf.PI);
            textTransform.localScale = Vector3.one * (1f + (bestLapPunchScale - 1f) * scaleT);
            bestLapText.color = Color.Lerp(bestLapHighlightColor, bestLapNormalColor, t);

            yield return null;
        }

        textTransform.localScale = Vector3.one;
        bestLapText.color = bestLapNormalColor;
    }

    public void FadeIn()
    {
        StartFade(1f);
    }

    public void FadeOut()
    {
        StartFade(0f);
    }

    private void StartFade(float target)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(Fade(target));
    }

    private IEnumerator Fade(float target)
    {
        float start = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = target;
    }
}
