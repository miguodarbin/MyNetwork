using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UnityWebRequestPlayground : MonoBehaviour
{
    public RawImage image;
    public AudioSource audioSource;

    void Start()
    {
        // UWRManager.Instance.UploadFile("http://192.168.0.188/macos_http_server/",
        //     Application.streamingAssetsPath + "/" + "qqq.png",
        //     "qqqUploadFromUnity.png",
        //     "qqq.png");

        //StartCoroutine(TestHandler());
        StartCoroutine(GetPrefabByHttp());
    }

    IEnumerator GetPrefabByHttp()
    {
        UnityWebRequest request = new UnityWebRequest("http://192.168.0.188/macos_http_server/www.txt");
        request.method = UnityWebRequest.kHttpVerbGET;
        var handler = new DownloadHandlerCustomFile();
        handler.saveFullPath = Application.persistentDataPath + "/www.txt";
        request.downloadHandler = handler;
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
        }
        else
        {
            Debug.Log(request.error);
        }
    }


    IEnumerator TestHandler()
    {
        UnityWebRequest request = new UnityWebRequest("http://192.168.0.188/macos_http_server/example_sfx.mp3");
        request.method = UnityWebRequest.kHttpVerbGET;
        request.downloadHandler = new DownloadHandlerAudioClip(request.url, AudioType.MPEG);
        
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            var audioClip = (request.downloadHandler as DownloadHandlerAudioClip).audioClip;
            audioSource.clip = audioClip;
            audioSource.Play();
        }
        else
        {
            Debug.Log(request.error + request.responseCode);
        }
    }

    IEnumerator LoadTxt()
    {
        //创建了一个unity封装好的HTTP协议下的GET请求对象，用这个请求对象，就可以向我指定的URI的服务器发送符合HTTP协议的GET操作
        UnityWebRequest request = UnityWebRequest.Get("http://192.168.0.188/macos_http_server/www.txt");
        //unity自动帮我处理异步等待，我只用C#侧的协程等待Unity处理好就行
        yield return request.SendWebRequest();
        //判断一下是请求结果是否成功
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            Debug.Log("失败了" + request.error);
        }
    }

    IEnumerator LoadTexture()
    {
        //依旧是创建一个unity封装好的HTTP协议下的GET亲求对象，用这个请求对象，就可以像我指定的URI的服务器发送符合HTTP协议的GET操作
        UnityWebRequest request = UnityWebRequest.Get("http://192.168.0.188/macos_http_server/000.png");
        //给request设置一下响应体处理器，我这边确定拿的肯定是纹理，所以我不需要unity给我默认的DownloadHandlerBuffer
        request.downloadHandler = new DownloadHandlerTexture();
        //unity侧会自动帮我处理异步等待之类的东西，我只需要用协程等待Unity处理完毕就好了
        yield return request.SendWebRequest();
        //处理完毕之后，判断一下是否操作成功
        if (request.result == UnityWebRequest.Result.Success)
        {
            //成功了，就拿出来，由于我配置请求的时候给这个属性放的是DownloadHandlerTexture，所以转换一下。并且DownloadHandlerTexture自动就帮我把字节解码成texture了，很舒服
            Texture texture = ((request.downloadHandler) as DownloadHandlerTexture).texture;
            image.texture = texture;
        }
        else
        {
            Debug.Log("失败了" + request.error + request.responseCode);
        }
    }

    IEnumerator LoadAssetBundle()
    {
        //依旧是创建一个unity封装好的HTTP协议下的GET亲求对象，用这个请求对象，就可以像我指定的URI的服务器发送符合HTTP协议的GET操作
        //并且这个对象，我一开始就用UnityWebRequestAssetBundle.GetAssetBundle指明了：我要用GET方法获取服务器上的AB包，到时候Unity会帮我把字节转成AB包，很舒服
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle("http://192.168.0.188/macos_http_server/_xframework_core");
        //然后发送请求，不让协程系统拿receipt做判断，一会我还要判断进度，我要让协程系统等一帧。所以先发送请求，通知Unity发送起来
        request.SendWebRequest();
        //当请求还没完成的时候，就一直等一帧
        while (request.isDone == false)
        {
            //把下面的逻辑注册给协程系统，等一帧回来继续执行，当一直等一帧，等到request.isDone == true的时候，就不等了
            Debug.Log(request.downloadProgress);
            //Debug.Log(request.downloadedBytes);
            yield return null;
        }

        //加载完了，最后打印一下进度
        Debug.Log(request.downloadProgress);

        if (request.result == UnityWebRequest.Result.Success)
        {
            AssetBundle assetBundle = DownloadHandlerAssetBundle.GetContent(request);
            Debug.Log(assetBundle.name);
        }
        else
        {
            Debug.Log("失败了" + request.error + request.responseCode);
        }
    }
}