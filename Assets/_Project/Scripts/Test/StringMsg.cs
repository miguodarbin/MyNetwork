using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StringMsg : MsgBase<StringMsg>
{
    private string msg;

    public override int GetAllFieldBytesLength()
    {
        return GetMsgTypeIDCount() + GetStringFieldAllCount(msg);
    }

    protected override void WriteInAllFieldBytes()
    {
        WriteStringFieldToAllFieldBytes(msg);
    }

    protected override void ReadFromAllFieldBytes()
    {
        msg = ReadAllFieldBytesToStringField();
    }

    protected override void SetMsgTypeID()
    {
        MsgTypeID = 1000;
    }
}