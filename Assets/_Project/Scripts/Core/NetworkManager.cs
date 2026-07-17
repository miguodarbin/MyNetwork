using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
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
        ProgressSendFrameAsync();
        //开启Manager的收服务器消息功能
        ProgressReceivedBytesAsync();
        return true;
    }

    //===================================================接收远程服务器frame===================================

    //专门开一个自动阻塞线程的队列，记录待发送的。然后有一个专门处理这个队列发送的异步方法作为后台线程，不会阻塞主线程.Unity主线程负责入队，后台线程发送线程负责出队。
    //没有数据时，消费线程会自动等待阻塞后台线程，后台线程不会空转。
    private readonly BlockingCollection<byte[]> _pendingSendFrame = new BlockingCollection<byte[]>();

    /// <summary>
    /// 向客户端发送消息，内部是把消息排到一个队列里，然后靠ProgressSendBytesAsync异步处理发送消息
    /// </summary>
    public void SendFrameToServer(byte[] frameBytes)
    {
        //连接已经关闭时，不再接受发送请求
        if (!_needConnectToServer)
        {
            return;
        }

        try
        {
            //尝试把完整帧放入待发送集合
            //如果入队失败，直接丢弃本次发送请求
            _pendingSendFrame.TryAdd(frameBytes);
        }
        catch (InvalidOperationException)
        {
            //检查连接状态之后，可能有其他线程刚好调用了 CompleteAdding。
            //这属于正常的关闭竞争，直接丢弃本次发送请求。
        }
    }

    //专门处理发送消息的异步方法，处理逻辑作为Task交给线程池，然后就会把执行权返回给调用者
    private async void ProgressSendFrameAsync()
    {
        await Task.Run(() =>
            {
                //获取可以消费的遍历集合，每遍历一个元素，就会从原集合中拿走一个元素。当没有元素的时候，就会阻塞这个执行Task的线程，当有元素的时候，自动唤醒执行这个Task的线程继续执行。
                foreach (byte[] frameBytes in _pendingSendFrame.GetConsumingEnumerable())
                {
                    //关闭连接后，即使还有机会进入循环，也直接退出
                    if (!_needConnectToServer)
                    {
                        break;
                    }

                    try
                    {
                        //发送一整帧。如果发送失败，就不能继续处理后面的帧了。
                        bool sendSucceeded = SendCompleteFrame(frameBytes);
                        if (!sendSucceeded)
                        {
                            _needConnectToServer = false;
                            break;
                        }
                    }
                    catch (SocketException e)
                    {
                        Debug.Log(e.Message);
                        _needConnectToServer = false;
                        break;
                    }
                }
            }
        );
    }

    // 由于Send方法可能一次不会把传给他的frame都发出去，所以需要持续发送，直到一整个完整帧全部发送完成。返回值代表是否完整的发送frame成功
    private bool SendCompleteFrame(byte[] frameBytes)
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
                return false;
            }

            //根据本次实际发送量，推进已发送位置
            offset += sentCount;
        }

        return true;
    }


    //===================================================发送本地客户端frame===================================

    //处理完分包粘包的frame说存放的队列：被 ProcessTCPStream 处理完毕成为一个frame之后才往里面放，一个字节数组代表一个完整frame。由外部取消费这个Queue<byte>消息
    //线程是安全的，unity主线程和后台处理接收消息的线程 可以同时操作_processedMsgQueue
    private readonly ConcurrentQueue<byte[]> _processedMsgQueue = new ConcurrentQueue<byte[]>();

    //不管 37 二十一，拿到了客户端的字节就往里面放,严禁业务层直接用这里面的数据,逻辑在处理这个的时候严禁改顺序
    private Queue<byte> _originalBytesQueue = new Queue<byte>();

    //声明一个容器，作为专门去操作系统那边的的Socket收消息缓冲区里面捞字节的水桶
    private byte[] originalBytesBucket = new byte[1024 * 1024];

    //专门处理接受消息的异步方法，处理逻辑作为Task交给线程池，然后就会把执行权返回给调用者。单开一个线程去接收消息的原因是_clientSocket.Receive(...) 是同步阻塞 API。不能卡住InitNetworkManager
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
                //Close方法会关闭并释放 Socket，这会让Receive方法报错，捕获错误并判断
                catch (SocketException e)
                {
                    //如果客户端都还没有来得及Close，就恢复了线程，此时已经执行过_clientSocket.Shutdown(SocketShutdown.Both);了，但还没有释放，但_needConnectToServer已经false了
                    if (!_needConnectToServer)
                    {
                        break;
                    }

                    // 当前仍然需要连接，却发生了 SocketException，
                    // 才说明出现了真正的网络异常。
                    Debug.LogError($"网络连接异常：{e.Message}");
                    _needConnectToServer = false;
                    break;
                }
                catch (ObjectDisposedException e) //如果是客户端自己调用的Close Socket，资源被释放了，会报资源释放的错误，并且_needConnectToServer为false，属于正常情况
                {
                    // 本地主动 Close Socket 后，报的资源释放错误是正常的
                    if (!_needConnectToServer)
                    {
                        break;
                    }

                    //如果不是自己调用的Close Socket，资源也被释放了，那就说明不是走的预期流程
                    Debug.LogError($"Socket 被异常释放：{e.Message}");
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

    /// <summary>
    /// 尝试从接收完成帧队列中取出一条完整帧。
    /// 返回 true 表示成功取到；返回 false 表示当前没有完整帧。
    /// </summary>
    public bool TryDequeueReceivedFrame(out byte[] frameBytes)
    {
        return _processedMsgQueue.TryDequeue(out frameBytes);
    }

    //关闭对服务器的连接
    public void CloseConnection()
    {
        _needConnectToServer = false;
        _clientSocket.Shutdown(SocketShutdown.Both);
        _clientSocket.Close();

        if (!_pendingSendFrame.IsAddingCompleted) //判断一下是否已经标记完成添加了，防止重复标记
        {
            _pendingSendFrame.CompleteAdding(); //将容器标记为完成了添加，不再添加新的项了
        }

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