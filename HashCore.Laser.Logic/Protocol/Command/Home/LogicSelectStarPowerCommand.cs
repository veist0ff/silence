namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Structures;
     
    public class LogicSelectStarPowerCommand : Command
    {
        public int CardInstanceId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            CardInstanceId=stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            //Debugger.Print("bruh");
            CardData spg = DataTables.Get(23).GetData<CardData>(CardInstanceId);
            //Debugger.Print(spg.Target.ToString());
            if (spg == null) return 0;
            Hero hero = homeMode.Avatar.GetHeroForCard(spg);
            if (hero == null) return 0;

            switch (spg.MetaType)
            {
                case 4:
                    hero.SelectedStarPowerId = spg.GetInstanceId();
                    break;
                case 6:
                    hero.SelectedOverChargeId = spg.GetInstanceId();
                    break;
                default:
                    hero.SelectedGadgetId = spg.GetInstanceId();
                    break;
            }

            homeMode.CharacterChanged?.Invoke(0);

            return 0;
        }

        public override int GetCommandType()
        {
            return 529;
        }
    }
}
