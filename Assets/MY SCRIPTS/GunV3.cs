/*
2016-07-06 Wed 20:19
bulletHole NG- ok

2016-06-28 Tue 00:10
triggers are rersponding but gun is invisible
    wand and gun goto outer space
       no longer child??

now unity crashes a lot !! what broke?
gun ok in hangar 

2016-06-18 Sat 23:33
from fuseman bow lesson
	var device = SteamVR_Controller.Input((int)trackedGun.index);
	if (device.GetTouchUp (SteamVR_Controller.ButtonMask.Trigger)) {


2016-06-18 Sat 23:25
OnDeviceConnected will not fucking get called when gun is child of wand !!! WTF  	
putting GunV2 onto wand also gives fucking nothing !!
do I need all this crap that stonefox has added???

2016-06-17 Fri 21:53
VRTK_ControllerEvents sees the trigger events ok, so should just use it!?
    but it still depends on OnDeviceConnected happening first?

moved this onto controller - but OnDeviceConnected still NG  
OnDeviceConnected does respond when this script is on Rig 

    
2016-06-17 Fri 19:17
there is no response to  SteamVR_Utils.Event.Listen("device_connected", OnDeviceConnected);
    child parent issue?
    do I have to put the gun script on the controller instead ??


2016-06-16 Thu 23:51
changed trigger method to direct listener 

2016-06-16 Thu 21:28
WTF  have to pull tigger twice to fire once 
    VRTK_ControllerEvents only does toggle 

2016-06-14 Tue 11:36
bullet is going in fucked direction 
set parernt?
transform.forward

NOTES
He fucking uses these as separate !!
trackedControllers
trackedController
so  I changed to trackedControllerList

bullet has to be manually positioned at gun tip 
***************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR; //added

public class GunV3 : MonoBehaviour{

    public SteamVR_TrackedObject trackedGun;

    GameObject pistol;
    GameObject bullet;
    GameObject bulletHole;

    float bulletSpeed = 20000f;
    float bulletLife = 1f;
    float bulletHoleLife = 20f;
    //---------------------------------------------------------------
    float maxDist = 1000000f;
    float floatInFrontOfWall = 0.00001f;

    //---------------------------------------------------------------
    private void Start()    {

//        pistol = this.transform.Find("PM-40_Variant1").gameObject;
//        pistol.SetActive(true);
//        pistol.transform.SetParent(this.transform);//added

//        bullet = this.transform.Find("Bullet").gameObject;
       bullet = this.transform.FindChild("Bullet").gameObject;
//        bullet = GameObject.Find("/Bullet");

        bullet.SetActive(false);
        bullet.transform.SetParent(this.gameObject.transform);//added

        bulletHole = this.transform.FindChild("BulletHole").gameObject;
        bulletHole.SetActive(false);

    }


    private void FixedUpdate()
    {
        FireControl();
    }

    public AudioClip gunShot;
    public AudioClip bulletHitSound;

    void FireControl()
    {
        var device = SteamVR_Controller.Input((int)trackedGun.index);

        if (device.GetTouchDown(SteamVR_Controller.ButtonMask.Trigger))
        {
//            FireBullet();
           StartCoroutine("FireBullet");
        }
    }


//    void FireBullet()
    IEnumerator FireBullet()
    {
//        Debug.Log("FireBullet GunV3");

        //play bang
        AudioSource.PlayClipAtPoint(gunShot, transform.position);
        //---------------------------------------------------------------
        GameObject bulletClone = Instantiate(bullet, bullet.transform.position, bullet.transform.rotation) as GameObject;
        //        GameObject bulletClone = Instantiate(bullet, bullet.transform.position, Quaternion.identity) as GameObject;

        bulletClone.SetActive(true);
        //        bulletClone.transform.SetParent(this.gameObject.transform);//added

        Rigidbody rb = bulletClone.GetComponent<Rigidbody>();
        //        rb.AddForce(-bullet.transform.forward * bulletSpeed);
        rb.AddForce(bullet.transform.up * bulletSpeed); //had to change due to prefab orientation

        Destroy(bulletClone, bulletLife);
        //---------------------------------------------------------------
        //need to wait so raycast does not hit the bullet, and spawn a bullethole in the air!
        yield return new WaitForSeconds(0.05f);

        //bulletholes 
        //        var fwd = transform.TransformDirection(Vector3.up);

        //        int layerMask = 5; 
        int layerMask = 1 << 5; ;
        layerMask = ~layerMask;

        RaycastHit hit;
        //        Debug.DrawRay(transform.position, fwd * 100, Color.green, 5, true);

//        RaycastHit[] hits;
//        hits = Physics.RaycastAll(transform.position, transform.up, out hit, maxDist, layerMask, QueryTriggerInteraction.Ignore);

        if (Physics.Raycast(transform.position, transform.up, out hit, maxDist, layerMask, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log("Hit; " + hit.transform.gameObject.name);

            GameObject bulletHoleClone = Instantiate(bulletHole, hit.point + (hit.normal * floatInFrontOfWall), Quaternion.FromToRotation(Vector3.up, hit.normal)) as GameObject;

            bulletHoleClone.SetActive(true);

//            var MetalTarget = LayerMask.NameToLayer("Metal Target");

            //play ping
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Metal Target"))
            {
                yield return new WaitForSeconds(0.01f);    
                AudioSource.PlayClipAtPoint(bulletHitSound, bulletHoleClone.transform.position);
            }

            Destroy(bulletHoleClone, bulletHoleLife);
            yield return null;
         }

    }//    IEnumerator FireBullet()

}

//---------------------------------------------------------------
/*
    void FireBulletV1()    {

        Debug.Log("FireBullet GunV3");

        //play bang
        AudioSource.PlayClipAtPoint(gunShot, transform.position);
        //---------------------------------------------------------------
        //bulletholes 
        var fwd = transform.TransformDirection(Vector3.up);

        RaycastHit hit;
//        Debug.DrawRay(transform.position, fwd * 100, Color.green, 5, true);

        if (Physics.Raycast(transform.position, transform.up, out hit, maxDist))
        {
                Debug.Log("Hit");

            GameObject bulletHoleClone = Instantiate(bulletHole, hit.point + (hit.normal * floatInFrontOfWall), Quaternion.FromToRotation(Vector3.up, hit.normal)) as GameObject;

            bulletHoleClone.SetActive(true);

            var MetalTarget = LayerMask.NameToLayer("Metal Target");

            //play ping
            //NG            if (hit.transform.tag == "Targets") { 
            if (hit.transform.gameObject.layer == MetalTarget) { 
                AudioSource.PlayClipAtPoint(bulletHitSound, bulletHoleClone.transform.position);
            }

            Destroy(bulletHoleClone, bulletHoleLife);
        }


        //---------------------------------------------------------------
        GameObject bulletClone = Instantiate(bullet, bullet.transform.position, bullet.transform.rotation) as GameObject;
//        GameObject bulletClone = Instantiate(bullet, bullet.transform.position, Quaternion.identity) as GameObject;

        bulletClone.SetActive(true);
//        bulletClone.transform.SetParent(this.gameObject.transform);//added

        Rigidbody rb = bulletClone.GetComponent<Rigidbody>();
//        rb.AddForce(-bullet.transform.forward * bulletSpeed);
        rb.AddForce(bullet.transform.up * bulletSpeed); //had to change due to prefab orientation

        Destroy(bulletClone, bulletLife);

    }
  */



