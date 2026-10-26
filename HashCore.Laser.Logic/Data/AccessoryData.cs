
namespace HashCore.Laser.Logic.Data
{
    public class AccessoryData : LogicData
    {
        enum Types
        {
            Heal=9,
            Reload=16,
            Place=10

        } 
        public AccessoryData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
            //Debugger.Print(this.ToString());
        }

        public string Name { get; set; }

        public string Type { get; set; }

        public int SubType { get; set; }

        //public int ChargeCount { get; set; }

        public int Cooldown { get; set; }

        public string UseEffect { get; set; }

        public string PetUseEffect { get; set; }

        public string LoopingEffect { get; set; }

        public string LoopingEffectPet { get; set; }

        public int ActivationDelay { get; set; }

        public int ActiveTicks { get; set; }

        public bool ShowCountdown { get; set; }

        public bool StopMovement { get; set; }

        public bool StopPetForDelay { get; set; }

        public int AnimationIndex{ get; set; }
        public bool SetAttackAngle { get; set; }

        public int AimGuideType { get; set; }

        public bool ConsumesAmmo { get; set; }

        public string AreaEffect { get; set; }

        public string PetAreaEffect { get; set; }

        public bool InterruptsAction { get; set; }

        public bool AllowStunActivation { get; set; }

        public int RequirePetDistance { get; set; }

        public bool DestroyPet { get; set; }

        public int Range { get; set; }
        public bool RequireEnemyInRange { get; set; }
        public bool TargetFriends { get; set; }
        public bool TargetIndirect { get; set; }

        public int ShieldPercent { get; set; }
        public int ShieldTicks { get; set; }

        public int SpeedBoost { get; set; }

        public  int SpeedBoostTicks { get; set; }

        public bool SkipTypeCondition { get; set; }

        public int UsableDuringCharge { get; set; }
        public string CustomObject { get; set; }

        public int CustomValue1 { get; set; }

        public int CustomValue2 { get; set; }

        public int CustomValue3 { get; set; }

        public int CustomValue4 { get; set; }

        public int CustomValue5 { get; set; }

        public int CustomValue6 { get; set; }

        public string MissingTargetText { get; set; }

        public string TargetTooFarText { get; set; }

        public string TargetAlreadyActiveText { get; set; }
    }
}
