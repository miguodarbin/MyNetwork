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

    public override int GetAllFieldBytesLength()
    {
        return GetStringFieldAllCount(playerName) + sizeof(float) + sizeof(int) + sizeof(bool);
    }

    protected override void WriteInAllFieldBytes()
    {
        WriteStringFieldToAllFieldBytes(playerName);
        WriteFloatFieldToAllFieldBytes(playerHealth);
        WriteIntFieldToAllFieldBytes(playerAge);
        WriteBoolFieldToAllFieldBytes(playerSex);
    }

    protected override void ReadFromAllFieldBytes()
    {
        playerName = ReadAllFieldBytesToStringField();
        playerHealth = ReadAllFieldBytesToFloatField();
        playerAge = ReadAllFieldBytesToIntField();
        playerSex = ReadAllFieldBytesToBoolField();
    }
}