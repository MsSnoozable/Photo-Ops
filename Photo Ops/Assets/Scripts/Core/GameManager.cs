using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [HideInInspector] public int teamAScore = 0;
    [HideInInspector] public int teamBScore = 0;
    [HideInInspector] public int roundNumber = 0;
    [HideInInspector] public Player[] teamA;
    [HideInInspector] public Player[] teamB;

    //think Play of the game. The highest scoring shot will be saved here and displayed at the end of the match.
    //Each time a photo is taken the damage is calculated and compared to the bestPhoto to see if it should be replaced
    public GameObject bestPhoto;


    //makes sure there is only one game manager in the scene at any time
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
    }

    private void Start()
    {

    }

    private void Update()
    {
        /* if attacking / defending team is all dead, players not dead get a point and end Round
         * if 
         */        
    }

    public string EndRound ()
    {
        roundNumber++;
        return "";
    }
    public static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }
}
