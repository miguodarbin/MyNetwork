using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyWindowsServer;

/// <summary>
/// 这个类就是对普通的C#Socket进行的一次封装。给原生的C#Socket多搞了一些“监听侧”相关常用的方法（虽然C#的Socket也是封装的SocketAPI里的Socket）
/// TODO：以后如果学了异步的TCP通讯方法后，要将这里处理接收消息的模型调整，
/// TODO：目前的模型是：统一开一个后台线程用阻塞方法接客户端Socket，然后接到一个客户端Socket就给他开启一个单独的线程去运行阻塞的接收消息方法，这就会导致客户端一多，占很多线程
/// TODO：以后要解决这个问题
/// </summary>
public class TcpServer
{
    //开启一个监听客户端的Socket，客户端的Socket不和这个监听Socket连接，只作为监听用
    private Socket _listenSocket;

    //这个监听Socket同时维护自己监听到的客户端通信Socket
    private ConcurrentDictionary<int, ClientSession> _connectClientSocketDict = new ConcurrentDictionary<int, ClientSession>();

    //是否启用这个监听Socket，比如一直去监听有没有客户端接入、一直接收客户端消息
    private bool _isEnable = false;

    /// <summary>
    /// 开始监听，内部会创建一个C#的Socket并自动开始监听，外部不可以获得这个原生的Socket，只能用这个类里面的方法操作
    /// </summary>
    public void StartListen(string ip, int port)
    {
        //创建一个C#原生的Socket，就用TCP协议进行网路通讯，以后要换UDP的话再说
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //给这个Socket设置一下服务器本地的IP、端口号，并设置为监听Socket，让客户端的Socket找过来
        _listenSocket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
        //目前就设置1024个最大监听数，不可变，想变的话以后再说
        _listenSocket.Listen(1024);
        //给这个封装侧的Socket打开
        _isEnable = true;
        //然后就可以开始取监听结果了，并且一直要能取结果。监听好的结果放一个Dictionary里面，key是通信Socket的ID，value是通信Socket
        //注意，目前我只学了同步取的方法，所以为了不阻塞这边的线程，所以我新开一个异步方法，也就相当于开了一个专门去接收客户端的线程，去取TCP连接的结果
        //取到结果后，也会为取到结果的通信Socket各自开一个自己接收客户端消息的能力：StartReceiveLoop()
        AutoAcceptSocket();
        //并且开启心跳检测,5秒检测一次
        DriveSessionHeartDetection(5000);
    }

    //自动去拿操作系统里_listenSocket的TCP连接信息的一个线程
    private async void AutoAcceptSocket()
    {
        //由于拿的方法Accept()是阻塞式的，所以把这个方法作为Task交给线程池去做
        await Task.Run(() =>
        {
            //一直拿
            while (_isEnable)
            {
                try
                {
                    if (_listenSocket == null)
                    {
                        return;
                    }

                    //拿到Socket给他用我自己搞的ConnectClientSocket封装起来
                    Socket originConnetClientSocket = _listenSocket.Accept();
                    if (originConnetClientSocket == null) //保险一点，应该是拿不到空的Socket吧？无所谓
                    {
                        Console.WriteLine("空的Socket？？？？");
                        return;
                    }

                    //接到客户端的连接信息了，那就初始化成session对象，并且把从字典中删除的方法作为回调注册给session，session只要一关闭就从TCPServer这边的字典里移除掉
                    ClientSession connetClientSocket = new ClientSession(originConnetClientSocket, OnClientSessionClosed, DateTime.Now);

                    //系统提示一下，有客户端接入了
                    Console.WriteLine("【系统】" + connetClientSocket.GetRemoteClientEndPoint() + "已接入");
                    //还要把它作为 _listenSocket监听到的 连接客户端的通信Socket 管理起来，
                    bool added = _connectClientSocketDict.TryAdd(connetClientSocket.ID, connetClientSocket);

                    //TryAdd() 返回 false 时，代表字典中已经存在相同的 ID,说明可能别的线程和这次拿了同一个TCP链接消息，并添加到字典了
                    if (!added)
                    {
                        Console.WriteLine(
                            $"【系统】客户端会话添加失败，ID重复：{connetClientSocket.ID}"
                        );

                        //不要启动未被服务器管理的会话
                        try
                        {
                            originConnetClientSocket.Close();
                        }
                        catch (ObjectDisposedException)
                        {
                            //Socket已经关闭
                        }

                        continue;
                    }


                    //每个和客户端通信的Socket用字节的接收循环，去接收客户端发来的消息
                    connetClientSocket.StartReceiveLoop();
                }
                catch (SocketException e)
                {
                    if (_isEnable)
                    {
                        Console.WriteLine("【系统】" + "客户端连入出错");
                        Console.WriteLine(e.Message);
                    }
                }
            }
        });
    }


    public void BroadcastToAllClients(byte[] frameBytes)
    {
        //获取当前活动会话的独立数组快照,因为可能在广播的过程中，又有新的Client加入字典
        ClientSession[] sessionSnapshot = _connectClientSocketDict.Values.ToArray();

        foreach (var session in sessionSnapshot)
        {
            try
            {
                session.SendBytesToClient(frameBytes);
            }
            catch (Exception e)
            {
                //一个客户端失败，不能阻止其他客户端接收广播
                Console.WriteLine(
                    $"【系统】向客户端 {session.ID} 广播失败"
                );
                Console.WriteLine(e.Message);

                session.Close();
            }
        }
    }

    //由服务端开一个线程，统一处理所有session的心跳检测
    private async void DriveSessionHeartDetection(int detechFrequency)
    {
        await Task.Run(() =>
        {
            while (_isEnable)
            {
                //获取当前活动会话的独立数组快照,因为可能在广播的过程中，又有新的Client加入字典
                ClientSession[] sessionSnapshot = _connectClientSocketDict.Values.ToArray();
                foreach (var session in sessionSnapshot)
                {
                    session.ClientHeartDetection();
                }

                Thread.Sleep(detechFrequency);
            }
        });
    }


    /// <summary>
    /// 客户端会话关闭后，从活动会话表中移除。
    /// </summary>
    private void OnClientSessionClosed(int sessionId)
    {
        //这边TryRemove会返回移除字典的那个session，不过这里用不到，也就不用他了
        bool removed = _connectClientSocketDict.TryRemove(sessionId, out ClientSession removedSession);

        if (removed)
        {
            Console.WriteLine(
                $"【系统】客户端会话已移除，ID：{sessionId}"
            );
        }
    }


    /// <summary>
    /// 关闭监听Socket，这也会同时移除掉这个ListenSocket下的通信Socket
    /// </summary>
    public void CloseTcpServerAndSessions()
    {
        //先停止Accept循环
        _isEnable = false;

        Socket listenSocket = _listenSocket;
        _listenSocket = null;

        try
        {
            //关闭监听Socket，唤醒阻塞中的Accept
            listenSocket?.Close();
        }
        catch (ObjectDisposedException)
        {
            //监听Socket已经关闭
        }


        //获取活动会话快照
        ClientSession[] sessionSnapshot = _connectClientSocketDict.Values.ToArray();

        foreach (ClientSession session in sessionSnapshot)
        {
            try
            {
                session.Close();
            }
            catch (Exception e)
            {
                //一个客户端关闭失败，继续关闭其他客户端
                Console.WriteLine(
                    $"【系统】关闭客户端会话 {session.ID} 时出错"
                );
                Console.WriteLine(e.Message);
            }
        }

        //把字典容器也清理一下
        _connectClientSocketDict.Clear();
    }
}