using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [SerializeField] private float motorForce = 14f;
    [SerializeField] private float maxSpeed = 25f;
    [SerializeField] private float turnSpeed = 200f;
    [SerializeField] private float rollOutDrag = 4f;
    [SerializeField] private float handbrakeForce = 20f;
    [SerializeField] private bool invertForward; // aus irgendeinem grund sind kenney assets inverted
    [SerializeField] private bool isPlayerControlled = true;
    [SerializeField] private float extraGravity = 20f;
    [SerializeField] private float uprightForce = 10f;
    [SerializeField] private float grip = 18f;

    [SerializeField] private Transform frontLeftWheel;
    [SerializeField] private Transform frontRightWheel;
    [SerializeField] private Transform rearLeftWheel;
    [SerializeField] private Transform rearRightWheel;
    [SerializeField] private float maxSteerAngle = 25f;
    [SerializeField] private float wheelSpinSpeed = 200f;

    private Rigidbody rb;
    private float moveInput;
    private float steerInput;
    private bool handbrakeHeld;
    private float wheelSpinAngle;
    private bool inputEnabled = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
    }

    public bool InputEnabled => inputEnabled;

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        // sonst bleibt der letzte gaswert stehen und die KI faehrt nach dem ziel weiter
        if (!enabled)
        {
            moveInput = 0f;
            steerInput = 0f;
            handbrakeHeld = false;
        }
    }

    private void Update()
    {
        if (isPlayerControlled)
        {
            if (inputEnabled)
            {
                moveInput = Input.GetAxis("Vertical");
                steerInput = Input.GetAxis("Horizontal");
                handbrakeHeld = Input.GetKey(KeyCode.Space);
            }
            else
            {
                moveInput = 0f;
                steerInput = 0f;
                handbrakeHeld = false;
            }
        }

        UpdateWheelVisuals();
    }

    private Vector3 CarForward => invertForward ? -transform.forward : transform.forward;

    public Vector3 CurrentVelocity => rb.linearVelocity;
    public float MaxSpeed => maxSpeed;
    public Vector3 Forward => CarForward;

    public void SetAIInput(float move, float steer)
    {
        if (!inputEnabled) return;
        moveInput = Mathf.Clamp(move, -1f, 1f);
        steerInput = Mathf.Clamp(steer, -1f, 1f);
    }

    public void ResetTo(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        Quaternion facing = invertForward ? rotation * Quaternion.Euler(0f, 180f, 0f) : rotation;

        rb.position = position;
        rb.rotation = facing;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, CarForward);

        Drive(forwardSpeed);
        Steer(forwardSpeed);
        ApplyGrip();
        KeepGrounded();

        if (handbrakeHeld)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, handbrakeForce * Time.fixedDeltaTime);
        }
    }

    private void ApplyGrip()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(CarForward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.001f) return;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 forwardVelocity = flatForward * Vector3.Dot(horizontalVelocity, flatForward);
        Vector3 sidewaysVelocity = horizontalVelocity - forwardVelocity;

        rb.linearVelocity -= sidewaysVelocity * grip * Time.fixedDeltaTime;
    }

    private void KeepGrounded()
    {
        rb.AddForce(Vector3.down * extraGravity * rb.mass);

        Vector3 tiltAxis = Vector3.Cross(transform.up, Vector3.up);
        rb.AddTorque(tiltAxis * uprightForce);
    }

    private void Drive(float forwardSpeed)
    {
        if (Mathf.Abs(moveInput) > 0.05f)
        {
            if (Mathf.Abs(forwardSpeed) < maxSpeed)
            {
                rb.AddForce(CarForward * moveInput * motorForce * rb.mass);
            }
        }
        else
        {
            Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(-flatVelocity * rollOutDrag * rb.mass);
        }
    }

    private void Steer(float forwardSpeed)
    {
        float speedRatio = Mathf.Abs(forwardSpeed) / maxSpeed;
        
        float turnAmount = Mathf.Clamp01(speedRatio / 0.3f);
        if (speedRatio > 0.6f)
        {
            turnAmount *= 1f - (speedRatio - 0.6f) * 0.5f;
        }

        float direction = forwardSpeed < 0f ? -1f : 1f;
        float yaw = steerInput * turnSpeed * turnAmount * direction * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));
    }

    private void UpdateWheelVisuals()
    {
        float steerAngle = steerInput * maxSteerAngle;
        wheelSpinAngle += Vector3.Dot(rb.linearVelocity, CarForward) * wheelSpinSpeed * Time.deltaTime;

        SetWheelRotation(frontLeftWheel, steerAngle);
        SetWheelRotation(frontRightWheel, steerAngle);
        SetWheelRotation(rearLeftWheel, 0f);
        SetWheelRotation(rearRightWheel, 0f);
    }

    private void SetWheelRotation(Transform wheel, float steerAngle)
    {
        if (wheel == null) return;
        wheel.localRotation = Quaternion.Euler(0f, steerAngle, 0f) * Quaternion.Euler(wheelSpinAngle, 0f, 0f);
    }
}
