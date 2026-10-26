namespace HashCore.Laser.Logic.Battle.Objects
{
    using HashCore.Laser.Logic.Battle.Level;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
     
    public static class GameObjectFactory//+32 type 64 encode 48IsAlive
    {
        public static Character CreateGameObjectByData(CharacterData characterData)
        {
            return new Character(characterData);
        }
        public static Projectile CreateGameObjectByData(ProjectileData characterData)
        {
            return new Projectile(characterData);
        }
        public static Item CreateGameObjectByData(ItemData characterData)
        {
            return new Item(characterData);
        }
        public static AreaEffect CreateGameObjectByData(AreaEffectData characterData)
        {
            return new AreaEffect(characterData);
        }
    }
}
