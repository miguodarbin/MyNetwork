using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace MyWindowsServer;

/// <summary>
/// 这个类就是对普通的C#Socket进行的一次封装。给原生的C#Socket多搞了一些“连接客户端侧”相关常用的方法（虽然C#的Socket也是封装的SocketAPI里的Socket）
/// </summary>
public class ClientSession
{
    //用来专门产出唯一ID的
    private static int IDCreator = -1;

    //这是原生的C#侧的Socket，本类主要就是对他进行封装，给他增加一些“连接客户端侧”相关常用的方法
    private Socket _connectClientSocket;

    //连接客户端的通信 ClientSession 的id
    public int ID { get; private set; }

    //构造函数，要求TcpServer必须要给到一个原生的 连接客户端 的通信Socket，才能创建我这个封装的 连接客户端的通信Socket
    //并且TcpServer还要能接收当这个客户端对话被关掉之后，客户端对话给到的自己的ID
    public ClientSession(Socket originConnetClientSocket, Action<int> onSessionClosed)
    {
        this._connectClientSocket = originConnetClientSocket;
        this.ID = ++IDCreator;
        _onSessionClosed = onSessionClosed;
    }

    //=========================================接收远程客户端frame=========================================

    //先搞一个水桶，方便去系统那边捞传输过来的字节
    byte[] buffer = new byte[1024 * 1024];


    //不管 37二十一，拿到了客户端的字节就往里面放,严禁业务层直接用这里面的数据,逻辑在处理这个的时候严禁改顺序
    private Queue<byte> _originalBytesQueue = new Queue<byte>();

    //当前客户端会话专属的接收线程。这个线程只负责当前客户端的阻塞 Receive 循环。
    private Thread _receiveThread;


    /// <summary>
    /// 启动当前和客户端通信的Socket自己的、专属接收循环。
    /// 每个会话只允许启动一次。
    /// </summary>
    public void StartReceiveLoop()
    {
        //不允许重复开始接收消息的线程
        if (_receiveThread != null)
        {
            return;
        }

        //创建一条真正由当前会话持有的线程。Receive 是同步阻塞方法，所以放到自己专属的线程里执行。
        _receiveThread = new Thread(ReceiveLoop);

        //设置为后台线程。防止服务端退出时被接收线程卡住
        //这样服务端主线程退出时，不会因为这条接收线程仍存在而阻止进程结束。
        _receiveThread.IsBackground = true;

        //启动后，线程会进入 ReceiveLoop。
        _receiveThread.Start();
    }


    /// <summary>
    /// 当前客户端会话的专属阻塞接收循环。
    /// </summary>
    private void ReceiveLoop()
    {
        //保存本次接收循环使用的Socket引用。
        //外部Close时即使字段被置空，也可以通过关闭这个Socket
        //使阻塞中的Receive返回或抛出异常。
        Socket socket = _connectClientSocket;

        if (socket == null)
        {
            return;
        }

        while (true)
        {
            try
            {
                //不再检查Available。
                //暂时没有数据时，当前客户端自己的后台线程阻塞在这里。
                int msgBytesCount = socket.Receive(buffer);

                //TCP Receive返回0，表示对方正常关闭了连接。
                if (msgBytesCount == 0)
                {
                    Console.WriteLine(
                        "【系统】客户端正常关闭连接：" + socket.RemoteEndPoint
                    );

                    Close();
                    break;
                }

                //把本次收到的有效字节加入流缓存区
                for (int i = 0; i < msgBytesCount; i++)
                {
                    _originalBytesQueue.Enqueue(buffer[i]);
                }

                //继续使用原来的分帧逻辑
                ProcessTCPOriginalBytes();
            }
            catch (SocketException e)
            {
                Console.WriteLine("【系统】接收客户端消息出错");
                Console.WriteLine(e.Message);

                Close();
                break;
            }
            catch (ObjectDisposedException)
            {
                //唤醒线程不仅是条件满足可以唤醒，比如这里，是Socket能拿到操作系统接收到的字节，唤醒条件不仅仅是操作系统那边有字节，如果Socket自己被释放掉，也是一个唤醒条件
                Close();
                break;
            }
        }
    }

    //这个方法主要是用来处理分包黏包，处理完之后交给ProgressReceivedMsg分析器去分析frame
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
                //游标 +4，从方法体长度开始读
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

                    ProcessReceivedFrame(msgBytes);

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

                    ProcessReceivedFrame(msgBytes);

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


