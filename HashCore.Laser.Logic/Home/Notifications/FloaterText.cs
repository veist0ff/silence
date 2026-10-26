using HashCore.Laser.Logic.Home;
using HashCore.Laser.Logic.Home.Items;
using HashCore.Laser.Logic.Notification;
using HashCore.Laser.Server;

public static class Text
{
    /// <summary>Всплывающий текст в доме (письмо 66). Во входящие не попадает.</summary>
    public static void SendMessage(string Text, HomeMode homeMode)
    {
        NotificationFactory.Send(homeMode, new FloaterTextNotification(Text));
        Logger.Print("[GUI::getFreeFloaterTextSlot] init!");
    }
}
