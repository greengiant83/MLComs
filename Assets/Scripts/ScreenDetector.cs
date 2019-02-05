using MagicLeap;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnitySocketIO.Events;

public class ScreenDetector : MonoBehaviour
{
    public MeshingVisualizer MeshVisualizer;
    public Transform SpatialMapper;
    public GameObject ScreenPrefab;
    public GameObject ControlPanel;

    Vector3 seedPoint;
    Vector3 projectionPoint;
    Vector3 searchUp;
    Vector3 searchRight;
    Vector3 searchForward;
    Vector3 top, right, bottom, left;

    float sampleRate = 0.02f;
    float maxSpread = 2;

    MLHand hand { get { return MLHands.Right; } }
    FHand fhand { get { return FHands.Right; } }
    bool wasPinch;
    bool isScreenPlaced;
    string socketId;

    GameObject screen;
    
    private void Start()
    {
    }

    public void EnrollNewScreen(SocketIOEvent e)
    {
        //iPad: 6.5 x 9.5" 
        var msg = e.Parse();
        var data = msg.Data;

        socketId = msg.From;

        var isHandHeldEl = data.Element("isHandHeld");
        if (isHandHeldEl != null && isHandHeldEl.Value == "true")
        {
            //Is a hand held screen
            screen = Instantiate(ScreenPrefab);
            screen.transform.localScale = new Vector3(0.1651f, 0.2413f, .005f);
            screen.GetComponent<ScreenProxy>().IsHandHeld = true;
            screenReady();
        }
        else
        {
            //Static screen
            var poseData = data.Element("pose");
            if (poseData != null)
            {
                screen = Instantiate(ScreenPrefab);
                screen.transform.position = vectorFromXElement(poseData.Element("position"));
                screen.transform.rotation = Quaternion.Euler(vectorFromXElement(poseData.Element("rotation")));
                screen.transform.localScale = vectorFromXElement(poseData.Element("scale"));
                screenReady();
            }
            else
            {
                MeshVisualizer.SetRenderers(MeshingVisualizer.RenderMode.Wireframe);

                isScreenPlaced = false;
                ControlPanel.SetActive(false);
            }
        }
    }

    Vector3 vectorFromXElement(XElement el)
    {
        return new Vector3(
            float.Parse(el.Element("x").Value),
            float.Parse(el.Element("y").Value),
            float.Parse(el.Element("z").Value));
    }

    void screenReady()
    {
        MeshVisualizer.SetRenderers(MeshingVisualizer.RenderMode.Occlusion);
        screen.GetComponent<ScreenProxy>().OnReady(socketId);
        Destroy(screen.GetComponent<StretchAndPose>());
        screen = null;
        gameObject.SetActive(false);
    }

    public void OnDonePressed()
    {
        if (!gameObject.activeInHierarchy) return;

        screenReady();
    }

    void Update()
    {
        SpatialMapper.position = Camera.main.transform.position;

        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            findScreen(ray);
            afterScreenFound();
        }

