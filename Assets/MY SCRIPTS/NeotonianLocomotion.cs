/*
2016-08-04 Thu
had to fucking change vrtk  code, due to vrtk updates 


2016-07-17 Sun 
gazevectoring countdown indicator needed 
        show dot or circle for center of gaze
        need to control it with code

        animated UI elemet
        vs LineRenderer ~ defining and animating all the points with be PIA

        vertical lines like window/corridor/walls?
            easier for showing view angle window threshold?
            cannot cover up the actual view
            distance to issue
                don't  want it to be behind objects 
                perspective can be faked, no need to be real 
                just put the plane at 1m in front?
            needs animation to show timing 
            vertical lines moving toward center?    

            arrows?
        circle that gets smaller toward center?
    
    how to make it not be in your face all the time 
        make Y view anlge narrow 
        Hmd_Transform.eulerAngles.x - zero is horozontal +90 is down 
            45 degs up, and 45 down?

        want to be able to just look in the direction, not have to look at ground 
        want to be able to look off window easily enough too         

        only showss when doing Locomotion 
---------------------------------------------------------------
2016-07-15 Fri 21:27
colliders 
    convex not needed 
    need decceleration on col
---------------------------------------------------------------
trying to make    void RunSpeedControl()
   StartCoroutine(CalcInputSpeed(RunSpeed, touchAxis.y)); NG WTF  
   fuckikng startcoutroutine won't  pass agrument!

---------------------------------------------------------------
2016-07-15 Fri 14:36
there is a strange stop and go, when changing acceleration  direciton - fixed 
    currentGazeVsInertiaOk == false is happening on direction change ?
    so it starts decceleration 

//have to account for multiple direction changes, not just from 1 to 2 
    use InertiaVector_Current ?

private void GazeVsInertiaCheck()
//cannot do InertiaVector_1 and 2 at same time, unless there is a separate condition to check 
       //&& InertiaVector_1.normalized != Vector3.zero

---------------------------------------------------------------
2016-07-13 Wed 23:51
won't  fucking move ata all - can't  stop it from colliding with floor  
    WTF is it   OnCollisionStay: LEVEL-01  and not the floor tiles?
    there was fucking invisible vertices left around from blender !!
    still NG 

---------------------------------------------------------------
   StartCoroutine(CalcInputSpeed(RunSpeed, touchAxis.y)); NG WTF  

   running speed onTouch makes bad sudden motion 
      but tyring to contorl speed on Click, conflickets with main click too much 
---------------------------------------------------------------
NO unexpected/unwanted motions allowed!!!
moving again when I want to stop is v.bad 

GazeVectoring is needed, due to lack of precision 
    return to 2 step chec  
     finally apears to work1

-------------------------------------------------------------
Locomotion not robust yet 
first acceleration NG?
trigger threshold 
    boost value 
---------------------------------------------------------------
acceleration curves?
    I do want gradual acceleration  
    slerp - use for gazevectoring?
        tried but doesnt seem to make difference much 
    animation?

---------------------------------------------------------------
2016-07-13 Wed 06:47
vrtk2.0 has changed to mauch shit, so have revert for now 
    to change my code too issue 
     should I try to use newtonvr instead?

---------------------------------------------------------------
2016-07-11 Mon 01:40
trying to make    void RunSpeedControl()
    fuckikng startcoutroutine won't  pass agrument!
    
2016-07-07 Thu 19:14
have to separate functions for 1st and 2nd acceleration ?
have to separate these? 
GazeVectoringPermitted 
RunSpeedPermitted
---------------------------------------------------------------
using gravity fucking creates too much drag !!
    stops immediately
    have to turn it on and off!?

2016-07-02 Sat 00:54
turned up the physics time 

Hmd_Transform.localPosition;
    how acureate is this ?

run+jump
    re organized buton press code !! so can do it 
//---------------------------------------------------------------
NOTES

//---------------------------------------------------------------
Built by Robert Stephenson and Company in 1824.
Locomotion No. 1  (originally named Active) is the first steam locomotive to carry passengers on a public rail line. 
Since this is the first implementation of artificial Locomotion, I wanted to pay homage to it. 

**************************************************************/
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using UnityStandardAssets.ImageEffects;//
    using System.Collections;
    using System.Collections.Generic;
    using Valve.VR; //
    using VRTK;

    public class NeotonianLocomotion : MonoBehaviour
    {
        public SteamVR_TrackedObject Wand_R;
//        public SteamVR_TrackedObject Wand_L;
//        private SteamVR_Controller.Device Wand1; //added


        [SerializeField]
        private bool leftController = true;
        public bool LeftController
        {
            get { return leftController; }
            set
            {
                leftController = value;
                SetControllerListeners(controllerManager.left);
            }
        }

        [SerializeField]
        private bool rightController = true;
        public bool RightController
        {
            get { return rightController; }
            set
            {
                rightController = value;
                SetControllerListeners(controllerManager.right);
            }
        }

        private SteamVR_ControllerManager controllerManager;

        private bool leftSubscribed;
        private bool rightSubscribed;

        private ControllerInteractionEventHandler touchpadAxisChanged;
        private ControllerInteractionEventHandler touchpadUntouched;

        private VRTK_PlayerPresence_Mod playerPresence;
                                                 
        //---------------------------------------------------------------
        public float maxRunSpeed = 10.0f;
        public float maxWalkSpeed = 4.0f;
        public float maxSlowSpeed = 1.0f;

        private Vector2 touchAxis;
        //        private float RunSpeed = 0f;
        //    private float strafeSpeed = 0f;
        //---------------------------------------------------------------
        [HideInInspector]
        public Transform InertiaTracker_Transform;
        private Transform Hmd_Transform;
        private Rigidbody Hmd_Rb;
        private Rigidbody Torso_Rb;
        private Collider Torso_Col;

        private Transform BullsEyeTransform;
        private Transform PlayAreaTransform;

        private Vector3 InertiaTrackerXYZPos_t1;
        private Vector3 InertiaTrackerXYZPos_t2;
        private Vector3 InertiaVector_1;
        private Vector3 InertiaVector_2;
        private Vector3 InertiaVector_Current;

        private float InertiaTrackerYPos;

        private Vector3 TorsoPos_t1;
        private Vector3 TorsoPos_t2;
        private Vector3 MeasuredVelocity;

        private bool DoingLocomotion = false;
        private bool SamplingVelocity = false;
        private bool TouchpadAccel = false;
        private bool DoingJump = false;

        public Animator Animator;

        private UI_Interactions _UI_Interactions;
    //---------------------------------------------------------------
        public void UI_Options() {

        _UI_Interactions = new UI_Interactions();
//        iUI_Interactions.Toggle.
    }

        //---------------------------------------------------------------
        private void Awake()
        {
            if (this.GetComponent<VRTK_PlayerPresence_Mod>())
            {
                playerPresence = this.GetComponent<VRTK_PlayerPresence_Mod>();
            }
            else
            {
                Debug.LogError("requires the VRTK_PlayerPresence script to be attached to the [CameraRig]");
            }

            touchpadAxisChanged = new ControllerInteractionEventHandler(DoTouchpadAxisChanged);
            touchpadUntouched = new ControllerInteractionEventHandler(DoTouchpadTouchEnd);
            controllerManager = GetComponent<SteamVR_ControllerManager>();
        }

        private void Start()
        {
            //vrtk 
            Utilities.SetPlayerObject(gameObject, VRTK_PlayerObject.ObjectTypes.CameraRig);

            var controllerManager = FindObjectOfType<SteamVR_ControllerManager>();

            SetControllerListeners(controllerManager.left);
            SetControllerListeners(controllerManager.right);

//---------------------------------------------------------------
//            SteamVR_Utils.Event.Listen("device_connected", OnDeviceConnected);

            Hmd_Transform = VRTK_DeviceFinder.HeadsetTransform();

            Hmd_Rb = Hmd_Transform.GetComponent<Rigidbody>();

            Torso_Rb = this.GetComponent<Rigidbody>();
            Torso_Rb.useGravity = false;
            Torso_Col = this.GetComponent<Collider>();

            //added
            Collider Floor_Col;
            Floor_Col = GameObject.FindWithTag("FLOOR").GetComponent<Collider>();
            Physics.IgnoreCollision(Torso_Col, Floor_Col, true);

            //---------------------------------------------------------------
            PlayAreaTransform = GetComponentInChildren<SteamVR_PlayArea>().transform as Transform;
            //        transform.SetParent(PlayArea);
            //        gameObject.GetComponentsInChildren<VignetteAndChromaticAberration>().Vignetting = 5;

            GazeHudCircle = this.transform.FindChild("GazeHudCircle").gameObject as GameObject;
            GazeHudCircle.SetActive(false);

            GazeCenterDot = this.transform.FindChild("GazeCenterDot").gameObject as GameObject;
            GazeCenterDot.SetActive(false);

            BullsEye = this.transform.FindChild("Bullseye").gameObject as GameObject;
            BullsEyeTransform = this.transform.FindChild("Bullseye").transform as Transform;

            FOVLimiter = GetComponentInChildren<VignetteAndChromaticAberration>();
            Animator = GetComponentInChildren<Animator>();

        }


        private void Update()
        {

            //        print("Hmd_Transform.eulerAngles.z; " + Hmd_Transform.eulerAngles.z);
            //        print("Hmd_Transform.eulerAngles.x; " + Hmd_Transform.eulerAngles.x);

            //        Debug.Log("GazeVectoringPermitted: " + GazeVectoringPermitted);
            //        Debug.Log("DoingLocomotion: " + DoingLocomotion);
            //        Debug.Log("Torso_Rb.velocity.magnitude:" + Torso_Rb.velocity.magnitude);

            //        RotatePlayArea();
        }

        private void FixedUpdate()
        {
            //        ShowGazeHud("ON");

            //        Debug.DrawRay(Hmd_Transform.position, ray.origin, Color.green);
            Debug.DrawRay(Hmd_Transform.position, ReverseRayOrigin, Color.green);
            //        Debug.DrawRay(ReverseRayOrigin, Hmd_Transform.position, Color.green);

            AccelControl();
            VelocityLimiter();
            //            RunSpeedControl();

            while (DoingLocomotion == true)
            {
                GazeVsInertiaCheck();
                GazeVectoringPermissionCheck();
                break;
            }

            while (GazeMaintained_t1 == true || GazeMaintained_t2 == true)
            {
                ShowGazeCenter();
                break;
            }

        }
        //---------------------------------------------------------------
        void RotatePlayArea()
        {

            var Wand1 = SteamVR_Controller.Input((int)Wand_R.index);

            while (Wand1.GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu))
            {
                Debug.Log("ApplicationMenuClicked");

                //NG        PlayAreaTransform.rotation = Hmd_Transform.rotation;
                //        PlayAreaTransform.rotation.Set(1, 1, 1);

                //nothin            PlayAreaTransform.Rotate(Vector3.up, Hmd_Transform.rotation.y);
                PlayAreaTransform.Rotate(Vector3.up, Hmd_Transform.rotation.y * Time.deltaTime);
                break;
            }
        }

        //---------------------------------------------------------------
        void AccelControl()
        {
            InertiaTracker_Transform = Hmd_Transform;

            //it does have to be here to worok 
            var Wand1 = SteamVR_Controller.Input((int)Wand_R.index);
            //var Wand2 = SteamVR_Controller.Input((int)Wand_L.index);

            //Vector3 Wand2Pos = Wand2.transform.pos;


            if (Wand1.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
            {
                //                            Debug.Log("GetPressDown");

                InertiaTrackerXYZPos_t1 = InertiaTracker_Transform.localPosition;
                //                Debug.Log("InertiaTrackerXYZPos_t1.x:" + InertiaTrackerXYZPos_t1.x);
                SamplingVelocity = true;
                InertiaVector_1 = Vector3.zero; //initial condittion
                InertiaVector_2 = Vector3.zero; //
            }


            if (Wand1.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
            {
                //                            Debug.Log("GetPressUp");
                InertiaTrackerXYZPos_t2 = InertiaTracker_Transform.localPosition;
                SamplingVelocity = false;

                if (DoingLocomotion == false)
                {
                    InertiaVector_1 = InertiaTrackerXYZPos_t2 - InertiaTrackerXYZPos_t1;
                    //                    Debug.Log("InertiaVector_1. X: " + InertiaVector_1.x);
                    //                 Debug.Log("InertiaVector_1. Y: " + InertiaVector_1.y);            
                }
                else if (DoingLocomotion == true)
                {
                    InertiaVector_2 = InertiaTrackerXYZPos_t2 - InertiaTrackerXYZPos_t1;
                    //                    Debug.Log("InertiaVector_2. X: " + InertiaVector_2.x);
                    //                Debug.Log("InertiaVector_2. Z: " + InertiaVector_2.z);
                }

                HorizontalVectorCheck();
                //                VerticalVelocityCheck(); 

            }

            //sometimes there is a bug 
            /*
            if (AccelCR_started == true && DeccelCR_started == true)
                {
                    StopCoroutine("Acceleration");//
                }
                */
        }

        //---------------------------------------------------------------
        void HorizontalVectorCheck()
        {
            //have to check this first 
            if (DoingLocomotion == true)
            {
                //stop if reverse inertia  - but magnitude should be a factor too?
                if (Vector3.Dot(InertiaVector_1.normalized, InertiaVector_2.normalized) < -0.70f)
                {
                    Debug.Log("Reverse inertia");
                    //                    DoingLocomotion = false;
                    //                SamplingVelocity = false;
                    Torso_Rb.useGravity = false;
                    InertiaVector_1 = Vector3.zero;
                    InertiaVector_2 = Vector3.zero;

                    StopCoroutine("Acceleration");//
                    StartCoroutine("Decceleration");
                    StopCoroutine("GazeTracking");

                }

                // stop Locomotion if no v on second click - have to allow some margin, esp for when things get intense 
                //0.01f maybe too low, 0.02f still low? 
                if ((Mathf.Abs(InertiaVector_2.z) < 0.03f) && (Mathf.Abs(InertiaVector_2.x) < 0.03f))
                {
                    Debug.Log("zero inertia");
                    //                    DoingLocomotion = false;
                    //                SamplingVelocity = false;
                    Torso_Rb.useGravity = false;
                    InertiaVector_1 = Vector3.zero;
                    InertiaVector_2 = Vector3.zero;

                    //                InertiaVector_2 = Vector3.zero;
                    //                        InertiaVector_2 = GazeVector;

                    StopCoroutine("Acceleration");//
                    StartCoroutine("Decceleration");
                    StopCoroutine("GazeTracking");

                    if (DeccelCR_started == false) Debug.LogError("Decceleration NOT started");
                }
            }
            else if (DoingLocomotion == false)
            {
                //first zx acceleration
                //0.005f is too high, 0.0025f still high 
                if ((Mathf.Abs(InertiaVector_1.z) > 0.0015f) || (Mathf.Abs(InertiaVector_1.x) > 0.0015f))
                {
                    Debug.Log("first inertia");
                    //              SamplingVelocity = false;
                    Torso_Rb.useGravity = false;
                    InertiaVector_1.y = 0f;

                    StopCoroutine("Decceleration");//
                    StartCoroutine("Acceleration");
                    StartCoroutine("GazeTracking");
                }
            }

            //check new acceleration while moving - needs to have higher threshold, to avoid unwanted motion 
            //0.005 maybe too low 
            if ((Mathf.Abs(InertiaVector_2.z) > 0.0075f) || (Mathf.Abs(InertiaVector_2.x) > 0.0075f))
            {
                //                            Debug.Log("secondainertia");
                //                DoingLocomotion = true;
                //            SamplingVelocity = false;
                Torso_Rb.useGravity = false;

                InertiaVector_1 = Vector3.zero;
                InertiaVector_2.y = 0f;

                StopCoroutine("Decceleration");//
                StartCoroutine("Acceleration");
                StartCoroutine("GazeTracking");
            }

        }


        //----------------------------------------------------------------
        public float accelBoost1 = 350f; //250f too low , 1000 too high  
        public float accelBoost2 = 10f; //

        public float accelSmooth = 10f; //larger means faster
        public float deccelSmooth = 10f; //larger means faster  - note:cannot stop until done  
        public float gazeBoost = 10f;
        public float anitiGravityBoost = 1000f;

        bool AccelCR_started;
        bool DeccelCR_started;

        IEnumerator Acceleration()
        {
            //            Debug.Log("Acceleration #0");
            AccelCR_started = true;
            Torso_Rb.useGravity = false;

            if (DoingLocomotion == false)
            {
                Debug.Log("Acceleration #1");
                Torso_Rb.velocity = Vector3.Lerp(InertiaVector_1, InertiaVector_1 * accelBoost1, 20f * Time.deltaTime);

                //                InertiaVector_Current = InertiaVector_1;
                DoingLocomotion = true;
                FOVRestriction("ON");
                AnimateBullseye("UP");

                yield return null;
            }
            else if (DoingLocomotion == true)
            {
                Debug.Log("Acceleration #2");
                //maybe NG            Torso_Rb.velocity = InertiaVector_1 + (InertiaVector_2 * 10f)  ; //can get too tricky?

                //have to account for multiple direction changes, not just from 1 to 2 
                //                Torso_Rb.velocity = Vector3.Lerp(InertiaVector_Current, InertiaVector_2 * accelBoost1, accelSmooth * Time.deltaTime);
                Torso_Rb.velocity = Vector3.Lerp(Torso_Rb.velocity, InertiaVector_2 * accelBoost1, 20f * Time.deltaTime);

                //                InertiaVector_Current = InertiaVector_2;
                DoingLocomotion = true;
                FOVRestriction("ON");
                AnimateBullseye("UP");
                yield return null;
            }

        }
        //---------------------------------------------------------------
        public float MaxFOVSpeed = 5f;
        public static float MaxFOV = 0.5f;
        public static float cutOffIntensity = .25f;
        public static float CRate = .01f;

        private VignetteAndChromaticAberration FOVLimiter;

        void FOVRestriction(string OnOff)
        {
            /*
                float targetIntensity = MaxFOV;

                if (Torso_Rb.velocity.magnitude < MaxFOVSpeed) //only update the MaxFOV if we are slower than the MaxSpeed
                    targetIntensity = (Torso_Rb.velocity.magnitude / MaxFOVSpeed) * MaxFOV;

                float currIntensity = FOVLimiter.intensity;

                if (currIntensity < cutOffIntensity)
                {
                    CRate *= 3; //fast rate since the field of view is large and fast changes are less noticeable
                }
                else {
                    CRate *= .5f; //slower rate since the field of view changes are more noticable for larger values. 
                }
                */
            if (OnOff == "ON")
            {
                //            FOVLimiter.intensity = Mathf.Lerp(currIntensity, targetIntensity, CRate);
                FOVLimiter.intensity = 0.5f;
                print("FOVRestriction ON");
            }

            if (OnOff == "OFF")
            {
                //                FOVLimiter.intensity = Mathf.Lerp(currIntensity, 0, 5);
                FOVLimiter.intensity = 0f;
                //                print("FOVRestriction OFF");
            }
        }


        private void VelocityLimiter()
        {
            if (TouchpadAccel == false)
            {
                if (DoingLocomotion == true && Torso_Rb.velocity.magnitude >= maxWalkSpeed)
                {
                    //            Debug.Log("VelocityLimiter");
                    Torso_Rb.velocity = Vector3.ClampMagnitude(Torso_Rb.velocity, maxWalkSpeed);

                    //this turns it off to quickly 
                    FOVRestriction("OFF");
                }
            }
            else if (TouchpadAccel == true) //is this necessary ?
            {
                Torso_Rb.velocity = Vector3.ClampMagnitude(Torso_Rb.velocity, maxRunSpeed);
            }


            //            if (DoingLocomotion == true && Torso_Rb.velocity.magnitude > 0.2f)
            //                AnimateBullseye("UP");

        }


        //    IEnumerator Decceleration(string callerID)
        IEnumerator Decceleration()
        {
            DeccelCR_started = true;

            while (Torso_Rb.velocity.magnitude >= 0.1f) //0.01 NG 
            {
                Debug.Log("Decceleration #1");
                Torso_Rb.velocity = Vector3.Lerp(Torso_Rb.velocity, Vector3.zero, deccelSmooth * Time.deltaTime);
                DoingLocomotion = false;
                //            DoingJump = false;
                //            Torso_Rb.useGravity = true;
                GazeVectoringPermitted = false;
                ShowGazeHud("OFF");
                FOVRestriction("OFF");
                AnimateBullseye("DOWN");
                yield return null;
            }

            //the tail takes too long, so speed it up
            while (Torso_Rb.velocity.magnitude > 0f && Torso_Rb.velocity.magnitude < 0.1f) //
            {
                //            Debug.Log("Decceleration #2");
                Torso_Rb.velocity = Vector3.Lerp(Torso_Rb.velocity, Vector3.zero, deccelSmooth * 2 * Time.deltaTime);

                //            DoingJump = false;
                DoingLocomotion = false;
                //            Torso_Rb.useGravity = true;
                GazeVectoringPermitted = false;
                yield return null;
            }

        }

        //---------------------------------------------------------------
        void VerticalVelocityCheck()
        {
            if (InertiaTracker_Transform.localPosition.y <= InertiaTrackerYPos)
            {
                DoingJump = false;
                Torso_Rb.useGravity = false;
            }

            //if in middle of jump, stop jump  and come down
            if (DoingJump == true)
            {
                Debug.Log("Stop Jump");
                Torso_Rb.useGravity = true;
            }

            if (DoingJump == false)
            {
                InertiaTrackerYPos = InertiaTracker_Transform.localPosition.y;

                //have to detect jump first, otherwise y=0
                if (InertiaVector_1.y > +0.05f)
                {
                    Debug.Log("Jump detected");
                    SamplingVelocity = false;
                    JumpDo();
                    DoingJump = true;
                }
                else
                {
                    //                    Debug.Log("Jump NOT detected");
                    SamplingVelocity = false;
                    DoingJump = false;
                    InertiaVector_1.y = 0f;
                }
            }
        }

        //---------------------------------------------------------------
        void JumpDo()
        {
            //        Torso_Rb.AddForce(transform.up * Physics.gravity.y * -anitiGravityBoost);// 
            Torso_Rb.AddForce(transform.up * Physics.gravity.y * -1000f);// 
        }

        //---------------------------------------------------------------
        GameObject BullsEye;

        void AnimateBullseye(string UPDOWN)
        {
            if (UPDOWN == "UP")
            {
                Animator.SetTrigger("RaiseBullseye");
            }

            if (UPDOWN == "DOWN")
            {
                Animator.SetTrigger("LowerBullseye");
                Debug.Log("LowerBullseye");
            }

            //        Animator.SetFloat(BullsEye.transform.position.y, Torso_Rb.velocity.magnitude);
            //        Animator.SetFloat(BullsEyeHeight, Torso_Rb.velocity.magnitude);

        }

        //want height to lerp according to velocity magnitude
        void ControlBullseyeHeight()
        {
            float BullsEyeTransformY;
            Vector3 BullsEyeTransformUp;

            BullsEyeTransformY = Mathf.Lerp(BullsEyeTransform.position.y, BullsEyeTransform.position.y + 1f, Time.deltaTime);

            BullsEyeTransformUp = new Vector3(BullsEyeTransform.position.x, BullsEyeTransformY, BullsEyeTransform.position.z);

            BullsEyeTransform.position = Vector3.Lerp(BullsEyeTransform.position, BullsEyeTransformUp, Time.deltaTime);

        }

        //---------------------------------------------------------------
        private Vector3 GazeVector;
        private float GazeCenter_t0;
        private float GazeCenter_t1;
        private float GazeCenter_t2;

        private float GazeLeftLimit;
        private float GazeRightLimit;
        private float GazeTimer;
        private bool GazeMaintained_t1 = false;
        private bool GazeMaintained_t2 = false;

        bool GazeVectoringPermitted = false;
        bool RunSpeedPermitted = false;

        //does need 3 steps because too easy to look away then back at same spot 
        //adjust angle window 
        IEnumerator GazeTracking()
        {
            //        Debug.Log("IEnumerator GazeTracking 1");

            while (DoingLocomotion == true)
            {

                ShowGazeHud("OFF");
                GazeMaintained_t1 = false;
                GazeMaintained_t2 = false;

                GazeCenter_t0 = Hmd_Transform.eulerAngles.y;
                GazeLeftLimit = GazeCenter_t0 - 10; //need to tune this , 360 issue ?
                GazeRightLimit = GazeCenter_t0 + 10;

                //            print("GazeCenter_t0 time:" + Time.time);
                //            Debug.Log("GazeCenter_t0 angle: " + GazeCenter_t0);

                yield return new WaitForSeconds(1.0f);

                GazeCenter_t1 = Hmd_Transform.eulerAngles.y;

                //            print("GazeCenter_t1 time:" + Time.time);
                //            Debug.Log("GazeCenter_t1 angle: " + GazeCenter_t1);

                //does this handle the 360-0 issue ?
                if (GazeLeftLimit < GazeCenter_t1 && GazeCenter_t1 < GazeRightLimit)
                {
                    //                  Debug.Log("IEnumerator GazeMaintained_t1 = true: " + Time.time);
                    GazeMaintained_t1 = true;
                    ShowGazeHud("ON");
                }
                else
                {
                    GazeMaintained_t1 = false;
                    ShowGazeHud("OFF");
                    yield return null;
                }

                yield return new WaitForSeconds(2.0f);

                if (GazeMaintained_t1 == true)
                {
                    GazeCenter_t2 = Hmd_Transform.eulerAngles.y;

                    while (GazeLeftLimit < GazeCenter_t2 && GazeCenter_t2 < GazeRightLimit)
                    {
                        //                        Debug.Log("IEnumerator GazeMaintained_t2 = true: " + Time.time);
                        GazeMaintained_t2 = true;
                        GazeVector = Hmd_Transform.forward;
                        GazeVector.y = 0f;
                        GazeVectoring();
                        break;
                    }

                    //ShowGazeHud("OFF");
                    yield return null;

                }//            if (GazeMaintained_t1 == true)

            }//        while (DoingLocomotion == true)

        }

        //---------------------------------------------------------------
        RaycastHit gazeRayHit;
        GameObject GazeHudCircle;
        GameObject GazeHudCircleClone;
        GameObject GazeCenterDot;
        GameObject GazeCenterDotClone;

        Vector3 ReverseRayOrigin;
        float GazeHudCircleDist = 1000f;

        //    IEnumerator ShowGazeCenter()
        void ShowGazeCenter()
        {
            //        print("ShowGazeCenter");

            // Bit shift the index of the layer (5) to get a bit mask. Cast rays only against colliders in layer 5.
            int layerMask = 1 << 5;

            //Raycasts will not detect Colliders for which the Raycast origin is inside the Collider. so have to make reverse ray
            Ray ray = new Ray(Hmd_Transform.position, Hmd_Transform.forward);
            ReverseRayOrigin = ray.GetPoint(100);
            //        ReverseRayOrigin = new Vector3(ReverseRayOrigin.x, Hmd_Transform.position.y, ReverseRayOrigin.z);

            while (Physics.Raycast(ReverseRayOrigin, -Hmd_Transform.forward, out gazeRayHit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
            {
                print("ShowGazeCenter gazeRayHit; " + gazeRayHit.collider);

                if (gazeRayHit.collider.name == "GazeUISphere")
                {
                    GameObject GazeCenterDotClone = Instantiate(GazeCenterDot, gazeRayHit.point, Quaternion.identity) as GameObject;
                    GazeCenterDotClone.SetActive(true);
                    Destroy(GazeCenterDotClone, 0.1f);
                }

                break;
            }

            //        yield return null;
        }

        //need to make it appear only just above the ground 
        //make rayhit return all collides ? but layer thing 
        //    IEnumerator ShowGazeHud(string OnOff)
        void ShowGazeHud(string OnOff)
        {
            //        print ("IEnumerator ShowGazeHud");

            // Bit shift the index of the layer (5) to get a bit mask. Cast rays only against colliders in layer 5.
            int layerMask = 1 << 5;

            //Raycasts will not detect Colliders for which the Raycast origin is inside the Collider. so have to make reverse ray
            Ray ray = new Ray(Hmd_Transform.position, Hmd_Transform.forward);
            ReverseRayOrigin = ray.GetPoint(2.1f);
            //        ReverseRayOrigin.y = Hmd_Transform.position.y;
            //NG        ReverseRayOrigin = new Vector3(ReverseRayOrigin.x, Hmd_Transform.position.y, ReverseRayOrigin.z);

            while (Physics.Raycast(ReverseRayOrigin, -Hmd_Transform.forward, out gazeRayHit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
            {
                print("ShowGazeHud gazeRayHit; " + gazeRayHit.collider);
                //            print("Hmd_Transform.rotation.y; " + Hmd_Transform.rotation.y);

                Quaternion GazeHudRotation;
                //NG            GazeHudRotation = new Quaternion(Hmd_Transform.rotation.x, Hmd_Transform.rotation.y, Hmd_Transform.rotation.z, 0f);
                //            GazeHudRotation = Quaternion.Euler(90 , Hmd_Transform.rotation.y + 0 , 90);
                GazeHudRotation = Quaternion.Euler(90, 0, 90);

                if (gazeRayHit.collider.name == "GazeUISphere")
                {
                    //                GameObject GazeHudCircleClone = Instantiate(GazeHudCircle, gazeRayHit.point, Quaternion.FromToRotation(Vector3.up, -gazeRayHit.normal)) as GameObject;
                    //NG   GameObject GazeHudCircleClone = Instantiate(GazeHudCircle, gazeRayHit.point, Quaternion.FromToRotation(GazeHudCircle.transform.up, Hmd_Transform.forward)) as GameObject;
                    GameObject GazeHudCircleClone = Instantiate(GazeHudCircle, gazeRayHit.point, Quaternion.FromToRotation(GazeHudCircle.transform.forward, Hmd_Transform.forward)) as GameObject;

                    //                GameObject GazeHudCircleClone = Instantiate(GazeHudCircle, gazeRayHit.point, Quaternion.Euler(90f,0f,-180f)) as GameObject;
                    //ok but NG                GameObject GazeHudCircleClone = Instantiate(GazeHudCircle, gazeRayHit.point, Hmd_Transform.rotation) as GameObject;
                    //ok but NG              GameObject GazeHudCircleClone = Instantiate(GazeHudCircle, gazeRayHit.point, GazeHudRotation) as GameObject;

                    //NG         GameObject GazeHudCircleClone = Instantiate(GazeHudCircle, gazeRayHit.point, Quaternion.LookRotation(Hmd_Transform.forward, Hmd_Transform.up) as GameObject;
                    //NG         GameObject GazeHudCircleClone = Instantiate(GazeHudCircle, gazeRayHit.point, Quaternion.LookRotation(Hmd_Transform.position, Hmd_Transform.up) as GameObject;
                    //NG        GameObject GazeHudCircleClone = Instantiate(GazeHudCircle, gazeRayHit.point, Quaternion.SetLookRotation(Hmd_Transform.position, Hmd_Transform.up) as GameObject;


                    GazeHudCircleClone.SetActive(true);
                    Destroy(GazeHudCircleClone, 2f);
                }

                break;
            }

            if (OnOff == "OFF")
            {
                print("Destroy hud ");
                GameObject.Destroy(GazeHudCircleClone, 0f);
            }

            //        yield return null;
        }


        //---------------------------------------------------------------
        void GazeVectoring()
        {
            if (GazeVectoringPermitted == true)
            {
                Debug.Log("Doing GazeVectoring ");
                //NG                Torso_Rb.AddForce(GazeVector * 1000f, ForceMode.VelocityChange);//
                //            Torso_Rb.AddForce(GazeVector * gazeBoost, ForceMode.Impulse);//seems to work 
                //           Torso_Rb.velocity = (GazeVector * 100f) + InertiaVector_1; //try this method

                //            Torso_Rb.velocity = Vector3.Slerp(InertiaVector_1, GazeVector * 100f, 1f * Time.deltaTime);
                Torso_Rb.velocity = Vector3.Lerp(Torso_Rb.velocity, GazeVector * 75f, 20f * Time.deltaTime);

            }
        }

        //----------------------------------------------------------------
        //GazeVectoringPermission window angle needs to be narrower, so I can strafe shoot 
        //also to limit when it kicks in 
        private void GazeVectoringPermissionCheck()
        {
            if (Vector3.Dot(Hmd_Transform.forward, Torso_Rb.velocity.normalized) > +0.25f)
            {
                //            print("GazeVectoringPermitted = true; " + Time.time);
                GazeVectoringPermitted = true;
                RunSpeedPermitted = true;
            }
            else
            {
                //            print("GazeVectoringPermitted = false; " + Time.time);
                GazeVectoringPermitted = false;
                RunSpeedPermitted = false;
            }
        }

        //---------------------------------------------------------------
        bool currentGazeVsInertiaOk;
        bool previousGazeVsInertiaOk;

        //check that gaze direction, torso velecoty are within same angle window, 
        //I can look around while moving, but stop moving when I turn more than angle x. 

        //For normalized vectors Dot returns -1 if they point in completely opposite directions 
        // 0 if the vectors are perpendicular.
        // Dot product is positive for vectors in the same general direction
        private void GazeVsInertiaCheck()
        {
            //        print("GazeVsInertiaCheck: " + Time.time);
            //cannot do InertiaVector_1 and 2 at same time, unless there is a separate condition to check ?
            //&& InertiaVector_1.normalized != Vector3.zero
            //if one of the vectors is zero - dot is zero issue !

            //Hmd_Transform.forward has y vector included so NG ? but GazeVector has to get fixupdated to use 
            //        if (Vector3.Dot(GazeVector.normalized, Torso_Rb.velocity.normalized) > -0.2f && GazeVector != Vector3.zero)
            if (Vector3.Dot(Hmd_Transform.forward, Torso_Rb.velocity.normalized) > -0.25f)
            {
                currentGazeVsInertiaOk = true;
            }
            else
            {
                currentGazeVsInertiaOk = false;
            }
            //---------------------------------------------------------------
            //if it goes from GazeVectoringPermitted = true to false;
            //then Locomotion should be shut off, and not start moving again 
            if (currentGazeVsInertiaOk == false && previousGazeVsInertiaOk == true)
            {
                Debug.Log("currentGazeVsInertiaOk == false: " + Time.time);
                StartCoroutine("Decceleration");
            }

            previousGazeVsInertiaOk = currentGazeVsInertiaOk;

        }

        //---------------------------------------------------------------

        //have to make sure that I am not auto colliding with floor on start
        void OnCollisionStay(Collision collision)
        {
            //Debug.Log("OnCollisionStay: " + collision.collider);

            if (DoingLocomotion == true && SamplingVelocity == false && collision.gameObject.isStatic == true)
            {

                //            StopCoroutine("Acceleration");//
                //            StartCoroutine("Decceleration");

                /*
                if (Mathf.Approximately(Torso_Rb.velocity.x, 0) && Mathf.Approximately(Torso_Rb.velocity.z, 0))
                { //redundancy 
                    DoingLocomotion = false;
                }
    */
                /*NG
                if (Mathf.Approximately(Torso_Rb.velocity.magnitude, 0))
                {
                    DoingLocomotion = false;
                }
    */
            }
        }

        //---------------------------------------------------------------
        /*
        void RunSpeedControl()
            {
                //it does have to be here to worok 
                var Wand1 = SteamVR_Controller.Input((int)Wand_R.index);

    //            if (Wand1.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
                if(Wand1.GetPressDown(SteamVR_Controller.ButtonMask.Axis0))
                {
    //            var axisVar = Wand1.GetPressDown(SteamVR_Controller.ButtonMask.Axis0);

                   Debug.Log("RunSpeedControl: " );
                }

            }
    */

        //---------------------------------------------------------------
        float RunSpeed;
        float SpeedDelta;

        private void RunSpeedControl()
        {
            //        var movement = playerPresence.GetHeadset().forward * RunSpeed * Time.deltaTime;
            SpeedDelta = RunSpeed * Time.deltaTime;
            //           Debug.Log("SpeedDelta: "+ SpeedDelta);

            if (DoingLocomotion == true && RunSpeedPermitted == true)
            {
                //            Torso_Rb.velocity = Vector3.Lerp(InertiaVector_1, GazeVector * 100f, accelSmooth * Time.deltaTime);
                //            Torso_Rb.AddForce(Torso_Rb.velocity * SpeedDelta, ForceMode.Impulse);//
                Torso_Rb.AddForce(Torso_Rb.velocity.normalized * SpeedDelta * 200f, ForceMode.Impulse);//
            }
        }

        //---------------------------------------------------------------
        private void CalcInputSpeedV1(ref float speed, float inputValue)
        {
            if (inputValue > 0f)
            {
                inputValue = inputValue + 1f; //to make it at least 1 
                speed = inputValue;
                TouchpadAccel = true;
            }

            if (inputValue <= 0f)
            {
                inputValue = 1f;
                speed = inputValue;
                TouchpadAccel = false;
            }
        }

        //---------------------------------------------------------------
        IEnumerator CalcInputSpeed(float speed, float inputValue)
        {
            yield return new WaitForSeconds(0.25f);

            while (inputValue > 0f) //if clicked up
            {
                inputValue = inputValue + 1f; //to make it at least 1 
                RunSpeed = inputValue;

                TouchpadAccel = true;
                yield return null;
            }

            while (inputValue <= 0f)
            {
                inputValue = 1f;
                RunSpeed = inputValue;
                TouchpadAccel = false;
                yield return null;
            }
        }
        //---------------------------------------------------------------
        private void DoTouchpadAxisChanged(object sender, ControllerInteractionEventArgs e)
        {
            //        var controller = e.controllerIndex;
            //            Debug.Log("controller index:" + e.controllerIndex);
            //        Debug.Log("sender:" + sender.ToString);

            touchAxis = e.touchpadAxis;

            if (DoingLocomotion == true)
            {
                //                StartCoroutine(CalcInputSpeed(speed, touchAxis.y));
            }
        }

        private void DoTouchpadTouchEnd(object sender, ControllerInteractionEventArgs e)
        {
            touchAxis = Vector2.zero;
        }

/*
        private void DoTouchpadUntouched(object sender, ControllerInteractionEventArgs e)
        {
            //want it to be where it was last touched -NG there is too much touching/clicking, speed becomes not predictable
            //        touchAxis = e.touchpadAxis;

            touchAxis = Vector2.zero;
        }
*/
        //---------------------------------------------------------------
        private void SetControllerListeners(GameObject controller)
        {
            if (controller && controller == controllerManager.left)
            {
                ToggleControllerListeners(controller, leftController, ref leftSubscribed);
            }
            else if (controller && controller == controllerManager.right)
            {
                ToggleControllerListeners(controller, rightController, ref rightSubscribed);
            }
        }

        private void ToggleControllerListeners(GameObject controller, bool toggle, ref bool subscribed)
        {
            var controllerEvent = controller.GetComponent<VRTK_ControllerEvents>();
            if (controllerEvent && toggle && !subscribed)
            {
                controllerEvent.TouchpadAxisChanged += touchpadAxisChanged;
                controllerEvent.TouchpadTouchEnd += touchpadUntouched;
                subscribed = true;
            }
            else if (controllerEvent && !toggle && subscribed)
            {
                controllerEvent.TouchpadAxisChanged -= touchpadAxisChanged;
                controllerEvent.TouchpadTouchEnd -= touchpadUntouched;
                touchAxis = Vector2.zero;
                subscribed = false;
            }
        }

    }


/***************************************************************
    TRIALS

*/



/***************************************************************

2016-07-01 Fri 07:11
buttons don't  get buttonUp event ?
    OnCollisionStay instanlly did decceleration and DoingLocomotion false

2016-06-29 Wed 22:42
gaze vectoring
   very unstable 
   has to be in fxiedup?
        
RunSpeedControl();
   if bool RunSpeedPermitted ;
   make it stay at last speed?

2016-06-29 Wed 00:41
had to make new project, due to constant crashing 

2016-06-27 Mon 21:00
WTF  is my gun_??????
deccel allwayson due to another wrong bool check 

2016-06-26 Sun 23:01
only touchpad not responding 
    but VRTK_ControllerEvents shows response 

2016-06-25 Sat 01:16
there is a conflict in the boolean logic causing 
    how to tell in hmd when stop NG ?

Acceleration and decceleration conflict 
WORKS but sometimes does not stop on press 
Coroutine("Acceleration"); sometimes just stays on 
button bouce issue?
transform.SetParent(PlayArea); - NG 

2016-06-23 Thu 15:54
why has shit fallen apart?
    IEnumerator Decceleration() NG 
    pinballing 

no reaction to moving 
    DoTouchpadClicked is too slow, has to be in update?
    made  AccelControl()

playa area moves, but then dissapears 

2016-06-22 Wed 21:58
changing one boolean condition, without properly checking the consequence, really tripped me up!!

IEnumerator GazeTracking()
    not sure if ok 

2016-06-22 Wed 16:34
private void VelocityLimiter() does not have instant effect!

playarea gets left behind !! ok now  
    child objet cannot move the parent object !duh!

2016-06-22 Wed 02:17
 GazeVsAccelVectorCheck(); does not seem to be working 

 if (Mathf.Approximately(Torso_Rb.velocity.x, 0) && Mathf.Approximately(Torso_Rb.velocity.z, 0)) 

2016-06-21 Tue 17:57
coroutines

2016-06-20 Mon 15:37
trying to access CharacterController velocity 

Decelerate_Velocity
  stop drifting after stop 

HeadBob_VR
    had to move out, due to compile CalcInputSpeeds

playarea rotation possible ?
    not worth it?

audio crakles 
    hit sound timing 
performance hits 
GazeVectoringPermitted()

reaction is too slow
      Torso_Rb.velocity = Vector3.ClampMagnitude(Torso_Rb.velocity, 2.5f);//2.5f

2016-06-18 Sat 17:06
//addforce ver 
    private void NewtonianAcceleration()    {

HTF  did I do using DoTouchpadClicked and controlling speed too ?

//cannt get this to work yet 
    private float SpeedDelta;
    private void RunSpeedControl()

2016-06-07 Tue 20:59
breakthru happend, proof of concept looks ok 
*/
