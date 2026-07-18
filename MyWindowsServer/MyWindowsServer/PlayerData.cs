using System.Collections;
using System.Collections.Generic;

public class PlayerData : CanBeBinarySerialize<PlayerData>
{
    public string playerName;
    public float playerHealth;
    public int playerAge;
    public bool playerSex;

    public PlayerData(string playerName, float playerHealth, int playerAge, bool playerSex)
    {
        this.playerName = playerName;
        this.playerHealth = playerHealth;
        this.playerAge = playerAge;
        this.playerSex = playerSex;
    }

    public PlayerData()
    {
    }

    public override int GetFrameBytesLength()
    {
        return GetStringTypeFrameBytes(playerName) + sizeof(float) + sizeof(int) + sizeof(bool);
    }

    protected override void WriteFrameBytes()
    {
        WriteStringTypeToFrameBytes(playerName);
        WriteFloatTypeToFrameBytes(playerHealth);
        WriteIntTypeToFrameBytes(playerAge);
        WriteBoolTypeToFrameBytes(playerSex);
    }

    protected override void ReadFromFrameBytes()
    {
        playerName = ReadFrameBytesToStringType();
        playerHealth = ReadFrameBytesToFloatType();
        playerAge = ReadFrameBytesToIntType();
        playerSex = ReadFrameBytesToBoolType();
    }
}