using HashCore.Laser.Logic.Command;
using HashCore.Laser.Logic.Home;
using HashCore.Laser.Logic.Home.Items;

/// <summary>
/// Команда 212. Досылает клиенту обновлённые данные игрока по событиям, чтобы новые
/// значения появлялись сразу, а не при следующей отправке дома.
///
/// Формат снят с LogicPlayerDataChangedCommand::decode (0x1003db968), подтипы —
/// с execute (0x1003dbbbc). Подтип выбирает, какой блок клиент применит;
/// остальные присутствия всё равно обязаны быть в потоке, иначе разбор уедет.
/// </summary>
public class LogicPlayerDataChangedCommand : Command
{
    /// <summary>Подтипы из диспетчера execute.</summary>
    public const int SUBTYPE_PIGGY_BANK  = 2;
    public const int SUBTYPE_COLLAB      = 3;   // setPlayerCollabEventData
    public const int SUBTYPE_SPECIAL     = 4;
    public const int SUBTYPE_CONTEST     = 5;
    public const int SUBTYPE_RECORDS     = 6;
    public const int SUBTYPE_EARLY_ACCESS = 8;
    public const int SUBTYPE_ROGUELITE   = 10;  // список, клиент сначала чистит свой

    public int Subtype;                                   // +32
    public PlayerCollabEventData Collab;                  // +48
    public PlayerBrawlerEarlyAccessEventData EarlyAccess; // +88
    public List<PlayerRogueliteEventData> Roguelite = new();   // +112
    public int HeroGlobalId;                              // +128, ссылка на героя

    public int ServerCommandId { get; set; } = -1;        // +28

    public override void Encode(ByteStream stream)
    {
        stream.WriteVInt(Subtype);                                    //  1  +32

        stream.WriteBoolean(false);                                   //  2  +40  Piggy Bank
        if (stream.WriteBoolean(Collab != null)) Collab.Encode(stream); // 3 +48  Collab
        stream.WriteBoolean(false);                                   //  4  +56  Special
        stream.WriteBoolean(false);                                   //  5  +64  Contest
        stream.WriteBoolean(false);                                   //  6  +72  Records
        stream.WriteBoolean(false);                                   //  7  +80  MergedEventReward
        if (stream.WriteBoolean(EarlyAccess != null)) EarlyAccess.Encode(stream); // 8 +88

        stream.WriteVInt(0);                                          //  9  +96  список спец-событий

        // 10  +112 список рогалика. Подтип 10 у клиента сначала чистит свой список,
        // но только при ненулевом счётчике — пустое обновление ничего не сбрасывает.
        stream.WriteVInt(Roguelite?.Count ?? 0);
        if (Roguelite != null)
            foreach (PlayerRogueliteEventData run in Roguelite)
                if (stream.WriteBoolean(run != null)) run.Encode(stream);

        ByteStreamHelper.WriteDataReference(stream, HeroGlobalId);    // 11  +128

        stream.WriteVInt(ServerCommandId);                            // 12  база LogicServerCommand
        base.Encode(stream);
    }

    public override int Execute(HomeMode homeMode) => 0;

    public override int GetCommandType() => 212;
}
