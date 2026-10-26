namespace HashCore.Laser.Logic.Command.Avatar
{
    using HashCore.Laser.Logic.Home;
    using System;
     
    public class LogicChangeAvatarNameCommand : Command
    {
        public string Name;
        public int ChangeNameCost;
        public int Timer;

        public override void Encode(ByteStream stream)
        {
            stream.WriteString(Name);
            stream.WriteVInt(ChangeNameCost);
            stream.WriteVInt(Timer);
        }

        public override int Execute(HomeMode homeMode)
        {
            int currentCost = homeMode.Avatar.GetNameChangeCost();
            if (currentCost > 0)
            {
                if (!homeMode.Avatar.UseDiamonds(currentCost)) 
                {
                    return -1;
                }
            }

            homeMode.Avatar.Name = Name;
            homeMode.Avatar.NameSetByUser = true;
            homeMode.Avatar.NameChangesCount++;
            homeMode.Avatar.LastNameChangeTime = DateTime.UtcNow;

            if (homeMode.Avatar.Friends.Count > 0)
            {
                foreach(var friend in homeMode.Avatar.Friends)
                {
                    if (friend.Avatar.Friends.Find(x => x.AccountId == homeMode.Avatar.AccountId) == null) continue;
                    friend.Avatar.Friends.Find(x => x.AccountId == homeMode.Avatar.AccountId).DisplayData = new Logic.Avatar.Structures.PlayerDisplayData(homeMode.Home.ThumbnailId, homeMode.Home.NameColorId, homeMode.Avatar.Name);
                }
            }

            LogicChangeAvatarNameCommand responseCommand = new LogicChangeAvatarNameCommand();
            responseCommand.Name = Name;
            responseCommand.ChangeNameCost = homeMode.Avatar.GetNameChangeCost(); 
            responseCommand.Timer = homeMode.Avatar.GetNameChangeTimer(); 

            HashCore.Laser.Logic.Message.Home.AvailableServerCommandMessage message = new HashCore.Laser.Logic.Message.Home.AvailableServerCommandMessage();
            message.Command = responseCommand;
            homeMode.GameListener.SendMessage(message);

            return 0;
        }

        public override int GetCommandType()
        {
            return 201;
        }
    }
}
