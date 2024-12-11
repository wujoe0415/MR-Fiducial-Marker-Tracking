using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

using Unity.XR.Oculus;

[RequireComponent(typeof(AnacondaStarter))]
public class MarkerPredictionReciver : MonoBehaviour
{
    private Action<double[]> _onTagRecieve;
    private Action _onCloseServer;
    private static int _port = 5555;
    private ConcurrentQueue<string> _terminateConnectionSignal = 
        new ConcurrentQueue<string>();
    private ConcurrentQueue<double[]> _predictMarkers = 
        new ConcurrentQueue<double[]>();

    private Thread _connection;
    private Socket _server;

    byte[] _hmdBuffer = new byte[88];
    private Transform _hmdTransform;

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
        _hmdTransform = Camera.main.transform;

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
        Debug.Log(OVRNodeStateProperties.IsHmdPresent());
        if (OVRNodeStateProperties.IsHmdPresent() && _server != null && _server.Connected)
        {
            SendHMDData();
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
                    _server = listener.AcceptSocket();
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

                // Receive the response from the server
                socketByteCount = _server.Receive(msg, SocketFlags.None);
                if (socketByteCount <= 8)
                    continue;
                
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

    private void SendHMDData()
    {
        try
        {
            int offset = 0;

            // Pack the message length (80 bytes for 10 doubles)
            BitConverter.GetBytes((long)88).CopyTo(_hmdBuffer, offset);
            offset += 8;

            // Pack timestamp
            BitConverter.GetBytes((double)Time.timeAsDouble).CopyTo(_hmdBuffer, offset);
            offset += 8;

            // Pack position
            Vector3 position = _hmdTransform.position;
            BitConverter.GetBytes((double)position.x).CopyTo(_hmdBuffer, offset);
            offset += 8;
            BitConverter.GetBytes((double)position.y).CopyTo(_hmdBuffer, offset);
            offset += 8;
            BitConverter.GetBytes((double)position.z).CopyTo(_hmdBuffer, offset);
            offset += 8;

            // Pack velocity
            Vector3 velocity = Vector3.zero;
            OVRNodeStateProperties.GetNodeStatePropertyVector3(UnityEngine.XR.XRNode.Head, NodeStatePropertyType.Velocity, OVRPlugin.Node.EyeCenter, OVRPlugin.Step.Render, out velocity);
            BitConverter.GetBytes((double)velocity.x).CopyTo(_hmdBuffer, offset);
            offset += 8;
            BitConverter.GetBytes((double)velocity.y).CopyTo(_hmdBuffer, offset);
            offset += 8;
            BitConverter.GetBytes((double)velocity.z).CopyTo(_hmdBuffer, offset);
            offset += 8;

            // Pack angular velocity
            Vector3 angularVelocity = Vector3.zero;
            OVRNodeStateProperties.GetNodeStatePropertyVector3(UnityEngine.XR.XRNode.Head, NodeStatePropertyType.AngularVelocity, OVRPlugin.Node.EyeCenter, OVRPlugin.Step.Render, out angularVelocity);
            BitConverter.GetBytes((double)angularVelocity.x).CopyTo(_hmdBuffer, offset);
            offset += 8;
            BitConverter.GetBytes((double)angularVelocity.y).CopyTo(_hmdBuffer, offset);
            offset += 8;
            BitConverter.GetBytes((double)angularVelocity.z).CopyTo(_hmdBuffer, offset);

            // Send the data
            _server.Send(_hmdBuffer, SocketFlags.None);
        }
        catch (SocketException e)
        {
            Debug.LogError($"Failed to send HMD data: {e.Message}");
        }
    }
    private void OnDisable()
    {
        _terminateConnectionSignal.Enqueue("Stop");
        _connection.Join();
        _onCloseServer?.Invoke();
    }   
}
