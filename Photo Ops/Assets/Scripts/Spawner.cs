using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] GameObject SpawnedObject;

    void Awake()
    {
        Instantiate(SpawnedObject, transform);
    }
}
