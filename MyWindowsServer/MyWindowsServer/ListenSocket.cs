using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyWindowsServer;

/// <summary>
/// 这个类就是对普通的C#Socket进行的一次封装。给原生的C#Socket多搞了一些“监听侧”相关常用的方法（虽然C#的Socket也是封装的SocketAPI里的Socket）
/// </summary>
public class ListenSocket
{
    private Socket _listenSocket;
    private Dictionary<int, ConnectClientSocket> _connectClientSocketDict = new Dictionary<int, ConnectClientSocket>();
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
        //注意，目前我只学了同步取的方法，所以为了不阻塞这边的线程，所以我新开一个异步方法，在异步方法里面用await Task去取结果
        AutoAcceptSocket();
        //用异步方法，同Task统一的、不停的处理所有通信Socket的接收信息
        ProcessAllClientMsg();
    }

    //自动去拿操作系统里_listenSocket的TCP连接信息
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

                    ConnectClientSocket connetClientSocket = new ConnectClientSocket(originConnetClientSocket);

                    //系统提示一下，有客户端接入了
                    Console.WriteLine("【系统】" + connetClientSocket.GetRemoteClientEndPoint() + "已接入");
                    //还要把它作为 _listenSocket监听到的 连接客户端的通信Socket 管理起来
                    _connectClientSocketDict.Add(connetClientSocket.ID, connetClientSocket);
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

    /// <summary>
    /// 统一处理所有通信Socket的接收信息
    /// </summary>
    private async void ProcessAllClientMsg()
    {
        await Task.Run(() =>
        {
            while (_isEnable)
            {
                if (_connectClientSocketDict.Count > 0)
                {
                    foreach (ConnectClientSocket connectClientSocket in _connectClientSocketDict.Values)
                    {
                        connectClientSocket.ReceiveClientMsgAndProgress();
                    }
                }
            }
        });
    }

    /// <summary>
    /// 关闭监听Socket，这也会同时移除掉这个ListenSocket下的通信Socket
    /// </summary>
    public void CloseListenSocketAndCommSocket()
    {
        if (_listenSocket == null)
        {
            return;
        }

        _isEnable = false;
        _listenSocket.Close();
        foreach (var item in _connectClientSocketDict)
        {
            item.Value.Close();
        }

        _connectClientSocketDict.Clear();
        _listenSocket = null;
    }

    public void BroadcastToAllClients(byte[] bytes)
    {
        foreach (var item in _connectClientSocketDict)
        {
            item.Value.SendBytesToClient(bytes);
        }
    }
}