using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorObserver : MonoBehaviour
{
    public List<HandCursor> ActiveCursors = new List<HandCursor>();
    public List<HandCursor> HoverCursors = new List<HandCursor>();

    private void OnDisable()
    {
        unsubscribe();
    }

    private void OnDestroy()
    {
        unsubscribe();
    }

    void unsubscribe()
    {
        foreach (var cursor in HoverCursors) cursor.CursorActivated -= Cursor_CursorActivated;
        foreach (var cursor in ActiveCursors) cursor.CursorDeactivated -= Cursor_CursorDeactivated;
        ActiveCursors.Clear();
        HoverCursors.Clear();
    }

    private void Cursor_CursorActivated(object sender, CursorEventArgs e)
    {
        var cursor = sender as HandCursor;
        cursor.CursorDeactivated += Cursor_CursorDeactivated;

        ActiveCursors.Add(cursor);

        SendMessage("CursorActivate", new CursorEventArgs() { Sender = this, Cursor = cursor }, SendMessageOptions.DontRequireReceiver);
    }

    private void Cursor_CursorDeactivated(object sender, CursorEventArgs e)
    {
        var cursor = sender as HandCursor;
        cursor.CursorDeactivated -= Cursor_CursorDeactivated;

        ActiveCursors.Remove(cursor);

        SendMessage("CursorDeactivate", new CursorEventArgs() { Sender = this, Cursor = cursor }, SendMessageOptions.DontRequireReceiver);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Cursor")
        {
            var cursor = other.gameObject.GetComponent<HandCursor>();
            if (cursor)
            {
                cursor.CursorActivated += Cursor_CursorActivated;
                HoverCursors.Add(cursor);
                SendMessage("CursorHoverEnter", new CursorEventArgs() { Sender = this, Cursor = cursor }, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Cursor")
        {
            var cursor = other.gameObject.GetComponent<HandCursor>();
            if (cursor)
            {
                cursor.CursorActivated -= Cursor_CursorActivated;
                HoverCursors.Remove(cursor);
                SendMessage("CursorHoverExit", new CursorEventArgs() { Sender = this, Cursor = cursor }, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
