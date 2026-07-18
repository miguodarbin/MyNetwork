using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public abstract class CanBeBinarySerializeBase
{
}

/// <summary>
/// 需要被二进制序列化的必须要继承此类，如果序列化的字段里面有自定义类型，那个自定义类型也需要继承此类,注意，string和自定义类型的话的都会写入字节长度的头信息
/// CanBeBinarySerialize里提供的frameBytes没有添加类型头和payload长度头
/// </summary>
/// <typeparam name="T">子类的类型</typeparam>
public abstract class CanBeBinarySerialize<T> : CanBeBinarySerializeBase where T : CanBeBinarySerialize<T>, new()
{
    //==================================================字段属性==================================================
    //内部来维护这个子类型的 总字节数组
    protected byte[] frameBytes;

    //用来记录当前已经写到总字节数组的哪里了？
    private int _bytesReadOrWritePosition = 0;


    //==================================================公开接口==================================================
    /// <summary>
    /// 只要一访问这个方法就触发写入字节，不用子类写了
    /// </summary>
    public byte[] SerializeToFrameBytes()
    {
        InitFrameBytesArray();
        WriteFrameBytes();
        return frameBytes;
    }


    public void DeSerializeFormFrameBytes(byte[] bytes)
    {
        AssignmentToFrameBytes(bytes);
        ReadFromFrameBytes();
    }

    //==================================================子类必须实现==================================================
    /// <summary>
    /// 要求子类必须实现获得 总字节数组总长度
    /// </summary>
    public abstract int GetFrameBytesLength();

    /// <summary>
    /// 要求子类必须实现对这个 总的字节数组 的写入，用父类提供的方法写入，必须按规则和字段声明顺序写入！！这个方法由SerializeToBytes驱动，只允许重写不允许乱调，要调就调SerializeToBytes
    /// </summary>
    protected abstract void WriteFrameBytes();

    /// <summary>
    ///要求子类必须实现对这个 总的字节数组 的写入，用父类提供的方法写入，必须按规则和字段声明顺序写入！！这个方法由DeSerializeFormBytes驱动，只允许重写不允许乱调，要调就调DeSerializeFormBytes
    /// </summary>
    protected abstract void ReadFromFrameBytes();


