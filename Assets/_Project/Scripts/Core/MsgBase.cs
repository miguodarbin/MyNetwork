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

    //这个是消息体的长度，是我自己定义的，负数的时候就是没初始化
    private int msgBodyCount = -888888888;

    public int MsgTypeID
    {
        get { return msgTypeID; }
        set { msgTypeID = value; }
    }

    public int MsgBodyCount
    {
        get { return msgBodyCount; }
        set { msgBodyCount = value; }
    }

    /// <summary>
    /// 子类设置消息类型ID是多少
    /// </summary>
    protected abstract void SetMsgTypeID();

    /// <summary>
    /// 子类实现消息的长度是多少
    /// </summary>
    protected abstract void SetMsgBodyCount();

    //==================================================提供给子类的方法==================================================
    /// <summary>
    /// 这个消息头，里面有类型头和消息体长度头，必须读写字节都要最先读写这个！！！！！
    /// </summary>
    /// <returns></returns>
    protected void WriteMsgHeaderToAllFieldBytes()
    {
        TrySetMsgTypeID();
        WriteIntTypeToAllBytes(msgTypeID);
        WriteIntTypeToAllBytes(msgBodyCount);
    }

    protected void ReadAllFieldBytesToMsgHeader()
    {
        msgTypeID = ReadAllBytesToIntType();
        msgBodyCount = ReadAllBytesToIntType();
    }

    protected int GetMsgHeaderCount()
    {
        TrySetMsgTypeID();
        //第一个int是类型Type的信息，第二个int是消息长度的信息
        return sizeof(int) + sizeof(int);
    }


    //外部肯定要用WriteMsgTypeIDToAllFieldBytes、ReadAllFieldBytesToMsgTypeID、GetMsgTypeIDCount三个方法，只要一用，就给这个SetTypeID、SetMsgBodyCount点火执行
    private void TrySetMsgTypeID()
    {
        if (msgTypeID == -999999999)
        {
            SetMsgTypeID();
        }

        if (msgBodyCount == -888888888)
        {
            SetMsgBodyCount();
        }
    }
}