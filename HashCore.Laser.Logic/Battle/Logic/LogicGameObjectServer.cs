namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data.Helper;

    /// <summary>
    /// Тип объекта боя. Значения взяты из LogicGameObjectManagerServer::
    /// addLogicGameObject (0x10046eee8): там объект после добавления в общий массив
    /// раскладывается по индексным спискам именно по этим кодам —
    /// 3 идёт в список +64, 1 в +80, 2 в +96, а всё остальное (то есть 0)
    /// попадает в ветку else со списком персонажей +32 и проверкой isCarryable.
    /// Ровно те же коды разбираются в doDestructing (0x10046e628).
    ///
    /// generateGameObjectGlobalID (0x10046f164) падает с
    /// "Index is out of bounds", если тип >= 4, — значит типов ровно четыре.
    /// </summary>
    public enum LogicGameObjectType
    {
        Character = 0,
        Projectile = 1,
        AreaEffect = 2,
        Item = 3,
    }

    /// <summary>
    /// Базовый объект боя, 1:1 с LogicGameObjectServer из либы.
    ///
    /// Раскладка виртуальных слотов, которые реально дёргает менеджер:
    ///   +16  attachLogicGameObjectManager   (addLogicGameObject, LABEL_7)
    ///   +32  tick                           (LogicGameObjectManagerServer::tick)
    ///   +40  shouldDestruct                 (doDestructing, снимает объект с массива)
    ///   +64  getGameObjectType              (роутинг по индексным спискам)
    ///   +104 resetEventsOnTick              (preTick)
    /// </summary>
    public abstract partial class LogicGameObjectServer
    {
        /// <summary>Данные строки CSV, с которыми объект создан.</summary>
        public LogicData Data;

        /// <summary>
        /// Глобальный id объекта. Выдаётся менеджером и НЕ совпадает с id данных:
        /// generateGameObjectGlobalID собирает его как createGlobalID(тип + 1, счётчик),
        /// где счётчик свой на каждый тип и лежит в менеджере по смещению 212 + 4 * тип.
        /// Ноль означает «id ещё не выдан» — addLogicGameObject проверяет именно это.
        /// </summary>
        public int GlobalId;

        /// <summary>Менеджер, к которому объект прицеплен.</summary>
        public LogicGameObjectManagerServer Manager;

        /// <summary>Позиция в логических единицах. 300 единиц — одна клетка тайлмапа.</summary>
        public int X;
        public int Y;

        /// <summary>Индекс команды владельца, -1 если объект ничей.</summary>
        public int TeamIndex = -1;

        /// <summary>Объект помечен на снятие и будет убран ближайшим doDestructing.</summary>
        public bool Destructed;

        /// <summary>
        /// Поле +56. Трейты объекта. Лежат прямо в объекте, а не в отдельной
        /// структуре, поэтому во всех вызовах оригинала видно this + 56.
        /// </summary>
        public readonly LogicTraitController Traits = new LogicTraitController();

        /// <summary>
        /// Измерение, в котором находится объект. getDimensionIndex, слот
        /// LogicGameObjectServer (0x1004738a0): объекты из разных измерений
        /// друг друга не видят и не задевают.
        /// </summary>
        public int DimensionIndex;

        /// <summary>
        /// 0x10045e818. Радиус объекта в логических единицах. У тех, кто ничего
        /// собой не занимает, он нулевой.
        /// </summary>
        public virtual int GetRadius()
        {
            return 0;
        }

        protected LogicGameObjectServer(LogicData data)
        {
            Data = data;
        }

        public abstract LogicGameObjectType GetGameObjectType();

        /// <summary>Слот +16. Вызывается из addLogicGameObject один раз при добавлении.</summary>
        public virtual void AttachLogicGameObjectManager(LogicGameObjectManagerServer manager)
        {
            Manager = manager;
        }

        /// <summary>Слот +32. Шаг объекта. Менеджер зовёт его по снимку списка.</summary>
        public virtual void Tick()
        {
        }

        /// <summary>
        /// Слот +40. Ненулевой ответ — объект снимается с массива тем же проходом
        /// doDestructing, который его и спросил.
        /// </summary>
        public virtual bool ShouldDestruct()
        {
            return Destructed;
        }

        /// <summary>Слот +104. Чистит события, накопленные за прошлый тик.</summary>
        public virtual void ResetEventsOnTick()
        {
        }

        /// <summary>Слот, который менеджер зовёт при снятии объекта.</summary>
        public virtual void RemoveGameObjectReferences(LogicGameObjectServer removed)
        {
        }

        public virtual bool IsAlive()
        {
            return !Destructed;
        }

        /// <summary>
        /// Глобальный id СТРОКИ ДАННЫХ объекта, а не самого объекта. Клиент по нему
        /// создаёт объект нужного вида, поэтому путать его с GlobalId нельзя.
        /// </summary>
        public int GetDataGlobalId()
        {
            return Data != null ? Data.GetGlobalId() : 0;
        }

        public void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
