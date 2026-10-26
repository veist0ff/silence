using HashCore.Laser.Logic.Command;
using HashCore.Laser.Logic.Home;
using HashCore.Laser.Logic.Home.Gatcha;

public class LogicGiveDeliveryItemsCommand : Command
{
    public int DeliveryPopupType { get; set; }                   // +32
    public readonly List<DeliveryUnit> DeliveryUnits;            // +40, count at +52
    public ForcedDrops ForcedDrops { get; set; }                 // +56
    public int RewardTrackType { get; set; }                     // +64
    public int RewardForRank { get; set; }                       // +68
    public int BrawlPassSeason { get; set; }                     // +72
    public int Unk76 { get; set; }                               // +76
    public bool Unk80 { get; set; }                              // +80
    public bool Unk81 { get; set; }                              // +81
    public int UnkGlobalId { get; set; }                         // +88
    public int Unk96 { get; set; } = -1;                         // +96
    public int Unk100 { get; set; } = -1;                        // +100
    public int Unk104 { get; set; }                              // +104
    public bool Unk108 { get; set; }                             // +108
    public bool Unk109 { get; set; }                             // +109

    public int ClaimMode { get; set; }                           // +112

    public int ServerCommandId { get; set; } = -1;                // +28

    public bool Echo { get; private set; }

    public LogicGiveDeliveryItemsCommand() : base()
    {
        DeliveryUnits = new List<DeliveryUnit>();
    }

    public override void Encode(ByteStream stream)
    {
        stream.WriteVInt(DeliveryPopupType);

        stream.WriteVInt(DeliveryUnits.Count);
        foreach (DeliveryUnit unit in DeliveryUnits)
        {
            unit.Encode(stream);
        }

        stream.WriteBoolean(ForcedDrops != null);
        if (ForcedDrops != null)
        {
            ForcedDrops.Encode(stream);
        }

        stream.WriteVInt(RewardTrackType);
        stream.WriteVInt(RewardForRank);
        stream.WriteVInt(BrawlPassSeason);
        stream.WriteVInt(Unk76);
        stream.WriteBoolean(Unk80);
        stream.WriteBoolean(Unk81);
        ByteStreamHelper.WriteDataReference(stream, UnkGlobalId);
        stream.WriteVInt(Unk96);
        stream.WriteVInt(Unk100);
        stream.WriteVInt(Unk104);
        stream.WriteBoolean(Unk108);
        stream.WriteBoolean(Unk109);
        stream.WriteVInt(ClaimMode);

        stream.WriteVInt(ServerCommandId);

        base.Encode(stream);
    }

    public override void Decode(ByteStream stream)
    {
        Echo = true;

        DeliveryPopupType = stream.ReadVInt();

        DeliveryUnits.Clear();
        for (int i = stream.ReadVInt(); i > 0; i--)
        {
            DeliveryUnit unit = new DeliveryUnit(0);
            unit.Decode(stream);
            DeliveryUnits.Add(unit);
        }

        ForcedDrops = null;
        if (stream.ReadBoolean())
        {
            ForcedDrops = new ForcedDrops();
            ForcedDrops.Decode(stream);
        }

        RewardTrackType = stream.ReadVInt();
        RewardForRank = stream.ReadVInt();
        BrawlPassSeason = stream.ReadVInt();
        Unk76 = stream.ReadVInt();
        Unk80 = stream.ReadBoolean();
        Unk81 = stream.ReadBoolean();
        UnkGlobalId = ByteStreamHelper.ReadDataReference(stream);
        Unk96 = stream.ReadVInt();
        Unk100 = stream.ReadVInt();
        Unk104 = stream.ReadVInt();
        Unk108 = stream.ReadBoolean();
        Unk109 = stream.ReadBoolean();
        ClaimMode = stream.ReadVInt();

        ServerCommandId = stream.ReadVInt();

        base.Decode(stream);
    }

    public override int Execute(HomeMode homeMode)
    {
        if (Echo)
        {
            return 0;
        }

        foreach (DeliveryUnit unit in DeliveryUnits)
        {
            foreach (GatchaDrop drop in unit.GetDrops())
            {
                drop.DoDrop(homeMode);
            }
        }

        return 0;
    }

    public override int GetCommandType()
    {
        return 203;
    }
}
