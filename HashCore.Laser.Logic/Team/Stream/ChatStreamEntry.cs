namespace HashCore.Laser.Logic.Team.Stream
{
     
    using HashCore.Laser.Titan.Math;

    public class ChatStreamEntry : TeamStreamEntry
    {
        public string Message { get; set; }

        public override void Encode(ByteStream encoder)
        {
            base.Encode(encoder);

            encoder.WriteString(Message);

            encoder.WriteBoolean(false);
        }

        public override int GetStreamEntryType()
        {
            return 2;
        }
    }
}
