namespace HashCore.Laser.Logic.Message.Club
{
    /// <summary>14469 — пин в чат клуба (AlliancePremadeChatMessage): DataRef пина + VInt.</summary>
    public class AlliancePremadeChatMessage : GameMessage
    {
        public int MessageDataId;
        public int Unknown;

        public override void Decode()
        {
            MessageDataId = ByteStreamHelper.ReadDataReference(Stream);
            Unknown = Stream.ReadVInt();
        }

        public override int GetMessageType() => 14469;
        public override int GetServiceNodeType() => 11;
    }
}
