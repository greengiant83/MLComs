using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnePointMove : MonoBehaviour
{
    public Transform PointA;
    public bool Reorient = true;
    public bool KeepVertical = false;

    Transform originalParent;
    
	void Start ()
    {
        originalParent = transform.parent;
        transform.parent = PointA;
	}

    private void OnDestroy()
    {
        transform.parent = originalParent;
    }

    void Update ()
    {
        if (Reorient)
        {
            var v = Camera.main.transform.position - transform.position;
            if (KeepVertical) v.y = 0;
            var rot = Quaternion.LookRotation(v);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 0.1f);
        }
    }
}
