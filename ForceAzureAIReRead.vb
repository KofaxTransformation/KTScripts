Option Explicit

' Project Script

' this script forces the fullpage OCR re-read (configured in class properties) before extraction, 
' if default profile is OmniPage or Mixed Print and document has been manually reclassified
' IMPORTANT: add reference to "Tungsten Memphis Forms (4.0)" (Edit > References... in script editor)

Private Sub Document_BeforeExtract(ByVal pXDoc As CASCADELib.CscXDocument)
   On Error GoTo ErrorHdlBE
   Dim profileId As Long
   'check if document has been classified
   If pXDoc.ExtractionClass <> "" Then
      'get re-read profile for that class
      'returns -1 if no re-read set, otherwise the ID of the page profile
      profileId = Project.ClassByName(pXDoc.ExtractionClass).PageRecogProfileId
      If profileId <> -1 Then
         'check if we need to run AzureAI again. Detect AzureAI profile by checking fallback property
         'true for Omnipage or Mixed Print, false for AzureAI
         If Not Project.RecogProfiles(profileId).CanBeUsedAsFallback Then
            'OCR should be AzureAI
            'now check if OCR representation exists
            If pXDoc.Representations.Count > 0 Then
               'and is not AzureAI
               If pXDoc.Representations(0).Name <> "AzureAI" Then
                  RunPageOCR(pXDoc, profileId)
               End If
            Else
               'also run when no page OCR present
               RunPageOCR(pXDoc, profileId)
            End If
         End If
      End If
   End If
   Exit Sub
ErrorHdlBE:
   'do nothing
   Exit Sub
End Sub

Private Sub RunPageOCR(ByVal pXDoc As CASCADELib.CscXDocument, ByVal profileId As Long)
   Dim PageRecognizer As New MpsPageRecognizing
   Dim nOCRRep As Long
   Dim nPage As Long
   'reset all OCR representations
   While pXDoc.Representations.Count > 0
      pXDoc.Representations.Remove(0)
   Wend
   'iterate pages
   For nPage = 0 To pXDoc.CDoc.Pages.Count - 1
      'run OCR again for any page that is not to be suppressed
      If Not pXDoc.CDoc.Pages(nPage).SuppressOCR Then
         PageRecognizer.Recognize(pXDoc, Project.RecogProfiles.ItemByID(profileId),nPage)
      End If
   Next
End Sub
