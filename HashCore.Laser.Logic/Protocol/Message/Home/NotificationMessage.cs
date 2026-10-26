using System.IO;
using HashCore.Laser.Logic.Notification;
 
namespace HashCore.Laser.Logic.Message.Home
{
    public class NotificationMessage : GameMessage
    {
        public BaseNotification Notification;

        public override int GetMessageType()
        {
            return 20801;
        }
        public override void Encode()
        {
            Notification.EncodeWithType(Stream);
        }
        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
