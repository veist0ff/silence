namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
     
    public class LogicPurchaseHeroLvlUpMaterialCommand : Command
    {
        private int PackIndex;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            PackIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (PackIndex < 0 || PackIndex > ClientHome.GoldPacksAmount.Length) return -1;

            if (!homeMode.Avatar.UseDiamonds(ClientHome.GoldPacksPrice[PackIndex])) return -2;

            homeMode.Avatar.AddGold(ClientHome.GoldPacksAmount[PackIndex]);

            return 0;
        }

        public override int GetCommandType()
        {
            return 521;
        }
    }
}
