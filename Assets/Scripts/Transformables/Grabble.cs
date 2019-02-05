using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class Grabble : MonoBehaviour
{
    public bool Reorient = true;
    public bool KeepVertical = false;

    bool isGrabbing;
    HandCursor cursor;
    Transform originalParent;

    void Update()
    {
        if(isGrabbing)
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

    private void Cursor_CursorActivated(object sender, CursorEventArgs e)
    {
        cursor.CursorDeactivated += Cursor_CursorDeactivated;
        isGrabbing = true;
        originalParent = transform.parent;
        transform.SetParent(cursor.transform);
    }

    private void Cursor_CursorDeactivated(object sender, CursorEventArgs e)
    {
        cursor.CursorDeactivated -= Cursor_CursorDeactivated;
        isGrabbing = false;
        transform.SetParent(originalParent);
        originalParent = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Cursor")
        {
            cursor = other.gameObject.GetComponent<HandCursor>();
            if(cursor)
            {
                cursor.CursorActivated += Cursor_CursorActivated;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Cursor")
        {
            cursor = other.gameObject.GetComponent<HandCursor>();
            if (cursor)
            {
                cursor.CursorActivated -= Cursor_CursorActivated;
            }
        }
    }
}
