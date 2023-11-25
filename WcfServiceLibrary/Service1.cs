using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WcfServiceLibrary
{
    public class DynamicTable
    {
        public List<(string, Type)> columnInfo;
        public List<Dictionary<string, object>> rows;

        public DynamicTable(List<(string, Type)> columnInfo)
        {
            this.columnInfo = columnInfo;
            this.rows = new List<Dictionary<string, object>>();
        }

        public void AddColumn(string columnName, Type columnType)
        {
            if (columnInfo.Any(col => col.Item1 == columnName))
            {
                throw new ArgumentException($"Column '{columnName}' already exists");
            }

            columnInfo.Add((columnName, columnType));

            foreach (var row in rows)
            {
                row[columnName] = null;
            }
        }

        public void DeleteColumn(string columnName)
        {
            if (!columnInfo.Any(col => col.Item1 == columnName))
            {
                throw new ArgumentException($"Column '{columnName}' does not exist");
            }

            int index = columnInfo.FindIndex(col => col.Item1 == columnName);
            columnInfo.RemoveAt(index);

            foreach (var row in rows)
            {
                row.Remove(columnName);
            }
        }

        public void AddRow(List<object> values)
        {
            if (values.Count != columnInfo.Count)
            {
                throw new ArgumentException("Number of values must match the number of columns");
            }

            var validatedValues = new List<object>();
            for (int i = 0; i < columnInfo.Count; i++)
            {
                var (columnName, columnType) = columnInfo[i];
                var value = values[i];

                if (!columnType.IsInstanceOfType(value))
                {
                    throw new ArgumentException($"Invalid type for column '{columnName}'. Expected {columnType}, got {value.GetType()}");
                }

                validatedValues.Add(value);
            }

            var row = columnInfo.Zip(validatedValues, (col, val) => new { col.Item1, val })
                               .ToDictionary(pair => pair.Item1, pair => pair.val);

            rows.Add(row);
        }

        public void AddNewRow()
        {
            var list = new List<object>(new object[columnInfo.Count]);

            var row = columnInfo.Zip(list, (col, val) => new { col.Item1, val })
                   .ToDictionary(pair => pair.Item1, pair => pair.val);

            rows.Add(row);
        }

        public void RemoveRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= rows.Count)
            {
                throw new ArgumentOutOfRangeException("rowIndex", "Invalid row index");
            }

            rows.RemoveAt(rowIndex);
        }

        public void RemoveDuplicates()
        {
            var seenRows = new HashSet<List<object>>(new ListEqualityComparer<object>());

            var uniqueRows = new List<Dictionary<string, object>>();

            foreach (var row in rows)
            {
                var values = row.Values.ToList();

                if (seenRows.Add(values))
                {
                    uniqueRows.Add(row);
                }
            }

            rows = uniqueRows;
        }
    }

    public class ListEqualityComparer<T> : IEqualityComparer<List<T>>
    {
        public bool Equals(List<T> x, List<T> y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(List<T> obj)
        {
            return obj.Aggregate(0, (hash, item) => {
                if (item == null)
                {
                    return hash ^ 0;
                }
                return hash ^ item.GetHashCode();
            });
        }
    }

    public class Database
    {
        public Dictionary<string, DynamicTable> tables;

        public Database()
        {
            tables = new Dictionary<string, DynamicTable>();
        }

        public void AddTable(string tableName, List<(string, Type)> columnInfo)
        {
            if (tables.ContainsKey(tableName))
            {
                throw new ArgumentException($"Table '{tableName}' already exists");
            }

            tables[tableName] = new DynamicTable(columnInfo);
        }

        public void RemoveTable(string tableName)
        {
            if (!tables.ContainsKey(tableName))
            {
                throw new ArgumentException($"Table '{tableName}' does not exist");
            }

            tables.Remove(tableName);
        }
    }


    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true)]
    public class Service1 : IService1
    {
        static Database database = new Database();

        public void CreateDatabase()
        {
            database = new Database();
        }

        public void AddTable(string tableName, TableInfo[] columnInfo)
        {
            database.AddTable(tableName, columnInfo.Select(info => (info.ColumnName, Type.GetType(info.ColumnType))).ToList());
        }

        public void RemoveTable(string tableName)
        {
            database.RemoveTable(tableName);
        }

        public void AddNewRow(string tableName)
        {
            if (database.tables.ContainsKey(tableName))
            {
                database.tables[tableName].AddNewRow();
            }
            else
            {
                throw new ArgumentException($"Table '{tableName}' does not exist");
            }
        }

        public void AddRow(string tableName, object[] values)
        {
            if (database.tables.ContainsKey(tableName))
            {
                database.tables[tableName].AddRow(values.ToList());
            }
            else
            {
                throw new ArgumentException($"Table '{tableName}' does not exist");
            }
        }

        public void DeleteRow(string tableName, int rowIndex)
        {
            if (database.tables.ContainsKey(tableName))
            {
                database.tables[tableName].RemoveRow(rowIndex - 1);
            }
            else
            {
                throw new ArgumentException($"Table '{tableName}' does not exist");
            }
        }

        public void AddColumn(string tableName, TableInfo columnInfo)
        {
            if (database.tables.ContainsKey(tableName))
            {
                database.tables[tableName].AddColumn(columnInfo.ColumnName, Type.GetType(columnInfo.ColumnType));
            }
            else
            {
                throw new ArgumentException($"Table '{tableName}' does not exist");
            }
        }

        public void DeleteColumn(string tableName, string columnName)
        {
            if (database.tables.ContainsKey(tableName))
            {
                database.tables[tableName].DeleteColumn(columnName);
            }
            else
            {
                throw new ArgumentException($"Table '{tableName}' does not exist");
            }
        }

        public void RemoveDuplicates(string tableName)
        {
            if (database.tables.ContainsKey(tableName))
            {
                database.tables[tableName].RemoveDuplicates();
            }
            else
            {
                throw new ArgumentException($"Table '{tableName}' does not exist");
            }
        }

        public List<TableInfo> GetColumnsInfo(string tableName)
        {
            if (database.tables.ContainsKey(tableName))
            {
                return database.tables[tableName].columnInfo.Select(x => new TableInfo() { ColumnName = x.Item1, ColumnType = x.Item2.ToString() }).ToList();
            }
            else
            {
                throw new ArgumentException($"Table '{tableName}' does not exist");
            }
        }

        public List<string> GetTables()
        {
            return database.tables.Keys.ToList();
        }

        public List<object[]> DisplayTable(string tableName)
        {
            if (!database.tables.ContainsKey(tableName))
            {
                throw new ArgumentException($"Table '{tableName}' does not exist.");
            }

            return database.tables[tableName].rows.Select(row => row.Values.ToArray()).ToList();
        }

        public bool UpdateTableCell(string tableName, int row, string colName, string value)
        {
            if (!database.tables.ContainsKey(tableName))
            {
                throw new ArgumentException($"Table '{tableName}' does not exist.");
            }

            var colType = database.tables[tableName].columnInfo.Where(e => e.Item1 == colName).Max(e => e.Item2);

            try
            {
                var converted = Convert.ChangeType(value, colType);
                database.tables[tableName].rows[row][colName] = converted;
                return true;
            }
            catch
            {
                return false;  
            }
        }
    }
}
