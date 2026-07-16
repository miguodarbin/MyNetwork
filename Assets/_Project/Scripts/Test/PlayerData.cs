using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public override int GetAllBytesLength()
    {
        return GetStringTypeAllCount(playerName) + sizeof(float) + sizeof(int) + sizeof(bool);
    }

    protected override void WriteInAllFieldBytes()
    {
        WriteStringTypeToAllBytes(playerName);
        WriteFloatTypeToAllBytes(playerHealth);
        WriteIntTypeToAllBytes(playerAge);
        WriteBoolTypeToAllBytes(playerSex);
    }

    protected override void ReadFromAllBytes()
    {
        playerName = ReadAllBytesToStringType();
        playerHealth = ReadAllBytesToFloatType();
        playerAge = ReadAllBytesToIntType();
        playerSex = ReadAllBytesToBoolType();
    }
}