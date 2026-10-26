namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Gatcha;
    using HashCore.Laser.Logic.Home.RandomReward;
    using HashCore.Laser.Logic.Message.Home;
    using HashCore.Laser.Server;

    public class LogicClaimTrophyWorldRewardCommand : Command
    {
        public int CharacterId;

        public int MilestoneIndex;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            CharacterId = ByteStreamHelper.ReadDataReference(stream);
            MilestoneIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            TrophyWorldMilestoneData milestoneData = GetMilestone(MilestoneIndex);

            if (milestoneData == null)
            {
                Logger.Error($"TrophyWorldMilestone data is NULL - index {MilestoneIndex}");
                return -1;
            }

            if (MilestoneIndex <= homeMode.Home.TrophyWorldProgress)
            {
                Logger.Warning($"trophy world milestone {MilestoneIndex} already claimed (progress: {homeMode.Home.TrophyWorldProgress})");
                return -2;
            }

            if (homeMode.Avatar.HighestTrophies < milestoneData.TrophyCount)
            {
                Logger.Warning($"current trophies: {homeMode.Avatar.HighestTrophies}, required: {milestoneData.TrophyCount}");
                return -3;
            }

            if (ProcessTrophyReward(homeMode, milestoneData, false) != 0) return -4;

            homeMode.Home.TrophyWorldProgress = MilestoneIndex;

            return 0;
        }

        private static TrophyWorldMilestoneData GetMilestone(int index)
        {
            foreach (LogicData data in DataTables.Get(DataType.TrophyWorldMilestone).GetDatas())
            {
                if (data is TrophyWorldMilestoneData milestone && milestone.Index == index) return milestone;
            }

            return null;
        }

        private int ProcessTrophyReward(HomeMode homeMode, TrophyWorldMilestoneData milestoneData, bool isFallback)
        {
            string rewardType = isFallback ? milestoneData.FallbackRewardType : milestoneData.RewardType;
            string rewardData = isFallback ? milestoneData.FallbackRewardData : milestoneData.RewardData;
            int rewardCount = isFallback ? milestoneData.FallbackRewardCount : milestoneData.RewardCount;

            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            bool refreshRandomRewards = false;

            switch (rewardType)
            {
                case "Coins":
                    {
                        DeliveryUnit unit = new DeliveryUnit(100);
                        unit.AddDrop(new GatchaDrop(7) { Count = rewardCount });
                        command.DeliveryUnits.Add(unit);
                        break;
                    }
                case "PowerPoints":
                case "Credits":
                    {
                        DeliveryUnit unit = new DeliveryUnit(100);
                        unit.AddDrop(new GatchaDrop(24) { Count = rewardCount });
                        command.DeliveryUnits.Add(unit);
                        break;
                    }
                case "Bling":
                    {
                        DeliveryUnit unit = new DeliveryUnit(100);
                        unit.AddDrop(new GatchaDrop(25) { Count = rewardCount });
                        command.DeliveryUnits.Add(unit);
                        break;
                    }
                case "RandomStarrDrop":
                    {
                        int requested = Math.Max(1, rewardCount);
                        if (RandomRewardManager.GiveStarrDrops(homeMode, requested, 0, false) != requested)
                            return -1;
                        refreshRandomRewards = true;
                    }
                    break;
                case "StarrDropContainer":
                    {
                        int rarity = isFallback
                            ? milestoneData.FallbackRewardExtraData
                            : milestoneData.RewardExtraData;
                        int requested = Math.Max(1, rewardCount);
                        if (RandomRewardManager.GiveStarrDrops(homeMode, requested, rarity, false) != requested)
                            return -1;
                        refreshRandomRewards = true;
                        break;
                    }
                case "OverchargeStarrDrop":
                    {
                        int requested = Math.Max(1, rewardCount);
                        if (RandomRewardManager.GiveStarrDrops(homeMode, requested, 6, false) != requested)
                            return -1;
                        refreshRandomRewards = true;
                    }
                    break;
                case "Hero":
                case "ChooseSuperRare":
                    {
                        // GetDataByGlobalId(0) returns the first row, so an empty pick must be rejected.
                        CharacterData character;

                        if (string.IsNullOrEmpty(rewardData))
                        {
                            character = CharacterId > 0
                                ? DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(CharacterId)
                                : null;
                        }
                        else
                        {
                            character = DataTables.Get(DataType.Character).GetData<CharacterData>(rewardData);
                        }

                        if (character == null)
                        {
                            Logger.Error($"trophy world hero reward has no character (data: '{rewardData}', picked: {CharacterId})");
                            return -1;
                        }

                        if (homeMode.Avatar.HasHero(character.GetGlobalId()))
                        {
                            if (!isFallback && !string.IsNullOrEmpty(milestoneData.FallbackRewardType))
                            {
                                return ProcessTrophyReward(homeMode, milestoneData, true);
                            }

                            // Client is already waiting for the 203 to release the mutex.
                            break;
                        }

                        DeliveryUnit unit = new DeliveryUnit(100);
                        unit.AddDrop(new GatchaDrop(1) { DataGlobalId = character.GetGlobalId(), Count = 1 });
                        command.DeliveryUnits.Add(unit);
                        break;
                    }
                case "EventSlot":
                case "DailyQuestsFeature":
                case "QuestsFeature":
                case "ClubFeature":
                case "SpraySlot":
                    {
                        // Feature unlocks are derived from the claimed index client-side.
                        break;
                    }
                default:
                    {
                        Logger.Warning($"Unhandled Trophy Road RewardType: {rewardType}");
                        break;
                    }
            }

            // Mode 2 takes the raw Index, unlike the rank-up tracks which are offset by 2.
            command.ClaimMode = 2;
            command.RewardForRank = milestoneData.Index;

            command.Execute(homeMode);

            AvailableServerCommandMessage message = new AvailableServerCommandMessage { Command = command };
            homeMode.GameListener.SendMessage(message);
            if (refreshRandomRewards) RandomRewardManager.Send(homeMode);
            return 0;
        }

        public override int GetCommandType()
        {
            return 664;
        }
    }
}
