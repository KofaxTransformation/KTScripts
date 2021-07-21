# Export a Table to CSV
This script can be used in Project Designer to export a Table Field to a CSV file for testing, reporting and benchmarking.  
To run it just Extract the Document (F6) in Project Designer.
```vb
Private Sub Document_AfterExtract(ByVal pXDoc As CASCADELib.CscXDocument)
   'Check that we are in the Designer and not in runtime
   If Project.ScriptExecutionMode = CscScriptExecutionMode.CscScriptModeServerDesign Then
      Table_ToCSV(pXDoc.Fields.ItemByName("Table").Table, "C:\temp\table.csv")
   End If
End Sub

Public Sub Table_ToCSV(Table As CscXDocTable, FileName As String)
   Dim R As Long, Row As CscXDocTableRow, C As Long, Cell As CscXDocTableCell, Delimiter As String
   Delimiter = vbTab
   Open FileName For Output As #1
   'Print headers
   For C=0 To Table.Columns.Count-1
      Print #1, Table.Columns(C).Name & Delimiter;   ' the semicolon suppresses newline
   Next
   Print #1, 'new line
   'Print each table row
   For R=0 To Table.Rows.Count-1
      Set Row=Table.Rows(R)
      For C=0 To Row.Cells.Count-1
         Set Cell=Row.Cells(C)
         Print #1, Cell.Text & Delimiter;
      Next
      Print #1,
   Next
   Close #1
End Sub
```
