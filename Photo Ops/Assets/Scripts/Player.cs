using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    CharacterController cc;
    private float standingHeight;

    [SerializeField] LayerMask IgnoreRaycastLayer;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] GameObject interactPopupText;

    [Header("References")]
    [SerializeField] Transform groundCheck;
    [SerializeField] GameObject objectivePrefab;
    [SerializeField] Camera cam;
    [SerializeField] GameObject head;
    [SerializeField] Transform Body;
    [SerializeField] Transform headPosition;

    [Header("Values")]
    public float standMoveSpeed;
    public float jumpHeight;
    public float mouseSensitivity;
    [SerializeField] float interactDistance;
    [SerializeField] float crouchHeight;
    [SerializeField] float crouchMoveSpeed;
    [SerializeField] float crouchScopeMoveSpeed;
    [SerializeField] float scopeMoveSpeed;
    [SerializeField] DSLR equippedCamera;

    bool isCrouched = false;
    bool isScoped = false;

    //internal values
    bool hasObjective; 
    float fallSpeed = -10;
    bool isGrounded;
    Vector3 currentVerticalVelocity;
    float xRotation = 0f;
    float jumpDistanceLenience = 0.4f; //the distance from the ground before you can execute another jump

    // Start is called before the first frame update
    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        standingHeight = Body.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
        Jump();
        LookAround();
        Interact();
        Gravity();
        Crouch();
        DropObjective();
    }

    void DropObjective ()
    {
        if (Input.GetButtonDown("Drop") && hasObjective)
        {
            hasObjective = false;
            Vector3 instantiatePos = new Vector3(transform.localPosition.x - 2, transform.localPosition.y, transform.localPosition.z);

            Instantiate(objectivePrefab, instantiatePos, Quaternion.Euler(90, 0, 0));
        }
    }

    void Crouch ()
    {
        if (Input.GetButton("Crouch")) //crouch
        {
            Body.localScale = new Vector3(Body.localScale.x, crouchHeight, Body.localScale.z);
            Body.gameObject.GetComponent<CapsuleCollider>().height = 1;
            cc.height = 1;
            head.transform.position = headPosition.position;
            isCrouched = true;
        }
        else //stand
        {
            Body.localScale = new Vector3(Body.localScale.x, standingHeight, Body.localScale.z);
            Body.gameObject.GetComponent<CapsuleCollider>().height = 2;
            cc.height = 2;
            head.transform.position = headPosition.position;
            isCrouched = false;
        }
    }

    void Gravity()
    {
        //checks if a sphere surrounding groundCheck with a radius of jumpDistanceLenience collides with any objects in the groundLayer
        isGrounded = Physics.CheckSphere(groundCheck.position, jumpDistanceLenience, groundLayer);

        if (isGrounded && currentVerticalVelocity.y < 0)
            currentVerticalVelocity.y = -2f; //-2 fixes issue where player wasn't fully touching the ground 
        
        currentVerticalVelocity.y += fallSpeed * Time.deltaTime;
        cc.Move(currentVerticalVelocity * Time.deltaTime);
    }

    void Movement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        float moveSpeed;
        isScoped = equippedCamera.isLookingThroughVF;

        if (isCrouched && isScoped)
            moveSpeed = crouchScopeMoveSpeed;
        else if (isCrouched)
            moveSpeed = crouchMoveSpeed;
        else if (isScoped)
            moveSpeed = scopeMoveSpeed;
        else
            moveSpeed = standMoveSpeed;

        cc.Move(moveSpeed * Time.deltaTime * (horizontal * transform.right + vertical * transform.forward));
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

    //open doors, pick up objective, smash cameras, etc.
    void Interact ()
    {
        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, interactDistance, IgnoreRaycastLayer))
        {
            //todo: display interactable on GUI
            if (hit.collider.gameObject == objectivePrefab)
            {
                Debug.Log("check");
                if (Input.GetButtonDown("Interact"))
                {
                    hasObjective = hit.collider.gameObject.GetComponent<Objective>().PickUp();
                }
                interactPopupText.SetActive(true);
            }

        }
        else
            interactPopupText.SetActive(false);

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