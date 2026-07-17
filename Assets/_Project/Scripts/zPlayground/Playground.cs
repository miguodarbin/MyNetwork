using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Playground : MonoBehaviour
{
    private static readonly object locker = new object();
    int count = 0;

    private void Awake()
    {
        Task[] tasks = new Task[100];
        for (int i = 0; i < 100; i++)
        {
            Task t = Task.Run(() =>
            {
                lock (locker)
                {
                    int oldCount = count;
                    Thread.Sleep(10);
                    int newCount = oldCount + 1;
                    count = newCount;
                }
            });
            tasks[i] = t;
        }

        Task.WhenAll(tasks).ContinueWith((tasks) => { Debug.Log(count); });
    }
}