using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{

    public Transform activeCam;

    // Update is called once per frame
    void LateUpdate()
    {
        //need way to check what is active camera
        //probably will do it through a game manager script

        transform.LookAt(transform.position + activeCam.forward);
    }
}
