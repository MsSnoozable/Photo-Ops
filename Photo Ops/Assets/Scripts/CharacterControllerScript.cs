using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControllerScript : MonoBehaviour
{
    CharacterController cc;
    [SerializeField] GameObject head;
    [SerializeField] Transform groundCheck;

    [Header("Values")]
    public float moveSpeed;
    public float jumpHeight;
    public float mouseSensitivity;

    private float fallSpeed = -10;
    bool isGrounded;
    Vector3 currentVerticalVelocity;

    [SerializeField] LayerMask groundLayer;

    //the distance from the ground before you can execute another jump
    float jumpDistanceLenience = 0.4f;

    float xRotation = 0f;

    // Start is called before the first frame update
    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
        Jump();
        LookAround();
        Gravity();
    }

    void Gravity()
    {
        //checks if a sphere surrounding groundCheck with a radius of jumpDistanceLenience collides with any objects in the groundLayer
        isGrounded = Physics.CheckSphere(groundCheck.position, jumpDistanceLenience, groundLayer);

        if (isGrounded && currentVerticalVelocity.y < 0)
            currentVerticalVelocity.y = 0;
        
        currentVerticalVelocity.y += fallSpeed * Time.deltaTime;
        cc.Move(currentVerticalVelocity * Time.deltaTime);
    }

    void Movement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        cc.Move(
            moveSpeed *
            Time.deltaTime *
           (horizontal * transform.right +
            vertical * transform.forward)
         );
    }

    void LookAround ()
    {
        float MouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float MouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= MouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.Rotate(Vector3.up * MouseX);
        head.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    void Jump()
    {
        if (Input.GetButton("Jump") && isGrounded)
        {
            //physics equation that determines the amount of velocity to add based on gravity and desired height
            //v = Sqrt(-2*height*gravity)
            currentVerticalVelocity.y = Mathf.Sqrt(-2 * jumpHeight * fallSpeed);
        }
    }
}