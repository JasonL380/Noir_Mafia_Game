// Name: Jason Leech
// Date: 04/05/2023
// Desc:

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace DefaultNamespace
{
    public class MinimapController : MonoBehaviour
    {
        public RenderTexture map;

        private void Start()
        {
            //new RenderTexture()
            map = new RenderTexture((int) (Camera.main.pixelWidth * 0.175),
                (int) (Camera.main.pixelHeight * 0.2722222), GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.None); //  56 * 49
            map.filterMode = FilterMode.Trilinear;
            GameObject.FindWithTag("MinimapCam").GetComponent<Camera>().targetTexture = map;
            GetComponent<MeshRenderer>().material.mainTexture = map;
        }
    }
}