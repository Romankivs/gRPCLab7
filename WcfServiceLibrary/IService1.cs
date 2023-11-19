using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WcfServiceLibrary
{
    public interface IService1Callback
    {
        [OperationContract(IsOneWay = true)]
        void OnDatabaseUpdated(List<TabItem> database);
    }

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceKnownType(typeof(ComplexInteger))]
    [ServiceKnownType(typeof(ComplexReal))]
    [ServiceContract(CallbackContract = typeof(IService1Callback))]
    public interface IService1
    {
        [OperationContract(IsOneWay = true)]
        void Register();

        [OperationContract(IsOneWay = true)]
        void CreateDatabase();

        [OperationContract(IsOneWay = true)]
        void CreateTable(string name);

        [OperationContract(IsOneWay = true)]
        void DeleteTable(string name);

        [OperationContract(IsOneWay = true)]
        void AddColumn(int tableIndex, string name, string colType);

        [OperationContract(IsOneWay = true)]
        void DeleteColumn(int tableIndex, string name, string colType);

        [OperationContract(IsOneWay = true)]
        void DeleteDuplicateRows(int tableIndex);

        [OperationContract(IsOneWay = true)]
        void UpdateTable(int tableIndex, DataTable table);

        [OperationContract(IsOneWay = true)]
        void UpdateDatabase(List<TabItem> db);
    }

    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    // You can add XSD files into the project. After building the project, you can directly use the data types defined there, with the namespace "WcfServiceLibrary.ContractType".
    [KnownType(typeof(ComplexInteger))]
    [KnownType(typeof(ComplexReal))]
    [DataContract]
    public class TabItem
    {
        DataTable boolValue = new DataTable("");
        string stringValue = "";

        [DataMember]
        public DataTable Content
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string Header
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }
}
