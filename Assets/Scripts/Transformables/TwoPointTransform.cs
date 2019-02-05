using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoPointTransform : MonoBehaviour
{
    public ScaleModeEnum ScaleMode;
    public Transform PointA;
    public Transform PointB;

    Transform originalParent;
    Transform center;
    Vector3 originalScale;
    Vector3 scaleAxis;
    float originalDistance;

    void Start ()
    {
        center = new GameObject("center").transform;

        var localA = transform.InverseTransformPoint(PointA.position);
        var localB = transform.InverseTransformPoint(PointB.position);

        scaleAxis = (localB - localA).normalized;
        scaleAxis.x = Mathf.Abs(scaleAxis.x);
        scaleAxis.y = Mathf.Abs(scaleAxis.y);
        scaleAxis.z = Mathf.Abs(scaleAxis.z);

        originalDistance = Vector3.Distance(PointA.position, PointB.position);
        originalScale = transform.localScale;
        originalParent = transform.parent;

        Update();

        transform.SetParent(center);
    }

    private void OnDestroy()
    {
    }

    void Update ()
    {
        center.position = Vector3.Lerp(PointA.position, PointB.position, 0.5f);
        center.LookAt(PointA);

        //Update scale
        if (ScaleMode != ScaleModeEnum.None)
        {
            var newDistance = Vector3.Distance(PointA.position, PointB.position);
            var distanceDelta = newDistance - originalDistance;
            var newScale = originalScale;
            var deltaV = distanceDelta * Vector3.one;
            if (ScaleMode == ScaleModeEnum.Stretch || ScaleMode == ScaleModeEnum.StretchXY)
            {
                deltaV.Scale(scaleAxis);
                if(ScaleMode == ScaleModeEnum.StretchXY) deltaV.z = 0; //Dont allow any stretching on the z axis
            }

            var divideByOriginal = new Vector3(
                1 / originalScale.x,
                1 / originalScale.y,
                1 / originalScale.z);
            deltaV.Scale(divideByOriginal);
            

            deltaV += Vector3.one;
            newScale.Scale(deltaV);
            transform.localScale = newScale;
        }
    }
}
