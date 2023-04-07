// Name: Jason Leech
// Date: 03/29/2023
// Desc:

using System;
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

        private void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
    }
}