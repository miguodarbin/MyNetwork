using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    //单例
    private static NetworkManager _instance;

    public static NetworkManager Instance
    {
        get { return _instance; }
        private set { _instance = value; }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            Instance = this;
            if (!InitNetworkManager("192.168.0.196", 8080))
            {
                Destroy(this.gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    //============================================================

    //声明一个通信Socket，用来用TCP协议去找服务器通信
    private Socket _clientSocket;

    //专门开一个队列，记录待发送的。然后有一个专门处理这个队列发送的异步方法，不会阻塞主线程
    private Queue<Byte[]> _pendingSendBytesQueue = new Queue<Byte[]>();

    //再专门开一个队列，专门用来记录自己从服务器那边收过来的消息
    private Queue<byte[]> _receivedBytesQueue = new Queue<byte[]>();

    //声明一个开关，记录是否要这个Manager的连接到服务器
    private bool _needConnectToServer = false;

    //初始化本管理器，在这里绑定服务器的ip和端口号
    private bool InitNetworkManager(string ip, int port)
    {
        if (_clientSocket != null)
        {
            return false;
        }

        _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            _clientSocket.Connect(ip, port);
        }
        catch (SocketException e)
        {
            Debug.Log(e.Message);
            Instance = null;
            Destroy(this);
            return false;
        }


        //连接到服务器功能打开
        _needConnectToServer = true;
        //开启Manager的发送到服务器信息功能
        ProgressSendBytesAsync();
        //开启Manager的收服务器消息功能
        ProgressReceivedBytesAsync();
        return true;
    }

    //发消息,单独用一个异步方法，把发消息作为一个Task单独去让线程池去做
    public void SendBytesToServer(string msg)
    {
        _pendingSendBytesQueue.Enqueue(System.Text.Encoding.UTF8.GetBytes(msg));
    }

    //作为从_pendingSendQueue取出来的字节数组容器
    private byte[] needSendBytesInQueue;

    //专门处理发送消息的异步方法，处理逻辑作为Task交给线程池，然后就会把执行权返回给调用者
    private async void ProgressSendBytesAsync()
    {
        await Task.Run(() =>
            {
                while (_needConnectToServer)
                {
                    if (_pendingSendBytesQueue.Count > 0)
                    {
                        needSendBytesInQueue = _pendingSendBytesQueue.Dequeue();
                        try
                        {
                            _clientSocket.Send(needSendBytesInQueue);
                        }
                        catch (SocketException e)
                        {
                            Debug.Log(e.Message);
                            _needConnectToServer = false;
                            break;
                        }
                    }
                }
            }
        );
    }

    //声明一个容器，作为专门去操作系统那边的的Socket收消息缓冲区里面捞字节的水桶
    private byte[] bytesBucket = new byte[1024];

    //专门处理接受消息的异步方法，处理逻辑作为Task交给线程池，然后就会把执行权返回给调用者
    private async void ProgressReceivedBytesAsync()
    {
        await Task.Run(() =>
        {
            while (_needConnectToServer)
            {
                try
                {
                    int bytesCount = _clientSocket.Receive(bytesBucket);
                    if (bytesCount == 0)
                    {
                        _needConnectToServer = false;
                        break;
                    }

                    _receivedBytesQueue.Enqueue(bytesBucket[..bytesCount]);
                }
                catch (SocketException e)
                {
                    Debug.Log(e.Message);
                    _needConnectToServer = false;
                    break;
                }
            }
        });
    }

    //暴露给外部一个从收消息的队列里看有没有东西的方法
    public bool IsHaveServerSendBytes()
    {
        if (_receivedBytesQueue.Count > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //暴露给外部一个从收消息的队列里拿东西的方法
    public byte[] GetServerSendBytes()
    {
        if (IsHaveServerSendBytes())
        {
            return _receivedBytesQueue.Dequeue();
        }

        return null;
    }

    //关闭对服务器的连接
    public void CloseConnection()
    {
        _needConnectToServer = false;
        _clientSocket.Shutdown(SocketShutdown.Both);
        _clientSocket.Close();
        _pendingSendBytesQueue.Clear();
        _receivedBytesQueue.Clear();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            CloseConnection();
            Instance = null;
        }
    }
}