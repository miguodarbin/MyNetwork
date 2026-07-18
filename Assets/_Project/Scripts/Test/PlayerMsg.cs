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


    public override int GetFrameBytesLength()
    {
        return GetMsgHeaderCount() + sizeof(int) + GetCustomTypeAllCount(playerData);
    }

    protected override void WriteFrameBytes()
    {
        WriteMsgHeaderToAllFieldBytes();
        WriteIntTypeToFrameBytes(playerId);
        WriteCustomTypeToFrameBytes(playerData);
    }

    protected override void ReadFromFrameBytes()
    {
        ReadAllFieldBytesToMsgHeader();
        playerId = ReadFrameBytesToIntType();
        playerData = ReadFrameBytesToCustomType<PlayerData>();
    }

    protected override void SetMsgTypeID()
    {
        MsgTypeID = 1001;
    }

    protected override void SetPayloadLength()
    {
        PayloadLength = sizeof(int) + GetCustomTypeAllCount(playerData);
    }
}