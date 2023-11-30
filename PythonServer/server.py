import grpc
from concurrent import futures
from google.protobuf import struct_pb2
from my_database_pb2 import (Empty, ColumnsInfoResponse, TablesResponse, DisplayTableResponse, UpdateTableCellResponse, TableInfo)
from my_database_pb2_grpc import MyDatabaseServiceServicer, add_MyDatabaseServiceServicer_to_server

class DynamicTable:
    def __init__(self):
        self.column_info = []
        self.rows = []

    def add_column(self, column_name, column_type):
        if any(col[0] == column_name for col in self.column_info):
            raise ValueError(f'Column "{column_name}" already exists')
        self.column_info.append((column_name, column_type))
        for row in self.rows:
            row[column_name] = None

    def delete_column(self, column_name):
        if not any(col[0] == column_name for col in self.column_info):
            raise ValueError(f'Column "{column_name}" does not exist')
        index = next(i for i, col in enumerate(self.column_info) if col[0] == column_name)
        del self.column_info[index]
        for row in self.rows:
            del row[column_name]

    def add_row(self, values):
        if len(values) != len(self.column_info):
            raise ValueError("Number of values must match the number of columns")
        validated_values = []
        for (column_name, column_type), value in zip(self.column_info, values):
            if not isinstance(value, column_type):
                raise ValueError(f'Invalid type for column "{column_name}". Expected {column_type}, got {type(value)}')
            validated_values.append(value)
        row = dict(zip((col[0] for col in self.column_info), validated_values))
        self.rows.append(row)

    def add_new_row(self):
        list_values = [None] * len(self.column_info)
        self.add_row(list_values)

    def remove_row(self, row_index):
        if not (0 <= row_index < len(self.rows)):
            raise ValueError("Invalid row index")
        del self.rows[row_index]

    def remove_duplicates(self):
        seen_rows = set(tuple(row.values()) for row in self.rows)
        unique_rows = [dict(zip((col[0] for col in self.column_info), values)) for values in seen_rows]
        self.rows = unique_rows

