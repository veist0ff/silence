namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Recruit;

    public abstract class LogicRecruitRoadCommand : Command
    {
        public int CharacterGlobalId;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            ByteStreamHelper.WriteDataReference(stream, CharacterGlobalId);
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            CharacterGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }
    }

    public class LogicRecruitRoadSelectBrawlerCommand : LogicRecruitRoadCommand
    {
        public override int Execute(HomeMode homeMode)
        {
            return BrawlerRecruitRoad.Select(homeMode, CharacterGlobalId);
        }

        public override int GetCommandType()
        {
            return 559;
        }
    }

    public class LogicRecruitRoadSwitchBrawlerCommand : LogicRecruitRoadCommand
    {
        public override int Execute(HomeMode homeMode)
        {
            return BrawlerRecruitRoad.Switch(homeMode, CharacterGlobalId);
        }

        public override int GetCommandType()
        {
            return 561;
        }
    }

    public class LogicRecruitRoadClaimBrawlerCommand : LogicRecruitRoadCommand
    {
        public override int Execute(HomeMode homeMode)
        {
            return BrawlerRecruitRoad.Claim(homeMode, CharacterGlobalId);
        }

        public override int GetCommandType()
        {
            return 562;
        }
    }

    public class LogicRecruitRoadAbandonNewBrawlerCommand : LogicRecruitRoadCommand
    {
        public override int Execute(HomeMode homeMode)
        {
            return BrawlerRecruitRoad.Abandon(homeMode, CharacterGlobalId);
        }

        public override int GetCommandType()
        {
            return 565;
        }
    }

    public class LogicPurchaseBrawlerCommand : Command
    {
        public int CharacterGlobalId;
        public int ResourceGlobalId;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            ByteStreamHelper.WriteDataReference(stream, CharacterGlobalId);
            ByteStreamHelper.WriteDataReference(stream, ResourceGlobalId);
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            CharacterGlobalId = ByteStreamHelper.ReadDataReference(stream);
            ResourceGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return BrawlerRecruitRoad.Purchase(homeMode, CharacterGlobalId, ResourceGlobalId);
        }

        public override int GetCommandType()
        {
            return 560;
        }
    }

    public class LogicRecruitRoadProcessExtraRecruitTokensCommand : Command
    {
        public override int Execute(HomeMode homeMode)
        {
            return BrawlerRecruitRoad.ProcessExtraTokens(homeMode);
        }

        public override int GetCommandType()
        {
            return 563;
        }
    }

    public class LogicRecruitRoadSelectNewBrawlerCommand : Command
    {
        public int CharacterGlobalId;
        public int Slot;

        public override void Encode(ByteStream stream)
        {
            stream.WriteInt(Slot);
            base.Encode(stream);
            ByteStreamHelper.WriteDataReference(stream, CharacterGlobalId);
        }

        public override void Decode(ByteStream stream)
        {
            Slot = stream.ReadInt();
            base.Decode(stream);
            CharacterGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return BrawlerRecruitRoad.Select(homeMode, CharacterGlobalId);
        }

        public override int GetCommandType()
        {
            return 564;
        }
    }
}
