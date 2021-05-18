using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Objective : MonoBehaviour
{
    [SerializeField] float rotationSpeed;
    [SerializeField] float bobSpeed;
    [SerializeField] float bobHeight;

    void RotateAndBob ()
    {
        transform.Rotate(new Vector3(0, rotationSpeed, 0), Space.World);
        transform.Translate(0, Mathf.Sin(Time.fixedTime * Mathf.PI * 2 * bobSpeed) * bobHeight, 0, Space.World);
    }

    private void Update()
    {
        RotateAndBob();
    }

    //todo: calls from player script, adds to that players inventory and despawns it.
    public bool PickUp ()
    {
        Destroy(this.gameObject);
        return true;
    }
}