class DatabaseService(MyDatabaseServiceServicer):
    def __init__(self):
        self.tables = {}

    def CreateDatabase(self, request, context):
        self.tables = {}
        return Empty()

    def AddTable(self, request, context):
        table_name = request.tableName
        if table_name not in self.tables:
            dynamic_table = DynamicTable()
            print(request)
            for column_info in request.columnInfo:
                dynamic_table.column_info.append((column_info.ColumnName, column_info.ColumnType))
            self.tables[table_name] = dynamic_table
            return Empty()
        else:
            context.set_code(grpc.StatusCode.ALREADY_EXISTS)
            context.set_details(f'Table "{table_name}" already exists.')
            return Empty()

    def RemoveTable(self, request, context):
        table_name = request.tableName
        if table_name in self.tables:
            del self.tables[table_name]
            return Empty()
        else:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f'Table "{table_name}" not found.')
            return Empty()

    def AddNewRow(self, request, context):
        table_name = request.tableName
        if table_name in self.tables:
            new_row = {}
            for column_name, column_type in self.tables[table_name].column_info:
                new_row[column_name] = None
            self.tables[table_name].rows.append(new_row)
            return Empty()
        else:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f'Table "{table_name}" not found.')
            return Empty()

    def AddRow(self, request, context):
        table_name = request.tableName
        if table_name in self.tables:
            new_row = {}
            for i, value in enumerate(request.values):
                column_name, column_type = self.tables[table_name].column_info[i]
                if column_type == 'System.String' or column_type == 'System.Char':
                    new_row[column_name] = value.str_value
                elif column_type == 'System.Int32':
                    new_row[column_name] = int(value.str_value)
                elif column_type == 'System.Double':
                    new_row[column_name] = float(value.str_value)
            self.tables[table_name].rows.append(new_row)
            return Empty()
        else:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f'Table "{table_name}" not found.')
            return Empty()

    def DeleteRow(self, request, context):
        table_name = request.tableName
        row_index = request.rowIndex - 1
        if table_name in self.tables and 0 <= row_index < len(self.tables[table_name].rows):
            del self.tables[table_name].rows[row_index]
            return Empty()
        else:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f'Table "{table_name}" or row index {row_index} not found.')
            return Empty()

    def AddColumn(self, request, context):
        table_name = request.tableName
        column_info = request.columnInfo
        if table_name in self.tables:
            for existing_column_name, _ in self.tables[table_name].column_info:
                if existing_column_name == column_info.ColumnName:
                    context.set_code(grpc.StatusCode.ALREADY_EXISTS)
                    context.set_details(f'Column "{column_info.ColumnName}" already exists in table "{table_name}".')
                    return Empty()

            self.tables[table_name].column_info.append((column_info.ColumnName, column_info.ColumnType))
            for row in self.tables[table_name].rows:
                row[column_info.ColumnName] = None
            return Empty()
        else:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f'Table "{table_name}" not found.')
            return Empty()

    def DeleteColumn(self, request, context):
        table_name = request.tableName
        column_name = request.columnName
        if table_name in self.tables:
            if any(existing_column_name == column_name for existing_column_name, _ in self.tables[table_name].column_info):
                self.tables[table_name].column_info = [(existing_column_name, column_type) for existing_column_name, column_type in self.tables[table_name].column_info if existing_column_name != column_name]
                for row in self.tables[table_name].rows:
                    del row[column_name]
                return Empty()
            else:
                context.set_code(grpc.StatusCode.NOT_FOUND)
                context.set_details(f'Column "{column_name}" not found in table "{table_name}".')
                return Empty()
        else:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f'Table "{table_name}" not found.')
            return Empty()

    def RemoveDuplicates(self, request, context):
        table_name = request.tableName
        if table_name in self.tables:
            seen_rows = set()
            unique_rows = []
            for row in self.tables[table_name].rows:
                values = tuple(row.values())
                if values not in seen_rows:
                    seen_rows.add(values)
                    unique_rows.append(row)

            self.tables[table_name].rows = unique_rows
            return Empty()
        else:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f'Table "{table_name}" not found.')
            return Empty()

    def GetColumnsInfo(self, request, context):
        table_name = request.tableName
        if table_name in self.tables:
            columns_info = [TableInfo(ColumnName=column_name, ColumnType=column_type) for column_name, column_type in self.tables[table_name].column_info]
            return ColumnsInfoResponse(columnsInfo=columns_info)
        else:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f'Table "{table_name}" not found.')
            return ColumnsInfoResponse()

    def GetTables(self, request, context):
        tables = [table_name for table_name in self.tables]
        print(tables)
        return TablesResponse(tables=tables)

    def DisplayTable(self, request, context):
        table_name = request.tableName
        if table_name in self.tables:
            rows = []
            for row in self.tables[table_name].rows:
                struct_values = {key: struct_pb2.Value(string_value=str(value)) if value != None else struct_pb2.Value(string_value="") for key, value in row.items()}
                rows.append(struct_pb2.Struct(fields=struct_values))
            return DisplayTableResponse(rows=rows)
        else:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f'Table "{table_name}" not found.')
            return DisplayTableResponse()

    def UpdateTableCell(self, request, context):
        table_name = request.tableName
        row_index = request.row
        col_name = request.colName
        value = request.value

        if table_name in self.tables and 0 <= row_index < len(self.tables[table_name].rows):
            col_index = None
            for i, (existing_col_name, _) in enumerate(self.tables[table_name].column_info):
                if existing_col_name == col_name:
                    col_index = i
                    break

            if col_index is not None:
                try:
                    print(self.tables[table_name].column_info[col_index][1])
                    if self.tables[table_name].column_info[col_index][1] == 'System.String':
                        self.tables[table_name].rows[row_index][col_name] = value
                    elif self.tables[table_name].column_info[col_index][1] == 'System.Int32':
                        self.tables[table_name].rows[row_index][col_name] = int(value)
                    elif self.tables[table_name].column_info[col_index][1] == 'System.Double':
                        self.tables[table_name].rows[row_index][col_name] = float(value)
                    elif self.tables[table_name].column_info[col_index][1] == 'System.Char':
                        if len(value) > 1:
                            return UpdateTableCellResponse(success=False)
                        self.tables[table_name].rows[row_index][col_name] = value
                    return UpdateTableCellResponse(success=True)
                except:
                    return UpdateTableCellResponse(success=False)
            else:
                context.set_code(grpc.StatusCode.NOT_FOUND)
                context.set_details(f'Column "{col_name}" not found in table "{table_name}".')
                return UpdateTableCellResponse(success=False)

        else:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f'Table "{table_name}" or row index {row_index} not found.')
            return UpdateTableCellResponse(success=False)

def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    add_MyDatabaseServiceServicer_to_server(DatabaseService(), server)
    server.add_insecure_port('[::]:5031')
    server.start()
    server.wait_for_termination()

if __name__ == '__main__':
    serve()
