using System.Net.Sockets;

namespace MyWindowsServer;

public class PlaygroundServer
{
    private Socket listenSocket;
    private void Test()
    {
        //创建一个监听Socket
        listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //监听Socket用异步的方式去操作系统那边取TCP连接信息，取到之后会调用我给到的callback方法，
        //并且我希望取到TCP连接信息之后，就立马把这个监听Socket所得到客户端通信Socket拿到，
        //所以我需要让系统给的IAsyncResult类型对象里的object AsyncState，是我这里给到的东西，也就是listenSocket
        listenSocket.BeginAccept(OnAcceptedConnectClientSocketCallback, listenSocket);
    }

    //当异步任务结束之后,此时还没有拿到Socket哦
    private void OnAcceptedConnectClientSocketCallback(IAsyncResult receipt)
    {
        //这里的receipt.AsyncState就是当时去执行异步时候，我给到的参数，也就是listenSocket
        Socket listenSockt = (Socket)receipt.AsyncState;
        try
        {
            //你看，异步执行完才会调此方法，所以这时候我必定能取到和客户端通信的Socket。
            //并且把这次异步的回执给到EndAccept。因为我可以把IAsyncResult存下来，不在这次Begin的完成回调里取结果，等之后某个时间点再来取，所以，EndAccept必须知道，自己这一次要取得是哪个IAsyncResult。
            Socket connectClientSocket = listenSockt.EndAccept(receipt);
            
            //领到一个TCP连接信息之后，再接着去领
            //同步的Socket方法，都是我自己处理：开线程、等阻塞的逻辑，虽然不堵主线程，但是堵我新开的线程。但是异步Socket方法是把等待交给了操作系统，操作系统再来回调.Net，.Net再来执行我给的回调函数
            listenSocket.BeginAccept(OnAcceptedConnectClientSocketCallback, listenSocket);
            
        }
        catch (SocketException e)
        {
            //listenSocket异常了
        }
        catch (ObjectDisposedException e)
        {
            //listenSocket被释放了
        }
    }
}