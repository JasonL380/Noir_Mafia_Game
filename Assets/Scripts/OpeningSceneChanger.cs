// Name: Jason Leech
// Date: 04/05/2023
// Desc: Timer to load the play scene after the cutscene

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefaultNamespace
{
    public class OpeningSceneChanger : MonoBehaviour
    {
        private float time = 33.8f;

        private void Update()
        {
            time -= Time.deltaTime;
            if (time < 0 || Input.GetKey(KeyCode.Escape))
            {
                SceneManager.LoadScene("PlayScene");
            }
        }
    }
}