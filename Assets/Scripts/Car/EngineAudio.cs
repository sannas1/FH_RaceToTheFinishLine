using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EngineAudio : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 25f;
    [SerializeField] private float minPitch = 0.6f;
    [SerializeField] private float maxPitch = 2f;
    [SerializeField] private float pitchSmoothing = 5f;
    [Range(0f, 2f)] [SerializeField] private float volume = 1f;

    private AudioSource audioSource;
    private Rigidbody rb;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        audioSource.loop = true;
        audioSource.volume = volume;
    }

    private void Start()
    {
        audioSource.Play();
    }

    private void Update()
    {
        float speedRatio = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);

        audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch, pitchSmoothing * Time.deltaTime);
    }
}
