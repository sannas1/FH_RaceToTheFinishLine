using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string raceSceneName = "MainScene";
    [SerializeField] private RectTransform transitionPanel;
    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private float transitionDuration = 0.6f;
    [SerializeField] private int checkerColumns = 10;
    [SerializeField] private int checkerRows = 8;

    private Image transitionImage;

    private void Awake()
    {
        transitionImage = transitionPanel.GetComponent<Image>();
        transitionImage.sprite = CreateCheckerSprite();
        transitionPanel.anchoredPosition = new Vector2(-transitionPanel.rect.width, 0f);
    }

    public void OnStartClicked()
    {
        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = false;
        }

        StartCoroutine(PlayTransitionAndLoad());
    }

    public void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator PlayTransitionAndLoad()
    {
        float startX = -transitionPanel.rect.width;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float eased = t * t * (3f - 2f * t);
            transitionPanel.anchoredPosition = new Vector2(Mathf.Lerp(startX, 0f, eased), 0f);
            yield return null;
        }

        transitionPanel.anchoredPosition = Vector2.zero;
        SceneManager.LoadScene(raceSceneName);
    }

    private Sprite CreateCheckerSprite()
    {
        var texture = new Texture2D(checkerColumns, checkerRows) { filterMode = FilterMode.Point };

        for (int y = 0; y < checkerRows; y++)
        {
            for (int x = 0; x < checkerColumns; x++)
            {
                bool isBlack = (x + y) % 2 == 0;
                texture.SetPixel(x, y, isBlack ? Color.black : Color.white);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, checkerColumns, checkerRows), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
    }
}
