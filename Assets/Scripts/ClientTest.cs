using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Protobuf;
using Yy.Protocol.App;
using Yy.Protocol.Core;

public class ClientTest : MonoBehaviour
{
    private Socket sock;
    public  string serverIP = "127.0.0.1";
    public  ushort serverPort = 16666;
    private byte[] buffer = new byte[1024];


    void Start()
    {
        //sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //sock.Connect(serverIP, serverPort);
        //if (sock.Connected)
        //{
        //    StartRecv();
        //}
    }

    void StartRecv()
    {
        sock.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, RecvCallback, null);
    }

    void RecvCallback(IAsyncResult result)
    {
        int len = sock.EndReceive(result);
        if (len == 0) return;
        string str = Encoding.UTF8.GetString(buffer, 0, len);
        Debug.Log(str);
    }

    void Update()
    {
    }
}
