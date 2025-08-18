using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera playerCamera;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float pitchClamp = 85f;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 4f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1.0f;
    [SerializeField] private KeyCode dashKey = KeyCode.E;

    CharacterController controller;
    float pitch;
    Vector3 velocity;
    bool isDashing;
    float lastDashTime;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Look();
        Move();
        Dash();
    }

    void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mx);
        pitch = Mathf.Clamp(pitch - my, -pitchClamp, pitchClamp);
        if (playerCamera) playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move()
    {
        bool grounded = controller.isGrounded;
        if (grounded && velocity.y < 0f) velocity.y = -2f;

        Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        Vector3 wish = (transform.right * input.x + transform.forward * input.y) * speed;
        if (!isDashing) controller.Move(wish * Time.deltaTime);

        if (grounded && Input.GetKeyDown(KeyCode.Space))
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void Dash()
    {
        if (isDashing || !Input.GetKeyDown(dashKey) || Time.time - lastDashTime < dashCooldown) return;

        Vector3 camF = playerCamera ? playerCamera.transform.forward : transform.forward;
        Vector3 camR = playerCamera ? playerCamera.transform.right : transform.right;
        camF.y = 0f; camR.y = 0f; camF.Normalize(); camR.Normalize();

        Vector3 input = new(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        Vector3 dir = (camF * input.z + camR * input.x);
        if (dir.sqrMagnitude < 0.01f) dir = camF;
        dir.Normalize();

        StartCoroutine(DashRoutine(dir));
        lastDashTime = Time.time;
    }

    System.Collections.IEnumerator DashRoutine(Vector3 dir)
    {
        isDashing = true;
        float t = 0f;
        Vector3 start = transform.position;
        Vector3 end = start + dir * dashDistance;

        while (t < dashDuration)
        {
            Vector3 target = Vector3.Lerp(start, end, t / dashDuration);
            controller.Move(target - transform.position);
            t += Time.deltaTime;
            yield return null;
        }
        controller.Move(end - transform.position);
        isDashing = false;
    }
}
