using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using WcfServiceLibrary;

namespace ClientApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IService1Callback
    {
        ObservableCollection<TabItem> database = new ObservableCollection<TabItem>();

        Service1Client client;

        public void OnDatabaseUpdated(OnDatabaseUpdated request)
        {
            var index = tabTables.SelectedIndex;

            database = new ObservableCollection<TabItem>(request.database);

            foreach(var table in database)
            {
                table.Content.RowChanged += RowChanged;
            }

            tabTables.ItemsSource = database;

            if (index > -1)
            {
                tabTables.SelectedIndex = index;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            var callback = this;
            var instanceContext = new InstanceContext(callback);
            client = new Service1Client(instanceContext);

            client.Register();

            tabTables.ItemsSource = database;
        }

        private void CreateDB(object sender, RoutedEventArgs e)
        {
            client.CreateDatabase();
        }

        private void SaveDB(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "Database";
            dialog.DefaultExt = ".dbase";
            dialog.Filter = "Database files (.dbase)|*.dbase";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;
                DataContractSerializer serial = new DataContractSerializer(typeof(List<TabItem>));

                FileStream writer = new FileStream(filename, FileMode.Create);

                serial.WriteObject(writer, database);
            }
        }

        private void LoadDB(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Database files (.dbase)|*.dbase";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;
                DataContractSerializer serial = new DataContractSerializer(typeof(List<TabItem>));

                FileStream reader = new FileStream(filename, FileMode.Open);

                var db = serial.ReadObject(reader) as List<TabItem>;

                client.UpdateDatabase(db.ToArray());
            }
        }

        private void CreateTable(object sender, RoutedEventArgs e)
        {
            AddTableDialog addDialog = new AddTableDialog();
            var result = addDialog.ShowDialog();

            if (result == true)
            {
                client.CreateTable(addDialog.TabName);
            }
        }

        private void RowChanged(object sender, DataRowChangeEventArgs e)
        {
            var index = tabTables.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            client.UpdateTable(index, database[index].Content);
        }

        private void DeleteTable(object sender, RoutedEventArgs e)
        {
            RemoveTableDialog removeDialog = new RemoveTableDialog();
            var result = removeDialog.ShowDialog();

            if (result == true)
            {
                client.DeleteTable(removeDialog.TabName);
            }
        }

        private void AddColumn(object sender, RoutedEventArgs e)
        {
            AddColumnDialog addDialog = new AddColumnDialog();
            var result = addDialog.ShowDialog();

            if (result == true)
            {
                var index = tabTables.SelectedIndex;
                if (index < 0)
                {
                    return;
                }
                client.AddColumn(index, addDialog.ColName, addDialog.ColType);
            }
        }

        private void DeleteColumn(object sender, RoutedEventArgs e)
        {
            RemoveColumnDialog removeDialog = new RemoveColumnDialog();
            var result = removeDialog.ShowDialog();

            if (result == true)
            {
                var index = tabTables.SelectedIndex;
                if (index < 0)
                {
                    return;
                }
                client.DeleteColumn(index, removeDialog.ColName, removeDialog.ColType);
            }
        }

        private void RemoveDuplicateRows(object sender, RoutedEventArgs e)
        {
            var index = tabTables.SelectedIndex;
            if (index < 0)
            {
                return;
            }
            client.DeleteDuplicateRows(index);
        }
    }
}
