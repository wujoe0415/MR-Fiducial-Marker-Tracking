using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(AnacondaStarter))]
public class TagPredictionReciver : MonoBehaviour
{
    private Action<double[]> _onTagRecieve;
    private Action _onCloseServer;
    private static int _port = 5555;
    private ConcurrentQueue<string> _terminateConnectionSignal = 
        new ConcurrentQueue<string>();
    private ConcurrentQueue<double[]> _predictMarkers = 
        new ConcurrentQueue<double[]>();
    private Thread _connection;
    
    public void AddTagRecieve(Action<double[]> action)
    {
        _onTagRecieve += action;
    }
    
    public void RemoveTagRecieve(Action<double[]> action)
    {
        if(_onTagRecieve != null)
        {
            _onTagRecieve -= action;
        }
    }

    public void AddCloseServer(Action action)
    {
        _onCloseServer += action;
    }
    public void RemoveCloseServer(Action action)
    {
        _onCloseServer -= action;
    }
    private void Start()
    {
        var loopbackAddress = Socket.OSSupportsIPv6 ? 
            IPAddress.Parse("::1") :
            IPAddress.Loopback;

        _connection = new Thread(CreateConnection);
        _connection.Start(_port);
    }

    private void Update()
    {
        if (_predictMarkers.TryDequeue(out double[] markers))
        {
            if (_onTagRecieve == null)
                return;

            _onTagRecieve?.Invoke(markers);
        }
    }

    private void CreateConnection(object port)
    {
        TcpListener listener;
        var loopbackAddress = Socket.OSSupportsIPv6 ? 
            IPAddress.Parse("::1") :
            IPAddress.Loopback;

        listener = new TcpListener(loopbackAddress, (int)port);

        try
        {
            Socket server;
            int socketByteCount;
            byte[] msg = new byte[65535];
            
            listener.Start();
            Debug.Log("Listener Start");

            while(true)
            {
                if (_terminateConnectionSignal.TryDequeue(out string signal))
                {
                    Debug.Log("Get stop signal");
                    return;
                }

                if (listener.Pending())
                {
                    server = listener.AcceptSocket();
                    break;
                }
            }
            
            while(true)
            {
                if (_terminateConnectionSignal.TryDequeue(out string signal))
                {
                    Debug.Log("Get stop signal");
                    break;
                }

                socketByteCount = server.Receive(msg, SocketFlags.None);

                if (socketByteCount <= 8)
                {
                    continue;
                }

                var markers = new double[socketByteCount/8];

                Buffer.BlockCopy(msg, 0, markers, 0, socketByteCount);
                _predictMarkers.Enqueue(markers);
            }
        }
        catch(SocketException e)
        {
            Debug.Log(String.Format("SocketException: {0}", e));
        }
        finally
        {
            Debug.Log("Server Stop");

            if (listener.Server.IsBound)
            {
                listener.Stop();
                Debug.Log("Stop active listener");
            }   
        }
    }

    private void OnDisable()
    {
        _terminateConnectionSignal.Enqueue("Stop");
        _connection.Join();
        _onCloseServer?.Invoke();
    }   
}
