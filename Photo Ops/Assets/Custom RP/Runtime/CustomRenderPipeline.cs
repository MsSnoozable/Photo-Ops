using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {

        //instantiate a cameraRenderer that will store the context and camera for each camera in the scene
        CameraRenderer CR = new CameraRenderer();

        foreach (Camera cameraElement in cameras)
        {

            CR.Render(context, cameraElement);
        }
    }
}
