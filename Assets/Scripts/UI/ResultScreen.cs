using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultScreen : MonoBehaviour
{
    public static ResultScreen Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private TMP_Text lapTimesText;
    [SerializeField] private TMP_Text bestLapText;
    [SerializeField] private float fadeDuration = 0.5f;

    public bool IsShowing => canvasGroup != null && canvasGroup.blocksRaycasts;

    private void Awake()
    {
        Instance = this;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Show(bool playerWon, int position, int participantCount, float totalTime, IReadOnlyList<float> lapTimes, float bestLapTime)
    {
        Cursor.visible = true;

        if (titleText != null)
        {
            titleText.text = playerWon ? "Victory!" : "Defeat";
        }

        if (positionText != null)
        {
            positionText.text = $"Final Position: {position}";
        }

        totalTimeText.text = $"Total Time: {RaceTimeFormatter.Format(totalTime)}";

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < lapTimes.Count; i++)
        {
            builder.AppendLine($"Lap {i + 1}: {RaceTimeFormatter.Format(lapTimes[i])}");
        }
        lapTimesText.text = builder.ToString();

        bestLapText.text = $"Best Lap: {RaceTimeFormatter.Format(bestLapTime)}";

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
