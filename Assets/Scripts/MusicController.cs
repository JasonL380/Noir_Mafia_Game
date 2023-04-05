// Name: Jason Leech
// Date: 04/05/2023
// Desc:

using System;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DefaultNamespace
{
    public class MusicController : MonoBehaviour
    {
        private AudioSource _source;

        public AudioClip DefaultClip;

        public LayerMask MusicAreaLayer;

        public GameObject player;
        
        private void Start()
        {
            _source = GetComponent<AudioSource>();
            _source.clip = DefaultClip;
            _source.Play();
        }
        
        private void OnTriggerEnter2D(Collider2D col)
        {
            MusicArea area = col.gameObject.GetComponent<MusicArea>();

            if (area != null)
            {
                _source.Stop();
                _source.clip = area.clip;
                _source.Play();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Collider2D collider2D = Physics2D.OverlapPoint(transform.position, MusicAreaLayer);

            if (collider2D != null)
            {
                _source.Stop();
                _source.clip = collider2D.GetComponent<MusicArea>().clip;
                _source.Play();
            }
            else
            {
                _source.Stop();
                _source.clip = DefaultClip;
                _source.Play();
            }
        }
    }
}