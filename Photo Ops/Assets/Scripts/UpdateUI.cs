using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateUI : MonoBehaviour
{
    //references
    [SerializeField] Text zoom;
    [SerializeField] Text aperture;
    [SerializeField] Text focus;

    //called from camera controller script. Passes in which to edit and what value to assign
    public void ChangeNotchValues (string type, float value)
    {
        switch (type)
        {
            case "zoom":
                zoom.text = "Zoom: " + value;
                break;
            case "focus":
                focus.text = "Focus: " + value;
                break;
            case "aperture":
                aperture.text = "Aperture: " + value;
                break;
            default:
                Debug.LogError("No type defined for ChangeNotchValues function");
                break;
        }
    }

     

}
