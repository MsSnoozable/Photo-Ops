using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageEnemies : MonoBehaviour
{
    public Slider EnemyHealthBar;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage (float damageDealt)
    {
        //Debug.Log("Health" + EnemyHealthBar.value);
        EnemyHealthBar.value -= damageDealt;
    }
}
