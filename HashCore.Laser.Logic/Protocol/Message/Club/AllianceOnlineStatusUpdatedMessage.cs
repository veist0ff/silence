namespace HashCore.Laser.Logic.Message.Friends
{
    using HashCore.Laser.Logic.Friends;

    public class AllianceOnlineStatusUpdatedMessage : GameMessage
    {
        public int Members;

        public int OnlineMembers;

        public long AvatarId;
        public int PlayerStatus;

        public override void Encode()
        {
            Stream.WriteVInt(Members);
            Stream.WriteVInt(OnlineMembers);

            Stream.WriteVInt(1);
            EncodeStatusChange(AvatarId, PlayerStatus);
        }

        private void EncodeStatusChange(long avatarId, int status)
        {
            Stream.WriteLong(avatarId);
            Stream.WriteVInt(status);
            Stream.WriteVInt(0);

            // Club league block: tickets used/max and participation.
            Stream.WriteBoolean(false);

            // Club piggy block: event, wins, tickets.
            Stream.WriteVInt(0);

            Stream.WriteVInt(0);
        }

        public override int GetMessageType()
        {
            return 20207;
        }

        public override int GetServiceNodeType()
        {
            return 3;
        }
    }
}
