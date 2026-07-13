using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 需要被二进制序列化的必须要继承此类，如果序列化的字段里面有自定义类型，那个自定义类型也需要继承此类
/// </summary>
public abstract class CanBinarySerialize
{
    //==================================================字段属性==================================================
    //内部来维护这个总字段字节数组
    protected byte[] allFieldBytes;

    //用来记录当前已经写到总字段字节数组的哪里了？
    private int _bytesWritePosition = 0;

    //==================================================子类必须实现==================================================
    //1.要求子类必须实现获得 总字段字节数组总长度
    public abstract int GetAllFieldBytesLength();

    //2. 父类声明了一个总字段的字节数组，要求子类必须实现对这个 总字节数组 的初始化和写入
    public abstract void WriteInAllFieldBytes();

    //3. 要求子类提供一个方法，返回出去这个 总字段的字节数组
    public abstract byte[] GetAllFieldBytes();

    //==================================================提供给子类的方法==================================================
    /// <summary>
    /// 为总字段的字节数组写入int字段，必须要按照子类里的字段顺序调
    /// </summary>
    protected void WriteIntFieldToAllFieldBytes(int value)
    {
        TryInitAllFieldBytes();
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allFieldBytes, _bytesWritePosition);
        _bytesWritePosition += sizeof(int);
    }

    protected void WriteFloatFieldToAllFieldBytes(float value)
    {
        TryInitAllFieldBytes();
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allFieldBytes, _bytesWritePosition);
        _bytesWritePosition += sizeof(float);
    }

    protected void WriteBoolFieldToAllFieldBytes(bool value)
    {
        TryInitAllFieldBytes();
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allFieldBytes, _bytesWritePosition);
        _bytesWritePosition += sizeof(bool);
    }

    protected void WriteStringFieldToAllFieldBytes(string value)
    {
        TryInitAllFieldBytes();
        byte[] valueBytes = Encoding.UTF8.GetBytes(value);
        WriteIntFieldToAllFieldBytes(valueBytes.Length);
        valueBytes.CopyTo(allFieldBytes, _bytesWritePosition);
        _bytesWritePosition += valueBytes.Length;
    }


    protected void WriteCustomFieldToAllFieldBytes(CanBinarySerialize value)
    {
        TryInitAllFieldBytes();
        byte[] valueBytes = value.GetAllFieldBytes();
        valueBytes.CopyTo(allFieldBytes, _bytesWritePosition);
        _bytesWritePosition += value.GetAllFieldBytesLength();
    }

    //==================================================辅助方法==================================================
    private void TryInitAllFieldBytes()
    {
        if (allFieldBytes != null)
        {
            return;
        }

        allFieldBytes = new byte[GetAllFieldBytesLength()];

        _bytesWritePosition = 0;
    }
}