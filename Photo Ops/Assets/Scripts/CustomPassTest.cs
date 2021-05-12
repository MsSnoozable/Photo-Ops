using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class CustomPassTest : CustomPass
{
    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    public Camera DSLR_cam;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) {

    }
    static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/carbons/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Executed every frame for all the camera inside the pass volume.
        // The context contains the command buffer to use to enqueue graphics commands.

    }

    //todo: make sure "cleanup" works properly because memory leaks https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@10.4/manual/Custom-Pass-Scripting.html
    protected override void Cleanup() {

        int resolutionWidth = Screen.width;
        int resolutionHeight = Screen.height;

        RenderTexture rt = new RenderTexture(resolutionWidth, resolutionHeight, 24);
        DSLR_cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        DSLR_cam.Render();
        RenderTexture.active = rt;

        screenShot.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);

        //needs to be conditional based on viewfinder state
        DSLR_cam.targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors

        //Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();
        string filename = ScreenShotName(resolutionWidth, resolutionHeight);
        System.IO.File.WriteAllBytes(filename, bytes);

        //TextureReading.
    }

}