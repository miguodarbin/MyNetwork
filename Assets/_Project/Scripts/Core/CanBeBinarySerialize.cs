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
    protected byte[] allFieldBytes;

    //用来记录当前已经写到总字段字节数组的哪里了？
    private int _bytesReadOrWritePosition = 0;


    //==================================================公开接口==================================================
    /// <summary>
    /// 只要一访问这个方法就触发写入字节，不用子类写了
    /// </summary>
    public byte[] SerializeToBytes()
    {
        InitAllFieldBytes();
        WriteInAllFieldBytes();
        return allFieldBytes;
    }


    public void DeSerializeFormBytes(byte[] bytes)
    {
        AssignmentAllFieldBytes(bytes);
        ReadFromAllFieldBytes();
    }

    //==================================================子类必须实现==================================================
    /// <summary>
    /// 要求子类必须实现获得 总字段字节数组总长度
    /// </summary>
    public abstract int GetAllFieldBytesLength();

    /// <summary>
    /// 要求子类必须实现对这个 总字段的字节数组 的写入，用父类提供的方法写入，必须按字段声明顺序写入！！这个方法由SerializeToBytes驱动，只允许重写不允许乱调，要调就调SerializeToBytes
    /// </summary>
    protected abstract void WriteInAllFieldBytes();

    /// <summary>
    /// 要求子类必须实现对 总字段的字节数组 的读取，用父类提供的方法读取，必须按字段声明顺序读取！！这个方法由DeSerializeFormBytes驱动，只允许重写不允许乱调，要调就调DeSerializeFormBytes
    /// </summary>
    protected abstract void ReadFromAllFieldBytes();


    //==================================================提供给子类的方法==================================================
    /// <summary>
    /// 为总字段的字节数组写入int字段，必须按字段声明顺序写入！！
    /// </summary>
    protected void WriteIntFieldToAllFieldBytes(int value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allFieldBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(int);
    }

    /// <summary>
    /// 为总字段的字节数组写入float字段，必须按字段声明顺序写入！！
    /// </summary>
    protected void WriteFloatFieldToAllFieldBytes(float value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allFieldBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(float);
    }

    /// <summary>
    /// 为总字段的字节数组写入bool字段，必须按字段声明顺序写入！！
    /// </summary>
    protected void WriteBoolFieldToAllFieldBytes(bool value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(allFieldBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(bool);
    }

    /// <summary>
    /// 为总字段的字节数组写入string字段，必须按字段声明顺序写入！！
    /// </summary>
    protected void WriteStringFieldToAllFieldBytes(string value)
    {
        byte[] valueBytes = Encoding.UTF8.GetBytes(value);
        WriteIntFieldToAllFieldBytes(valueBytes.Length);
        valueBytes.CopyTo(allFieldBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += valueBytes.Length;
    }

    /// <summary>
    /// 为总字段的字节数组写入自定义类型但必须继承CanBinarySerialize的字段，必须按字段声明顺序写入！！
    /// </summary>
    /// <param name="value">自定义类型对象</param>
    /// <typeparam name="TK">自定义类型对象的类型</typeparam>
    protected void WriteCustomFieldToAllFieldBytes<TK>(TK value) where TK : CanBeBinarySerialize<TK>, new()
    {
        byte[] valueBytes = value.SerializeToBytes();
        int valueBytesCount = valueBytes.Length;
        WriteIntFieldToAllFieldBytes(valueBytesCount);

        valueBytes.CopyTo(allFieldBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += valueBytes.Length;
    }

    /// <summary>
    /// 获得string类型的value的字节数组长度 + 记录这个字节数组长度的那个int的字节数组长度，也就是头信息和内容的总字节长度
    /// </summary>
    protected int GetStringFieldAllCount(string value)
    {
        int valueBytesCount = Encoding.UTF8.GetBytes(value).Length;
        return valueBytesCount + sizeof(int);
    }

    protected int GetCustomFieldAllCount<TK>(TK value) where TK : CanBeBinarySerialize<TK>, new()
    {
        return sizeof(int) + value.GetAllFieldBytesLength();
    }

    //-------------------------------以下是读数组赋值---------------------------
    protected int ReadAllFieldBytesToIntField()
    {
        int value = BitConverter.ToInt32(allFieldBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(int);
        return value;
    }

    protected float ReadAllFieldBytesToFloatField()
    {
        float value = BitConverter.ToSingle(allFieldBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(float);
        return value;
    }

    protected bool ReadAllFieldBytesToBoolField()
    {
        bool value = BitConverter.ToBoolean(allFieldBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(bool);
        return value;
    }

    protected string ReadAllFieldBytesToStringField()
    {
        int stringBytesLength = ReadAllFieldBytesToIntField();
        string value = Encoding.UTF8.GetString(allFieldBytes, _bytesReadOrWritePosition, stringBytesLength);
        _bytesReadOrWritePosition += stringBytesLength;
        return value;
    }

    protected TK ReadAllFieldBytesToCustomField<TK>() where TK : CanBeBinarySerialize<TK>, new()
    {
        int fieldBytesLength = ReadAllFieldBytesToIntField();
        byte[] fieldBytes = new byte[fieldBytesLength];
        Array.Copy(allFieldBytes, _bytesReadOrWritePosition, fieldBytes, 0, fieldBytesLength);
        TK fieldValue = new TK();
        fieldValue.DeSerializeFormBytes(fieldBytes);
        _bytesReadOrWritePosition += fieldBytesLength;
        return fieldValue;
    }


    //==================================================辅助方法==================================================
    //每次开始完整序列化时，重新创建字节数组并重置写入位置。
    private void InitAllFieldBytes()
    {
        allFieldBytes = new byte[GetAllFieldBytesLength()];
        _bytesReadOrWritePosition = 0;
    }

    //每次开始反序列化时，将外部给到的总字段的字节数组赋值
    private void AssignmentAllFieldBytes(byte[] bytes)
    {
        allFieldBytes = bytes;
        _bytesReadOrWritePosition = 0;
    }
}