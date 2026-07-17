using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace MyWindowsServer;

/// <summary>
/// 这个类就是对普通的C#Socket进行的一次封装。给原生的C#Socket多搞了一些“连接客户端侧”相关常用的方法（虽然C#的Socket也是封装的SocketAPI里的Socket）
/// </summary>
public class ConnectClientSocket
{
    //用来专门产出唯一ID的
    private static int IDCreator = -1;

    //这是原生的C#侧的Socket，本类主要就是对他进行封装，给他增加一些“连接客户端侧”相关常用的方法
    private Socket _connectClientSocket;

    //连接客户端的通信Socket的id
    public int ID { get; private set; }

    //构造函数，要求外部必须要给到一个原生的 连接客户端 的通信Socket，才能创建我这个封装的 连接客户端的通信Socket
    public ConnectClientSocket(Socket originConnetClientSocket)
    {
        this._connectClientSocket = originConnetClientSocket;
        this.ID = ++IDCreator;
    }

    //=========================================接收远程客户端frame=========================================

    //先搞一个水桶，方便去系统那边捞传输过来的字节
    byte[] buffer = new byte[1024 * 1024];

    //处理完分包粘包的字节数组区Queue<byte[]>：被 ProcessTCPStream 处理过的字节数组才往里面放，一个字节数组代表一个完整消息。由外部取消费这个Queue<byte>消息
    private Queue<byte[]> _processedMsgQueue = new Queue<byte[]>();

    //不管 37二十一，拿到了客户端的字节就往里面放,严禁业务层直接用这里面的数据,逻辑在处理这个的时候严禁改顺序
    private Queue<byte> _originalBytesQueue = new Queue<byte>();


    /// <summary>
    /// 去操作系统那边捞字节
    /// </summary>
    public void ReceiveClientMsgAndProgress()
    {
        if (_connectClientSocket == null)
        {
            return;
        }

        try
        {
            //必须确认有字节才能去捞，否则会严重阻塞调用者线程。
            if (_connectClientSocket.Available <= 0)
            {
                return;
            }

            //这个Receive是一个阻塞方法，如果在操作系统那边捞不着消息，就会一直卡在这里，我觉得没问题，不用开Task
            int msgBytesCount = _connectClientSocket.Receive(buffer);

            for (int i = 0; i < msgBytesCount; i++)
            {
                _originalBytesQueue.Enqueue(buffer[i]);
            }

            ProcessTCPOriginalBytes();
        }
        catch (SocketException e)
        {
            Console.WriteLine("【系统】" + "接收消息出错");
            Close();
            Console.WriteLine(e.Message);
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

                    _processedMsgQueue.Enqueue(msgBytes);
                    ProgressReceivedMsg();

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
                    ProgressReceivedMsg();

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

    //单独开一个处理消息的异步方法任务，交给Task去处理
    private async void ProgressReceivedMsg()
    {
        await Task.Run(() =>
        {
            //处理接到的字节的方法！！
            byte[] needProgressMsg = _processedMsgQueue.Dequeue();
            //先读取头四个字节也就是int MsgType
            int MsgType = BitConverter.ToInt32(needProgressMsg, 0);
            switch (MsgType)
            {
                case 1000:
                    StringMsg stringMsg = new StringMsg();
                    stringMsg.DeSerializeFormBytes(needProgressMsg);
                    Console.WriteLine(stringMsg.msg);
                    break;

                case 1001:
                    PlayerMsg playerMsg = new PlayerMsg();
                    playerMsg.DeSerializeFormBytes(needProgressMsg);
                    Console.WriteLine(playerMsg.playerId);
                    Console.WriteLine(playerMsg.playerData.playerAge);
                    Console.WriteLine(playerMsg.playerData.playerHealth);
                    Console.WriteLine(playerMsg.playerData.playerName);
                    Console.WriteLine(playerMsg.playerData.playerSex);
                    break;
            }
        });
    }


    //=========================================发送本地服务器frame=========================================
    public void SendBytesToClient(byte[] bytes)
    {
        if (_connectClientSocket == null)
        {
            return;
        }

        try
        {
            SendCompleteFrame(bytes);
        }
        catch (SocketException e)
        {
            Console.WriteLine("【系统】" + "发送消息出错");
            Close();
            Console.WriteLine(e.Message);
        }
    }


    // 由于Send方法可能一次不会把传给他的frame都发出去，所以需要持续发送，直到一整个完整帧全部发送完成。
    private void SendCompleteFrame(byte[] frameBytes)
    {
        int offset = 0;

        while (offset < frameBytes.Length)
        {
            int remainingLength = frameBytes.Length - offset;

            int sentCount = _connectClientSocket.Send(
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


    public void Close()
    {
        if (_connectClientSocket == null)
        {
            return;
        }

        _connectClientSocket.Shutdown(SocketShutdown.Both);
        _connectClientSocket.Close();
        _connectClientSocket = null;
    }
}