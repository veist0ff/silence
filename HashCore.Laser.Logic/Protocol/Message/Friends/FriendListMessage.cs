namespace HashCore.Laser.Logic.Message.Friends
{
    using System.IO;
    using System.Threading;
    using System.Xml.Linq;
    using HashCore.Laser.Logic.Friends;
    using HashCore.Laser.Logic.Listener;

    public class FriendListMessage : GameMessage
    {
        public Friend[] Friends;

        public override void Encode()
        {
            Stream.WriteInt(0);
            Stream.WriteBoolean(true);
            Stream.WriteBoolean(false);//FriendRequestBlocked
            Stream.WriteInt(Friends.Length);

            foreach (Friend friend in Friends)
            {
                friend.Encode(Stream);
            }
            //Stream.WriteLong(1);

            //Stream.WriteBoolean(false); // Alliance entry

        }

        public override int GetMessageType()
        {
            return 20105;
        }

        public override int GetServiceNodeType()
        {
            return 3;
        }
    }
}
