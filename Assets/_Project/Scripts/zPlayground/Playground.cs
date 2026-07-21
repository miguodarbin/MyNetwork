using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;


public class Playground : MonoBehaviour
{
    public Transform cube;

    private async void Start()
    {
        await DoSomething();
        Debug.Log("DoSomethingDone");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //这里不去等待 DownloadAsync()，只要遇到未完成的的Task，比如request.GetResponseAsync。就会返回执行权给到DownloadAsync()，
            //DownloadAsync()也有await，就会继续等待，并把执行权给到Update()，Update()这里没有await，所以继续执行接下来的代码。
            //如果Update()这里也有await，那Update这里就被卡住了，但调用Update那边的流程不会卡住
            _ = DownloadAsync();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            //这里不去等待异步操作，遇到暂停，直接继续执行
            _ = UploadAsync();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            _taskSource.SetResult(true);
        }

        cube.transform.Translate(0, 0, 1 * Time.deltaTime);
    }

    //去异步执行下载文件
    private async Task DownloadAsync()
    {
        bool success;
        try
        {
            //遇到暂停，就等着，等到执行完毕拿到结果，判断结果
            success = await HttpManager.Instance.DownloadFileAsync("http://192.168.0.188/macos_http_server/000.png", "111.png");
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            return;
        }

        if (success)
        {
            Debug.Log("Done");
        }
        else
        {
            Debug.Log("Failed");
        }
    }

    //Task表示的是一个操作，未来的完成状态，看看外部要不要等这个方法有结果咯~
    private async Task UploadAsync()
    {
        bool success;
        try
        {
            //这里遇到未完成的Task需要暂停，并等到Task完成，拿到结果
            success = await HttpManager.Instance.UploadFileAsync(
                "http://192.168.0.188/macos_http_server/",
                Application.streamingAssetsPath + "/qqq.png",
                "Image",
                "qqq.png");
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            return;
        }

        if (success)
        {
            Debug.Log("Done");
        }
        else
        {
            Debug.Log("Failed");
        }
    }

    private TaskCompletionSource<bool> _taskSource;

    private Task DoSomething()
    {
        Debug.Log("DoSomething");
        _taskSource = new TaskCompletionSource<bool>();
        return _taskSource.Task;
    }
}