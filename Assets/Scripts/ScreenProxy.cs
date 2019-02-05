using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnitySocketIO.Events;

[RequireComponent(typeof(CursorObserver))]
public class ScreenProxy : MonoBehaviour
{
    public GameObject ContentLoaderPrefab;
    public Material ReadyMaterial;
    public string SocketId;
    public bool IsHandHeld;

    MeshRenderer renderer;
    CursorObserver cursorObserver;
    HandCursor activeCursor;
    HandCursor disabledCursor;

    XElement elXPos = new XElement("x");
    XElement elYPos = new XElement("y");
    XElement elZPos = new XElement("z");
    XElement elData = new XElement("d");
    
    bool isReady;
    
    void Start ()
    {
        renderer = GetComponent<MeshRenderer>();
        cursorObserver = GetComponent<CursorObserver>();
        elData.Add(elXPos);
        elData.Add(elYPos);
        elData.Add(elZPos);
    }

    private void OnDestroy()
    {
        if (disabledCursor != null) disabledCursor.gameObject.SetActive(true);
        SocketController.Instance.io.Off("drop", onScreenDrop); //unsubscribe
    }

    void CursorHoverEnter(CursorEventArgs e)
    {
        SocketController.Instance.Send("cursorenter", null, SocketId);
    }

    void CursorHoverExit(CursorEventArgs e)
    {
        SocketController.Instance.Send("cursorexit", null, SocketId);
    }

    void Update()
    {
        if (IsHandHeld && MLHands.IsStarted)
        {
            if (FHands.Left.KeyPose == MLHandKeyPose.NoHand)
            {
                renderer.enabled = false;
                return;
            }
            else renderer.enabled = true;
            
            float height = (Camera.main.transform.position - MLHands.Left.Center).y;
            float tilt = height.Remap(0, 0.4f, 0, -30, true); //values discovered experimentally

            transform.LookAt(Camera.main.transform);
            transform.Rotate(Vector3.right, tilt, Space.Self);
            transform.position = MLHands.Left.Wrist.Center.Position + transform.forward * -0.02f + transform.up * (transform.localScale.y / 2) + transform.right * -(transform.localScale.x / 2); //add in offset from being held at the corner
        }

        if (cursorObserver.HoverCursors.Count > 0)
        {
            var cursor = cursorObserver.HoverCursors[0];
            var localPos = transform.InverseTransformPoint(cursor.Cursor.transform.position);
            elXPos.Value = localPos.x.ToString("0.000");
            elYPos.Value = localPos.y.ToString("0.000");
            elZPos.Value = (localPos.z * transform.localScale.z).ToString("0.000");
            SocketController.Instance.Send("cursor", elData, SocketId);
        }

        
    }

    public void OnReady(string SocketId)
    {
        this.SocketId = SocketId;
        GetComponent<MeshRenderer>().material = ReadyMaterial;

        var data = new XElement("root",
            new XElement("pose", 
                new XElement("position",
                    new XElement("x", transform.position.x.ToString("0.000")),
                    new XElement("y", transform.position.y.ToString("0.000")),
                    new XElement("z", transform.position.z.ToString("0.000"))),
                new XElement("rotation",
                    new XElement("x", transform.rotation.eulerAngles.x.ToString("0.000")),
                    new XElement("y", transform.rotation.eulerAngles.y.ToString("0.000")),
                    new XElement("z", transform.rotation.eulerAngles.z.ToString("0.000"))),
                new XElement("scale",
                    new XElement("x", transform.localScale.x.ToString("0.000")),
                    new XElement("y", transform.localScale.y.ToString("0.000")),
                    new XElement("z", transform.localScale.z.ToString("0.000")))));

        SocketController.Instance.Send("enrolled", data, SocketId);
        SocketController.Instance.io.On("drop", onScreenDrop);

        if(IsHandHeld)
        {
            var cursors = FindObjectsOfType<HandCursor>();
            foreach(var cursor in cursors)
            {
                if(cursor.HandType == UnityEngine.XR.MagicLeap.MLHandType.Left)
                {
                    disabledCursor = cursor;
                    disabledCursor.gameObject.SetActive(false);
                    break;
                }
            }
        }

        isReady = true;
    }

    void onScreenDrop(SocketIOEvent e)
    {
        var msg = e.Parse();
        if (msg.From == SocketId) Destroy(this.gameObject);
    }

    

    void CursorActivate(CursorEventArgs e)
    {
        if (!isReady) return;

        activeCursor = e.Cursor;

        var loader = Instantiate(ContentLoaderPrefab);
        loader.transform.position = e.Cursor.Cursor.transform.position;
        loader.transform.LookAt(Camera.main.transform);
        loader.GetComponent<ContentLoader>().RequestContent(SocketId, activeCursor);
    }

    void CursorDeactivate(CursorEventArgs e)
    {
        activeCursor = null;
    }
}

public static class ExtensionMethods
{

    public static float Remap(this float value, float from1, float to1, float from2, float to2, bool isClamped)
    {
        var v = (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        if (isClamped)
        {
            if(to2 > from2)
                v = Mathf.Clamp(v, from2, to2);
            else
                v = Mathf.Clamp(v, to2, from2);
        }
        return v;
    }

}