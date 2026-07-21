using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

//每个UnityWebRequest对象，都会有一个downloadHandler，并且UnityWebRequest对象在特定时候，都会调用自己的handler里的函数，并且把响应体字节相关的发给这个脚本对象，
//也就是说，我需要在在UnityWebRequest对象调的这些函数里，写处理响应体的字节
public class DownloadHandlerCustomFile : DownloadHandlerScript
{
    public string saveFullPath;
    private byte[] cacheResponseBody;

    //UnityWebRequest对象接收到响应体的总字节长度之后,就会调这个方法，并且给到长度，所以我可以初始化我缓存响应体的字节数组
    protected override void ReceiveContentLengthHeader(ulong contentLength)
    {
        cacheResponseBody = new byte[contentLength];
    }

    //UnityWebRequest对象收到字节数据之后，就会掉这个方法，这个方法可能会调多次，因为可能响应体一次发不完,所以我需要有一个游标来记录每次写到cacheResponseBody哪里了
    private int cursor = -1; //写到第几个了

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        //只要还没写到请求头中给到的长度，那就一直写
        if (cursor < cacheResponseBody.Length)
        {
            data.CopyTo(cacheResponseBody, cursor + 1);
            cursor += dataLength;
            //告诉Unity继续ReceiveData
            return true;
        }

        //都接收完了，那就告诉Unity，不接受了
        return false;
    }

    //UnityWebRequest对象所有数据都接收完之后，UnityWebRequest对象就会调用这个方法
    protected override void CompleteContent()
    {
        File.WriteAllBytes(saveFullPath, cacheResponseBody);
    }
}