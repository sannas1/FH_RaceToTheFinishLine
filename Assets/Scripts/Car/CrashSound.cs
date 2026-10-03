using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CrashSound : MonoBehaviour
{
    [SerializeField] private AudioClip crashClip;
    [SerializeField] private string barrierTag = "Barrier";
    [SerializeField] private float minImpactSpeed = 3f;
    [Range(0f, 2f)] [SerializeField] private float volume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(barrierTag)) return;
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;

        audioSource.PlayOneShot(crashClip, volume);
    }
}
