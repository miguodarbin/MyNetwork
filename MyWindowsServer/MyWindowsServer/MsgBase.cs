using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 继承MsgBase类的，完成GetAllFieldBytesLength、 WriteInAllFieldBytes、ReadFromAllFieldBytes时也要考虑MsgTypeID！！！把Msg的读写都放在最开头！！！
/// 只有MsgBase才提供类型头和payload长度头
/// </summary>
public abstract class MsgBase<T> : CanBeBinarySerialize<T> where T : CanBeBinarySerialize<T>, new()
{
    //这个ID是类型的标识符，是我自己定义的,这个msgTypeID似乎还不能重复，不过这里就先不考虑这个问题了，解决思路应该是引用一个管理这个ID的功能
    private int msgTypeID = -999999999;

    //这个是消息体的长度，是我自己定义的，负数的时候就是没初始化
    private int payloadLength = -888888888;

    public int MsgTypeID
    {
        get { return msgTypeID; }
        set { msgTypeID = value; }
    }

    public int PayloadLength
    {
        get { return payloadLength; }
        set { payloadLength = value; }
    }

    /// <summary>
    /// 子类设置消息类型ID是多少
    /// </summary>
    protected abstract void SetMsgTypeID();

    /// <summary>
    /// 子类实现消息的长度是多少
    /// </summary>
    protected abstract void SetPayloadLength();

    //==================================================提供给子类的方法==================================================
    /// <summary>
    /// 这个消息头，里面有类型头和消息体长度头，必须读写字节都要最先读写这个！！！！！
    /// </summary>
    /// <returns></returns>
    protected void WriteMsgHeaderToAllFieldBytes()
    {
        TrySetMsgTypeID();
        WriteIntTypeToFrameBytes(msgTypeID);
        WriteIntTypeToFrameBytes(payloadLength);
    }

    protected void ReadAllFieldBytesToMsgHeader()
    {
        msgTypeID = ReadFrameBytesToIntType();
        payloadLength = ReadFrameBytesToIntType();
    }

    protected int GetMsgHeaderCount(bool havePayload = true)
    {
        TrySetMsgTypeID();
        if (havePayload)
        {
            //第一个int是类型Type的信息，第二个int是消息长度的信息
            return sizeof(int) + sizeof(int);
        }
        else
        {
            //没有有效荷载的话，就一个Type的信息
            return sizeof(int);
        }
    }


    //外部肯定要用WriteMsgTypeIDToAllFieldBytes、ReadAllFieldBytesToMsgTypeID、GetMsgTypeIDCount三个方法，只要一用，就给这个SetTypeID、SetMsgBodyCount点火执行
    private void TrySetMsgTypeID()
    {
        if (msgTypeID == -999999999)
        {
            SetMsgTypeID();
        }

        if (payloadLength == -888888888)
        {
            SetPayloadLength();
        }
    }
}