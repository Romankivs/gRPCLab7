import tkinter as tk
from tkinter import messagebox
from tkinter import simpledialog
from tksheet import Sheet
from zeep import Client
from zeep import xsd

wsdl = 'http://localhost:8000/Database/?wsdl'
client = Client(wsdl=wsdl)

# Add a table to the database
table_info = [
    {"ColumnName": "Name", "ColumnType": "System.String"},
    {"ColumnName": "Age", "ColumnType": "System.Int32"},
    {"ColumnName": "Grade", "ColumnType": "System.Double"}
]

client.service.AddTable("Students", {"TableInfo": table_info})

value = xsd.AnyObject(xsd.String(), 'Alice')
intValue = xsd.AnyObject(xsd.Int(), 42)
floatValue = xsd.AnyObject(xsd.Double(), 90.5)

# Add data to the table

strings = [value, intValue, floatValue]

client.service.AddRow("Students", {"anyType": strings})
client.service.AddRow("Students", {"anyType": strings})
client.service.AddRow("Students", {"anyType": strings})
client.service.AddRow("Students", {"anyType": strings})

class DatabaseGUI:
    def __init__(self, master):
        self.master = master
        self.master.title("Database GUI")

        wsdl = 'http://localhost:8000/Database/?wsdl'
        self.client = Client(wsdl=wsdl)

        button_frame = tk.Frame(master)
        button_frame.pack()

        self.create_db_button = tk.Button(button_frame, text="Create Database", command=self.create_database)
        self.create_db_button.pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

        self.add_table_button = tk.Button(button_frame, text="Add Table", command=self.add_table)
        self.add_table_button.pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

        self.delete_table_button = tk.Button(button_frame, text="Delete Table", command=self.delete_table)
        self.delete_table_button.pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

        self.add_row_button = tk.Button(button_frame, text="Add Row", command=self.add_row)
        self.add_row_button.pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

        self.delete_row_button = tk.Button(button_frame, text="Delete Row", command=self.delete_row)
        self.delete_row_button.pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

        self.add_column_button = tk.Button(button_frame, text="Add Column", command=self.add_column)
        self.add_column_button.pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

        self.delete_column_button = tk.Button(button_frame, text="Delete Column", command=self.delete_column)
        self.delete_column_button.pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

        self.remove_duplicates_button = tk.Button(button_frame, text="Remove duplicates", command=self.remove_duplicates)
        self.remove_duplicates_button.pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

        self.table_canvas = tk.Canvas(master, height=30)
        self.table_canvas.pack(fill=tk.X)

        self.table_frame = tk.Frame(self.table_canvas)

        self.display_tables_button = tk.Button(button_frame, text="Display Tables", command=self.display_tables)
        self.display_tables_button.pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

        self.sheet = Sheet(self.master, data=[] , header=[], width=1000, height=500)
        self.sheet.enable_bindings(("single", "edit", "edit_cell"))
        self.sheet.cell_edit_binding(enable=True)
        self.sheet.extra_bindings([("end_edit_cell", self.end_edit_cell)])
        self.sheet.pack()

        self.display_tables()

    def end_edit_cell(self, event = None):
        if event.text is not None:
            res = self.client.service.UpdateTableCell(self.selected_table, event.row, self.column_names[event.column], event.text)
            if res:
                return event.text
            messagebox.showinfo("Error", "Invalid input", parent=self.master)
            return None

    def convert_to_csharp_type(self, user_type):
        type_mapping = {
            'int': 'System.Int32',
            'real': 'System.Double',
            'str': 'System.String',
            'char': 'System.Char'
        }
        return type_mapping.get(user_type, user_type)

    def create_database(self):
        self.client.service.CreateDatabase()
        self.display_tables()
        self.sheet.headers([])
        self.sheet.set_sheet_data(data=[])
        messagebox.showinfo("Database Created", "Database has been created.", parent=self.master)

    def add_table(self):
        table_name = simpledialog.askstring("Input", "Enter table name:", parent=self.master)
        if table_name:
            self.client.service.AddTable(table_name, [])
            self.client.service.AddColumn(table_name, { "ColumnName": "Column1", "ColumnType": self.convert_to_csharp_type("str") })
            self.client.service.AddNewRow(table_name)
            self.display_tables()

    def delete_table(self):
        if self.selected_table:
            self.client.service.RemoveTable(self.selected_table)
            self.display_tables()
            if (self.tables):
                self.display_selected_table_content(self.tables[0])
            else:
                self.sheet.headers([])
                self.sheet.set_sheet_data(data=[])

    def add_row(self):
        if self.selected_table:
            self.client.service.AddNewRow(self.selected_table)
            self.refresh_selected_table_content()

    def delete_row(self):
        row_index = simpledialog.askstring("Input", "Enter row index:", parent=self.master)
        if self.selected_table and row_index:
            self.client.service.DeleteRow(self.selected_table, row_index)
            self.refresh_selected_table_content()

    def add_column(self):
        if self.selected_table:
            column_name = simpledialog.askstring("Input", "Enter column name:", parent=self.master)
            column_type = simpledialog.askstring("Input", "Enter column type (e.g., int, char, str, real):", parent=self.master)
            converted_type = self.convert_to_csharp_type(column_type)
            if column_name and converted_type:
                self.client.service.AddColumn(self.selected_table, { "ColumnName": column_name, "ColumnType": converted_type })
                self.refresh_selected_table_content()

    def delete_column(self):
        if self.selected_table:
            column_name = simpledialog.askstring("Input", "Enter column name:", parent=self.master)
            if column_name:
                self.client.service.DeleteColumn(self.selected_table, column_name)
                self.refresh_selected_table_content()

    def get_tables(self):
        tables = self.client.service.GetTables()
        self.tables = tables
        table_content = None
        if tables:
            table_content = {table: self.client.service.DisplayTable(table) for table in tables}
        return tables, table_content

    def get_columns_info(self):
        self.columns_info = self.client.service.GetColumnsInfo(self.selected_table)
        print(self.columns_info)
 
        self.column_names = []
        for info in self.columns_info:
            self.column_names.append(info["ColumnName"])

    def remove_duplicates(self):
        if self.selected_table:
            self.client.service.RemoveDuplicates(self.selected_table)
            self.refresh_selected_table_content()

    def display_tables(self):
        tables, _ = self.get_tables()

        for widget in self.table_frame.winfo_children():
            widget.destroy()

        if tables:
            for table in tables:
                tk.Button(self.table_frame, text=table, command=lambda t=table: self.display_selected_table_content(t)).pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

            self.table_frame.update_idletasks()
            self.table_frame_id = self.table_canvas.create_window((0, 0), window=self.table_frame, anchor=tk.NW)

    def display_selected_table_content(self, selected_table):
        self.selected_table = selected_table
        self.refresh_selected_table_content()

    def refresh_selected_table_content(self):
        _, table_content = self.get_tables()
        print(table_content)

        if self.selected_table in table_content:
            rows = table_content[self.selected_table]
            for i in range(len(rows)):
                rows[i] = rows[i]['anyType']

            self.get_columns_info()

            self.sheet.headers(self.column_names)
            self.sheet.set_sheet_data(rows)



if __name__ == "__main__":
    root = tk.Tk()
    app = DatabaseGUI(root)
    root.mainloop()