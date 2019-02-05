using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnitySocketIO.Events;

public class ContentLoader : MonoBehaviour
{
    public GameObject NotePrefab;
    public GameObject Indicator;

    GameObject spawnedItem;
    HandCursor cursor;

    static int requestCount = 0;
    string requestKey;

    void Start()
    {
        //Invoke("mock", 1);
    }

    private void OnDestroy()
    {
        SocketController.Instance.io.Off("content", onContentArrived);
    }

    void mock()
    {
        Debug.Log("Requesting content");
        RequestContent(null, null);
    }

    public void RequestContent(string SocketId, HandCursor Cursor)
    {
        this.cursor = Cursor;

        requestCount = (requestCount + 1) % 1000;
        requestKey = requestCount.ToString();

        var data = new XElement("root", new XElement("key", requestKey));
        SocketController.Instance.io.On("content", onContentArrived);
        SocketController.Instance.Send("query", data, SocketId);

        if (cursor != null) cursor.CursorDeactivated += Cursor_CursorDeactivated;
    }

    private void Cursor_CursorDeactivated(object sender, CursorEventArgs e)
    {
        if (spawnedItem != null)
        {
            Destroy(spawnedItem.GetComponent<OnePointMove>());
        }
        cursor = null;
    }

    void onContentArrived(SocketIOEvent e)
    {
        var msg = e.Parse();
        var data = msg.Data;

        var responseKey = data.Element("key").Value;
        if (responseKey != requestKey) return; //This response is not for us

        Destroy(Indicator);

        var type = data.Element("type").Value;
        SocketController.Instance.io.Off("content", onContentArrived); 

        switch (type)
        {
            case "image":
                spawnedItem = spawnImage(data.Element("img").Value);
                break;
            case "text":
                break;
        }

        if (spawnedItem != null)
        {
            spawnedItem.transform.position = transform.position;
            spawnedItem.transform.rotation = transform.rotation;
            var animator = spawnedItem.AddComponent<AnimateToLocation>();

            if (cursor != null)
            {
                animator.Target = cursor.PinchPointObject;
                animator.AnimationCompleteCallback = () =>
                {
                    var mover = spawnedItem.AddComponent<OnePointMove>();
                    mover.PointA = cursor.PinchPointObject;
                };
            }
            else
            {
                var targetPlace = new GameObject();
                targetPlace.transform.position = transform.position + transform.up * 0.5f + Random.insideUnitSphere * 0.1f;
                animator.Target = targetPlace.transform;
                animator.AnimationCompleteCallback = () =>
                {
                    Destroy(targetPlace);
                };
            }
        }

        Destroy(this.gameObject); //Our job is done.
    }

    GameObject spawnImage(string imageData)
    {
        var item = Instantiate(NotePrefab);

        imageData = imageData.Substring(imageData.IndexOf(",") + 1); //trim off the data url prefix "data:image/png;base64,"

        byte[] bytes = System.Convert.FromBase64String(imageData);

        var tex = new Texture2D(1, 1);
        var renderer = item.GetComponent<MeshRenderer>();
        tex.LoadImage(bytes);
        tex.Apply();

        renderer.material.mainTexture = tex;

        var scale = item.transform.localScale;
        scale.y = scale.x * tex.height / tex.width;
        item.transform.localScale = scale;

        return item;
    }
}
