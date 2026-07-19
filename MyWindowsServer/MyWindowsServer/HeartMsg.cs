using System.Collections;
using System.Collections.Generic;

public class HeartMsg : MsgBase<HeartMsg>
{
    public override int GetFrameBytesLength()
    {
        return GetMsgHeaderCount(false);
    }

    protected override void WriteFrameBytes()
    {
    }

    protected override void ReadFromFrameBytes()
    {
    }

    protected override void SetMsgTypeID()
    {
        MsgTypeID = 0;
    }

    protected override void SetPayloadLength()
    {
        PayloadLength = 0;
    }
}