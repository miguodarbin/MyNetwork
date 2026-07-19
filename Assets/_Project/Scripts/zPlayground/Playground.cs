using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Playground : MonoBehaviour
{
    private void Start()
    {
        DateTime currentHeatTime = DateTime.Now;
        Debug.Log(currentHeatTime);
    }
}