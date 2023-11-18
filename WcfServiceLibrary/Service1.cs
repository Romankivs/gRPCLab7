using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WcfServiceLibrary
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class Service1 : IService1
    {
        private int test = 0;

        private DataTable table = new DataTable("Wow");

        public string GetData(int value)
        {
            return string.Format("You entered: {0}", test);
        }

        public DataTable GetDataUsingDataContract()
        {
            table.TableNewRow += (s, e) => {
                Console.Out.WriteLine("WWWWWWW"); 
                test = 333;
            };
            Console.Out.WriteLine("Cool");
            return table;
        }
    }
}
