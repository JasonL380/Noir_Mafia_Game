using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class AnimatableSound : MonoBehaviour
    {
        public AudioClip sound;

        public bool playSound;

        private bool played = false;
        
        private AudioSource _source;

        private void Start()
        {
            _source = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (playSound && !played)
            {
                played = true;
                PlaySound();
            }
            else if(!playSound && played)
            {
                played = false;
            }
        }

        public void PlaySound()
        {
            _source.PlayOneShot(sound);
        }
    }
}