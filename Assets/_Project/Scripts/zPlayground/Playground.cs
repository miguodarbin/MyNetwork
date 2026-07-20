using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


public class Playground : MonoBehaviour
{
    //定义一个可以装1024个字节的水桶数组，去操作系统那边捞接收到的字节
    public byte[] bucket = new byte[1024];

    private void Start()
    {
        //先声明一个客户端Socket，用于程序是用TCP协议进行网络通讯
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //然后开始异步连接，然后就继续主线程，我需要注册一个连接任务完成后的回调，注意这里调用回调的时候并不代表已经连接上了，只是说连接有结果了，具体是失败还是成功，需要EndConnect的时候才知道
        clientSocket.BeginConnect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080), (receipt) =>
        {
            Socket clientSocket = receipt.AsyncState as Socket;
            try
            {
                //到这一步才能知道到底连接结果是成功还是失败
                clientSocket.EndConnect(receipt);
            }
            catch (SocketException)
            {
            }
        }, clientSocket);

        //然后开始异步接收消息
        clientSocket.BeginReceive(bucket, 0, 0, SocketFlags.None, OnReceiveHaveResult, clientSocket);

        //发送一个消息帧
        byte[] msgFrame = Encoding.UTF8.GetBytes("Hello World!");
        clientSocket.BeginSend(msgFrame, 0, msgFrame.Length, SocketFlags.None, (receipt) =>
        {
            Socket clientSocket = receipt.AsyncState as Socket;
            try
            {
                clientSocket.EndSend(receipt);
            }
            catch (SocketException)
            {
                //发送的时候，ClientSocket有错误了
            }

        }, clientSocket);
    }

    //当接受消息的操作有了结果
    private void OnReceiveHaveResult(IAsyncResult receipt)
    {
        Socket clientSocket = receipt.AsyncState as Socket;
        int receivedBytesCount;
        try
        {
            receivedBytesCount = clientSocket.EndReceive(receipt);
        }
        catch (SocketException)
        {
            //接受的时候，ClientSocket有错误了
            return;
        }

        //处理消息
        string msg = Encoding.UTF8.GetString(bucket, 0, receivedBytesCount);
        //然后继续接受消息
        clientSocket.BeginReceive(bucket, 0, 0, SocketFlags.None, OnReceiveHaveResult, clientSocket);
    }
}