        if (!isScreenPlaced && MLHands.IsStarted)
        {
            bool isPinch = fhand.KeyPose == MLHandKeyPose.Pinch || fhand.KeyPose == MLHandKeyPose.Fist || fhand.KeyPose == MLHandKeyPose.Ok;
            if(isPinch && !wasPinch)
            {
                var pinchPoint = Vector3.Lerp(hand.Index.Tip.Position, hand.Thumb.Tip.Position, 0.5f);
                var ray = new Ray(Camera.main.transform.position, pinchPoint - Camera.main.transform.position);
                findScreen(ray);
                afterScreenFound();                
            }
            wasPinch = isPinch;
        }
    }

    void afterScreenFound()
    {
        isScreenPlaced = true;
        ControlPanel.SetActive(true);
        ControlPanel.transform.position = screen.transform.position + (Vector3.up * (screen.transform.localScale.y / 2 + .3f));
        ControlPanel.transform.LookAt(Camera.main.transform.position);
    }

    void findScreen(Ray ray)
    {   
        RaycastHit hitInfo;
            
        if (Physics.Raycast(ray, out hitInfo))
        {            
            projectionPoint = Camera.main.transform.position;
            seedPoint = hitInfo.point;
            
            searchForward = seedPoint - projectionPoint;
            searchUp = Vector3.up;
            searchRight = Vector3.Cross(searchUp, searchForward);

            var normal = sampleNormals();
            screen = Instantiate(ScreenPrefab); 
            screen.transform.position = seedPoint;
            screen.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);

            var collider = screen.GetComponent<BoxCollider>();
            collider.enabled = false;

            top = searchForEdge(screen.transform.up);
            right = searchForEdge(screen.transform.right);
            bottom = searchForEdge(screen.transform.up * -1);
            left = searchForEdge(screen.transform.right * -1);

            var localRight = screen.transform.InverseTransformPoint(right);
            var localTop = screen.transform.InverseTransformPoint(top);
            var localBottom = screen.transform.InverseTransformPoint(bottom);
            var localLeft = screen.transform.InverseTransformPoint(left);
            var localCenter = new Vector3(Vector3.Lerp(localRight, localLeft, 0.5f).x, Vector3.Lerp(localTop, localBottom, 0.5f).y, 0);

            screen.transform.position = screen.transform.TransformPoint(localCenter);
            screen.transform.localScale = new Vector3((localRight - localLeft).magnitude, (localTop - localBottom).magnitude, 0.01f);
            collider.enabled = true;
        }
	}

    Vector3 searchForEdge(Vector3 direction)
    {
        float maxSearchDistance = 1;
        float offsetDistance = 0.2f;
        float distance = 0;
        float maxDeviation = 0.02f;
        Vector3 offset = offsetDistance * screen.transform.forward;
        Ray ray = new Ray();
        RaycastHit hitInfo;
        Vector3 lastGoodHit = Vector3.zero;
        
        ray.direction = screen.transform.forward * -1;
        for(float i=0; i<maxSearchDistance; i += sampleRate)
        {
            ray.origin = seedPoint + direction * i + offset;
            if (Physics.Raycast(ray, out hitInfo))
            {
                float currentDistance = hitInfo.distance - offsetDistance;
                if (distance == 0)
                {
                    distance = currentDistance;
                }
                else
                {
                    var delta = Mathf.Abs(distance - currentDistance);
                    if (delta > maxDeviation)
                        return lastGoodHit;

                    distance = Mathf.Lerp(distance, currentDistance, 0.5f);
                }
                
                lastGoodHit = hitInfo.point;
            }
            else return lastGoodHit;
        }
        return lastGoodHit;
    }

    Vector3 sampleNormals()
    {
        var ray = new Ray(projectionPoint, Vector3.forward);
        var searchDiameter = 0.1f;
        RaycastHit hitInfo;
        Vector3 normal = Vector3.zero;

        for (int i = 0; i < 10; i++)
        {
            ray.direction = (Random.insideUnitSphere * searchDiameter + seedPoint) - ray.origin;
            if(Physics.Raycast(ray, out hitInfo))
            {
                if(normal == Vector3.zero)
                    normal = hitInfo.normal;
                else
                    normal = Vector3.Lerp(normal, hitInfo.normal, 0.5f).normalized;
            }
        }

        return normal;
    }

    Vector3 getHitPoint(Vector3 targetPoint)
    {
        RaycastHit hitInfo;
        if(Physics.Raycast(new Ray(projectionPoint, targetPoint - projectionPoint), out hitInfo))
        {
            return hitInfo.point;
        }
        return Vector3.zero;
    }

    void placeMarker(Vector3 position, Color color, string name = "Marker", float scale = 1)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.position = position;
        item.transform.localScale = new Vector3(0.025f, 0.05f, 0.025f) * scale;
        item.transform.SetParent(this.transform);
        item.GetComponent<MeshRenderer>().material.color = color;
        DestroyImmediate(item.GetComponent<BoxCollider>());
    }

    void clearChildren()
    {
        List<GameObject> deadChildren = new List<GameObject>();
        foreach (Transform childTransform in transform)
        {
            deadChildren.Add(childTransform.gameObject);
        }

        foreach(var deadChild in deadChildren)
        {
            DestroyImmediate(deadChild);
        }
    }

}
