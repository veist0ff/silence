namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Поиск пути, 1:1 с LogicPathFinder.
    ///
    /// Работает не по клеткам, а по подклеткам: три подклетки на клетку, то есть
    /// втрое подробнее карты. Узел адресуется одним числом x + y * ширина.
    ///
    /// Смещения оригинала:
    ///   +24  состояние узла: 0 не тронут, 1 в очереди, 2 закрыт
    ///   +32  собранный путь
    ///   +40  откуда пришли в узел
    ///   +48  накопленная стоимость
    ///   +56  узлы, которых надо избегать
    ///   +64  длина собранного пути
    ///   +68  ширина в подклетках
    ///   +72  высота в подклетках
    ///   +76  цель по X, +80 цель по Y
    ///   +84  размер кучи
    ///   +112 эвристика узла
    ///   +120 узел, до которого поиск реально дошёл
    /// </summary>
    public class LogicPathFinder
    {
        /// <summary>Стоимость шага по прямой.</summary>
        public const int StraightCost = 100;

        /// <summary>
        /// Стоимость шага по диагонали. Это корень из двух, умноженный на сто и
        /// округлённый — отсюда и 141.
        /// </summary>
        public const int DiagonalCost = 141;

        /// <summary>
        /// Штраф за узел, который помечен как опасный. Он настолько велик, что
        /// путь через такой узел выбирается только если другого нет вовсе.
        /// </summary>
        public const int AvoidPenalty = 100000;

        /// <summary>
        /// Во сколько раз эвристика перевешивает накопленную стоимость.
        ///
        /// Оригинал кладёт в кучу g + 2 * h, а не g + h. Это взвешенный поиск:
        /// он заметно быстрее обычного, но путь может выйти длиннее кратчайшего.
        /// Менять множитель нельзя — маршруты разойдутся с клиентом.
        /// </summary>
        public const int HeuristicWeight = 2;

        private const byte StateUntouched = 0;
        private const byte StateOpen = 1;
        private const byte StateClosed = 2;

        private readonly LogicTileMap _map;

        private readonly int _width;
        private readonly int _height;

        private readonly byte[] _state;
        private readonly int[] _cameFrom;
        private readonly int[] _cost;
        private readonly int[] _heuristic;
        private readonly bool[] _avoid;

        private readonly int[] _heapNodes;
        private readonly int[] _heapPriorities;
        private readonly int[] _heapIndexByNode;
        private int _heapSize;

        private int _goalX;
        private int _goalY;

        /// <summary>Собранный путь в узлах, от цели к началу.</summary>
        public readonly List<int> Path = new List<int>();

        /// <summary>Узел, до которого поиск реально дошёл. Он же ближайший к цели.</summary>
        public int ReachedNode;

        public LogicPathFinder(LogicTileMap map)
        {
            _map = map;

            _width = map.Width * LogicTile.SubTilesPerTile;
            _height = map.Height * LogicTile.SubTilesPerTile;

            int count = _width * _height;

            _state = new byte[count];
            _cameFrom = new int[count];
            _cost = new int[count];
            _heuristic = new int[count];
            _avoid = new bool[count];

            _heapNodes = new int[count];
            _heapPriorities = new int[count];
            _heapIndexByNode = new int[count];
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        /// <summary>Номер узла по координатам подклетки.</summary>
        public int GetNode(int subX, int subY)
        {
            return subX + subY * _width;
        }

        /// <summary>Помечает узел опасным: путь через него станет крайне дорогим.</summary>
        public void SetAvoid(int subX, int subY, bool avoid)
        {
            if (subX < 0 || subY < 0 || subX >= _width || subY >= _height) return;
            _avoid[GetNode(subX, subY)] = avoid;
        }

        public void ClearAvoid()
        {
            for (int i = 0; i < _avoid.Length; i++) _avoid[i] = false;
        }

        /// <summary>
        /// 0x10050a38c. Ищет путь от начала к цели.
        ///
        /// Возвращает истину, только если цель действительно достигнута. Даже при
        /// неудаче поле ReachedNode содержит ближайший к цели узел, до которого
        /// поиск добрался, — по нему можно пойти хотя бы в нужную сторону.
        /// </summary>
        public bool FindPath(int layer, int startNode, int goalNode, bool ignoreA, bool ignoreB, bool allowTeleport)
        {
            Path.Clear();

            _goalX = goalNode % _width;
            _goalY = goalNode / _width;

            for (int i = 0; i < _state.Length; i++) _state[i] = StateUntouched;

            _cameFrom[startNode] = -1;
            _cameFrom[goalNode] = -1;
            _cost[startNode] = 0;
            _heuristic[startNode] = int.MaxValue;
            _heapSize = 0;

            int best = goalNode;

            if (startNode != goalNode)
            {
                int bestHeuristic = int.MaxValue;
                best = startNode;

                int current = startNode;

                do
                {
                    int heuristic = _heuristic[current];
                    if (bestHeuristic > heuristic) best = current;

                    _state[current] = StateClosed;
                    ExpandNeighbours(layer, current, ignoreA, ignoreB, allowTeleport);

                    if (_heapSize == 0) break;

                    if (bestHeuristic >= heuristic) bestHeuristic = heuristic;

                    current = _heapNodes[0];
                    RemoveFromHeapIndex(0);
                }
                while (current != goalNode);
            }

            ReachedNode = _state[goalNode] == StateClosed ? goalNode : best;

            // Путь собирается от цели назад. Если цель не достигнута, оттуда
            // некуда идти, и путь получается вырожденным.
            int node = goalNode;

            while (node != -1)
            {
                Path.Add(node);
                node = _cameFrom[node];
            }

            if (Path.Count < 2)
            {
                Path.Clear();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 0x10050a4f4. Раскрывает соседей узла.
        ///
        /// Диагональ добавляется ТОЛЬКО когда оба соседних прямых прохода открыты.
        /// Из-за этого персонаж не срезает углы сквозь стык двух стен.
        /// </summary>
        private void ExpandNeighbours(int layer, int node, bool ignoreA, bool ignoreB, bool allowTeleport)
        {
            int x = node % _width;
            int y = node / _width;

            bool up = TryAddNeighbour(layer, node, x, y - 1, StraightCost, ignoreA, ignoreB);
            bool down = TryAddNeighbour(layer, node, x, y + 1, StraightCost, ignoreA, ignoreB);
            bool left = TryAddNeighbour(layer, node, x - 1, y, StraightCost, ignoreA, ignoreB);
            bool right = TryAddNeighbour(layer, node, x + 1, y, StraightCost, ignoreA, ignoreB);

            if (up && left) TryAddNeighbour(layer, node, x - 1, y - 1, DiagonalCost, ignoreA, ignoreB);
            if (down && left) TryAddNeighbour(layer, node, x - 1, y + 1, DiagonalCost, ignoreA, ignoreB);
            if (down && right) TryAddNeighbour(layer, node, x + 1, y + 1, DiagonalCost, ignoreA, ignoreB);
            if (up && right) TryAddNeighbour(layer, node, x + 1, y - 1, DiagonalCost, ignoreA, ignoreB);

            if (!allowTeleport) return;

            int teleportX;
            int teleportY;

            if (!TryGetTeleportPair(x / LogicTile.SubTilesPerTile, y / LogicTile.SubTilesPerTile,
                    out teleportX, out teleportY))
                return;

            TryAddNeighbour(layer, node,
                LogicTile.SubTilesPerTile * teleportX,
                LogicTile.SubTilesPerTile * teleportY,
                StraightCost, ignoreA, ignoreB);
        }

        /// <summary>
        /// 0x10050a780. Пробует добавить соседа в очередь.
        ///
        /// Истина означает «через этого соседа можно пройти» — в том числе когда
        /// он уже закрыт. Именно по этому ответу решается, разрешена ли диагональ.
        /// </summary>
        private bool TryAddNeighbour(
            int layer, int fromNode, int x, int y, int moveCost, bool ignoreA, bool ignoreB)
        {
            if (y < 0 || y >= _height) return false;
            if (x < 0 || x >= _width) return false;

            int tileCost = _map.GetPathFinderCost(x, y, layer, ignoreA, ignoreB);
            if (tileCost == int.MaxValue) return false;

            int node = GetNode(x, y);

            if (_state[node] == StateClosed) return true;

            int penalty = _avoid[node] ? AvoidPenalty : 0;
            int cost = tileCost + moveCost + _cost[fromNode] + penalty;

            if (_state[node] == StateOpen)
            {
                if (cost >= _cost[node]) return true;
                RemoveFromHeapIndex(_heapIndexByNode[node]);
            }
            else
            {
                _state[node] = StateOpen;
            }

            _cameFrom[node] = fromNode;
            _cost[node] = cost;

            int heuristic = GetHeuristic(x, y);
            _heuristic[node] = heuristic;

            AddToHeap(node, cost + HeuristicWeight * heuristic);
            return true;
        }

        /// <summary>
        /// Октильное расстояние до цели: сначала идём по диагонали, сколько можем,
        /// остаток по прямой.
        /// </summary>
        private int GetHeuristic(int x, int y)
        {
            int dx = _goalX - x;
            if (dx < 0) dx = -dx;

            int dy = _goalY - y;
            if (dy < 0) dy = -dy;

            int longer = dx > dy ? dx : dy;

            int difference = dx - dy;
            if (difference < 0) difference = -difference;

            return StraightCost * difference + DiagonalCost * (longer - difference);
        }

        /// <summary>0x10050a8fc. Вставка в двоичную кучу с просеиванием вверх.</summary>
        private void AddToHeap(int node, int priority)
        {
            int index = _heapSize;
            _heapSize++;

            while (index >= 1)
            {
                int parent = (index - 1) >> 1;
                if (_heapPriorities[parent] <= priority) break;

                int parentNode = _heapNodes[parent];

                _heapPriorities[index] = _heapPriorities[parent];
                _heapNodes[index] = parentNode;
                _heapIndexByNode[parentNode] = index;

                index = parent;
            }

            _heapPriorities[index] = priority;
            _heapNodes[index] = node;
            _heapIndexByNode[node] = index;
        }

        /// <summary>0x10050b114. Удаление из кучи с просеиванием вниз.</summary>
        private void RemoveFromHeapIndex(int index)
        {
            _heapIndexByNode[_heapNodes[index]] = -1;

            _heapSize--;
            int last = _heapSize;

            if (last == index) return;

            int priority = _heapPriorities[last];
            int node = _heapNodes[last];

            int child = 2 * index + 2;

            if (child < last)
            {
                int sibling = 2 * index + 1;
                if (_heapPriorities[sibling] < _heapPriorities[child]) child = sibling;

                int childPriority = _heapPriorities[child];

                while (childPriority < priority)
                {
                    int childNode = _heapNodes[child];

                    _heapPriorities[index] = childPriority;
                    _heapNodes[index] = childNode;
                    _heapIndexByNode[childNode] = index;

                    index = child;
                    child = 2 * index + 2;

                    if (child >= last) break;

                    sibling = 2 * index + 1;
                    if (_heapPriorities[sibling] < _heapPriorities[child]) child = sibling;

                    childPriority = _heapPriorities[child];
                }
            }

            if (child == last)
            {
                int only = last - 1;

                if (_heapPriorities[only] < priority)
                {
                    int onlyNode = _heapNodes[only];

                    _heapPriorities[index] = _heapPriorities[only];
                    _heapNodes[index] = onlyNode;
                    _heapIndexByNode[onlyNode] = index;

                    index = only;
                }
            }

            _heapPriorities[index] = priority;
            _heapNodes[index] = node;
            _heapIndexByNode[node] = index;
        }

        /// <summary>
        /// 0x10050b018. Первое препятствие на прямой между двумя подклетками.
        ///
        /// Это обычный Брезенхэм, но четырёхсвязный: за шаг двигается только одна
        /// ось, поэтому всего шагов dx + dy + 1. Последние stopBefore шагов не
        /// проверяются — так вызывающий исключает саму цель.
        /// </summary>
        public bool GetFirstCollisionOnLine(
            int layer, int fromX, int fromY, int toX, int toY, int stopBefore,
            bool ignoreA, bool ignoreB, out int hitX, out int hitY)
        {
            hitX = 0;
            hitY = 0;

            int stepX = toX > fromX ? 1 : -1;
            int stepY = toY > fromY ? 1 : -1;

            int dx = toX - fromX;
            if (dx < 0) dx = -dx;

            int dy = toY - fromY;
            if (dy < 0) dy = -dy;

            int steps = dx + dy + 1;
            if (steps <= stopBefore) return false;

            int x = fromX;
            int y = fromY;
            int error = dx - dy;

            while (_map.IsPassablePathFinder(x, y, layer, ignoreA, ignoreB))
            {
                if (error > 0)
                {
                    x += stepX;
                    error -= 2 * dy;
                }
                else
                {
                    y += stepY;
                    error += 2 * dx;
                }

                steps--;
                if (steps <= stopBefore) return false;
            }

            hitX = x;
            hitY = y;
            return true;
        }

        /// <summary>
        /// 0x10050adac. Свободен ли прямой проход.
        ///
        /// Для широкого прохода мало проверить одну линию: у диагонального хода
        /// дополнительно проверяются две линии, сдвинутые на шаг по каждой оси,
        /// иначе персонаж шириной больше подклетки задевал бы угол.
        /// </summary>
        public bool IsClearPath(
            int layer, int fromX, int fromY, int toX, int toY, int stopBefore,
            bool wide, bool ignoreA, bool ignoreB)
        {
            int hitX;
            int hitY;

            if (GetFirstCollisionOnLine(layer, fromX, fromY, toX, toY, stopBefore, ignoreA, ignoreB,
                    out hitX, out hitY))
                return false;

            if (!wide) return true;
            if (toX == fromX || toY == fromY) return true;

            int stepX = toX - fromX >= 1 ? 1 : -1;
            int stepY = toY - fromY >= 1 ? 1 : -1;

            if (GetFirstCollisionOnLine(layer, fromX + stepX, fromY, toX, toY - stepY, stopBefore,
                    ignoreA, ignoreB, out hitX, out hitY))
                return false;

            return !GetFirstCollisionOnLine(layer, fromX, fromY + stepY, toX - stepX, toY, stopBefore,
                ignoreA, ignoreB, out hitX, out hitY);
        }

        /// <summary>0x1003b1af4. Парная точка телепорта, если она есть на этой клетке.</summary>
        protected virtual bool TryGetTeleportPair(int tileX, int tileY, out int pairX, out int pairY)
        {
            pairX = 0;
            pairY = 0;
            return false;
        }
    }
}
