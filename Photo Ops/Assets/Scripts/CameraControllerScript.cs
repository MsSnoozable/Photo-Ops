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
    
    int apertureNotch;
    int focusRingNotch;
    int zoomNotch;

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

    float[] zoomNotchValues = { 5f, 10f, 15f, 20f, 30f, 100f };
    float[] focusRingValues = { 0.5f, 1f, 5f, 15f, 30f, float.MaxValue };
    float[] apertureValues = { 3.5f, 4f, 5.6f, 8f, 16f, 22f };

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
            focusFarPlane = float.MaxValue;

        //center of DOF is called focal plane
        float centerOfDamageZone = (focusNearPlane + focusFarPlane) / 2;
        float sizeOfDamageZone = focusFarPlane - focusNearPlane;

        //Change Damage zones based on calculation
        //todo: fix this?
        //DamageZones.transform.localPosition = new Vector3(DamageZones.transform.localPosition.x, DamageZones.transform.localPosition.y, centerOfDamageZone);
        //DamageZones.transform.localScale = new Vector3(DamageZones.transform.localScale.x, DamageZones.transform.localScale.y, sizeOfDamageZone);

        //vertices of trapezoidal prism
        /*notated as min / max of each type
          *x coordinate: left / right = l or r
          *y coordinate: bottom / top = b or t
          *z coordinate: near / far = n or f
          */

        float aspectRatio = Screen.height / Screen.width;
        float fieldOfViewHorizontal = Camera.VerticalToHorizontalFieldOfView(DSLR_cam.fieldOfView, aspectRatio);
        
        float x_offset_near = focusNearPlane * (float) Math.Tan(fieldOfViewHorizontal / 2);
        float x_offset_far = focusFarPlane * (float) Math.Tan(fieldOfViewHorizontal / 2);

        float y_offset_near = focusNearPlane * (float) Math.Tan(DSLR_cam.fieldOfView / 2);
        float y_offset_far = focusFarPlane * (float) Math.Tan(DSLR_cam.fieldOfView / 2);

        Vector3 lbn = new Vector3(- x_offset_near, - y_offset_near, focusNearPlane);
        Vector3 lbf = new Vector3(- x_offset_far, - y_offset_far, focusFarPlane);
        Vector3 ltn = new Vector3(- x_offset_near, + y_offset_near, focusNearPlane);
        Vector3 ltf = new Vector3(- x_offset_far, + y_offset_far, focusFarPlane);

        Vector3 rbn = new Vector3(+ x_offset_near, - y_offset_near,focusNearPlane);
        Vector3 rbf = new Vector3(+ x_offset_far, - y_offset_far, focusFarPlane);
        Vector3 rtn = new Vector3(+ x_offset_near, + y_offset_near, focusNearPlane);
        Vector3 rtf = new Vector3(+ x_offset_far, + y_offset_far, focusFarPlane);

        Vector3[] vertices = new Vector3[8] {
            lbn, lbf, ltn, ltf, rbn, rbf, rtn, rtf
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
        //damageZone.GetComponent<MeshFilter>().mesh.vertices = vertices;
        //damageZone.GetComponent<MeshFilter>().mesh.triangles = triangles;
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
        //todo: confirm that this works properly
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
                if (DSLR_cam.targetTexture)
                {
                    //active camera switches to DSLR
                    StartCoroutine(SwitchToDSLRView());
                }
                else
                {
                    //active camera switches to character
                    DSLR_cam.targetTexture = Photo_Result;
                    Player_cam.targetDisplay = 0;
                    DSLR_cam.targetDisplay = 1;

                    damageZone.GetComponent<MeshRenderer>().enabled = true;
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

        damageZone.GetComponent<MeshRenderer>().enabled = false;
    }

    public void TakePicture()
    {
        //todo: change to shutter button
        isTakingPicture = Input.GetMouseButtonDown(0);
        if (isTakingPicture)
        {
            audioSource.PlayOneShot(shutterSFX);
            customPass.SetActive(true);

            if (isSavingShots)
            {
                int resolutionWidth = 256;
                int resolutionHeight = 256;

                RenderTexture rt = new RenderTexture(resolutionWidth, resolutionHeight, 24);
                DSLR_cam.targetTexture = rt;
                Texture2D screenShot = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
                DSLR_cam.Render();
                RenderTexture.active = rt;

                screenShot.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);

                //needs to be conditional based on viewfinder state
                DSLR_cam.targetTexture = null;
                RenderTexture.active = null; // JC: added to avoid errors

                Destroy(rt);
                byte[] bytes = screenShot.EncodeToPNG();
                string filename = ScreenShotName(resolutionWidth, resolutionHeight);
                System.IO.File.WriteAllBytes(filename, bytes);

                //somewhere here attach screenshot texture to photo result
            }
            ScreenSpaceOfEnemyCalculation();
            StartCoroutine(ToggleFlash());

            isTakingPicture = false;
        }
    }

    //should return float percent of the enemy bounds passed into it
    void ScreenSpaceOfEnemyCalculation(/*GameObject Enemey*/)
    {
        //todo: make it accept an array of enemies

        /* Gets bounds of enemy
         * notated as min/max of each type
         * x coordinate: left/right = l or r
         * y coordinate: bottom/top = b or t
         * z coordinate: near/far = n or f
         */

        float left = Enemy_Renderer.bounds.min.x;
        float bottom = Enemy_Renderer.bounds.min.y;
        float near = Enemy_Renderer.bounds.min.z;

        float right = Enemy_Renderer.bounds.max.x;
        float top = Enemy_Renderer.bounds.max.y;
        float far = Enemy_Renderer.bounds.max.z;

        Vector3 lbn = new Vector3(left, bottom, near);
        Vector3 lbf = new Vector3(left, bottom, far);
        Vector3 ltn = new Vector3(left, top, near);
        Vector3 ltf = new Vector3(left, top, far);

        Vector3 rbn = new Vector3(right, bottom, near);
        Vector3 rbf = new Vector3(right, bottom, far);
        Vector3 rtn = new Vector3(right, top, near);
        Vector3 rtf = new Vector3(right, top, far);

        lbn = DSLR_cam.WorldToScreenPoint(lbn);
        lbf = DSLR_cam.WorldToScreenPoint(lbf);
        ltn = DSLR_cam.WorldToScreenPoint(ltn);
        ltf = DSLR_cam.WorldToScreenPoint(ltf);

        rbn = DSLR_cam.WorldToScreenPoint(rbn);
        rbf = DSLR_cam.WorldToScreenPoint(rbf);
        rtn = DSLR_cam.WorldToScreenPoint(rtn);
        rtf = DSLR_cam.WorldToScreenPoint(rtf);

        //find highest/lowest X/Y for each vector
        float[] X_values = new float[8] {
            lbn.x, lbf.x, ltn.x, ltf.x, rbn.x, rbf.x, rtn.x, rtf.x
        };

        float[] Y_values = new float[8] {
            lbn.y, lbf.y, ltn.y, ltf.y, rbn.y, rbf.y, rtn.y, rtf.y
        };

        float smallest_X = Mathf.Min(X_values);
        float smallest_Y = Mathf.Min(Y_values);

        float biggest_X = Mathf.Max(X_values);
        float biggest_Y = Mathf.Max(Y_values);

        float rectX = smallest_X;
        float rectY = Screen.height - biggest_Y;
        float rectW = biggest_X - smallest_X;
        float rectH = biggest_Y - smallest_Y;

        //check if enemy is visible in viewport
        isEnemyVisible = (
            (rectX <= -rectW ||         //off sides left
             rectX >= Screen.width) &&  //off sides right
            (rectY <= rectH ||          //off sides up
             rectY >= Screen.height)    //off sides bottom
            ) ? false : true;

        //use clamp to make sure the box is not larger than the screen

        //rectW = Mathf.Clamp(rectW, 0, Screen.width);
        //rectH = Mathf.Clamp(rectH, 0, Screen.height);
        //
        //rectX = Mathf.Clamp(rectX, 0, rectW);
        //rectY = Mathf.Clamp(rectY, 0, rectH);

        // use screen.height - biggest because the GUI layer and the Screen layer have different starting points for coordinates

        redEnemyRect = new Rect(rectX, rectY, rectW, rectH);

        //Debug.Log("rect: " + redEnemyRect);

        if (isEnemyVisible)
        {
            //bounds area divided by Screen Area to get percentage of screen taken up by the shot, should be between 0 and 1
            float relativePercent = (redEnemyRect.width * redEnemyRect.height) / (Screen.width * Screen.height);
            damageEnemiesScript.TakeDamage(relativePercent * 100);
        }

        //Debug.Log("Percent On Screen: " + relativePercent);
    }

    private void OnGUI()
    {
        if (isEnemyVisible /* && isTakingPicture */)
        {
            GUI.Box(redEnemyRect, "Enemy");
        }
    }

    IEnumerator ToggleFlash()
    {
        flash.enabled = true;
        yield return new WaitForSeconds(flashDuration);
        flash.enabled = false;
        customPass.SetActive(false);

        //if an enemy is visisble by DSLR_cam then run screen space calc
    }
}