namespace HashCore.Laser.Logic.Message.Friends
{
    using HashCore.Laser.Logic.Friends;
    using HashCore.Laser.Logic.Team;

    public class FriendOnlineStatusEntryMessage : GameMessage
    {
        public long AvatarId;
        public int PlayerStatus;
        public TeamEntry AllianceTeamEntry;

        public override void Encode()
        {
            Stream.WriteLong(AvatarId);
            if (Stream.WriteBoolean(PlayerStatus >= 0))
            {
                Stream.WriteLong(AvatarId);
                Stream.WriteVInt(PlayerStatus);
                Stream.WriteVInt(0);
                Stream.WriteBoolean(false);
                Stream.WriteBoolean(AllianceTeamEntry!=null);
                if(AllianceTeamEntry!=null) AllianceTeamEntry.EncodeAllianceTeamEntry(Stream);
                Stream.WriteVInt(0);
                Stream.WriteBoolean(false);
            }
        }

        public override int GetMessageType()
        {
            return 24555;
        }

        public override int GetServiceNodeType()
        {
            return 3;
        }
    }
}
