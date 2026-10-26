namespace HashCore.Laser.Logic.Battle.Component
{
    using System.Runtime.InteropServices;
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Util;
  
    public class Buff
    {
        public enum BuffTypes
        {
            Damage = 1,
            DamageAndSize = 2,
            SpeedSlower = 3,
            SpeedFaster = 4,
            Damage2 = 5,
            HealthRegen = 10,
            BelleWeapon = 14,
            BelleUlti = 15
        }
        public int Type;
        public int Duration;
        public int dword8;
        public int Modifier;
        public int SizeIncrease;
        public int SourceIndex;
        public int EffectType;
        public int NormalDMG;
        public int BelleWeaponBounces;
        public Buff(int Type, int Dura, int Modi, int SizeInc)
        {
            this.Type = Type;
            this.Duration = Dura;
            this.dword8 = Dura;
            this.Modifier = Modi;
            SizeIncrease = SizeInc;
        }
        public bool Tick(Character a2)
        {
            Duration--;
            if (Duration < 1)
            {
                OnBuffEnd(a2);
                return true;
            }
            return false;
        }
        public bool CanStack()
        {
            return Type == 10;
        }
        public void OnBuffEnd(Character Owner)
        {
            if (Type == 14)
            {
                int d = 1200;

                Character c = null;
                foreach (Character v32 in Owner.GetGameObjectManager().GetCharacters())
                {
                    if (v32.IsAlive())
                    {
                        if (!(v32 == Owner || v32.GetIndex() / 16 != Owner.GetIndex() / 16))
                        {
                            if (!v32.IsImmuneAndBulletsGoThrough(Owner.IsInRealm))
                            {
                                if (!v32.CharacterData.IsTrain())
                                {
                                    int v30 = GamePlayUtil.GetDistanceBetween(v32.GetX(), v32.GetY(), Owner.GetX(), Owner.GetY());
                                    if (v30 < d)
                                    {
                                        d = v30;
                                        c = v32;
                                    }

                                }
                            }
                        }
                    }
                }
                if (c == null)
                {
                    Character Source = GamePlayUtil.GetCharacterFromPlayerIndex(SourceIndex, Owner.GetGameObjectManager());
                    if (Source != null)
                    {
                        if (Source.GetGearBoost(19) >= 1) Owner.CauseDamage(Source, Source.GetGearBoost(19)/2, 0, false, null);
                    }
                    return;
                }
                Projectile p = Projectile.ShootProjectile(
                    Owner.GetX(),
                    Owner.GetY(),
                    GamePlayUtil.GetCharacterFromPlayerIndex(SourceIndex, Owner.GetGameObjectManager()),
                    Owner,
                    DataTables.GetProjectileByName("ElectroSniperSecondaryProjectile_001"),
                    c.GetX(),
                    c.GetY(),
                    Modifier,
                    NormalDMG,
                    0, false, 0, Owner.GetBattle(), 0, 1);
                p.IgnoredTargets.Add(new Ignoring(Owner.GetGlobalID(), 999999999));
                p.HomingTarget = c;
                p.SetIndex(SourceIndex);
                p.BelleWeaponBounces = BelleWeaponBounces + 1;
            }
        }
    }
}