    //就用普通的同步方法处理这个Frame就行，之前想的是怕处理Frame太耗时，影响处理别的消息分包黏包，用的是异步Task单开了一个线程处理Frame，等以后真耗时了再说吧
    //并且就算这边处理的耗时，也不会影响最外部的主线程，只是会影响每个Socket自己接收消息的那个线程，问题不大
    private void ProcessReceivedFrame(byte[] frameBytes)
    {
        //完整帧的前4个字节是消息类型ID
        int msgType = BitConverter.ToInt32(frameBytes, 0);

        switch (msgType)
        {
            case 1000:
            {
                StringMsg stringMsg = new StringMsg();
                stringMsg.DeSerializeFormBytes(frameBytes);

                Console.WriteLine(stringMsg.msg);
                break;
            }

            case 1001:
            {
                PlayerMsg playerMsg = new PlayerMsg();
                playerMsg.DeSerializeFormBytes(frameBytes);

                Console.WriteLine(playerMsg.playerId);
                Console.WriteLine(playerMsg.playerData.playerAge);
                Console.WriteLine(playerMsg.playerData.playerHealth);
                Console.WriteLine(playerMsg.playerData.playerName);
                Console.WriteLine(playerMsg.playerData.playerSex);
                break;
            }
        }
    }


    //=========================================发送本地服务器frame=========================================
    public void SendBytesToClient(byte[] bytes)
    {
        Socket socket;
        //只有拿到钥匙才能对socket赋值，目的是不让关闭所产生的置空，会影响到这里的赋值，这里一定能拿到一个不为null的socket
        //但需要注意的是，即使不为null，因为锁过了这边的赋值就失效了，接下来用SendCompleteFrame可能也无法发送，因为Socket对象已经关闭了
        lock (_socketLock)
        {
            socket = _connectClientSocket;
        }

        //只有赋上值才能走发送逻辑
        if (socket == null)
        {
            return;
        }

        //为了防止这个执行SendCompleteFrame时候，有别的地方给Socket关闭了，导致发送报错，需要在外部捕获一下错误，因为try catch不一定非要写在报错那一行，也可以写在调用报错那一行的父方法那边，到时候报错会一层层往上找trycatch的
        try
        {
            //把这个不为null的socket执行发送完整信息的方法
            SendCompleteFrame(socket, bytes);
        }
        catch (SocketException e)
        {
            Console.WriteLine("【系统】" + "发送消息出错");
            Close();
            Console.WriteLine(e.Message);
        }
        catch (ObjectDisposedException)
        {
            //拿到局部Socket以后，关闭线程仍可能将该Socket关闭
            Close();
        }
    }


    // 由于Send方法可能一次不会把传给他的frame都发出去，所以需要持续发送，直到一整个完整帧全部发送完成。
    private void SendCompleteFrame(Socket socket, byte[] frameBytes)
    {
        int offset = 0;

        while (offset < frameBytes.Length)
        {
            int remainingLength = frameBytes.Length - offset;

            int sentCount = socket.Send(
                frameBytes,
                offset,
                remainingLength,
                SocketFlags.None
            );

            //服务端无法继续发送时，关闭当前客户端会话
            if (sentCount <= 0)
            {
                Close();
                return;
            }

            offset += sentCount;
        }
    }


    /// <summary>
    /// 获得这个ConnectClientSocket的所连接的那个客户端的IP端口号
    /// </summary>
    public string GetRemoteClientEndPoint()
    {
        return _connectClientSocket.RemoteEndPoint.ToString();
    }


    //===========================================关闭相关======================================================
    //当前会话关闭后，拿到TCPServer那边的移除字典中记录的回调函数，然后再Close里面掉
    private readonly Action<int> _onSessionClosed;

    //保护_connectClientSocket字段的读取和置空操作
    private readonly object _socketLock = new object();
    
    public void Close()
    {
        Socket socket;

        //锁住对socket的操作，除非拿到钥匙，才能对socket赋上值，并走close逻辑
        lock (_socketLock)
        {
            socket = _connectClientSocket;

            //这里只是对共享变量进行置空，如果别的地方还有对_connectClientSocket所指向的对象有所引用，那别的地方虽然还有Socket，但是指向的那个socket也已经被关闭了，因为后面就要处理关闭Socket的逻辑了
            _connectClientSocket = null;
        }

        try
        {
            //只有真正拿到Socket的线程负责关闭
            if (socket != null)
            {
                try
                {
                    //关闭收发方向，同时唤醒阻塞中的Receive
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                    //对方可能已经断开
                }
                catch (ObjectDisposedException)
                {
                    //Socket可能已经被其他路径释放
                }
                finally
                {
                    socket.Close();
                }
            }
        }
        finally
        {
            //允许重复通知。
            //ListenSocket使用TryRemove，所以重复移除不会报错。
            _onSessionClosed?.Invoke(ID);
        }
    }
}