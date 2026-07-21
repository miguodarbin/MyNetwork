using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class UWRManager : MonoBehaviour
{
    //单例
    public static UWRManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    //上传文件的启动器
    public void UploadFile(string url, string fileFullPath, string fileServerName, string fileName)
    {
        StartCoroutine(ReallyUploadFile(url, fileFullPath, fileServerName, fileName));
    }

    //上传文件的真正逻辑
    private IEnumerator ReallyUploadFile(string url, string fileFullPath, string fileServerName, string fileName)
    {
        //首先得先判断一下这个文件在不在
        if (!File.Exists(fileFullPath))
        {
            Debug.Log("文件不存在");
            yield break;
        }

        //然后把文件转换成字节数组
        byte[] fileBytes = File.ReadAllBytes(fileFullPath);

        //然后上传文件是一个HTTP请求，并且报文结构里面的请求方法是Post，所以说先创建一个UnityWebRequest的请求对象.
        //然后还要把上传的对象封装到Unity提供的容器类里：MultipartFormDataSection（主要用来装键值对参数）、MultipartFormFileSection（主要是用来装文件数据）
        //并把这些文件统一放到List<IMultipartFormSection>中，传给UnityWebRequest的Post，Unity封装的这个Post方法会自动帮我把文件格式转成HTTP所能接受的格式，很爽了
        //首先先创建交给Post方法的那个参数对象
        List<IMultipartFormSection> datas = new List<IMultipartFormSection>();
        //然后具体往datas里面添加内容了，题目说的是要上传文件，那就不用MultipartFormDataSection这种装键值对参数的容器了
        //用MultipartFormFileSection容器来装
        MultipartFormFileSection fileSection = new MultipartFormFileSection(fileServerName, fileBytes, fileName, "application/octet-stream");
        
        //装完之后放到List中，作为容器给到Post
        datas.Add(fileSection);
        using (UnityWebRequest request = UnityWebRequest.Post(url, datas))
        {
            //还得配置一下权限
            string userInfo = "weijia:123";
            string base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(userInfo));
            request.SetRequestHeader("Authorization", $"Basic {base64}");
            //然后就算配置完请求，发送请求就好了
            request.SendWebRequest();
            //然后等待一帧，等待的条件是：这次请求还没完成
            while (!request.isDone)
            {
                //不光等一帧，还顺手打印一下进度，美滋滋
                Debug.Log(request.uploadProgress);
                yield return null;
            }

            //完成之后也打印一下进度，看着变成1舒服点
            Debug.Log(request.uploadProgress);
            //然后看看请求这个操作的结果
            if (request.result == UnityWebRequest.Result.Success)
            {
                //如果成功了，那就打印一下成功
                Debug.Log("上传成功");
            }
            else
            {
                //失败了，就打印失败
                Debug.Log("失败了" + request.error);
            }
        }
    }
}