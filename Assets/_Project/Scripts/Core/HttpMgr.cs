using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class HttpManager
{
    //单例
    private static HttpManager _instance = new HttpManager();

    public static HttpManager Instance
    {
        get { return _instance; }
    }

    private HttpManager()
    {
    }

    public string downloadPath => Application.persistentDataPath;

    //下载文件,用异步方法，返回值是整个这个DownloadFile的任务，下载成功了返回True，失败了返回False
    public async Task<bool> DownloadFileAsync(string url, string fileName)
    {
        //首先，我要下载文件，那就说明，我需要让我的程序用HTTP的规则去和一个我指定的URL进行交互，这边的交互是下载资源，那我想让我的应用程序能用HTTP规则去和URL交互该怎么办？
        //用HttpWebRequest：这是一个C#封装好的，可以按HTTP处理数据的规则去访问服务器的一个对象，通过这个对象，我可以向我指定的服务器发送请求，并拿到结果
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //创建完这个request之后，此时还没有真正的发送这个请求，还要配置一下这个request，一般情况下都是从C#提供的属性、HTTP报文结构这两个方面去考虑看看有没有需要配置的
        //比如可以设置一个超时时间
        request.Timeout = 2000;
        //并且我这个Download方法主要是去下载资源的，所以我让报文结构请求行李的请求方法为：Get
        request.Method = "GET";
        //然后就可以把这个请求发出去了
        //到这一步，进程就可以继续往下了，说是往下，其实是回到调用者那边去。下面的代码会根据request.GetResponseAsync()的完成情况来决定是同步执行还是等request.GetResponseAsync()完成了在执行，如果request.GetResponseAsync()没有完成的话，下面的代码会注册为request.GetResponseAsync()完成时的回调来执行
        //await还能拿到Task的结果
        try //网络相关的都捕获一下异常，早已习惯
        {
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                //到这一步，Task完成了，且response也被赋上值了
                //首先看一下服务器给到的回应状态码,如果不OK的话，那就不用继续了，直接结束
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                //然后我需要创建一个文件,这里就简单一点，就直接下载到persist文件夹，并且按照外部给的文件名创建这个文件,这里不需要传后缀吗？困惑......
                using (FileStream fileStream = File.Create(downloadPath + "/" + fileName))
                {
                    //然后声明一个中转站，作为把response里面的字节传到fileStream里的容器，最后由fileStream.Write方法写入磁盘的文件当中
                    byte[] buffer = new byte[1024];
                    //得到response的Stream
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        //开始用Stream这个字节管道，把response里的字节拿出来，由于可能不是一次性都能拿出来，所以需要专门处理一下
                        int readBytesCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                        //读出一部分了，先写入到fileStream里
                        await fileStream.WriteAsync(buffer, 0, readBytesCount);
                        //然后可能没读完，不过有个信号可以利用，就是读不出东西来的时候，readBytesCount就是0了
                        //所以就可以这样：只要readBytesCount不为0，那就一直读，读到为0为止
                        while (readBytesCount != 0)
                        {
                            readBytesCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                            await fileStream.WriteAsync(buffer, 0, readBytesCount);
                        }

                        //读完了，Flush一下，把fileStream的字节冲到文件里
                        fileStream.Flush();
                    }
                }
            }
        }
        catch (WebException e)
        {
            Debug.Log(e.Message);
            return false;
        }

        return true;
    }

    //上传文件
    public async Task<bool> UploadFileAsync(string url, string fileFullPath, string serverFileName, string fileName)
    {
        //首先先判断一下给的文件存不存在
        if (!File.Exists(fileFullPath))
        {
            Debug.Log("文件不存在");
            return false;
        }

        //然后创建一个HTTP请求，作为我代码可以访问HTTP服务器的一个对象
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //然后配置一下这个request，超时设置10秒
        request.Timeout = 10000;
        //请求行的请求方法为POST
        request.Method = "POST";
        //正式发出请求之前还要处理一下权限
        request.Credentials = new NetworkCredential("weijia", "123");
        //并开启权限
        request.PreAuthenticate = true;
        //然后就可以设置一下请求体的格式了，记住还要有boundary
        request.ContentType = "multipart/form-data; boundary=weijia";
        //然后就开始写请求体了，按照格式写
        string descriptionHeader = "--weijia\r\n" +
                                   $"Content-Disposition: form-data; name=\"{serverFileName}\"; filename=\"{fileName}\"\r\n" +
                                   "Content-Type: application/octet-stream\r\n\r\n";
        string tail = "\r\n--weijia--\r\n";
        //然后把字符串转成字节数组
        byte[] descriptionHeaderBytes = Encoding.UTF8.GetBytes(descriptionHeader);
        byte[] tailBytes = Encoding.UTF8.GetBytes(tail);
        //这里要想一下，是把文件内容直接先都读到内存里好，还是读一点，存一点，上传一点？
        //嗯，还是分块读取写入吧
        request.AllowWriteStreamBuffering = false;
        using (FileStream fileStream = File.OpenRead(fileFullPath))
        {
            //然后还要配置一下请求体的长度
            request.ContentLength = descriptionHeaderBytes.Length + tailBytes.Length + fileStream.Length;

            //得到request的Stream，开始给request的流写入请求体
            using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                //先写入描述头
                await requestStream.WriteAsync(descriptionHeaderBytes, 0, descriptionHeaderBytes.Length);
                //在读取文件，读一点，写一点,每次都读1024个字节
                byte[] buffer = new byte[1024];

                int readBytes = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                await requestStream.WriteAsync(buffer, 0, readBytes);
                //可能一次读不完，一直读到读不出东西来
                while (readBytes != 0)
                {
                    readBytes = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                    await requestStream.WriteAsync(buffer, 0, readBytes);
                }


                //最后再写入尾巴
                await requestStream.WriteAsync(tailBytes, 0, tailBytes.Length);
            }
        }

        //写完之后就可以提交request，等回应了
        try
        {
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                //看下状态码
                Debug.Log(response.StatusCode);
            }
        }
        catch (WebException e)
        {
            Debug.Log(e.Message);
            return false;
        }

        return true;
    }
}