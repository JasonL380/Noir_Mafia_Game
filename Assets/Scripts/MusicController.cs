// Name: Jason Leech
// Date: 04/05/2023
// Desc:

using UnityEngine;

namespace DefaultNamespace
{
    public class MusicController : MonoBehaviour
    {
        private AudioSource[] _source;

        public AudioClip DefaultClip;

        public AudioClip chaseMusic;
        
        public LayerMask musicAreaLayer;

        public PlayerMovement player;

        public float fadeTime;
        
        private int currentSource;

        private bool chaseMusicPlaying = false;
        
        private void Start()
        {
            _source = GetComponents<AudioSource>();
            _source[0].clip = DefaultClip;
            _source[0].Play();
        }

        public void Update()
        {
            if (_source[currentSource].volume < 0.99)
            {
                _source[currentSource].volume += (1 / fadeTime) * Time.deltaTime;
            }

            if (_source[(currentSource == 1 ? 0 : 1)].volume > 0.01)
            {
                _source[(currentSource == 1 ? 0 : 1)].volume -= (1 / fadeTime) * Time.deltaTime;
            }
            else if(_source[(currentSource == 1 ? 0 : 1)].isPlaying)
            {
                _source[(currentSource == 1 ? 0 : 1)].Stop();
            }

            if (player.chased && !chaseMusicPlaying)
            {
                chaseMusicPlaying = true;
                switch_music(chaseMusic);
            }
            else if (!player.chased && chaseMusicPlaying)
            {
                chaseMusicPlaying = false;
                Collider2D collider2D = Physics2D.OverlapPoint(transform.position, musicAreaLayer);

                if (collider2D != null)
                {
                    switch_music(collider2D.GetComponent<MusicArea>().clip);
                }
                else
                {
                    switch_music(DefaultClip);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!player.chased)
            {
                MusicArea area = col.gameObject.GetComponent<MusicArea>();

                if (area != null)
                {
                    switch_music(area.clip);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!player.chased)
            {
                if (other.GetComponent<MusicArea>() != null)
                {
                    Collider2D collider2D = Physics2D.OverlapPoint(transform.position, musicAreaLayer);

                    if (collider2D != null)
                    {
                        switch_music(collider2D.GetComponent<MusicArea>().clip);
                    }
                    else
                    {
                        switch_music(DefaultClip);
                    }
                }
            }
        }

        private void switch_music(AudioClip clip)
        {
            currentSource = (currentSource == 1 ? 0 : 1);

            _source[currentSource].clip = clip;
            _source[currentSource].Play();
        }
    }
}