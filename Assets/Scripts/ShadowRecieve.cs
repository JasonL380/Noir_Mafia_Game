using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class ShadowRecieve : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<SpriteRenderer>().receiveShadows = true;
        }
    }
}