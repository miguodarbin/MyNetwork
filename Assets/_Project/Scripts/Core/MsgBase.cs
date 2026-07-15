using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 继承MsgBase类的，完成GetAllFieldBytesLength、 WriteInAllFieldBytes、ReadFromAllFieldBytes时也要考虑MsgTypeID！！！把Msg的读写都放在最开头！！！
/// </summary>
public abstract class MsgBase<T> : CanBeBinarySerialize<T> where T : CanBeBinarySerialize<T>, new()
{
    //这个ID是类型的标识符，是我自己定义的,这个msgTypeID似乎还不能重复，不过这里就先不考虑这个问题了，解决思路应该是引用一个管理这个ID的功能
    private int msgTypeID = -999999999;

    public int MsgTypeID
    {
        get { return msgTypeID; }
        set { msgTypeID = value; }
    }

    protected abstract void SetTypeID();

    //==================================================提供给子类的方法==================================================
    /// <summary>
    /// 这个类型头，必须读写字节都要最先读写这个！！！！！
    /// </summary>
    /// <returns></returns>
    protected void WriteMsgTypeIDToAllFieldBytes()
    {
        TrySetMsgTypeID();
        WriteIntFieldToAllFieldBytes(msgTypeID);
    }

    protected void ReadAllFieldBytesToMsgTypeID()
    {
        TrySetMsgTypeID();
        msgTypeID = ReadAllFieldBytesToIntField();
    }

    protected int GetMsgTypeIDCount()
    {
        TrySetMsgTypeID();
        return sizeof(int);
    }


    //外部肯定要用WriteMsgTypeIDToAllFieldBytes、ReadAllFieldBytesToMsgTypeID、GetMsgTypeIDCount三个方法，只要一用，就给这个SetTypeID点火执行
    private void TrySetMsgTypeID()
    {
        if (msgTypeID == -999999999)
        {
            SetTypeID();
        }
    }
}