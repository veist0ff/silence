namespace HashCore.Laser.Logic.Message.Home
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Home.BattleLog;

    /// <summary>Запрос журнала боёв, тип 14114. Полей не читаем.</summary>
    public class GetBattleLogMessage : GameMessage
    {
        public override void Decode() { }

        public override int GetMessageType() => 14114;

        public override int GetServiceNodeType() => 9;
    }

    /// <summary>Журнал боёв, тип 23458 (Android decode 0xA75088): bool, VInt число, записи.</summary>
    public class BattleLogMessage : GameMessage
    {
        public List<BattleLogEntry> Entries = new List<BattleLogEntry>();

        public override void Encode()
        {
            // +0x90: полный журнал. С false клиент (BattleLogManager::onMessage 0x7AB338) только
            // проверяет последние победы и не закрывает экран загрузки журнала.
            Stream.WriteBoolean(true);
            Stream.WriteVInt(Entries.Count);
            foreach (BattleLogEntry e in Entries) e.Encode(Stream);
        }

        public override int GetMessageType() => 23458;

        public override int GetServiceNodeType() => 9;
    }
}
