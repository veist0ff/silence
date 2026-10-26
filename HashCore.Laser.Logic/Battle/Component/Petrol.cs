using HashCore.Laser.Logic.Battle.Level;
using HashCore.Laser.Logic.Battle.Objects;
using HashCore.Laser.Logic.Data;
using HashCore.Laser.Logic.Util;
using HashCore.Laser.Titan.Math;

namespace HashCore.Laser.Logic.Battle.Structures
{

    public class Petrol
    {
        public int DestructedTicks;
        public bool Ignited;
        public int PlayerIndex;
        public int TeamIndex;
        public int X;
        public int Y;
        public int Scale;
        public int Damage;
        public int Slowing;
        public Petrol(int a2, int a3, int a4, int a5, int a6, int a7)
        {
            Damage = 0;
            DestructedTicks = 0;
            Ignited = false;
            PlayerIndex = a5;
            TeamIndex = a6;
            X = a2;
            Y = a3;
            Scale = a4 * a4;
        }
        public bool Tick(GameObjectManager a2)
        {
            if (!Ignited)
            {
                Projectile[] v10 = a2.GetProjectiles();
                if (v10.Count() > 0)
                {
                    for (int i = 0; i < v10.Count(); i++)
                    {
                        Projectile v13 = v10[i];
                        if (v13.GetIndex() == PlayerIndex && !v13.ProjectileData.Indirect)
                        {
                            if (GamePlayUtil.GetDistanceSquaredBetween(v13.GetX(), v13.GetY(), X * 300 + 150, Y * 300 + 150) < v13.ProjectileData.Radius + Scale) Ignite(a2, -1);
                        }
                    }
                }
            }
            if (Slowing >= 1)
            {
                foreach (GameObject gameObject in a2.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 0)
                        continue;
                    Character v10 = (Character)gameObject;
                    if (!v10.IsAlive())
                        continue;
                    if (GamePlayUtil.GetDistanceBetween(v10.GetX(), v10.GetY(), X * 300 + 150, Y * 300 + 150) >= v10.CharacterData.CollisionRadius + LogicMath.Sqrt(Scale)) continue;
                    if (!v10.IsImmuneAndBulletsGoThrough(false) && v10.GetIndex() / 16 != PlayerIndex / 16)
                    {
                        v10.GiveSpeedSlowerBuff(-v10.CharacterData.Speed * Slowing / 100, 2);
                    }
                }
            }

            if (DestructedTicks < 1) return false;
            DestructedTicks--;
            return DestructedTicks == 0;
        }
        public void Ignite(GameObjectManager a2, int a3)
        {
            if (!Ignited)
            {
                Tile v10 = a2.GetBattle().GetTileMap().GetTile(X, Y, true);
                if (v10 != null && v10.Data.HidesHero || v10.Data.TileCode == "B") v10.Destruct();
                AreaEffectData v11 = DataTables.GetAreaEffectByName("PetrolFire");
                string aed = GamePlayUtil.GetCharacterFromPlayerIndex(PlayerIndex, a2)?.GetPlayer()?.SkinConfData?.AreaEffectUlti;
                if (aed != null) v11 = DataTables.GetAreaEffectByName(aed);
                AreaEffect v12 = GameObjectFactory.CreateGameObjectByData(v11);
                v12.SetPosition(X * 300 + 150, Y * 300 + 150, 0);
                v12.SetIndex(PlayerIndex);
                if (a3 == -1)
                {
                    Character v16 = GamePlayUtil.GetCharacterFromPlayerIndex(PlayerIndex, a2);
                    if (v16 != null)
                    {
                        a3 = v11.Damage * ((v16.GetUltimateSkill().Level - 1) * 10 + 100) / 100;
                        a3 = a3 * (100 + v16.GetDamageBuffTemporary()) / 100;

                    }
                }
                Damage = a3;
                v12.Damage = a3;
                v12.NormalDMG = a3;
                v12.SetSource(GamePlayUtil.GetCharacterFromPlayerIndex(PlayerIndex, a2), 2);
                a2.AddGameObject(v12);
                v12.Trigger();
                DestructedTicks = 4;
                Ignited = true;
            }
        }
    }
}
