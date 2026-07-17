using System.Collections;
using System.Collections.Generic;

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


    public override int GetAllBytesLength()
    {
        return GetMsgHeaderCount() + sizeof(int) + GetCustomTypeAllCount(playerData);
    }

    protected override void WriteInAllFieldBytes()
    {
        WriteMsgHeaderToAllFieldBytes();
        WriteIntTypeToAllBytes(playerId);
        WriteCustomTypeToAllBytes(playerData);
    }

    protected override void ReadFromAllBytes()
    {
        ReadAllFieldBytesToMsgHeader();
        playerId = ReadAllBytesToIntType();
        playerData = ReadAllBytesToCustomType<PlayerData>();
    }

    protected override void SetMsgTypeID()
    {
        MsgTypeID = 1001;
    }

    protected override void SetMsgBodyCount()
    {
        MsgBodyCount = sizeof(int) + GetCustomTypeAllCount(playerData);
    }
}