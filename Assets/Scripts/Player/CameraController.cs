/*
 * Name: Jason Leech
 * Date: 9/28/22
 * Desc: Camera that will follow a target smoothly and has screen shake available (add to main camera object)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class CameraController : MonoBehaviour
{
    [Tooltip("Will center this object")]
    public GameObject target;
    [Tooltip("Keep between 0 and 1, the closer to 1 the faster it centers"), Range(0,1)]
    public float smoothVal = 0.5f;
    
    //screen shake variables
    [Tooltip("Time to shake if you want it t start immediately"), Min(0)]
    private static float shakeDuration = 0;

    [Tooltip("How violently to shake if shaking initially"), Min(0)]
    private static float shakeMag = 0;

    private static float startShakeDuration;
    
    // Start is called before the first frame update
    void Start()
    {
        startShakeDuration = shakeDuration;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //make sure target exists
        if (target != null)
        {
            //grab the target location
            Vector3 targetPos = target.transform.position;
            
            //adjust the z value correctly
            targetPos.z = transform.position.z;

            //screen shake effect stuff
            if (shakeDuration > 0)
            {
                shakeDuration -= Time.fixedDeltaTime;
                //setup a random shake amount
                Vector2 randShake = Random.insideUnitCircle *
                                    Mathf.Lerp(shakeMag, 0, 1 - (shakeDuration / startShakeDuration));

                transform.position += (Vector3) randShake;
            }
            
            //move towards that position each fixed update
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothVal);
        }
    }
    
    //call this function to start the screen shake
    public static void StartShake(float duration, float magnitude)
    {
        //only set if greater than previous values
        if (duration > shakeDuration)
        {
            shakeDuration = duration;
            startShakeDuration = duration;
        }

        if (magnitude > shakeMag)
        {
            shakeMag = magnitude;
        }
    }
}
