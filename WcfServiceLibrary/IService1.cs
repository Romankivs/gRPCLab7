using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WcfServiceLibrary
{
    [DataContract]
    public class TableInfo
    {
        string name = "";
        string type = "";

        [DataMember]
        public string ColumnName
        {
            get { return name; }
            set { name = value; }
        }

        [DataMember]
        public string ColumnType
        {
            get { return type; }
            set { type = value; }
        }
    }

    [ServiceContract]
    public interface IService1
    {
        [OperationContract]
        void CreateDatabase();

        [OperationContract]
        void AddTable(string tableName, TableInfo[] columnInfo);

        [OperationContract]
        void RemoveTable(string tableName);

        [OperationContract]
        void AddNewRow(string tableName);

        [OperationContract]
        void AddRow(string tableName, object[] values);

        [OperationContract]
        void DeleteRow(string tableName, int rowIndex);

        [OperationContract]
        void AddColumn(string tableName, TableInfo columnInfo);

        [OperationContract]
        void DeleteColumn(string tableName, string columnName);

        [OperationContract]
        void RemoveDuplicates(string tableName);

        [OperationContract]
        List<TableInfo> GetColumnsInfo(string tableName);

        [OperationContract]
        List<string> GetTables();

        [OperationContract]
        List<object[]> DisplayTable(string tableName);

        [OperationContract]
        bool UpdateTableCell(string tableName, int row, string colName, string value);
    }
}
