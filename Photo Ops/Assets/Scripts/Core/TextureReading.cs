using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureReading
{
    #region Private Members
        static float centerMultiplier = 1f;
        static float edgeMultiplier = 0.5f;
        static float cornerMultiplier = 0.25f;

        static float faceMultiplier = 0.5f;

        static int cornerPixels = 0;
        static int edgePixels = 0;
        static int centerPixels = 0;

        static int bodyPixels = 0;
        static int facePixels = 0;
    #endregion

    public static (float damage, string reason)CheckEnemyPercentOnScreen(Texture2D photoResult, Color body, Color face) {
        //might break if not fixed aspect ratio
        int horizontalThird = photoResult.width / 3;
        int verticalThird = photoResult.height / 3;

        Color[] screenPixels = photoResult.GetPixels();

        //todo: might be able to decrease complexity if I didn't check this then do the subsections later
        //could check this inside subsections instead. Will have to see later
        //checks if there is a row above the highest face pixel and a row below the lowest face pixel.
        //if both of these are true then the entire face is in the image

        //todo: make this not have to run an extra go around
        //checks if face even is in shot and does 0 if not. Need to make all of this more efficient
        bool isFaceInShot = false;
        bool isEnemyInShot = false;
        for (int i = 0; i < screenPixels.Length && !(isFaceInShot && isEnemyInShot); i++)
        {
            if (screenPixels[i] == body)
                isEnemyInShot = true;
            else if (screenPixels[i] == face)
                isFaceInShot = true;
        }

        if (!isEnemyInShot)
            return (0, "No enemies in shot");
        else if (!isFaceInShot)
            return (0, "Face not in shot");

        bool isBottomPixelChecked = false;
        bool isTopPixelChecked = false;

        //todo: DOESN'T ACCOUNT FOR LEFT and RIGHT CUTOFFS
        //the color block is read from the bottom left corner
        for (int i = 0; i < screenPixels.Length && !isTopPixelChecked; i++)
        {
            if (screenPixels[i] == face && !isBottomPixelChecked)
            {
                isBottomPixelChecked = true; 
                if (i - photoResult.width < 0)
                    return (0, "Face is cuttoff"); //below row doesn't exist
            }
            if (isBottomPixelChecked)
            {
                //run the function in reverse to get the bottom pixel
                //todo: too inefficient?
                for (int j = screenPixels.Length - 1; j > i && !isTopPixelChecked; j--)
                {
                    if (screenPixels[j] == face && !isTopPixelChecked)
                    {
                        isTopPixelChecked = true;
                        if (j + photoResult.width > screenPixels.Length - 1)
                            return (0, "Face is cuttoff"); //above row doesn't exist
                    } 
                }
            }
        }

        //top, middle, bottom / Left, Center, Right
        //tC is topCenter third of the photo
        Color[] tL = photoResult.GetPixels(0, 0, horizontalThird, verticalThird);
        Color[] tC = photoResult.GetPixels(horizontalThird, 0, horizontalThird, verticalThird);
        Color[] tR = photoResult.GetPixels(horizontalThird * 2, 0, horizontalThird, verticalThird);
        Color[] mL = photoResult.GetPixels(0, verticalThird, horizontalThird, verticalThird);
        Color[] mC = photoResult.GetPixels(horizontalThird, verticalThird, horizontalThird, verticalThird);
        Color[] mR = photoResult.GetPixels(horizontalThird * 2, verticalThird, horizontalThird, verticalThird);
        Color[] bL = photoResult.GetPixels(0, verticalThird * 2, horizontalThird, verticalThird);
        Color[] bC = photoResult.GetPixels(horizontalThird, verticalThird * 2, horizontalThird, verticalThird);
        Color[] bR = photoResult.GetPixels(horizontalThird * 2, verticalThird * 2, horizontalThird, verticalThird);

        CheckSubsection(tL, "corner", body, face);
        CheckSubsection(tR, "corner", body, face);
        CheckSubsection(bL, "corner", body, face);
        CheckSubsection(bR, "corner", body, face);
        CheckSubsection(tC, "edge", body, face);
        CheckSubsection(mL, "edge", body, face);
        CheckSubsection(mR, "edge", body, face);
        CheckSubsection(bC, "edge", body, face);
        CheckSubsection(mC, "center", body, face);

        //todo: face:body ratio??
        float faceToBodyRatioMultipler = facePixels * faceMultiplier;
        float sizingDamage = (faceToBodyRatioMultipler + bodyPixels) / (photoResult.width * photoResult.height);

        float centeringDamage = (centerPixels * centerMultiplier) + (edgePixels * edgeMultiplier) + (cornerPixels * cornerMultiplier);

        //damage equation
        float totalDamage = sizingDamage + centeringDamage;

        //todo: if this changes from static to nonstatic this can be removed 
        //it builds up without this reset
        cornerPixels = 0;
        edgePixels = 0;
        centerPixels = 0;
        bodyPixels = 0;
        facePixels = 0;

        return (totalDamage, "NICE!");
    }

    static void CheckSubsection (Color[] subsection, string subsectionType, Color body, Color face)
    {
        foreach (Color pixel in subsection)
        {
            if (pixel == body || pixel == face)
            {
                switch (subsectionType)
                {
                    case "edge":
                        edgePixels++;
                        break;
                    case "corner":
                        cornerPixels++;
                        break;
                    case "center":
                        centerPixels++;
                        break;
                }
                if (pixel == body)
                    bodyPixels++;
                else if (pixel == face)
                    facePixels++;
            }
        }
    }
}
