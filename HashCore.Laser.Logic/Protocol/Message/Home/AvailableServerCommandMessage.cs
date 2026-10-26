namespace HashCore.Laser.Logic.Message.Home
{
    using HashCore.Laser.Logic.Command;

    public class AvailableServerCommandMessage : GameMessage
    {
        public Command Command;

        public AvailableServerCommandMessage() : base()
        {
            ;
        }

        public override void Encode()
        {
            Stream.WriteVInt(Command.GetCommandType());
            Command.Encode(Stream);
        }

        public override int GetMessageType()
        {
            return 24111;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
