namespace HashCore.Laser.Logic.Message.Club
{
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Club;
    using HashCore.Laser.Logic.Data.Helper;
     
    public class MyAllianceMessage : GameMessage
    {
        public int OnlineMembers;
        public AllianceRole Role;
        public AllianceHeader AllianceHeader { get; set; }

        public bool OpenClubHub { get; set; }

        public override void Encode()
        {
            Stream.WriteVInt(OnlineMembers);
            if (Stream.WriteBoolean(AllianceHeader != null))
            {
                ByteStreamHelper.WriteDataReference(Stream, GlobalId.CreateGlobalId(25, (int)Role));
                AllianceHeader.Encode(Stream);
            }
            Stream.WriteBoolean(false);

            Stream.WriteVInt(0);
            Stream.WriteBoolean(OpenClubHub);
        }

        public override int GetMessageType()
        {
            return 24399;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
