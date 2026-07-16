using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMsg : MsgBase<PlayerMsg>
{
    public int playerId;
    public PlayerData playerData;

    public PlayerMsg()
    {
    }

    public PlayerMsg(int playerId, PlayerData playerData)
    {
        this.playerId = playerId;
        this.playerData = playerData;
    }


    public override int GetAllFieldBytesLength()
    {
        return GetMsgTypeIDCount() + sizeof(int) + GetCustomFieldAllCount(playerData);
    }

    protected override void WriteInAllFieldBytes()
    {
        WriteMsgTypeIDToAllFieldBytes();
        WriteIntFieldToAllFieldBytes(playerId);
        WriteCustomFieldToAllFieldBytes(playerData);
    }

    protected override void ReadFromAllFieldBytes()
    {
        ReadAllFieldBytesToMsgTypeID();
        playerId = ReadAllFieldBytesToIntField();
        playerData = ReadAllFieldBytesToCustomField<PlayerData>();
    }

    protected override void SetMsgTypeID()
    {
        MsgTypeID = 1001;
    }
}