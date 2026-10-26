namespace HashCore.Laser.Logic.Message.Account
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;

    public class AnalyticEventMessage : GameMessage
    {
        public string str1;
        public string str2;
        public override int GetMessageType()
        {
            return 10110;
        }
        public override void Decode()//
        {
            str1 = Stream.ReadString();
            str2 = Stream.ReadString();
        }
        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
