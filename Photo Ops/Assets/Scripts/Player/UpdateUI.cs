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
    [SerializeField] Text ammo;
    [SerializeField] Text excessAmmoText;

    //called from camera controller script. Passes in which to edit and what value to assign
    public void ChangeFocus (float value)
    {
        if (value == Mathf.Infinity)
            focus.text = "Focus: ∞";
        else
            focus.text = "Focus: " + value;
    }
    public void ChangeAperture(float value)
    {
        aperture.text = "Aperture: " + value;
    }
    public void ChangeZoom(float value)
    {
        zoom.text = "Zoom: " + value;
    }

    public void ChangeAmmo(int loadedAmmo, int excessAmmo)
    {
        ammo.text = "Ammo: " + loadedAmmo + " / 8"; 
        excessAmmoText.text = "" + excessAmmo;
        
    }

    public void Interactable () { 

    }
}
