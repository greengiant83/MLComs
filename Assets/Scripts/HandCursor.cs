using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class CursorEventArgs : EventArgs
{
    public HandCursor Cursor;
    public CursorObserver Sender;
}

public class HandCursor : MonoBehaviour
{
    public event EventHandler<CursorEventArgs> CursorActivated;
    public event EventHandler<CursorEventArgs> CursorDeactivated;

    public MLHandType HandType;
    public GameObject Cursor;
    public Transform PinchPointObject;
    public MeshRenderer CursorRenderer;
    public Material ActiveMaterial;
    public Material InactiveMaterial;

    MLHand hand { get { return HandType == MLHandType.Left ? MLHands.Left : MLHands.Right; } }
    FHand fhand { get { return HandType == MLHandType.Left ? FHands.Left : FHands.Right; } }

    FVector cursorPosition = new FVector();
    FVector indexTip = new FVector();
    FVector thumbTip = new FVector();
    bool wasActive;
    float eyeToItemDistance;
    float eyeToPinchDistance;
    float pinchToItemDistance;
    int uiMask;

    void Start ()
    {
        uiMask = LayerMask.GetMask("UI");
	}
	
	void Update ()
    {
        if (!MLHands.IsStarted) return;
        if (!hand.Index.Tip.IsValid || !hand.Thumb.Tip.IsValid) return;
        if (hand.KeyPose == MLHandKeyPose.NoHand) return;
        bool isActive = fhand.KeyPose == MLHandKeyPose.Pinch || fhand.KeyPose == MLHandKeyPose.Fist || fhand.KeyPose == MLHandKeyPose.Ok;

        indexTip.Push(hand.Index.Tip.Position);
        thumbTip.Push(hand.Thumb.Tip.Position);

        if (isActive)
        {
            var pinchDistance = Vector3.Distance(indexTip.Value, thumbTip.Value);
            if (pinchDistance > 0.2) isActive = false;
        }

        if (isActive && !wasActive)
        {
            //Pinch start
            CursorRenderer.material = ActiveMaterial;
            if (CursorActivated != null) CursorActivated(this, new CursorEventArgs() { Sender = null, Cursor = this });
        }
        else if(wasActive && !isActive)
        {
            //Pinch end
            CursorRenderer.material = InactiveMaterial;
            if (CursorDeactivated != null) CursorDeactivated(this, new CursorEventArgs());
        }


        var pinchPoint = Vector3.Lerp(indexTip.Value, thumbTip.Value, 0.5f);
        if (!isActive)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(new Ray(Camera.main.transform.position, pinchPoint - Camera.main.transform.position), out hitInfo, 100, uiMask))
            {
                Cursor.transform.position = hitInfo.point;
                eyeToItemDistance = hitInfo.distance;
                pinchToItemDistance = Vector3.Distance(hitInfo.point, pinchPoint);
                eyeToPinchDistance = Vector3.Distance(Camera.main.transform.position, pinchPoint);
            }
            else
            {
                Cursor.transform.position = pinchPoint;
                pinchToItemDistance = 0;
            }
        }
        else
        {
            var v = (pinchPoint - Camera.main.transform.position).normalized;
            Cursor.transform.position = pinchPoint + v * pinchToItemDistance;
        }
        PinchPointObject.position = pinchPoint;
        wasActive = isActive;
        
	}
}
