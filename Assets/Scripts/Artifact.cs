// Name: Jason Leech
// Date: 03/21/2023
// Desc:

using UnityEngine;

namespace DefaultNamespace
{
    public enum Artifact_Type
    {
        Aztec = 0,
        Islamic = 1,
        Japanese = 2,
        Renaissance = 3
    }
    
    public class Artifact : MonoBehaviour
    {
        public Artifact_Type type;
    }
}