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
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true)]
    public class Service1 : IService1
    {
        private static List<IService1Callback> callbacks = new List<IService1Callback>();

        static List<TabItem> database = new List<TabItem>();

        public void Register()
        {
            var callback = OperationContext.Current.GetCallbackChannel<IService1Callback>();
            callbacks.Add(callback);

            callback.OnDatabaseUpdated(database);

            Console.Out.WriteLine("Registered client");
        }

        public void CreateDatabase()
        {
            database = new List<TabItem>();

            foreach (var callback in callbacks)
            {
                callback.OnDatabaseUpdated(database);
            }
        }

        public void CreateTable(string name)
        {
            var tabItem = new TabItem();
            tabItem.Content = new DataTable(name);
            tabItem.Content.Columns.Add(new DataColumn("Column (String)", typeof(string)));
            tabItem.Header = name;
            database.Add(tabItem);
            foreach (var callback in callbacks)
            {
                callback.OnDatabaseUpdated(database);
            }
        }

        public void DeleteTable(string name)
        {
            database.RemoveAll(table => table.Header == name);

            foreach (var callback in callbacks)
            {
                callback.OnDatabaseUpdated(database);
            }
        }

        public void AddColumn(int tableIndex, string name, string colType)
        {
            DataTable selectedTable = database[tableIndex].Content;

            Type type = ColTypes.ColStringToType(colType);
            DataColumn newCol = new DataColumn(name + " (" + colType + ")", type);
            selectedTable.Columns.Add(newCol);

            foreach (var callback in callbacks)
            {
                callback.OnDatabaseUpdated(database);
            }
        }

        public void DeleteColumn(int tableIndex, string name, string colType)
        {
            DataTable selectedTable = database[tableIndex].Content;
            for (int i = 0; i < selectedTable.Columns.Count; i++)
            {
                if (selectedTable.Columns[i].ColumnName == name + " (" + colType + ")")
                {
                    selectedTable.Columns.RemoveAt(i);
                }
            }

            foreach (var callback in callbacks)
            {
                callback.OnDatabaseUpdated(database);
            }
        }

        public void DeleteDuplicateRows(int tableIndex)
        {
            var table = database[tableIndex].Content;

            List<string> uniqueRowHashes = new List<string>();
            List<DataRow> rowsToRemove = new List<DataRow>();
            StringBuilder rowHashBuilder = new StringBuilder();

            foreach (DataRow row in table.Rows)
            {
                rowHashBuilder.Clear();
                foreach (DataColumn column in table.Columns)
                {
                    rowHashBuilder.Append(row[column.ColumnName].ToString());
                }

                string rowHash = rowHashBuilder.ToString();
                if (uniqueRowHashes.Contains(rowHash))
                {
                    rowsToRemove.Add(row);
                }
                else
                {
                    uniqueRowHashes.Add(rowHash);
                }
            }

            foreach (DataRow row in rowsToRemove)
            {
                table.Rows.Remove(row);
            }

            foreach (var callback in callbacks)
            {
                callback.OnDatabaseUpdated(database);
            }
        }

        public void UpdateTable(int tableIndex, DataTable table)
        {
            database[tableIndex].Content = table;

            foreach (var callback in callbacks)
            {
                if (callback != OperationContext.Current.GetCallbackChannel<IService1Callback>())
                {
                    callback.OnDatabaseUpdated(database);  
                }
            }
        }

        public void UpdateDatabase(List<TabItem> db)
        {
            database = db;

            foreach (var callback in callbacks)
            {
                callback.OnDatabaseUpdated(database);
            }
        }
    }
}