    //==================================================提供给子类的方法==================================================
    /// <summary>
    /// 为总字节数组写入int类型，必须按规则和字段声明顺序写入！！
    /// </summary>
    protected void WriteIntTypeToFrameBytes(int value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(frameBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(int);
    }

    /// <summary>
    /// 为总字节数组写入float类型，必须按规则和字段声明顺序写入！！
    /// </summary>
    protected void WriteFloatTypeToFrameBytes(float value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(frameBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(float);
    }

    /// <summary>
    /// 为总字节数组写入bool类型，必须按规则和字段声明顺序写入！！
    /// </summary>
    protected void WriteBoolTypeToFrameBytes(bool value)
    {
        byte[] valueBytes = BitConverter.GetBytes(value);
        valueBytes.CopyTo(frameBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(bool);
    }

    /// <summary>
    /// 为总字节数组写入string类型，必须按规则和字段声明顺序写入！！
    /// </summary>
    protected void WriteStringTypeToFrameBytes(string value)
    {
        byte[] valueBytes = Encoding.UTF8.GetBytes(value);
        WriteIntTypeToFrameBytes(valueBytes.Length);
        valueBytes.CopyTo(frameBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += valueBytes.Length;
    }

    /// <summary>
    /// 为总字节数组写入自定义类型但必须继承CanBinarySerialize的类型，必须按规则和字段声明顺序写入！！
    /// </summary>
    /// <param name="value">自定义类型对象</param>
    /// <typeparam name="TK">自定义类型对象的类型</typeparam>
    protected void WriteCustomTypeToFrameBytes<TK>(TK value) where TK : CanBeBinarySerialize<TK>, new()
    {
        byte[] valueBytes = value.SerializeToFrameBytes();
        int valueBytesCount = valueBytes.Length;
        WriteIntTypeToFrameBytes(valueBytesCount);

        valueBytes.CopyTo(frameBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += valueBytes.Length;
    }

    /// <summary>
    /// 获得string类型的value的字节数组长度 + 记录这个字节数组长度的那个int的字节数组长度，也就是头信息和内容的总字节长度
    /// </summary>
    protected int GetStringTypeFrameBytes(string value)
    {
        int valueBytesCount = Encoding.UTF8.GetBytes(value).Length;
        return valueBytesCount + sizeof(int);
    }

    protected int GetCustomTypeAllCount<TK>(TK value) where TK : CanBeBinarySerialize<TK>, new()
    {
        return sizeof(int) + value.GetFrameBytesLength();
    }

    //-------------------------------以下是读数组赋值---------------------------
    protected int ReadFrameBytesToIntType()
    {
        EnsureReadableBytes(sizeof(int));
        int value = BitConverter.ToInt32(frameBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(int);
        return value;
    }

    protected float ReadFrameBytesToFloatType()
    {
        EnsureReadableBytes(sizeof(float));
        float value = BitConverter.ToSingle(frameBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(float);
        return value;
    }

    protected bool ReadFrameBytesToBoolType()
    {
        EnsureReadableBytes(sizeof(bool));
        bool value = BitConverter.ToBoolean(frameBytes, _bytesReadOrWritePosition);
        _bytesReadOrWritePosition += sizeof(bool);
        return value;
    }

    protected string ReadFrameBytesToStringType()
    {
        int stringBytesLength = ReadFrameBytesToIntType();

        // 网络提供的字符串长度不能为负数。
        if (stringBytesLength < 0)
        {
            throw new InvalidDataException(
                $"协议读取失败：字符串字节长度不能为负数，" +
                $"length = {stringBytesLength}"
            );
        }

        // 确认字符串内容没有越过当前数组末尾。
        EnsureReadableBytes(stringBytesLength);


        string value = Encoding.UTF8.GetString(frameBytes, _bytesReadOrWritePosition, stringBytesLength);
        _bytesReadOrWritePosition += stringBytesLength;
        return value;
    }

    protected TK ReadFrameBytesToCustomType<TK>() where TK : CanBeBinarySerialize<TK>, new()
    {
        int fieldBytesLength = ReadFrameBytesToIntType();

        if (fieldBytesLength < 0)
        {
            throw new InvalidDataException(
                $"协议读取失败：自定义类型字节长度不能为负数，" +
                $"length = {fieldBytesLength}"
            );
        }

        // 必须先验证，再创建数组。
        EnsureReadableBytes(fieldBytesLength);


        byte[] fieldBytes = new byte[fieldBytesLength];
        Array.Copy(frameBytes, _bytesReadOrWritePosition, fieldBytes, 0, fieldBytesLength);
        TK fieldValue = new TK();
        fieldValue.DeSerializeFormFrameBytes(fieldBytes);
        _bytesReadOrWritePosition += fieldBytesLength;
        return fieldValue;
    }


    //==================================================辅助方法==================================================
    //每次开始完整序列化时，重新创建字节数组并重置写入位置。
    private void InitFrameBytesArray()
    {
        frameBytes = new byte[GetFrameBytesLength()];
        _bytesReadOrWritePosition = 0;
    }

    //每次开始反序列化时，将外部给到的总字节数组赋值
    private void AssignmentToFrameBytes(byte[] bytes)
    {
        frameBytes = bytes;
        _bytesReadOrWritePosition = 0;
    }

    /// <summary>
    /// 先检查，后执行~~我接下来要读 count 个字节，当前AllBytes剩余字节够不够？够的话就真的去走赋值和移动指针的逻辑，不够的话，不会去执行赋值和移动指针的逻辑
    /// </summary>
    private void EnsureReadableBytes(int count)
    {
        // 需要读取的长度本身不能是负数。
        if (count < 0)
        {
            throw new InvalidDataException(
                $"协议读取失败：读取长度不能为负数，count = {count}"
            );
        }

        if (frameBytes == null)
        {
            throw new InvalidDataException(
                "协议读取失败：当前字节数组为空。"
            );
        }

        // 理论上的状态保护，防止游标已经处于非法位置。
        if (_bytesReadOrWritePosition < 0 ||
            _bytesReadOrWritePosition > frameBytes.Length)
        {
            throw new InvalidDataException(
                $"协议读取失败：读取位置非法，" +
                $"position = {_bytesReadOrWritePosition}，" +
                $"arrayLength = {frameBytes.Length}"
            );
        }

        int remainingBytes =
            frameBytes.Length - _bytesReadOrWritePosition;

        // 比较剩余长度，而不是计算 position + count，
        // 避免加法自身发生整数溢出。
        if (count > remainingBytes)
        {
            throw new InvalidDataException(
                $"协议读取失败：剩余字节不足。" +
                $"需要读取 {count} 字节，" +
                $"实际只剩 {remainingBytes} 字节。"
            );
        }
    }
}