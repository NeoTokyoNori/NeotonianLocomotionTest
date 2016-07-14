/*
2016-07-10 Sun 01:14
some of his posted syntax appears to be wrong 

**************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HashSetTest : MonoBehaviour {

    void Start()
    {
    }

    void FixedUpdate()
    {
        for (int i = 0; i < ColList.Count; i++)
        {
            //Do all your on stay stuffs
            //what can I do?
           // Debug.Log("STUFF" + ColList.IndexOf(i).ToString);

        }
    }

    List<Collider> ColList = new List<Collider>();
//    HashSet<Collider> ColHashes = new List<HashSet>();
    HashSet<Collider> ColHashes = new HashSet<Collider>();

    void OnTriggerEnter(Collider col)
    {
        if (ColHashes.Add(col))
        {
            ColList.Add(col);
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (ColHashes.Remove(col))
        {
            ColList.Remove(col);
        }
    }

}
