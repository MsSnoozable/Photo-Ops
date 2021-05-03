using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class CameraControllerScript : MonoBehaviour
{
    //references
    [Header("Cameras")]
    public Camera DSLR_cam;
    public Camera Player_cam;

    [Header("Picture Taking")]
    public RenderTexture Photo_Result;
    public VolumeProfile postProcess;

    [Header("Animation")]
    public Animator cameraStateMachine;
    public AnimationClip VF_Toggle_Clip;
    
    //variables
    bool isTakingPicture = false;

    [Header("Testing")]
    [SerializeField] bool isSavingShots;

    Rect redEnemyRect;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip shutterSFX;

    Renderer Enemy_Renderer;

    [Header("Other")]
    public float flashDuration;
    public Light flash;
    bool isEnemyVisible = false;

    public GameObject damageZone;
    public GameObject Enemy;
    public DamageEnemies damageEnemiesScript;
    public GameObject customPass;
    bool isVFAxisInUse = false;

    bool isLookingThroughVF = false;

    float[] zoomNotchValues = { 18f, 20f, 24f, 35f, 45f, 50f, 55f };
    float[] focusRingValues = { 1f, 2f, 3f, 5f, 8f, 11f, Mathf.Infinity }; 
    float[] apertureValues = { 3.5f, 4f, 4.5f, 5f, 5.6f, 6.3f, 7.1f };
    
    //saves where the current notch is on
    int apertureNotch;
    int focusRingNotch;
    int zoomNotch;

    static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    // Start is called before the first frame update
    void Start()
    {
        SetInitialCameraNotches();

        Enemy_Renderer = Enemy.GetComponent<Renderer>();
        damageZone.GetComponent<MeshFilter>().mesh = new Mesh();
    }

    void SetInitialCameraNotches ()
    {
        apertureNotch = 2;
        focusRingNotch = 4;
        zoomNotch = 6;

        ApertureChange();
        ZoomChange();
        FocusRingChange();
    }

    void Update()
    {
        ScrollControls();
        ViewfinderToggle();
        ChangeDamageZone();
    }

    void LateUpdate()
    {
        TakePicture();
    }

    // Calculates near and far plane then modifies Damage zone accordingly
    void ChangeDamageZone ()
    {
        float superLargeValue = 100000;

        //u is in meters, N is in mm, f is in mm, coc is in mm
        float N = DSLR_cam.GetComponent<HDAdditionalCameraData>().physicalParameters.aperture;
        float f = DSLR_cam.focalLength;
        float CoC = 0.03f; // Circle of Confusion
        float u = 0; //set to zero to stop compiler from complaining

        //convert u to mm by multiplying by 1000
        //if it is infinity set to have the max
        if (postProcess.TryGet<DepthOfField>(out var DoF)) 
            u = DoF.focusDistance.value == Mathf.Infinity ? float.MaxValue / 2 : DoF.focusDistance.value * 1000;

        //hyperfocal distance (exact definition not the approximate)
        float H = (Mathf.Pow(f, 2) / (N * CoC)) + f;

        float focusNearPlane = u * H / (H + u - f) / 1000;
        float focusFarPlane =  u * H / (H - u - f) / 1000;

        //set focus far plane to as a far as possible
        if (focusFarPlane <= 0)
            focusFarPlane = superLargeValue;

        //center of DOF is called focal plane
        float centerOfDamageZone = (focusNearPlane + focusFarPlane) / 2;
        float sizeOfDamageZone = focusFarPlane - focusNearPlane;

        Debug.Log(DSLR_cam.aspect);
        float fieldOfViewHorizontal = Camera.VerticalToHorizontalFieldOfView(DSLR_cam.fieldOfView, DSLR_cam.aspect);

        float x_offset_near = focusNearPlane * (float) Math.Tan(fieldOfViewHorizontal / 2);
        float x_offset_far = focusFarPlane * (float) Math.Tan(fieldOfViewHorizontal / 2);

        float y_offset_near = focusNearPlane * (float) Math.Tan(DSLR_cam.fieldOfView / 2);
        float y_offset_far = focusFarPlane * (float) Math.Tan(DSLR_cam.fieldOfView / 2);

        //vertices of trapezoidal prism
        /*notated as min / max of each type
          *x coordinate: left / right = l or r
          *y coordinate: bottom / top = b or t
          *z coordinate: near / far = n or f
          */
        Vector3[] vertices = new Vector3[8] {
            new Vector3(-x_offset_near, -y_offset_near, focusNearPlane), //lbn
            new Vector3(-x_offset_far,  -y_offset_far,  focusFarPlane),  //lbf
            new Vector3(-x_offset_near, +y_offset_near, focusNearPlane), //ltn
            new Vector3(-x_offset_far,  +y_offset_far,  focusFarPlane),  //ltf
            new Vector3(+x_offset_near, -y_offset_near, focusNearPlane), //rbn
            new Vector3(+x_offset_far,  -y_offset_far,  focusFarPlane),  //rbf     
            new Vector3(+x_offset_near, +y_offset_near, focusNearPlane), //rtn    
            new Vector3(+x_offset_far,  +y_offset_far,  focusFarPlane)   //rtf      
        };                                                                   

        int[] triangles = new int[] {
            0, 1, 2,
            1, 3, 2,
            5, 7, 1,
            7, 3, 1,
            4, 6, 5,
            6, 7, 5,
            0, 2, 4,
            2, 6, 4,
            3, 7, 6,
            6, 2, 3,
            5, 1, 0,
            5, 0, 4
        };

        //todo: fix the math here

        Mesh mesh = damageZone.GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
    }

    void ScrollControls() {
        //todo: change to use get axis instead
        if (Input.mouseScrollDelta.y != 0) { 
            if (Input.GetKey(KeyCode.Q))
                ApertureChange();
            else if (Input.GetKey(KeyCode.LeftShift))
                FocusRingChange();
            else
                ZoomChange();
        }
    }

    public void ZoomChange()
    {
        zoomNotch += (int)Input.mouseScrollDelta.y;
        zoomNotch = Mathf.Clamp(zoomNotch, 0, 5);
        DSLR_cam.focalLength = zoomNotchValues[zoomNotch];
    }

    public void FocusRingChange()
    {
        focusRingNotch += (int) Input.mouseScrollDelta.y;
        focusRingNotch = Mathf.Clamp(focusRingNotch, 0, 5);
        if (postProcess.TryGet<DepthOfField>(out var DoF))
            DoF.focusDistance.value = focusRingValues[focusRingNotch];
    }

    public void ApertureChange()
    {
        apertureNotch += (int)Input.mouseScrollDelta.y;
        apertureNotch = Mathf.Clamp(apertureNotch, 0, 5);
        DSLR_cam.GetComponent<HDAdditionalCameraData>().physicalParameters.aperture = apertureValues [apertureNotch]; 
    }

    void ViewfinderToggle ()
    {
        //Right Click
        if (Input.GetAxisRaw("VF Toggle") != 0)
        {
            if (!isVFAxisInUse)
            {
                isVFAxisInUse = true;
                cameraStateMachine.SetBool("isRightClicking", !cameraStateMachine.GetBool("isRightClicking"));

                //swap active camera
                if (isLookingThroughVF)
                {
                    //active camera switches to character
                    DSLR_cam.targetTexture = Photo_Result;
                    Player_cam.targetDisplay = 0;
                    DSLR_cam.targetDisplay = 1;

                    //reenables DamageZone
                    damageZone.GetComponent<MeshRenderer>().enabled = true;

                    isLookingThroughVF = false;
                }
                else
                {
                    //active camera switches to DSLR
                    StartCoroutine(SwitchToDSLRView());
                }
            }
        }
        else
            isVFAxisInUse = false;
    }

    IEnumerator SwitchToDSLRView ()
    {
        yield return new WaitForSeconds(VF_Toggle_Clip.length);

        DSLR_cam.targetTexture = null;
        DSLR_cam.targetDisplay = 0;
        Player_cam.targetDisplay = 1;

        isLookingThroughVF = true;

        //removes damage zone from view when looking through VF
        damageZone.GetComponent<MeshRenderer>().enabled = false;
    }

    public void TakePicture()
    {
        //todo: change to shutter button Axis input manager

        //prevents user from taking pictures while changing views
        if (Input.GetMouseButtonDown(0))
        {
            //if transitioning to or in a transition state to move, you can't take a picture
            if (cameraStateMachine.GetCurrentAnimatorStateInfo(0).IsName("DSLR_ScopeMove") ||
                cameraStateMachine.GetCurrentAnimatorStateInfo(0).IsName("DSLR_ScopeMove_Reverse") ||
                cameraStateMachine.GetAnimatorTransitionInfo(0).IsName("DSLR_UnscopedIdle -> DSLR_ScopeMove") ||
                cameraStateMachine.GetAnimatorTransitionInfo(0).IsName("DSLR_ScopedIdle -> DSLR_ScopeMove_Reverse")
               )
                isTakingPicture = false;
            else
                isTakingPicture = true;
        }

        if (isTakingPicture)
        {
            audioSource.PlayOneShot(shutterSFX);
            //todo: change this back
            customPass.SetActive(true);

            if (isSavingShots)
            {
                int resolutionWidth = Screen.width;
                int resolutionHeight = Screen.height;

                RenderTexture rt = new RenderTexture(resolutionWidth, resolutionHeight, 24);
                DSLR_cam.targetTexture = rt;
                Texture2D screenShot = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
                DSLR_cam.Render();
                RenderTexture.active = rt;

                screenShot.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);

                if (isLookingThroughVF)
                    DSLR_cam.targetTexture = null;
                else
                    DSLR_cam.targetTexture = Photo_Result;
                RenderTexture.active = null; // JC: added to avoid errors

                Destroy(rt);
                byte[] bytes = screenShot.EncodeToPNG();
                string filename = ScreenShotName(resolutionWidth, resolutionHeight);
                System.IO.File.WriteAllBytes(filename, bytes);

                //somewhere here attach screenshot texture to photo result
            }
            //todo: ScreenSpaceOfEnemyCalculation();
            StartCoroutine(ToggleFlash());

            isTakingPicture = false;
        }
    }

    IEnumerator ToggleFlash()
    {
        flash.enabled = true;
        yield return new WaitForSeconds(flashDuration / 60);
        flash.enabled = false;
        customPass.SetActive(false);

        //todo: if an enemy is visisble by DSLR_cam then run screen space calc
    }
}