using HashCore.Laser.Logic.Command.Home;
using HashCore.Laser.Logic.Avatar.Structures;

namespace HashCore.Laser.Logic.Message.Home
{
    public class GetPlayerProfileMessage : GameMessage
    {
        public long AccountId;

        public GetPlayerProfileMessage() : base()
        {
            ;
        }

        public override void Decode()
        {
            // unknown 11 x VInt block
            if (Stream.ReadBoolean())
            {
                for (int i = 0; i < 11; i++)
                {
                    Stream.ReadVInt();
                }
            }

            Stream.ReadVInt();

            AccountId = Stream.ReadLong();

            // BattleLogPlayerEntry
            if (Stream.ReadBoolean())
            {
                Stream.ReadVInt();
                Stream.ReadLong();
                Stream.ReadVInt();
                Stream.ReadBoolean();

                for (int i = Stream.ReadVInt(); i > 0; i--)
                {
                    ByteStreamHelper.ReadDataReference(Stream);
                    Stream.ReadVInt();
                    Stream.ReadVInt();
                    Stream.ReadVInt();
                }

                Stream.ReadVInt();

                new PlayerDisplayData().Decode(Stream);

                if (Stream.ReadBoolean())
                {
                    Stream.ReadLong();
                }

                ByteStreamHelper.ReadDataReference(Stream);
                ByteStreamHelper.ReadDataReference(Stream);
                ByteStreamHelper.ReadDataReference(Stream);

                for (int i = Stream.ReadVInt(); i > 0; i--)
                {
                    Stream.ReadVInt();
                }
            }
        }

        public override int GetMessageType()
        {
            return 15081;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
