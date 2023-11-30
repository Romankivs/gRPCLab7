import tkinter as tk
from tkinter import messagebox, simpledialog
from tksheet import Sheet
import grpc
from my_database_pb2 import AddTableRequest, AddColumnRequest, DeleteColumnRequest, AddRowRequest, DeleteRowRequest, AddNewRowRequest, RemoveTableRequest, DisplayTableRequest, RemoveDuplicatesRequest, GetColumnsInfoRequest, UpdateTableCellRequest, TableInfo, Empty
from my_database_pb2_grpc import MyDatabaseServiceStub

class DatabaseGUI:
    def __init__(self, master):
        self.master = master
        self.master.title("Database GUI")

        grpc_server_address = 'localhost:5031'
        channel = grpc.insecure_channel(grpc_server_address)
        self.stub = MyDatabaseServiceStub(channel)

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

        self.sheet = Sheet(self.master, data=[], header=[], width=1000, height=500)
        self.sheet.enable_bindings(("single", "edit", "edit_cell"))
        self.sheet.cell_edit_binding(enable=True)
        self.sheet.extra_bindings([("end_edit_cell", self.end_edit_cell)])
        self.sheet.pack()

        self.display_tables()

    def end_edit_cell(self, event=None):
        if event.text is not None:
            try:
                res = self.stub.UpdateTableCell(
                    UpdateTableCellRequest(
                        tableName=self.selected_table,
                        row=event.row,
                        colName=self.column_names[event.column],
                        value=event.text
                    )
                ).success
                if res:
                    return event.text
            except grpc.RpcError as e:
                print(f"Error: {e}")
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
        try:
            self.stub.CreateDatabase(Empty())
            self.display_tables()
            self.sheet.headers([])
            self.sheet.set_sheet_data(data=[])
            messagebox.showinfo("Database Created", "Database has been created.", parent=self.master)
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def add_table(self):
        try:
            table_name = simpledialog.askstring("Input", "Enter table name:", parent=self.master)
            if table_name:
                self.stub.AddTable(
                    AddTableRequest(
                        tableName=table_name,
                        columnInfo=[
                            TableInfo(ColumnName="Column1", ColumnType=self.convert_to_csharp_type("str")),
                        ]
                    )
                )
                self.stub.AddNewRow(AddRowRequest(tableName=table_name))
                self.display_tables()
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def delete_table(self):
        try:
            if self.selected_table:
                self.stub.RemoveTable(RemoveTableRequest(tableName=self.selected_table))
                self.display_tables()
                if self.tables:
                    self.display_selected_table_content(self.tables[0])
                else:
                    self.sheet.headers([])
                    self.sheet.set_sheet_data(data=[])
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def add_row(self):
        try:
            if self.selected_table:
                self.stub.AddNewRow(
                    AddNewRowRequest(
                        tableName=self.selected_table
                    )
                )
                self.refresh_selected_table_content()
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def delete_row(self):
        try:
            row_index = simpledialog.askstring("Input", "Enter row index:", parent=self.master)
            if self.selected_table and row_index:
                self.stub.DeleteRow(
                    DeleteRowRequest(
                        tableName=self.selected_table,
                        rowIndex=int(row_index),
                    )
                )
                self.refresh_selected_table_content()
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def add_column(self):
        try:
            if self.selected_table:
                column_name = simpledialog.askstring("Input", "Enter column name:", parent=self.master)
                column_type = simpledialog.askstring("Input", "Enter column type (e.g., int, char, str, real):", parent=self.master)
                converted_type = self.convert_to_csharp_type(column_type)
                if column_name and converted_type:
                    self.stub.AddColumn(
                        AddColumnRequest(
                            tableName=self.selected_table,
                            columnInfo=TableInfo(ColumnName=column_name, ColumnType=converted_type)
                        )
                    )
                    self.refresh_selected_table_content()
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def delete_column(self):
        try:
            if self.selected_table:
                column_name = simpledialog.askstring("Input", "Enter column name:", parent=self.master)
                if column_name:
                    self.stub.DeleteColumn(
                        DeleteColumnRequest(
                            tableName=self.selected_table,
                            columnName=column_name
                        )
                    )
                    self.refresh_selected_table_content()
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def get_tables(self):
        try:
            tables = self.stub.GetTables(Empty()).tables
            self.tables = tables
            table_content = None
            if tables:
                table_content = {table: self.stub.DisplayTable(DisplayTableRequest(tableName=table)).rows for table in tables}
            return tables, table_content
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def get_columns_info(self):
        try:
            self.columns_info = self.stub.GetColumnsInfo(GetColumnsInfoRequest(tableName=self.selected_table)).columnsInfo
            self.column_names = [info.ColumnName for info in self.columns_info]
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def remove_duplicates(self):
        try:
            if self.selected_table:
                self.stub.RemoveDuplicates(RemoveDuplicatesRequest(tableName=self.selected_table))
                self.refresh_selected_table_content()
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def display_tables(self):
        try:
            tables, _ = self.get_tables()

            for widget in self.table_frame.winfo_children():
                widget.destroy()

            if tables:
                for table in tables:
                    tk.Button(self.table_frame, text=table, command=lambda t=table: self.display_selected_table_content(t)).pack(side=tk.LEFT, padx=(5, 5), pady=(5, 5))

                self.table_frame.update_idletasks()
                self.table_frame_id = self.table_canvas.create_window((0, 0), window=self.table_frame, anchor=tk.NW)
        except grpc.RpcError as e:
            print(f"Error: {e}")

    def display_selected_table_content(self, selected_table):
        self.selected_table = selected_table
        self.refresh_selected_table_content()

    def refresh_selected_table_content(self):
        _, table_content = self.get_tables()

        if self.selected_table in table_content:
            rows = table_content[self.selected_table]

            self.get_columns_info()

            print(rows)

            data = []
            for i in range(len(rows)):
                data.append([])
                for column in self.column_names:
                    data[i].append(rows[i][column])
            print("data:", data)

            self.sheet.headers(self.column_names)
            self.sheet.set_sheet_data(data)


if __name__ == "__main__":
    root = tk.Tk()
    app = DatabaseGUI(root)
    root.mainloop()
