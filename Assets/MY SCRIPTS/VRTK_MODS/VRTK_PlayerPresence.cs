﻿/*
2016-07-13 Wed 23:51
it won't  fucking move ata all 

2016-06-08 Wed 16:54
he had box collider, I changed to capsule 
CreateCollider()
UpdateCollider() updates the size, with hmd y height 

**************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VRTK_PlayerPresence : MonoBehaviour
{
    public float headsetYOffset = 0.2f;
    public bool ignoreGrabbedCollisions = true;

    private Transform headset;
    private Rigidbody rb;
    private BoxCollider bc;
    private CapsuleCollider CapsuleCol;

    private GameObject floorTouching;
    private Vector3 lastGoodPosition;
    private bool lastGoodPositionSet = false;
    private float highestHeadsetY = 0f;
    private float crouchMargin = 0.5f;
    private float lastPlayAreaY = 0f;

    private List<SteamVR_TrackedObject> trackedControllers;

    public Transform GetHeadset()
    {
        return headset;
    }

    private void Start()
    {
        this.name = "PlayerObject_" + this.name;
        trackedControllers = new List<SteamVR_TrackedObject>();
        lastGoodPositionSet = false;
        headset = DeviceFinder.HeadsetTransform();
        CreateCollider();
        InitHeadsetListeners();
        SteamVR_Utils.Event.Listen("device_connected", OnDeviceConnected);
    }

    private void InitHeadsetListeners()
    {
        if (headset.GetComponent<VRTK_HeadsetCollisionFade>())
        {
            headset.GetComponent<VRTK_HeadsetCollisionFade>().HeadsetCollisionDetect += new HeadsetCollisionEventHandler(OnHeadsetCollision);
        }
    }

    private void OnGrabObject(object sender, ObjectInteractEventArgs e)
    {
        Physics.IgnoreCollision(this.GetComponent<Collider>(), e.target.GetComponent<Collider>(), true);
    }

    private void OnUngrabObject(object sender, ObjectInteractEventArgs e)
    {
        if (e.target.GetComponent<VRTK_InteractableObject>() && !e.target.GetComponent<VRTK_InteractableObject>().IsGrabbed())
        {
            Physics.IgnoreCollision(this.GetComponent<Collider>(), e.target.GetComponent<Collider>(), false);
        }
    }

    private void OnHeadsetCollision(object sender, HeadsetCollisionEventArgs e)
    {
        if (lastGoodPositionSet)
        {
            SteamVR_Fade.Start(Color.black, 0f);
            this.transform.position = lastGoodPosition;
        }
    }

    private void CreateCollider()
    {
        //        rb = this.gameObject.AddComponent<Rigidbody>();
        //        rb.mass = 10;
        //        rb.freezeRotation = true;

        //        bc = this.gameObject.AddComponent<BoxCollider>();
//        CapsuleCol = this.gameObject.AddComponent<CapsuleCollider>();
        CapsuleCol = this.gameObject.GetComponent<CapsuleCollider>();

        CapsuleCol.center = new Vector3(0f, 1f, 0f);
        //        CapsuleCol.size = new Vector3(0.25f, 1f, 0.25f);
        CapsuleCol.height = 2f;
        CapsuleCol.radius = 0.25f; //0.2 is too narrow 

        this.gameObject.layer = 2;


    }

    private void UpdateCollider()
    {
        var playAreaHeightAdjustment = 0.009f;
        var newBCYSize = (headset.transform.position.y - headsetYOffset) - this.transform.position.y;
        var newBCYCenter = (newBCYSize != 0 ? (newBCYSize / 2) + playAreaHeightAdjustment : 0);

        //        CapsuleCol.size = new Vector3(CapsuleCol.size.x, newBCYSize, CapsuleCol.size.z);
        CapsuleCol.height = newBCYSize;
        CapsuleCol.center = new Vector3(headset.localPosition.x, newBCYCenter, headset.localPosition.z);
    }

    private void SetHeadsetY()
    {
        //if the play area height has changed then always recalc headset height
        var floorVariant = 0.005f;

        if (this.transform.position.y > lastPlayAreaY + floorVariant || this.transform.position.y < lastPlayAreaY - floorVariant)
        {
            highestHeadsetY = 0f;
        }

        if (headset.transform.position.y > highestHeadsetY)
        {
            highestHeadsetY = headset.transform.position.y;
        }

        if (headset.transform.position.y > highestHeadsetY - crouchMargin)
        {
            lastGoodPositionSet = true;
            lastGoodPosition = this.transform.position;
        }

        lastPlayAreaY = this.transform.position.y;
    }

    private void Update()
    {
        SetHeadsetY();
        UpdateCollider();
    }

    private void OnDeviceConnected(params object[] args)
    {
        StartCoroutine(InitListeners((uint)(int)args[0], (bool)args[1]));
    }

    IEnumerator InitListeners(uint trackedControllerIndex, bool trackedControllerConnectedState)
    {
        var trackedController = DeviceFinder.ControllerByIndex(trackedControllerIndex);
        var tries = 0f;
        while (!trackedController && tries < DeviceFinder.initTries)
        {
            tries += Time.deltaTime;
            trackedController = DeviceFinder.ControllerByIndex(trackedControllerIndex);
            yield return null;
        }

        if (trackedController)
        {
            var grabbingController = trackedController.GetComponent<VRTK_InteractGrab>();
            if (grabbingController && ignoreGrabbedCollisions)
            {
                if (trackedControllerConnectedState && !trackedControllers.Contains(trackedController))
                {
                    grabbingController.ControllerGrabInteractableObject += new ObjectInteractEventHandler(OnGrabObject);
                    grabbingController.ControllerUngrabInteractableObject += new ObjectInteractEventHandler(OnUngrabObject);
                    trackedControllers.Add(trackedController);
                }
            }
        }
    }
}
