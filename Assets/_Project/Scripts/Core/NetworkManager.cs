using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    //TODO:之后把这个单例维护到XFramework的时候要接入框架的单例
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
    
    //===============================================================================================

    //声明一个通信Socket，用来用TCP协议去找服务器通信
    private Socket _clientSocket;

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

    //===================================================接收远程服务器frame===================================

    //专门开一个队列，记录待发送的。然后有一个专门处理这个队列发送的异步方法，不会阻塞主线程
    private Queue<Byte[]> _pendingSendBytesQueue = new Queue<Byte[]>();


    /// <summary>
    /// 向客户端发送消息，内部是把消息排到一个队列里，然后靠ProgressSendBytesAsync异步处理发送消息
    /// </summary>
    /// <param name="bytes"></param>
    public void SendBytesToServer(byte[] bytes)
    {
        _pendingSendBytesQueue.Enqueue(bytes);
    }

    //专门处理发送消息的异步方法，处理逻辑作为Task交给线程池，然后就会把执行权返回给调用者
    private async void ProgressSendBytesAsync()
    {
        await Task.Run(() =>
            {
                while (_needConnectToServer)
                {
                    if (_pendingSendBytesQueue.Count > 0)
                    {
                        //从队列中取出一整个完整帧
                        byte[] frameBytes = _pendingSendBytesQueue.Dequeue();

                        try
                        {
                            SendCompleteFrame(frameBytes);
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
    
    // 由于Send方法可能一次不会把传给他的frame都发出去，所以需要持续发送，直到一整个完整帧全部发送完成。
    private void SendCompleteFrame(byte[] frameBytes)
    {
        //offset 表示：前面已经成功发送了多少字节
        int offset = 0;

        //只要还有字节没发完，就继续发送
        while (offset < frameBytes.Length)
        {
            //本次还剩多少字节需要发送
            int remainingLength = frameBytes.Length - offset;

            //从 offset 位置开始，尝试发送剩余的所有字节
            int sentCount = _clientSocket.Send(
                frameBytes,
                offset,
                remainingLength,
                SocketFlags.None
            );

            //没有产生任何发送进度，连接已经不能继续正常发送
            if (sentCount <= 0)
            {
                Debug.Log("发送消息失败");
                _needConnectToServer = false;
                return;
            }

            //根据本次实际发送量，推进已发送位置
            offset += sentCount;
        }
    }


    //===================================================发送本地客户端frame===================================

    //处理完分包粘包的字节数组区Queue<byte[]>：被 ProcessTCPStream 处理过的字节数组才往里面放，一个字节数组代表一个完整消息。由外部取消费这个Queue<byte>消息
    private Queue<byte[]> _processedMsgQueue = new Queue<byte[]>();

    //不管 37二十一，拿到了客户端的字节就往里面放,严禁业务层直接用这里面的数据,逻辑在处理这个的时候严禁改顺序
    private Queue<byte> _originalBytesQueue = new Queue<byte>();

    //声明一个容器，作为专门去操作系统那边的的Socket收消息缓冲区里面捞字节的水桶
    private byte[] originalBytesBucket = new byte[1024 * 1024];

    //专门处理接受消息的异步方法，处理逻辑作为Task交给线程池，然后就会把执行权返回给调用者
    private async void ProgressReceivedBytesAsync()
    {
        await Task.Run(() =>
        {
            while (_needConnectToServer)
            {
                try
                {
                    int bytesCount = _clientSocket.Receive(originalBytesBucket);
                    if (bytesCount == 0)
                    {
                        _needConnectToServer = false;
                        break;
                    }

                    for (int i = 0; i < bytesCount; i++)
                    {
                        _originalBytesQueue.Enqueue(originalBytesBucket[i]);
                    }

                    //不断的接收客户端的发来的字节，然后放到缓存区里。只要一有内容放到缓存区就调用ProcessTCPOriginalBytes()
                    ProcessTCPOriginalBytes();
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

    //这个方法应该不需要开异步方法，感觉并不耗时，因为和网络好坏也没关系，算法循环次数也多不了几次
    private void ProcessTCPOriginalBytes()
    {
        //循环判断：缓存区有字节就进入循环，无字节就退出循环。还有靠break打破循环
        while (_originalBytesQueue.Count > 0)
        {
            //游标：处理到缓存区的哪里了？
            int cursor = 0;
            //消息头长度：基本上就是两个int就是8个字节
            int headCount = 8;
            //缓存区目前里面的长度
            int originalBytesListCount = _originalBytesQueue.Count;
            //如果缓存区的字节长度大于等于消息头的长度,那就说明可以继续解析消息头的具体内容
            if (originalBytesListCount >= headCount)
            {
                //游标 +4，从消息体长度开始读
                cursor += sizeof(int);
                int msgBodyCount = BitConverter.ToInt32(_originalBytesQueue.ToArray(), cursor);
                //游标再 +4，指向消息体
                cursor += sizeof(int);
                //拿缓存区的总个数-目前游标位置得到  缓存区剩余的字节数,也就是消息体的字节数
                int remainBytesCount = originalBytesListCount - cursor;
                //游标再 +消息体长度，指向下一个消息头部
                cursor += msgBodyCount;
                //与刚才解析出来的消息体长度比较,如果缓存区剩余的字节数大于消息体长度，说明粘包了。
                if (remainBytesCount > msgBodyCount)
                {
                    //然后把从0到对象字节长度那么多的字节从缓存区拿走，加入成功区。
                    byte[] msgBytes = new byte[cursor];
                    for (int i = 0; i < cursor; i++)
                    {
                        msgBytes[i] = _originalBytesQueue.Dequeue();
                    }

                    _processedMsgQueue.Enqueue(msgBytes);

                    //游标置零，让下一次循环时，游标指向消息头。否则游标一直是旧值，而Queue队列不断变化，会无法对齐。不用打破循环，一直走到打破循环的分支上去。
                    cursor = 0;
                }

                //与刚才解析出来的消息体长度比较,如果缓存区剩余的字节数等于消息体长度，说明没有发生粘包分包。
                if (remainBytesCount == msgBodyCount)
                {
                    //然后把从0到对象字节长度那么多的字节从缓存区拿走，加入成功区。
                    byte[] msgBytes = new byte[cursor];
                    for (int i = 0; i < cursor; i++)
                    {
                        msgBytes[i] = _originalBytesQueue.Dequeue();
                    }

                    _processedMsgQueue.Enqueue(msgBytes);

                    //打破循环，因为缓存区再拿完这条消息之后就没字节了。
                    break;
                }

                //与刚才解析出来的消息体长度比较,如果缓存区剩余的字节数小于消息体长度，说明发生分包了。
                if (remainBytesCount < msgBodyCount)
                {
                    //此时不应该继续处理了，直接打破循环，等之后的再发消息过来，再来判断能不能处理。
                    break;
                }
            }
            else //如果缓存区的字节长度小于消息头的长度,也不进行处理，直接打破循环，等之后的再发消息过来，再来判断能不能处理。
            {
                break;
            }
        }
    }

    //暴露给外部一个从收消息的队列里看有没有东西的方法
    public bool IsHaveServerSendMsg()
    {
        if (_processedMsgQueue.Count > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 暴露给外部一个从收消息的队列里拿东西的方法,只要能拿到，就说明消息一定是完整的，一定是经过分包黏包处理过的
    /// </summary>
    public byte[] GetServerSendMsg()
    {
        if (IsHaveServerSendMsg())
        {
            return _processedMsgQueue.Dequeue();
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
        _processedMsgQueue.Clear();
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