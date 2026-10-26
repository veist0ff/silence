using System.Diagnostics;

namespace HashCore.Laser.Logic.Message.Home
{
    public class LobbyInfoMessage : GameMessage
    {
        public int playercount;
        public override void Encode()
        {
            long megabytesUsed = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
            Stream.WriteVInt(4480);
            Stream.WriteString(@"porno");
            Stream.WriteVInt(0);
        }

        public override int GetMessageType()
        {
            return 23457;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
