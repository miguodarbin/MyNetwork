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

    //==================================================公开接口==================================================
    /// <summary>
    /// 只要一访问这个方法就触发写入字节
    /// </summary>
    public byte[] GetAllFieldBytes()
    {
        InitAllFieldBytes();
        WriteInAllFieldBytes();
        return allFieldBytes;
    }

    //==================================================子类必须实现==================================================
    /// <summary>
    /// 要求子类必须实现获得 总字段字节数组总长度
    /// </summary>
    public abstract int GetAllFieldBytesLength();

    /// <summary>
    /// 要求子类必须实现对这个 总字节数组 的和写入，必须按字段声明顺序写入！！这个方法由GetAllFieldBytes驱动
    /// </summary>
    protected abstract void WriteInAllFieldBytes();


    //==================================================提供给子类的方法==================================================
    /// <summary>
    /// 为总字段的字节数组写入int字段，必须按字段声明顺序写入！！
    /// </summary>
    protected void WriteIntFieldToAllFieldBytes(int value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allFieldBytes, _bytesWritePosition);
        _bytesWritePosition += sizeof(int);
    }

    /// <summary>
    /// 为总字段的字节数组写入float字段，必须按字段声明顺序写入！！
    /// </summary>
    protected void WriteFloatFieldToAllFieldBytes(float value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allFieldBytes, _bytesWritePosition);
        _bytesWritePosition += sizeof(float);
    }

    /// <summary>
    /// 为总字段的字节数组写入bool字段，必须按字段声明顺序写入！！
    /// </summary>
    protected void WriteBoolFieldToAllFieldBytes(bool value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allFieldBytes, _bytesWritePosition);
        _bytesWritePosition += sizeof(bool);
    }

    /// <summary>
    /// 为总字段的字节数组写入string字段，必须按字段声明顺序写入！！
    /// </summary>
    protected void WriteStringFieldToAllFieldBytes(string value)
    {
        byte[] valueBytes = Encoding.UTF8.GetBytes(value);
        WriteIntFieldToAllFieldBytes(valueBytes.Length);
        valueBytes.CopyTo(allFieldBytes, _bytesWritePosition);
        _bytesWritePosition += valueBytes.Length;
    }

    /// <summary>
    /// 为总字段的字节数组写入自定义类型但必须继承CanBinarySerialize的字段，必须按字段声明顺序写入！！
    /// </summary>
    protected void WriteCustomFieldToAllFieldBytes(CanBinarySerialize value)
    {
        byte[] valueBytes = value.GetAllFieldBytes();
        valueBytes.CopyTo(allFieldBytes, _bytesWritePosition);
        _bytesWritePosition += valueBytes.Length;
    }

    /// <summary>
    /// 获得string类型的value的字节数组长度 + 记录这个字节数组长度的那个int的字节数组长度，也就是头信息和内容的总字节长度
    /// </summary>
    protected int GetStringFieldAllCount(string value)
    {
        int valueBytesCount = Encoding.UTF8.GetBytes(value).Length;
        return valueBytesCount + sizeof(int);
    }

    //==================================================辅助方法==================================================
    //每次开始完整序列化时，重新创建字节数组并重置写入位置。
    private void InitAllFieldBytes()
    {
        allFieldBytes = new byte[GetAllFieldBytesLength()];
        _bytesWritePosition = 0;
    }
}