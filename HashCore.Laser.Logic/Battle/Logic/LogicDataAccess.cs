namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Data.Reader;

    /// <summary>
    /// Чтение колонок таблицы напрямую из строки.
    ///
    /// Классы данных привязываются к колонкам по имени свойства, и там, где имя
    /// свойства разошлось с именем колонки, свойство молча остаётся пустым.
    /// Старый движок живёт с этим расхождением, и трогать его нельзя, поэтому
    /// новый слой берёт такие колонки из строки сам — по настоящему имени.
    ///
    /// Читается это не на каждом кадре, а один раз при сборке объекта, так что
    /// цена разбора строки здесь роли не играет.
    /// </summary>
    public static class LogicDataAccess
    {
        public static string GetString(LogicData data, string column, int level = 0)
        {
            if (data == null) return null;

            Row row = data.GetCSVRow();
            if (row == null) return null;

            string value = row.GetValue(column, level);
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public static int GetInt(LogicData data, string column, int level = 0)
        {
            string value = GetString(data, column, level);
            if (value == null) return 0;

            int parsed;
            return int.TryParse(value, out parsed) ? parsed : 0;
        }

        public static bool GetBool(LogicData data, string column, int level = 0)
        {
            string value = GetString(data, column, level);
            return value != null && value.ToLowerInvariant() == "true";
        }

        /// <summary>
        /// Все непустые значения колонки-списка. У таблиц Supercell одна запись
        /// может занимать несколько строк, и такая колонка тогда хранит массив.
        /// </summary>
        public static List<string> GetStringList(LogicData data, string column)
        {
            List<string> values = new List<string>();
            if (data == null) return values;

            Row row = data.GetCSVRow();
            if (row == null) return values;

            int size = row.GetArraySize(column);

            for (int i = 0; i < size; i++)
            {
                string value = row.GetValue(column, i);
                if (!string.IsNullOrEmpty(value)) values.Add(value);
            }

            return values;
        }
    }
}
