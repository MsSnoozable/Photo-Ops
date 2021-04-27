using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureReading
{
    static float CheckEnemyPercentOnScreen(Texture2D photoResult) {
        float percent = 0;
        Color[] screen_pixels = photoResult.GetPixels();

        foreach (Color pixel in screen_pixels) {
            if (pixel == Color.white)
                percent++;
        }

        return percent;
    }
}
