using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

//思路
//创建一个ListenSocket，作为服务器的监听插座，客户端的插座都来找这个监听插座
//然后给这个ListenSocket绑定服务器的IP和端口号
//然后开始ListenSocket开始监听
//如何处理监听到的客户端——————单开一个Task，专门处理监听客户端，实现允许多个客户端连接，并在连接到客户端的时候向客户端发送消息
//怎么对客户端进行收发消息————再单开一个Task专门进行对客户端收消息，发消息通过b：来发送
//关闭服务器

namespace MyWindowsServer;

public class Program
{
    public static void Main(string[] args)
    {
        ListenSocket listenSocket = new ListenSocket();
        listenSocket.StartListen("192.168.0.196", 8080);
        Console.WriteLine("开始监听...");

        //服务器的控制台交互
        while (true)
        {
            string input = Console.ReadLine();
            if (String.IsNullOrEmpty(input))
            {
                continue;
            }

            if (input == "quit")
            {
                listenSocket.CloseListenSocketAndCommSocket();
                break;
            }

            if (input == "SendPlayerMsg")
            {
                PlayerMsg playerMsg = new PlayerMsg(3, new PlayerData("小炮", 3.14f, 88, false));
                listenSocket.BroadcastToAllClients(playerMsg.SerializeToBytes());
            }

            if (input == "nianbao")
            {
                StringMsg msg = new StringMsg("黏包的String");
                StringMsg msg2 = new StringMsg("NianBaoのString");
                byte[] nianBaoBytes = msg.SerializeToBytes().Concat(msg2.SerializeToBytes()).ToArray();
                listenSocket.BroadcastToAllClients(nianBaoBytes);
                Console.WriteLine("已发送");
            }

            if (input == "fenbao")
            {
                StringMsg msg = new StringMsg("黏包的String");
                byte[] nianBaoBytes = msg.SerializeToBytes();
                byte[] first = nianBaoBytes[0..8];
                byte[] second = nianBaoBytes[8..];
                listenSocket.BroadcastToAllClients(first);
                Thread.Sleep(1000);
                listenSocket.BroadcastToAllClients(second);
                Console.WriteLine("已发送");
            }

            if (input == "fn")
            {
                StringMsg msg = new StringMsg("黏包的String");
                StringMsg msg2 = new StringMsg("NianBaoのString");
                byte[] nianBaoBytes = msg.SerializeToBytes().Concat(msg2.SerializeToBytes()).ToArray();

                byte[] first = nianBaoBytes[0..25];
                byte[] second = nianBaoBytes[25..];

                listenSocket.BroadcastToAllClients(first);
                Thread.Sleep(1000);
                listenSocket.BroadcastToAllClients(second);
                Console.WriteLine("已发送");
            }

            if (input[0..Math.Min(2, input.Length)] == "b:")
            {
                string msg = input[Math.Min(2, input.Length)..input.Length];
                StringMsg stringMsg = new StringMsg(msg);
                listenSocket.BroadcastToAllClients(stringMsg.SerializeToBytes());
            }
        }

        Console.ReadKey();
    }
}