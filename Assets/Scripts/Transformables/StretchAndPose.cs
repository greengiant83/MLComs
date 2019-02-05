using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ScaleModeEnum
{
    None,
    Uniform,
    Stretch,
    StretchXY
}

[RequireComponent(typeof(CursorObserver))]
public class StretchAndPose : MonoBehaviour
{
    public bool ReorientOnMove = true;
    public bool KeepVertical = false;
    public ScaleModeEnum ScaleMode = ScaleModeEnum.Uniform;

    OnePointMove onePointComp;
    TwoPointTransform twoPointComp;
    
    void addOnePoint(Transform PointA)
    {
        onePointComp = gameObject.AddComponent<OnePointMove>();
        onePointComp.PointA = PointA;
        onePointComp.Reorient = ReorientOnMove;
        onePointComp.KeepVertical = KeepVertical;
    }

    void addTwoPoint(Transform PointA, Transform PointB)
    {
        twoPointComp = gameObject.AddComponent<TwoPointTransform>();
        twoPointComp.PointA = PointA;
        twoPointComp.PointB = PointB;
        twoPointComp.ScaleMode = ScaleMode;
    }

    void CursorActivate(CursorEventArgs e)
    {
        switch (e.Sender.ActiveCursors.Count)
        {
            case 1:
                addOnePoint(e.Sender.ActiveCursors[0].transform);
                break;
            case 2:
                DestroyImmediate(onePointComp);
                addTwoPoint(e.Sender.ActiveCursors[0].transform, e.Sender.ActiveCursors[1].transform);
                break;
        }
    }

    void CursorDeactivate(CursorEventArgs e)
    {
        switch (e.Sender.ActiveCursors.Count)
        {
            case 1:
                DestroyImmediate(twoPointComp);
                addOnePoint(e.Sender.ActiveCursors[0].transform);
                break;
            case 0:
                DestroyImmediate(onePointComp);
                break;
        }
    }

}
