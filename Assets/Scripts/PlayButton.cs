// Name: Jason Leech
// Date: 03/29/2023
// Desc:

using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefaultNamespace
{
    public class PlayButton : MonoBehaviour
    {
        public void play()
        {
            SceneManager.LoadScene("Opening Cutscene");
        }
    }
}