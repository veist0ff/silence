namespace HashCore.Laser.Logic.Message.Home
{
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Club;
    using HashCore.Laser.Logic.Data.Helper;
     
    using HashCore.Laser.Logic.Home.Structures;

    public class PlayerProfileMessage : GameMessage
    {
        public Profile Profile;
        public AllianceHeader AllianceHeader;
        public AllianceRole AllianceRole;

        public PlayerProfileMessage() : base()
        {
            ;
        }

        public override void Encode()
        {
            Profile.Encode(Stream);

            if (Stream.WriteBoolean(AllianceHeader != null))
            {
                AllianceHeader.Encode(Stream);
            }

            if (AllianceRole > AllianceRole.None)
                ByteStreamHelper.WriteDataReference(Stream, GlobalId.CreateGlobalId(25, (int)AllianceRole));
            else
                Stream.WriteVInt(0);

            Stream.WriteVInt(Profile?.Hero ?? 0);
        }

        public override int GetMessageType()
        {
            return 24113;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
