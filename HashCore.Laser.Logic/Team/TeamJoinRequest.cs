namespace HashCore.Laser.Logic.Team
{
    using HashCore.Laser.Logic.Avatar.Structures;

    public class TeamJoinRequest
    {
        public long Id;
        public long AccountId;
        public int Trophies;
        public PlayerDisplayData DisplayData;

        public void Encode(ByteStream stream)
        {
            stream.WriteLong(Id);
            stream.WriteLong(AccountId);

            EncodeFriendEntry(stream);
        }

        private void EncodeFriendEntry(ByteStream stream)
        {
            stream.WriteLong(AccountId);

            for (int i = 0; i < 6; i++)
            {
                stream.WriteString(null);
            }

            stream.WriteVInt(Trophies);
            stream.WriteVInt(0); // friend state
            stream.WriteVInt(0); // friend reason
            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteBoolean(false); // club block: id, badge, name, ...

            stream.WriteString(null);
            stream.WriteVInt(0); // seconds since last online
            stream.WriteVInt(0);
            stream.WriteVInt(0); // ranked rank

            if (stream.WriteBoolean(DisplayData != null))
            {
                DisplayData.Encode(stream);
            }

            stream.WriteVInt(0);
        }
    }
}
