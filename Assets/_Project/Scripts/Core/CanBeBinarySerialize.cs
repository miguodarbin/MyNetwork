using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public abstract class CanBeBinarySerializeBase
{
}

/// <summary>
/// 需要被二进制序列化的必须要继承此类，如果序列化的字段里面有自定义类型，那个自定义类型也需要继承此类,注意，string和自定义类型的话的都会写入字节长度的头信息
/// </summary>
/// <typeparam name="T">子类的类型</typeparam>
public abstract class CanBeBinarySerialize<T> : CanBeBinarySerializeBase where T : CanBeBinarySerialize<T>, new()
{
    //==================================================字段属性==================================================
    //内部来维护这个子类型的 总字段字节数组
    protected byte[] allBytes;

    //用来记录当前已经写到总字段字节数组的哪里了？
    private int _bytesReadOrWritePosition = 0;


    //==================================================公开接口==================================================
    /// <summary>
    /// 只要一访问这个方法就触发写入字节，不用子类写了
    /// </summary>
    public byte[] SerializeToBytes()
    {
        InitAllBytes();
        WriteInAllFieldBytes();
        return allBytes;
    }


    public void DeSerializeFormBytes(byte[] bytes)
    {
        AssignmentAllBytes(bytes);
        ReadFromAllBytes();
    }

    //==================================================子类必须实现==================================================
    /// <summary>
    /// 要求子类必须实现获得 总字节数组总长度
    /// </summary>
    public abstract int GetAllBytesLength();

    /// <summary>
    /// 要求子类必须实现对这个 总的字节数组 的写入，用父类提供的方法写入，必须按规则和字段声明顺序写入！！这个方法由SerializeToBytes驱动，只允许重写不允许乱调，要调就调SerializeToBytes
    /// </summary>
    protected abstract void WriteInAllFieldBytes();

    /// <summary>
    ///要求子类必须实现对这个 总的字节数组 的写入，用父类提供的方法写入，必须按规则和字段声明顺序写入！！这个方法由DeSerializeFormBytes驱动，只允许重写不允许乱调，要调就调DeSerializeFormBytes
    /// </summary>
    protected abstract void ReadFromAllBytes();


    //==================================================提供给子类的方法==================================================
    /// <summary>
    /// 为总字节数组写入int类型，必须按规则和字段声明顺序写入！！
    /// </summary>
    protected void WriteIntTypeToAllBytes(int value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(int);
    }

    /// <summary>
    /// 为总字节数组写入float类型，必须按规则和字段声明顺序写入！！
    /// </summary>
    protected void WriteFloatTypeToAllBytes(float value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(float);
    }

    /// <summary>
    /// 为总字节数组写入bool类型，必须按规则和字段声明顺序写入！！
    /// </summary>
    protected void WriteBoolTypeToAllBytes(bool value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(bool);
    }

    /// <summary>
    /// 为总字节数组写入string类型，必须按规则和字段声明顺序写入！！
    /// </summary>
    protected void WriteStringTypeToAllBytes(string value)
    {
        byte[] valueBytes = Encoding.UTF8.GetBytes(value);
        WriteIntTypeToAllBytes(valueBytes.Length);
        valueBytes.CopyTo(allBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += valueBytes.Length;
    }

    /// <summary>
    /// 为总字节数组写入自定义类型但必须继承CanBinarySerialize的类型，必须按规则和字段声明顺序写入！！
    /// </summary>
    /// <param name="value">自定义类型对象</param>
    /// <typeparam name="TK">自定义类型对象的类型</typeparam>
    protected void WriteCustomTypeToAllBytes<TK>(TK value) where TK : CanBeBinarySerialize<TK>, new()
    {
        byte[] valueBytes = value.SerializeToBytes();
        int valueBytesCount = valueBytes.Length;
        WriteIntTypeToAllBytes(valueBytesCount);

        valueBytes.CopyTo(allBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += valueBytes.Length;
    }

    /// <summary>
    /// 获得string类型的value的字节数组长度 + 记录这个字节数组长度的那个int的字节数组长度，也就是头信息和内容的总字节长度
    /// </summary>
    protected int GetStringTypeAllCount(string value)
    {
        int valueBytesCount = Encoding.UTF8.GetBytes(value).Length;
        return valueBytesCount + sizeof(int);
    }

    protected int GetCustomTypeAllCount<TK>(TK value) where TK : CanBeBinarySerialize<TK>, new()
    {
        return sizeof(int) + value.GetAllBytesLength();
    }

    //-------------------------------以下是读数组赋值---------------------------
    protected int ReadAllBytesToIntType()
    {
        int value = BitConverter.ToInt32(allBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(int);
        return value;
    }

    protected float ReadAllBytesToFloatType()
    {
        float value = BitConverter.ToSingle(allBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(float);
        return value;
    }

    protected bool ReadAllBytesToBoolType()
    {
        bool value = BitConverter.ToBoolean(allBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(bool);
        return value;
    }

    protected string ReadAllBytesToStringType()
    {
        int stringBytesLength = ReadAllBytesToIntType();
        string value = Encoding.UTF8.GetString(allBytes, _bytesReadOrWritePosition, stringBytesLength);
        _bytesReadOrWritePosition += stringBytesLength;
        return value;
    }

    protected TK ReadAllBytesToCustomType<TK>() where TK : CanBeBinarySerialize<TK>, new()
    {
        int fieldBytesLength = ReadAllBytesToIntType();
        byte[] fieldBytes = new byte[fieldBytesLength];
        Array.Copy(allBytes, _bytesReadOrWritePosition, fieldBytes, 0, fieldBytesLength);
        TK fieldValue = new TK();
        fieldValue.DeSerializeFormBytes(fieldBytes);
        _bytesReadOrWritePosition += fieldBytesLength;
        return fieldValue;
    }


    //==================================================辅助方法==================================================
    //每次开始完整序列化时，重新创建字节数组并重置写入位置。
    private void InitAllBytes()
    {
        allBytes = new byte[GetAllBytesLength()];
        _bytesReadOrWritePosition = 0;
    }

    //每次开始反序列化时，将外部给到的总字节数组赋值
    private void AssignmentAllBytes(byte[] bytes)
    {
        allBytes = bytes;
        _bytesReadOrWritePosition = 0;
    }
}