using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateToLocation : MonoBehaviour
{
    public Transform Target;
    public float Duration = 0.25f;
    public Action AnimationCompleteCallback;

    float startTime;
    Vector3 startPosition;

	void Start ()
    {
        startTime = Time.time;
        startPosition = transform.position;
	}
	
	void Update ()
    {
        float t = (Time.time - startTime) / Duration;
        t = t * t; //easing

		if(t < 1)
        {
            transform.position = Vector3.Lerp(startPosition, Target.position, t);
        }
        else
        {
            //We are done
            transform.position = Target.position;
            Destroy(this);
            if (AnimationCompleteCallback != null) AnimationCompleteCallback();
        }
	}
}
