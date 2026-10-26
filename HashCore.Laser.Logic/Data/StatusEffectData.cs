namespace HashCore.Laser.Logic.Data
{
    /// <summary>
    /// status_effects_logic.csv — таблица 87 в клиенте v64
    /// (LogicDataTableResource, индекс снят с загрузчика таблиц).
    /// До этого сервер таблицу не грузил вообще, и все ссылки на статус-эффекты
    /// из карточек, персонажей и area effect'ов разрешались в null.
    /// </summary>
    public class StatusEffectData : LogicData
    {
        public StatusEffectData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }

        public int DurationTicks { get; set; }
        public int GradualWearOffAfter { get; set; }

        public bool CancelOnDamage { get; set; }
        public bool CancelOnCharge { get; set; }
        public bool CancelOnChargeEnd { get; set; }
        public bool CancelOnHealthRegen { get; set; }
        public bool CancelOnSourceDeath { get; set; }

        public int TriggerRate { get; set; }
        public bool TriggerOnFirstTick { get; set; }
        public bool Stacking { get; set; }
        public bool Refreshable { get; set; }

        public int PulsatingActiveTicksOn { get; set; }
        public int PulsatingActiveTicksOff { get; set; }

        public int Damage { get; set; }
        public int DamageHealSource { get; set; }
        public int UltiChargePercent { get; set; }
        public int UltiChargeBoostPercent { get; set; }
        public int AmmoReducePercent { get; set; }
        public int Healing { get; set; }
        public int ShieldPercent { get; set; }
        public int SpeedBoostPercent { get; set; }
    }
}
