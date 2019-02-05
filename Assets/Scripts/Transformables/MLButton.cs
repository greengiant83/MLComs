using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CursorObserver))]
public class MLButton : MonoBehaviour
{
    public UnityEvent OnClick;

    Vector3 restingScale;
    Vector3 restingPosition;

    Vector3 targetScale;

    void Start()
    {
        restingScale = targetScale = transform.localScale;
        restingPosition = transform.localPosition;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 0.1f);
    }

    void CursorHoverEnter(CursorEventArgs e)
    {
        targetScale = restingScale * 1.25f;
    }

    void CursorHoverExit(CursorEventArgs e)
    {
        targetScale = restingScale;
    }

    void CursorActivate(CursorEventArgs e)
    {
        transform.localPosition = restingPosition + transform.forward * -1 * 0.05f;
    }

    void CursorDeactivate(CursorEventArgs e)
    {
        transform.localPosition = restingPosition;
        OnClick.Invoke();
    }

    
    //HandCursor cursor;

    //private void Cursor_CursorActivated(object sender, CursorEventArgs e)
    //{
    //    OnClick.Invoke();
    //}

    //private void OnDisable()
    //{
    //    if (cursor) cursor.CursorActivated -= Cursor_CursorActivated;
    //}

    //private void OnDestroy()
    //{
    //    if (cursor) cursor.CursorActivated -= Cursor_CursorActivated;
    //}

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.tag == "Cursor")
    //    {
    //        cursor = other.gameObject.GetComponent<HandCursor>();
    //        if (cursor) cursor.CursorActivated += Cursor_CursorActivated;
    //        this.Log("Subscribed MLButton");
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.gameObject.tag == "Cursor")
    //    {
    //        cursor = other.gameObject.GetComponent<HandCursor>();
    //        if (cursor) cursor.CursorActivated -= Cursor_CursorActivated;
    //        this.Log("Unsubscribing MLButton");
    //    }
    //}
}
