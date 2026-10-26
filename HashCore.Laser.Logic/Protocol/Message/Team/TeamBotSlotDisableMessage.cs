namespace HashCore.Laser.Logic.Message.Team
{
    public class TeamBotSlotDisableMessage : GameMessage
    {
        public int BotSlot { get; set; }
        public bool Disable;

        public override void Decode()
        {
            base.Decode();

            BotSlot = Stream.ReadInt();
            Disable = Stream.ReadBoolean();
        }

        public override int GetMessageType()
        {
            return 14373;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
