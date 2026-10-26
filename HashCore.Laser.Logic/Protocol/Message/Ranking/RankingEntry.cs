namespace HashCore.Laser.Logic.Message.Ranking;

using HashCore.Laser.Logic.Avatar.Structures;

public sealed class AllianceRankingData
{
    public string? Name;      // +0x08
    public int Value;         // +0x10
    public int BadgeId;       // +0x18, global id

    public void Encode(ByteStream stream)
    {
        stream.WriteString(Name);
        stream.WriteVInt(Value);
        ByteStreamHelper.WriteDataReference(stream, BadgeId);
    }

    public void Decode(ByteStream stream)
    {
        Name = stream.ReadString(900000);
        Value = stream.ReadVInt();
        BadgeId = ByteStreamHelper.ReadDataReference(stream);
    }
}

public sealed class PlayerRankingData
{
    public string? Name;                  // +0x08
    public PlayerDisplayData DisplayData; // +0x10

    public PlayerRankingData()
    {
        DisplayData = new PlayerDisplayData();
    }

    public void Encode(ByteStream stream)
    {
        stream.WriteString(Name);
        DisplayData.Encode(stream);
    }

    public void Decode(ByteStream stream)
    {
        Name = stream.ReadString(900000);
        DisplayData = new PlayerDisplayData();
        DisplayData.Decode(stream);
    }
}

public sealed class RankingEntry
{
    public int Rank;                              // +0x10
    public AllianceRankingData? AllianceData;     // +0x28
    public int Score;                             // +0x18
    public PlayerRankingData? PlayerData;         // +0x20
    public long? Id;                              // +0x08, written as Int+Int
    public int Trophies;                          // +0x14

    public void Encode(ByteStream stream)
    {
        stream.WriteVInt(Rank);

        if (stream.WriteBoolean(AllianceData != null))
        {
            AllianceData!.Encode(stream);
        }

        stream.WriteVInt(Score);

        if (stream.WriteBoolean(PlayerData != null))
        {
            PlayerData!.Encode(stream);
        }

        if (stream.WriteBoolean(Id.HasValue))
        {
            stream.WriteLong(Id!.Value);
        }

        stream.WriteVInt(Trophies);
    }

    public void Decode(ByteStream stream)
    {
        Rank = stream.ReadVInt();

        AllianceData = null;
        if (stream.ReadBoolean())
        {
            AllianceData = new AllianceRankingData();
            AllianceData.Decode(stream);
        }

        Score = stream.ReadVInt();

        PlayerData = null;
        if (stream.ReadBoolean())
        {
            PlayerData = new PlayerRankingData();
            PlayerData.Decode(stream);
        }

        Id = null;
        if (stream.ReadBoolean())
        {
            Id = stream.ReadLong();
        }

        Trophies = stream.ReadVInt();
    }
}
