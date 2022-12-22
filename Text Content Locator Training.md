# Training the Text Content Locator with Text Files
*(This guide is compatible with KTM, KTA, RPA, KTT and RTTI.)*

The [**Text Content Locator**](https://docshield.kofax.com/KTT/en_US/6.3.0-v15o2fs281/help/PB/ProjectBuilder/450_Extraction/TextContentLocator/c_TextContentLocator.html) is a [**Natural Language Processing**](https://en.wikipedia.org/wiki/Natural_language_processing) **(NLP)** locator for finding any tokens in a text. The other NLP locator in Kofax Transfromation is the Named Entity Locator, which is not trainable.

*   purely text based and does not use any word coordinates.
*   the only locator that ignores line-wrapping.
*   requires training from many documents. You should have many hundreds if not thousands of documents.  
*   can be trained to find any values in the text. The Text Content Locator internally [tokenizes](https://www.analyticsvidhya.com/blog/2020/05/what-is-tokenization-nlp/) the text and then extracts any values you have trained for. The Named Entity Locator looks for specific Named Entities amongst the tokens.

This guide will assume that you have an Excel file with the following format, where the exact text is in one cell and the exact values (**Amount** and **Person**) perfectly match the text. _It is VERY important that the spelling of the field values **perfectly** matches the spelling in the text, because the Text Content Locator needs to learn the context of each value. For example "600$" is directly before the word "Stone" and two words before "Rob"._

<table><tbody><tr><td>Text</td><td>Amount</td><td>Person</td></tr><tr><td>Please pay Rob Stone 600$</td><td>600$</td><td>Rob Stone</td></tr><tr><td>I want to transfer five hundred dollars to Ben Senf</td><td>five hundred dollars</td><td>Ben Senf</td></tr><tr><td>Please pay the amount of 400.30 USD to the account of Erich Kelp</td><td>400.30 USD</td><td>Erick Kelp</td></tr></tbody></table>

## Convert the Data to Text Files.

1.  Create a folder on your harddrive to put the training files.  
![](https://user-images.githubusercontent.com/47416964/123088026-88447b80-d425-11eb-8edb-73882ef6b13c.png)
1.  Enter the data into Microsoft Excel.  
![](https://user-images.githubusercontent.com/47416964/123087797-3bf93b80-d425-11eb-8108-1ca80d19d26a.png)
1.  In Microsoft Excel press **ALT-F11** to open the visual basic editor.  
![](https://user-images.githubusercontent.com/47416964/123086368-a4dfb400-d423-11eb-96f9-0cc7f5a6e867.png)
1.  Open **View/Code (F7)** to see the code window.  
![](https://user-images.githubusercontent.com/47416964/123086578-df495100-d423-11eb-9d58-a4728cd78361.png)
1.  Paste the following code. Check the starting cell "A2" and the output path "C:\\temp\\moneytransfer"  
```vba
Sub WriteTextFiles()
    Dim Cell As Range, t As String
    Set Cell = ActiveSheet.Range("e2")
    While Cell.Text <> ""
        If True Then 'Cell.Offset(0, 7).Value = True Then
            Filename = "C:\Document Transformation\Projects\TCL_test\samples\sickleave2\" + Format(Cell.Row - 1, "0000") & ".txt"
            Open Filename For Output As #1
            Print #1, Cell.Text
            Close #1
        End If
        Set Cell = Cell.Offset(1, 0)
    Wend

End Sub
```
2. Put the Cursor into the function and press F5 to run it. This will generate a text file for each row.
2. Add the following script to Kofax Transformation Document Class 

```vba
'#Language "WWB-COM"
Option Explicit

Private Sub Document_AfterExtract(ByVal pXDoc As CASCADELib.CscXDocument)
   XDocument_CreateImageFromText(pXDoc)
   XDocument_LoadTruth(pXDoc)
End Sub

Sub XDocument_LoadTruth(pXDoc As CscXDocument)
   Dim Textline As String, Values() As String, I As Long
   Dim FileId As Long, Word As CscXDocWord, FieldNames() As String, W As Long
   Dim StartWord As CscXDocWord, LastWord As CscXDocWord, F As Long, Field As CscXDocField, Path As String, ImageName As String, Image As CscImage
   Dim width As Long, height As Long
   Path=Left(pXDoc.FileName,InStrRev(pXDoc.FileName,"\"))
   FileId=CLng(Replace(Mid(pXDoc.FileName,InStrRev(pXDoc.FileName,"\")+1),".xdc",""))
   If pXDoc.Fields(0).Confidence>1.0 Then Exit Sub
   Open Path & "truth.dat" For Input As #1
   Line Input #1, Textline
   'the first line of the truth file has the field names
   FieldNames=Split(Textline,vbTab)
   'Search through truth file for the correct row
   While Not EOF(1) And I<FileId
      Line Input #1, Textline
      I=I+1
   Wend
   Close #1
   Values=Split(Textline,vbTab)
   'Loop through each field in the truth file
   For F=0 To UBound(Values)
      Set Field=pXDoc.Fields.ItemByName(FieldNames(F))
      'find the start and last word in the text matching the value
      Phrase_FindInWords(Values(F),pXDoc.Words, StartWord, LastWord)
      While Field.Words.Count>0
         Field.Words.Remove(0)
      Wend
      If Not StartWord Is Nothing Then
         'add the entire phrase to the field. Now the fields know the word id's and the Text Locator can train
         For W= StartWord.IndexOnDocument To LastWord.IndexOnDocument
            Field.Words.Append(pXDoc.Words(W))
         Next
         Field.Text=Field.Words.Text 'prevent any text reduplication
         Field.Confidence=1.00 ' it is the truth! so set the confidence to 100%
         Field.ExtractionConfident=True
      End If
   Next
End Sub

Sub Phrase_FindInWords(searchText As String ,Words As CscXDocWords,ByRef StartWord As CscXDocWord, ByRef LastWord As CscXDocWord)
   'Find a phrase in a longer text and return the first and last word of that phrase
   Dim W As Long, Start As String, C As Long, Pos As Long
   Set StartWord=Nothing
   Set LastWord=Nothing
   If searchText="" Then Exit Sub
   searchText=Trim(searchText) 'some Excel text cells end or start with space
   searchText=Replace(searchText,"(", "( ")
   searchText=Replace(searchText,")", " )")

   Pos=InStr(LCase(Replace(Words.Text,vbCrLf," ")),LCase(searchText))
   Select Case Pos
   Case Is <1
      Exit Sub 'Nothing to search for. Err.Raise(1234,,"Cannot find '" & searchText & "' in ' " & Words.Text & "'.")
   Case 1
      Start="" 'first word of text is a match
   Case Else ' match found in middle of text
      Start=Left(Replace(Words.Text,vbCrLf," "),Pos-1)
   End Select

   For C=1 To Len(Start)
      If Mid(Start,C,1)=" " Then W=W+1
   Next
   Set StartWord=Words(W)
   Set LastWord=Words(W+UBound(Split(searchText," ")))
End Sub

Sub XDocument_CreateImageFromText(pXDoc As CscXDocument)
   'Download the font you want from https://github.com/KofaxRPA/Sentiment/tree/master/font
   'along with the widths.bin file

   Const FontName="Arial"
   Const FontSize=12  'point
   Const PageWidth=210  'mm   A4
   Const PageHeight=297  'mm  A4
   Const PageBorder=20 'mm   so we don't write text to the edge of the page
   Const DPI=100    'Dots Per Inch. A normal KT scanned image needs 300 dpi B&W . This image will never need OCR, It is full color with https://en.wikipedia.org/wiki/Subpixel_rendering and so 100 dpi is completely adequate.
   Const Res=DPI/25.4   'dots per mm = dots per inch/25.4
   Const CharacterWidth = 20 'pixels . The grid size in the font images
   Const CharacterHeight = 20 'pixels height of character in the font image
   Const SpaceWidth = 5 ' pixels width for the space between words

   Dim Page As New CscImage, P As Long, W As Long, Word As CscXDocWord, Fonts() As CscImage, F As Long
   Dim x As Long, Y As Long, width As Long, Widths() As Byte, FontPath As String, C As Long, Ch As String, Unicode As Long
   Dim TL As Long, FileName As String

   'The path that stores all 64 font images and the widths file
   FontPath=Left(Project.FileName,InStrRev(Project.FileName,"\")) & "font\"
   FontPath=FontPath & "font_" & FontName & "_" & CStr(FontSize) & "_"


   'Open the font width file - this stores the pixel width of all 65536 Unicode characters in the font, so that we get proportional spacing
   FileName=FontPath & "widths.bin"
   Open FileName For Binary Access Read As #1
   ReDim Widths(LOF(1))
   Get #1,, Widths
   Close #1
   If UBound(Widths)<>65536 Then Err.Raise (574,, "Widths file is invalid. Download '" & FileName & "' from https://github.com/KofaxRPA/Sentiment/tree/master/font")
   ReDim Fonts(63) ' to store all 64 pages of Unicode Plane 0 which includes most languages including Chinese, Japanese & Korean https://en.wikipedia.org/wiki/Unicode#Code_planes_and_blocks
   ' Page 0    = Latin, Greek
   ' Page 1    = Cyrillic, Amharic, Hebrew, Arabic...
   ' Page 2-7  = Indian
   ' Page 8    = €, arrows, mathematical symbols, long dashes, fractions.
   ' Page 11.. = Chinese, Japanese, etc

   Page.CreateImage(CscImgColFormatRGB24,PageWidth*Res, PageHeight*Res,DPI,DPI) ' create a full color A4 image @ 100 DPI

   x=PageBorder*Res  'set the cursor at top left corner of page considering the page margins

   Y=PageBorder*Res
   For TL=0 To pXDoc.TextLines.Count-1  'loop through all paragraphs of the text
      For W=0 To pXDoc.TextLines(TL).Words.Count-1  'loop through all words of the paragraph
         Set Word = pXDoc.TextLines(TL).Words(W)
         Word.Text=Replace(Word.Text,vbCr,"")
         Word.Text=Replace(Word.Text,vbLf,"")
         'check that the word will still fit on this line
         width=SpaceWidth
         For C=1 To Len(Word.Text)
            Ch=Mid(Word.Text,C,1)
            Unicode=(AscW(Ch)+65536) Mod 65536 'AscW returns a number between -32768 & 32767 - we need it 0-65536. This affects Japanese characters
            width=width+Widths(Unicode)
         Next
         If x+width>=Page.Width-PageBorder*Res Then 'place the word on the next line
            x=PageBorder
            Y=Y+CharacterHeight
         End If
         Word.Left=x 'update the coordinates of the word in the XDocument
         Word.Top=Y
         Word.Height=18
         'copy each character of the word from the font images to the page
         For C=1 To Len(Word.Text)
            Ch=Mid(Word.Text,C,1)
            Unicode=(AscW(Ch)+65536) Mod 65536
            F=Unicode\32^2 ' each image contains 32*32=1024 characters. This finds the correct font page from 0-63.
            If Fonts(F) Is Nothing Then 'only load each font page as needed
               Set Fonts(F)=New CscImage
               FileName=FontPath & Format(F,"00") & ".png"
                If Not File_Exists(FileName) Then
                  Err.Raise (575,,"Font file is missing. Download '" & FileName & "' from https://github.com/KofaxRPA/Sentiment/tree/master/font")
                End If
               Fonts(F).Load(FileName)
            End If
            ' "print" the character onto the page
            Page.CopyRect(Fonts(F),(Unicode Mod 32) * CharacterWidth+3, ((Unicode Mod 32^2) \ 32 )*CharacterHeight,x,Y,Widths(Unicode),CharacterHeight) ' Each font character has 3 pixels left padding
            x=x+Widths(Unicode) 'move the cursor by the character width
         Next
         Word.Width=x-Word.Left ' update the Width of the Word In the XDocument
         x=x+SpaceWidth ' add a space between words
      Next
      x=PageBorder  'new line for each text line
      Y=Y+CharacterHeight*2 'double spacing for each new paragraph=textline
      'todo: wrap over to other pages
   Next
   Y=Y+PageBorder*Res
   'TODO: Page.cre(Page,0,0,0,0,Page.Width,Y)  'crop the bottom off the page
   Select Case pXDoc.CDoc.SourceFiles(0).FileType
   Case "TEXT"
      FileName=Left(pXDoc.CDoc.SourceFiles(0).FileName,InStrRev(pXDoc.CDoc.SourceFiles(0).FileName,".")) & "png"
      Page.Save(FileName,CscImgFileFormatPNG)
      pXDoc.ReplacePageSourceFile(FileName,"TIFF",0,0)  ' even PNG files are called 'TIFF' inside the XDoc!
   Case Else
      Page.Save(pXDoc.CDoc.SourceFiles(0).FileName,CscImgFileFormatPNG)
   End Select
   pXDoc.Representations(0).AnalyzeLines ' recalculate all the textlines as the words have changed coordinates and maybe pages
End Sub

Function File_Exists(file As String) As Boolean
      On Error GoTo ErrorHandler
      Return (GetAttr(file) And vbDirectory) = 0
      Exit Function
  ErrorHandler:
End Function
```
1. Press the **Reload Document Set** icon so that Project Builder sees that these are image files and not text files. The document icon is no longer a letter "A".
![image](https://user-images.githubusercontent.com/47416964/123135180-cb1c4880-d451-11eb-9450-c4db2514a56a.png)
3. Select your documents and **Extract (F6)**. You will see the correct values in the Extraction results with confidence=100%, a green check mark and in the document window
![image](https://user-images.githubusercontent.com/47416964/123102044-f04e8e00-d434-11eb-8970-23d1d969837f.png)  
1. If there is an error in your data, the script will crash with an error message. Correct your text file and try again.  
*In this example I had "Erich" in the text, but was looking for "Erick".*  
![image](https://user-images.githubusercontent.com/47416964/123102285-30157580-d435-11eb-8cf8-408b07a66371.png)
1. Save all of your documents (and the * will disappear from after the names)  
![image](https://user-images.githubusercontent.com/47416964/123102691-9dc1a180-d435-11eb-8080-95a288be829a.png)
1. If you open the Xdocument with the XDoc Browser you will not see the words in the field, but if you unizp the XDoc using 7zip and open it in an xml viewer you will see the word id's in the Field.  Here you can see that "Rob Stone" is words 2 & 3. These are the values that the training will be using.
![image](https://user-images.githubusercontent.com/47416964/123103908-bbdbd180-d436-11eb-9e33-a9835d0a3712.png)

##Training and Benchmarking##
You are now ready to run the first benchmark and see the zero results. After training this should be much better!
1. Right-click on **Test Set** and convert your test set to a **Benchmark Set**.  
![image](https://user-images.githubusercontent.com/47416964/123104479-445a7200-d437-11eb-994a-85ce3b66ceef.png)  
1. Open the **Extraction Benchmark** from the View Menu and press **Start**
![image](https://user-images.githubusercontent.com/47416964/123104565-5b00c900-d437-11eb-96ec-6c836007f92d.png)
1. You have perfect results because that **Document_AfterExtract** is still in the script!!  
![image](https://user-images.githubusercontent.com/47416964/123104917-a87d3600-d437-11eb-91c6-4475c766847e.png)
2. Remove the **Document_AfterExtract** from the script and re-run the Extraction Benchmark.  Here you will see that there are no results, and that the project has 100% [false negatives](https://en.wikipedia.org/wiki/False_positives_and_false_negatives) (yellow).  
![image](https://user-images.githubusercontent.com/47416964/123105088-cba7e580-d437-11eb-805f-85b4f39e15f8.png)
3. Drag all of your documents to the **Extract Set** to add them to the extraction training documents.  
![image](https://user-images.githubusercontent.com/47416964/123105407-14f83500-d438-11eb-8fc8-1b7b07caec17.png)
1. Click on **Extraction Set**, select all your documents, right-click and select **Use for Training** 
![image](https://user-images.githubusercontent.com/47416964/123435730-2c692680-d5ce-11eb-833b-df005d724b7e.png)
1. Click on **Process/Train/Extraction** on the Ribbon to train the Text Content Locator.
![image](https://user-images.githubusercontent.com/47416964/123435821-460a6e00-d5ce-11eb-8b11-ea716a1033f5.png)
1. Right-click on **Test Set** and select **Convert to Benchmark Set** to convert your test set into a benchmark set.  
![image](https://user-images.githubusercontent.com/47416964/123437533-0f355780-d5d0-11eb-8aa6-bb6c59223a77.png)
1. Run the Extraction Benchmark from Ribbon **View/Extraction Benchmark** to see the performance of the Text Content Locator.  
![image](https://user-images.githubusercontent.com/47416964/123437635-2e33e980-d5d0-11eb-8246-92d4dbcecc18.png)



