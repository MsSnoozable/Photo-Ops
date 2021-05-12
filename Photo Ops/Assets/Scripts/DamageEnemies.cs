using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageEnemies : MonoBehaviour
{
    public Slider EnemyHealthBar;
    public GameObject enemy;


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
        float damagePercent = Mathf.InverseLerp(0, Screen.width * Screen.height, damageDealt);
        EnemyHealthBar.value -= damagePercent * EnemyHealthBar.maxValue;

        if (EnemyHealthBar.value <= 0)
            Destroy(enemy);
    }
}
