using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySocketIO;
using UnitySocketIO.Events;

public class SocketController : MonoBehaviour
{
    public static SocketController Instance;
    public ScreenDetector ScreenDetector;
    public SocketIOController io;

    public void Log(string msg, string socketId = null)
    {
        if (socketId == null) socketId = "";

        string data = "{ \"msg\": " + JsonConvert.ToString(msg);
        if (!string.IsNullOrEmpty(socketId)) data += ", \"socketId\": \"" + socketId + "\"";
        data += "}";
        Debug.Log("Socket Log: " + data);
        io.Emit("log", data);
    }

    public void Send(string command, string data)
    {
        io.Emit(command, data);
    }

    public void Send(string command, XElement data, string SocketId = null)
    {
        if (data == null) data = new XElement("root");
        if (!string.IsNullOrEmpty(SocketId)) addSocketId(data, SocketId);
        var json = JsonConvert.SerializeXNode(data, Formatting.None, true);
        Send(command, json);
    }

    void addSocketId(XElement el, string id)
    {
        var elTo = el.Element("to");
        if (elTo != null)
            elTo.Value = id;
        else
            el.Add(new XElement("to", id));
    }

    void Start ()
    {
        Instance = this;
        io = FindObjectOfType<SocketIOController>();

        io.On("connect", e =>
        {
            //We just connected to the socket server
            Debug.Log("Connected");
            io.Emit("vieweronline");
        });

        io.On("reset", e =>
        {
            SceneManager.LoadScene(0);
        });

        io.On("enroll", e =>
        {
            ScreenDetector.gameObject.SetActive(true);
            ScreenDetector.EnrollNewScreen(e);
        });

        io.Connect();
	}

    string stripQuotes(string data)
    {
        //String data from socketIO comes across enclosed in double quotes. This strips them off
        return data.Substring(1, data.Length - 2);
    }
}

public class SocketMessage
{
    public string From;
    public XElement Data;
}

public static class SocketHelper
{
    public static void Log(this MonoBehaviour item, string msg, string socketId = null)
    {
        SocketController.Instance.Log(msg, socketId);
    }

    public static SocketMessage Parse(this SocketIOEvent e)
    {
        if (!string.IsNullOrEmpty(e.data))
        {
            var msg = new SocketMessage();
            msg.Data = JsonConvert.DeserializeXNode(e.data, "Root").Root;
            msg.From = msg.Data.Element("from").Value;
            return msg;
        }
        else return null;
    }
}