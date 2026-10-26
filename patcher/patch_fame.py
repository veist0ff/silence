p = "HashCore.Laser.Logic/Protocol/Command/Home/Client/ClientShopCommands.cs"
s = open(p, encoding="utf-8").read()
old = """            Index = stream.ReadInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 578;"""
new = """            Index = stream.ReadInt();
        }

        /// <summary>
        /// Как LogicClaimFameRewardCommand::execute/canExecute (0x1003c4bd0/0x1003c4c48):
        /// славы должно хватать, награда — вещь уровня этой славы, уже открытая не
        /// выдаётся повторно. Раньше сервер ничего не делал, клиент же добавлял вещь
        /// себе — и игрока выбивало рассинхроном.
        /// </summary>
        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Avatar == null || homeMode.Home == null) return 1;
            if (homeMode.Avatar.FameCredits < Index) return 1;

            int globalId = HashCore.Laser.Logic.Home.Fame.FameTiers.GetRewardGlobalId(Index);
            if (globalId == 0) return 0;

            ClientHome home = homeMode.Home;
            List<int> target = (globalId / 1000000) switch
            {
                28 => home.UnlockedThumbnails ??= new List<int>(),
                52 => home.UnlockedEmotes ??= new List<int>(),
                85 => home.UnlockedFrames ??= new List<int>(),
                _ => null,
            };
            if (target == null) return 0;
            if (target.Contains(globalId)) return 2;

            target.Add(globalId);
            return 0;
        }

        public override int GetCommandType()
        {
            return 578;"""
assert s.count(old) == 1
s = s.replace(old, new)
open(p, "w", encoding="utf-8").write(s)

p = "HashCore.Laser.Logic/Home/Avatar/ClientAvatar.cs"
s = open(p, encoding="utf-8").read()
old = """            stream.WriteVInt(5 + Heroes.Count + rogueliteDeckCards.Count"""
new = """            stream.WriteVInt(6 + Heroes.Count + rogueliteDeckCards.Count"""
assert s.count(old) == 1
s = s.replace(old, new)
old = """                ByteStreamHelper.WriteDataReference(stream, 5000022);
                stream.WriteVInt(0);
                stream.WriteVInt(PowerPoints);
"""
new = """                // Слава (resources.csv, Fame). Без неё клиент считал свою, и награды
                // за славу расходились с сервером.
                ByteStreamHelper.WriteDataReference(stream, 5000021);
                stream.WriteVInt(0);
                stream.WriteVInt(FameCredits);

                ByteStreamHelper.WriteDataReference(stream, 5000022);
                stream.WriteVInt(0);
                stream.WriteVInt(PowerPoints);
"""
assert s.count(old) == 1
s = s.replace(old, new)
open(p, "w", encoding="utf-8").write(s)
print("ok")
