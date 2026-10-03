using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;

    public int Index => checkpointIndex;

    public Vector3 ResetPosition
    {
        get
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null) return transform.position;

            Vector3 worldCenter = transform.TransformPoint(box.center);
            return new Vector3(worldCenter.x, transform.position.y, worldCenter.z);
        }
    }

    public Quaternion ResetRotation => transform.rotation;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody hitBody = other.attachedRigidbody;
        if (hitBody == null) return;

        RaceParticipant participant = hitBody.GetComponentInParent<RaceParticipant>();
        if (participant == null) return;

        participant.CheckpointPassed(this);
    }
}
