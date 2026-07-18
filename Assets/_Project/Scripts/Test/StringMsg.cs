using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StringMsg : MsgBase<StringMsg>
{
    public string msg;

    public StringMsg()
    {
    }

    public StringMsg(string msg)
    {
        this.msg = msg;
    }

    //这里我没有想到好怎么优化，就先用缓存方案吧，怕以后忘了这里需要缓存，不过也无所谓，不缓存的话，多运行几次GetStringTypeAllCount之类的方法而已
    private int cacheMsgBodyCount;

    public override int GetFrameBytesLength()
    {
        cacheMsgBodyCount = GetStringTypeFrameBytes(msg);
        return GetMsgHeaderCount() + cacheMsgBodyCount;
    }

    protected override void WriteFrameBytes()
    {
        WriteMsgHeaderToAllFieldBytes();
        WriteStringTypeToFrameBytes(msg);
    }

    protected override void ReadFromFrameBytes()
    {
        ReadAllFieldBytesToMsgHeader();
        msg = ReadFrameBytesToStringType();
    }

    protected override void SetMsgTypeID()
    {
        MsgTypeID = 1000;
    }

    protected override void SetPayloadLength()
    {
        PayloadLength = cacheMsgBodyCount;
    }
}