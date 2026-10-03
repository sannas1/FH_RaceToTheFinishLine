using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CountdownUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float numberDisplayTime = 1f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private AudioClip shortBeepClip;
    [SerializeField] private AudioClip longBeepClip;

    private AudioSource audioSource;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        StartCoroutine(RunCountdown());
    }

    private IEnumerator RunCountdown()
    {
        yield return ShowNumber("3");
        yield return ShowNumber("2");
        yield return ShowNumber("1");
        yield return ShowNumber("GO!");

        RaceManager.Instance.BeginTiming();
    }

    private IEnumerator ShowNumber(string text)
    {
        countdownText.text = text;
        audioSource.PlayOneShot(text == "GO!" ? longBeepClip : shortBeepClip);
        yield return Fade(0f, 1f);
        yield return new WaitForSeconds(numberDisplayTime);
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
