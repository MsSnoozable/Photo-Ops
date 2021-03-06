using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class DSLR : MonoBehaviour
{
    #region Public Members
        public Camera DSLR_cam;
        public Camera Player_cam;
        public RenderTexture Photo_Result;
        public VolumeProfile postProcess;
        public Animator cameraStateMachine;
        public AnimationClip VF_Toggle_Clip;
        
        [HideInInspector] public bool isLookingThroughVF = false;
        [SerializeField] bool isSavingShots;

        public AudioSource audioSource;
        public AudioClip shutterSFX;

        [Header("Other")]
        public float flashDuration;
        public Light flash;

        public GameObject damageZone;
        public GameObject Enemy;
        public DamageEnemies damageEnemiesScript;
        public GameObject customPass;
        public UpdateUI updateUI;

        GameObject[] killedPlayers;

        [Header("Notch Values")]
        [SerializeField] float[] zoomNotchValues;
        [SerializeField] float[] focusRingValues;
        [SerializeField] float[] apertureValues;

        [SerializeField] int startingZoomNotchValue;
        [SerializeField] int startingFocusRingValue;
        [SerializeField] int startingApertureValue;
    #endregion
    #region Private Members
        Renderer Enemy_Renderer;

        bool isTakingPicture = false;
        bool isEnemyVisible = false;

        HDPhysicalCamera additionalCamData;

        //saves where the current notch is on
        int apertureNotch;
        int focusRingNotch;
        int zoomNotch;

        int currentAmmo;
        int currentExcessAmmo;
        const int maxExcessAmmo = 20;
        const int maxAmmo = 8;
    #endregion

    void Reload() {
        if (Input.GetButtonDown("Reload"))
        { 
            if (currentExcessAmmo > 0)
            {
                //todo: tests
                int originalExcessAmmo = currentExcessAmmo;
                currentExcessAmmo -= Mathf.Clamp((maxAmmo - currentAmmo), 0, currentExcessAmmo);
                currentAmmo += originalExcessAmmo - currentExcessAmmo;

                updateUI.ChangeAmmo(currentAmmo, currentExcessAmmo);
                //todo: reload animation
            }
            else
            {
                print("out of ammo");
            }
        }
    }

    #region Functions
    #region Monobehaviour

    private void Awake()
    {
        Enemy_Renderer = Enemy.GetComponent<Renderer>();
        additionalCamData = DSLR_cam.GetComponent<HDAdditionalCameraData>().physicalParameters;
        //initialize mesh
        damageZone.GetComponent<MeshFilter>().mesh = new Mesh();
    }
    void Start()
    {
        //ResizeCameraOnScreen();
        SetInitialCameraNotches();
        currentExcessAmmo = 3;
        currentAmmo = maxAmmo;
        updateUI.ChangeAmmo(currentAmmo, currentExcessAmmo);
    }
    void Update()
    {
        ScrollControls();
        ViewfinderToggle();
        ChangeDamageZone();
        Reload();
        TakePicture();
    }
    #endregion
        #region Scroll Wheel
            // Calculates near and far plane then modifies Damage zone accordingly
            void ChangeDamageZone()
            {
                float superLargeValue = 100000;

                //u is in meters, N is in mm, f is in mm, coc is in mm
                float N = additionalCamData.aperture;
                float f = DSLR_cam.focalLength;
                float CoC = 0.03f; // Circle of Confusion
                float u = 0; //set to zero to stop compiler from complaining

                //convert u to mm by multiplying by 1000
                //if it is infinity set to have the max
                if (postProcess.TryGet<DepthOfField>(out var DoF))
                    u = DoF.focusDistance.value == Mathf.Infinity ? superLargeValue : DoF.focusDistance.value * 1000;

                //hyperfocal distance (exact definition not the approximate)
                float H = (Mathf.Pow(f, 2) / (N * CoC)) + f;

                float focusNearPlane = u * H / (H + u - f) / 1000;
                float focusFarPlane = u * H / (H - u - f) / 1000;

                //todo: this might be depricated double check
                if (focusFarPlane <= 0)
                    focusFarPlane = superLargeValue;

                Vector3[] nearCorners = new Vector3[4];
                DSLR_cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), focusNearPlane, Camera.MonoOrStereoscopicEye.Mono, nearCorners);

                Vector3[] farCorners = new Vector3[4];
                DSLR_cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), focusFarPlane, Camera.MonoOrStereoscopicEye.Mono, farCorners);

                Vector3[] frustumCorners = new Vector3[8] {
                    nearCorners[0], nearCorners[1], nearCorners[2], nearCorners[3],
                    farCorners[0], farCorners[1], farCorners[2], farCorners[3]
                };
                int[] triangles = new int[] {
                    0, 1, 2, /**/ 3, 0, 2, //near
                    4, 6, 5, /**/ 4, 7, 6, //far
                    4, 0, 3, /**/ 7, 4, 3, //bottom
                    1, 5, 2, /**/ 5, 6, 2, //top
                    5, 1, 0, /**/ 5, 0, 4, //left
                    2, 7, 3, /**/ 7, 2, 6  //right
                };

                Mesh mesh = damageZone.GetComponent<MeshFilter>().mesh;
                mesh.Clear();
                mesh.vertices = frustumCorners;
                mesh.triangles = triangles;
                damageZone.GetComponent<MeshCollider>().sharedMesh = mesh;
            }
            void SetInitialCameraNotches()
            {
                apertureNotch = startingApertureValue;
                focusRingNotch = startingFocusRingValue;
                zoomNotch = startingZoomNotchValue;

                ApertureChange();
                ZoomChange();
                FocusRingChange();
            }
            void ScrollControls()
            {
                if (Input.mouseScrollDelta.y == 0) return; //gaurd clause
                
                if (Input.GetButton("Aperture Toggle"))
                    ApertureChange();
                else if (Input.GetButton("Focus Toggle"))
                    FocusRingChange();
                else
                    ZoomChange();
                
            }
            public void ZoomChange()
            {
                zoomNotch += (int)Input.mouseScrollDelta.y;
                zoomNotch = Mathf.Clamp(zoomNotch, 0, zoomNotchValues.Length - 1);
                DSLR_cam.focalLength = zoomNotchValues[zoomNotch];

                updateUI.ChangeZoom(zoomNotchValues[zoomNotch]);
            }
            public void FocusRingChange()
            {
                focusRingNotch += (int)Input.mouseScrollDelta.y;
                focusRingNotch = Mathf.Clamp(focusRingNotch, 0, focusRingValues.Length - 1);
                if (postProcess.TryGet<DepthOfField>(out var DoF))
                    DoF.focusDistance.value = focusRingValues[focusRingNotch];

                updateUI.ChangeFocus(focusRingValues[focusRingNotch]);
            }
            public void ApertureChange()
            {
                apertureNotch += (int)Input.mouseScrollDelta.y;
                apertureNotch = Mathf.Clamp(apertureNotch, 0, apertureValues.Length - 1);
                DSLR_cam.GetComponent<HDAdditionalCameraData>().physicalParameters.aperture = apertureValues[apertureNotch];

                updateUI.ChangeAperture(apertureValues[apertureNotch]);
            }
        #endregion
        #region Scoping
            void ViewfinderToggle()
            {
                //Right Click
                if (Input.GetButtonDown("VF Toggle"))
                {
                    cameraStateMachine.SetBool("isRightClicking", !cameraStateMachine.GetBool("isRightClicking"));

                    Debug.Log(DSLR_cam.aspect);

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
            IEnumerator SwitchToDSLRView()
            {
                yield return new WaitForSeconds(VF_Toggle_Clip.length);

                DSLR_cam.targetTexture = null;
                DSLR_cam.targetDisplay = 0;
                Player_cam.targetDisplay = 1;

                isLookingThroughVF = true;

                //removes damage zone from view when looking through VF
                damageZone.GetComponent<MeshRenderer>().enabled = false;
            }
        #endregion
        #region Picture Taking
        private void shutterButtonRestrictions ()
        {
            //Restricts when shutter button is usable, can't be used during animation transitions or when there is 0 ammo
            if (Input.GetButtonDown("Shutter Button"))
            {
                if (currentAmmo == 0)
                {
                    Reload();
                    return;
                }

                //if transitioning to or in a transition state to move, you can't take a picture
                if (cameraStateMachine.GetCurrentAnimatorStateInfo(0).IsName("DSLR_ScopeMove") ||
                        cameraStateMachine.GetCurrentAnimatorStateInfo(0).IsName("DSLR_ScopeMove_Reverse") ||
                        cameraStateMachine.GetAnimatorTransitionInfo(0).IsName("DSLR_UnscopedIdle -> DSLR_ScopeMove") ||
                        cameraStateMachine.GetAnimatorTransitionInfo(0).IsName("DSLR_ScopedIdle -> DSLR_ScopeMove_Reverse")
                       )
                {
                    isTakingPicture = false;
                return;

            }
            else
                {
                    isTakingPicture = true;
                return;

            }
        }
        }

        public void TakePicture()
        {
            shutterButtonRestrictions();

            if (isTakingPicture)
            {
                audioSource.PlayOneShot(shutterSFX);
                customPass.SetActive(true);

                //todo: check if enemy is collided with damage zone. Make script in damage zone to add an array of enemies

                int resolutionWidth = DSLR_cam.scaledPixelWidth;
                int resolutionHeight = DSLR_cam.scaledPixelHeight;

                RenderTexture rt = new RenderTexture(resolutionWidth, resolutionHeight, 24);
                DSLR_cam.targetTexture = rt;
                Texture2D screenShot = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
                DSLR_cam.Render();
                RenderTexture.active = rt;

                screenShot.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);

                //todo: change this part for multiple enemies in a shot
                var (damage, reason) = TextureReading.CheckEnemyPercentOnScreen(screenShot, Color.white, Color.black);
                damageEnemiesScript.TakeDamage(damage);
                Debug.Log(reason);

                DSLR_cam.targetTexture = isLookingThroughVF ? null : Photo_Result;

                RenderTexture.active = null;

                if (isSavingShots)
                {
                    Destroy(rt);
                    byte[] bytes = screenShot.EncodeToPNG();
                    string filename = ScreenShotName(resolutionWidth, resolutionHeight);
                    System.IO.File.WriteAllBytes(filename, bytes);

                    //somewhere here attach screenshot texture to photo result
                }

                //ammo decrease
                updateUI.ChangeAmmo(--currentAmmo, currentExcessAmmo);

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
        }
        #endregion
    #endregion

    static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }
       
    //todo: doesn't work
    //changes dslr to have a 3:2 ratio
    void ResizeCameraOnScreen()
    {
        Debug.Log(DSLR_cam.aspect);
        Rect camRect = DSLR_cam.rect;
        float shrinkAmount = 0.84375f; //conversion from 16:9 to 3:2 across both left and right sides
        camRect.xMax = shrinkAmount;
        //camRect.xMin = shrinkAmount;

        DSLR_cam.rect = camRect;

        DSLR_cam.ResetAspect();
        Debug.Log(DSLR_cam.aspect);

    }

    //when the player dies drop the camera that has all the players you killed
    void Dies() {

    }
}