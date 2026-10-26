using System.Text;
using HashCore.Laser.Titan.Library;

namespace HashCore.Laser.Logic.Message.Security
{
    public class ServerHelloMessage : GameMessage
    {
        private byte[] _serverHelloToken;
        public string Nonce;
        public ServerHelloMessage() : base()
        {
            _serverHelloToken = new byte[24];
            TweetNaCl.RandomBytes(_serverHelloToken);
        }

        public override void Encode()
        {
            
            Stream.WriteBytes(_serverHelloToken, 24);
            //Stream.WriteString(Nonce);
        }

        public void SetServerHelloToken(byte[] token)
        {
            //_serverHelloToken = token;
        }

        public byte[] RemoveServerHelloToken()
        {
            byte[] token = _serverHelloToken;
            _serverHelloToken = null;
            return token;
        }

        public override int GetMessageType()
        {
            return 20100;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
