// Name: Jason Leech
// Date: 03/21/2023
// Desc:

using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Player
{
    public class ArtifactCollector : MonoBehaviour
    {
        public bool[] artifacts = {false, false, false, false};

        private void OnTriggerEnter2D(Collider2D col)
        {
            Artifact artifact = col.gameObject.GetComponent<Artifact>();
            if (artifact != null)
            {
                artifacts[(int) artifact.type] = true;
                Destroy(artifact.gameObject);
            }
        }
    }
}