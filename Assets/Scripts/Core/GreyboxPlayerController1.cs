using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GreyboxPlayerController1 : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turnSpeed = 14f;

    private Rigidbody body;
    private Vector3 moveInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        Vector3 velocity = moveInput * moveSpeed;
        body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);

        if (moveInput.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput, Vector3.up);
            body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
    }
}
