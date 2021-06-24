using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Photo 
{
    Player photographer; //person taking picture
    Player[] subjects; //people in the picture

    int[] damageDealt; // should be corresponding to each subject

    bool[] subjectsThatDied;
}
