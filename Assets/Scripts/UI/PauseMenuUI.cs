using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    private const string MainMenuSceneName = "MainMenu";
    private const float FadeDuration = 0.25f;

    private CanvasGroup canvasGroup;
    private bool isPaused;
    private Coroutine fadeCoroutine;

    // baut sich selbst, damit man im editor nichts verdrahten muss.
    // RuntimeInitialize feuert nur einmal, also nochmal an sceneLoaded haengen (menu -> rennen)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += (scene, mode) => TrySpawn();
        TrySpawn();
    }

    private static void TrySpawn()
    {
        if (Instance != null) return;
        if (Object.FindAnyObjectByType<RaceManager>() == null) return; // nur im rennen, nicht im menu

        var host = new GameObject("PauseMenu");
        host.AddComponent<PauseMenuUI>();
    }

    private void Awake()
    {
        Instance = this;
        BuildUI();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Continue();
            else Pause();
        }
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true; // timeScale stoppt kein audio, das muss extra
        Cursor.visible = true;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        StartFade(1f);
    }

    private void Continue()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = false;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        StartFade(0f);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f; // sonst bleibt das menu eingefroren
        AudioListener.pause = false; // sonst bleibt der sound auch im hauptmenu stumm
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void Exit()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Application.Quit tut im editor nichts
#else
        Application.Quit();
#endif
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

        // unscaled, sonst steht der fade bei timeScale 0 auch still
        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / FadeDuration);
            yield return null;
        }

        canvasGroup.alpha = target;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f; // nicht eingefroren lassen falls szene waehrend pause wechselt
        AudioListener.pause = false;
        if (Instance == this) Instance = null;
    }

    // ui komplett per code, kein prefab noetig
    private void BuildUI()
    {
        EnsureEventSystem();

        // canvas ueber dem HUD
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // abgedunkelter hintergrund
        var backdrop = CreateChild("Backdrop", transform);
        StretchFull(backdrop);
        backdrop.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.72f);

        // titel + buttons mittig gestapelt
        var panel = CreateChild("Panel", transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(520f, 520f);
        panel.anchoredPosition = Vector2.zero;

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 24f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;

        CreateTitle("PAUSED", panel);
        CreateButton("Continue", panel, new Color(0.20f, 0.62f, 0.34f), Continue);
        CreateButton("Main Menu", panel, new Color(0.24f, 0.42f, 0.72f), GoToMainMenu);
        CreateButton("Exit", panel, new Color(0.72f, 0.24f, 0.24f), Exit);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static RectTransform CreateChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateTitle(string text, Transform parent)
    {
        var rect = CreateChild("Title", parent);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 72f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        var element = rect.gameObject.AddComponent<LayoutElement>();
        element.minHeight = element.preferredHeight = 110f;
    }

    private void CreateButton(string text, Transform parent, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var rect = CreateChild($"{text}Button", parent);

        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var element = rect.gameObject.AddComponent<LayoutElement>();
        element.minHeight = element.preferredHeight = 80f;

        var labelRect = CreateChild("Label", rect);
        StretchFull(labelRect);
        var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 38f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
    }
}
