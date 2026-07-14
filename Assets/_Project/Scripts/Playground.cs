using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Playground : MonoBehaviour
{
    private void Start()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("Hello World!");
        string value = Encoding.UTF8.GetString(bytes, 1, 1);
        Debug.Log(value);
    }
}