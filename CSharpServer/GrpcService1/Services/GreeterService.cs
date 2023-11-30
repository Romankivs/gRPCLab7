using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GrpcService1;

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

public class DatabaseService : MyDatabaseService.MyDatabaseServiceBase
{
    private static Database database = new Database();

    public override Task<Empty> CreateDatabase(CreateDatabaseRequest request, ServerCallContext context)
    {
        database = new Database();
        return Task.FromResult(new Empty());
    }

    public override Task<Empty> AddTable(AddTableRequest request, ServerCallContext context)
    {
        string tableName = request.TableName;
        List<(string, Type)> columnInfo = request.ColumnInfo
            .Select(info => (info.ColumnName, Type.GetType(info.ColumnType)))
            .ToList();

        database.AddTable(tableName, columnInfo);
        return Task.FromResult(new Empty());
    }

    public override Task<Empty> RemoveTable(RemoveTableRequest request, ServerCallContext context)
    {
        string tableName = request.TableName;
        database.RemoveTable(tableName);
        return Task.FromResult(new Empty());
    }

    public override Task<Empty> AddNewRow(AddNewRowRequest request, ServerCallContext context)
    {
        string tableName = request.TableName;
        if (database.tables.ContainsKey(tableName))
        {
            database.tables[tableName].AddNewRow();
        }
        else
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Table '{tableName}' does not exist"));
        }
        return Task.FromResult(new Empty());
    }

    public override Task<Empty> DeleteRow(DeleteRowRequest request, ServerCallContext context)
    {
        string tableName = request.TableName;
        int rowIndex = request.RowIndex;

        if (database.tables.ContainsKey(tableName))
        {
            database.tables[tableName].RemoveRow(rowIndex - 1);
        }
        else
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Table '{tableName}' does not exist"));
        }

        return Task.FromResult(new Empty());
    }

    public override Task<Empty> AddColumn(AddColumnRequest request, ServerCallContext context)
    {
        string tableName = request.TableName;
        TableInfo columnInfo = request.ColumnInfo;

        if (database.tables.ContainsKey(tableName))
        {
            database.tables[tableName].AddColumn(columnInfo.ColumnName, Type.GetType(columnInfo.ColumnType));
        }
        else
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Table '{tableName}' does not exist"));
        }

        return Task.FromResult(new Empty());
    }

    public override Task<Empty> DeleteColumn(DeleteColumnRequest request, ServerCallContext context)
    {
        string tableName = request.TableName;
        string columnName = request.ColumnName;

        if (database.tables.ContainsKey(tableName))
        {
            database.tables[tableName].DeleteColumn(columnName);
        }
        else
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Table '{tableName}' does not exist"));
        }

        return Task.FromResult(new Empty());
    }

    public override Task<Empty> RemoveDuplicates(RemoveDuplicatesRequest request, ServerCallContext context)
    {
        string tableName = request.TableName;

        if (database.tables.ContainsKey(tableName))
        {
            database.tables[tableName].RemoveDuplicates();
        }
        else
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Table '{tableName}' does not exist"));
        }

        return Task.FromResult(new Empty());
    }

    public override Task<ColumnsInfoResponse> GetColumnsInfo(GetColumnsInfoRequest request, ServerCallContext context)
    {
        string tableName = request.TableName;

        if (database.tables.ContainsKey(tableName))
        {
            var columnsInfo = database.tables[tableName].columnInfo
                .Select(x => new TableInfo { ColumnName = x.Item1, ColumnType = x.Item2?.ToString() ?? "" })
                .ToList();

            return Task.FromResult(new ColumnsInfoResponse { ColumnsInfo = { columnsInfo } });
        }
        else
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Table '{tableName}' does not exist"));
        }
    }

    public override Task<TablesResponse> GetTables(Empty request, ServerCallContext context)
    {
        var tables = database.tables.Keys.ToList();
        return Task.FromResult(new TablesResponse { Tables = { tables } });
    }

    public override Task<DisplayTableResponse> DisplayTable(DisplayTableRequest request, ServerCallContext context)
    {
        string tableName = request.TableName;

        if (database.tables.ContainsKey(tableName))
        {
            var rows = database.tables[tableName].rows
                .Select(row => new Google.Protobuf.WellKnownTypes.Struct
                {
                    Fields = { row.ToDictionary(kvp => kvp.Key, kvp => Google.Protobuf.WellKnownTypes.Value.ForString(kvp.Value?.ToString() ?? "")) }
                })
                .ToList();

            return Task.FromResult(new DisplayTableResponse { Rows = { rows } });
        }
        else
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Table '{tableName}' does not exist"));
        }
    }

    public override Task<UpdateTableCellResponse> UpdateTableCell(UpdateTableCellRequest request, ServerCallContext context)
    {
        string tableName = request.TableName;
        int row = request.Row;
        string colName = request.ColName;
        string value = request.Value;

        if (database.tables.ContainsKey(tableName))
        {
            var colType = database.tables[tableName].columnInfo
                .Where(e => e.Item1 == colName)
                .Max(e => e.Item2);

            try
            {
                var converted = Convert.ChangeType(value, colType);
                database.tables[tableName].rows[row][colName] = converted;
                return Task.FromResult(new UpdateTableCellResponse { Success = true });
            }
            catch
            {
                return Task.FromResult(new UpdateTableCellResponse { Success = false });
            }
        }
        else
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Table '{tableName}' does not exist"));
        }
    }
}
