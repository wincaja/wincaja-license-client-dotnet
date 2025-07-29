' VB6 Integration Example for WincajaLicenseManager
' This module demonstrates how to integrate the .NET license manager with VB6

Option Explicit

' Late binding example - no reference needed
Public Function ValidateLicenseOnStartup() As Boolean
    On Error GoTo ErrorHandler
    
    Dim licenseManager As Object
    Dim result As String
    Dim success As Boolean
    Dim status As String
    Dim errorMsg As String
    
    ' Create the license manager COM object
    Set licenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")
    
    ' Validate the license
    result = licenseManager.ValidateLicense()
    
    ' Parse the JSON result manually (simple parsing)
    success = InStr(result, """success"":true") > 0
    
    If success Then
        ' Extract status
        status = ExtractJsonValue(result, "status")
        
        Select Case status
            Case "active"
                ValidateLicenseOnStartup = True
                
                ' Optional: Check days until expiration
                Dim daysRemaining As String
                daysRemaining = ExtractJsonValue(result, "daysUntilExpiration")
                If IsNumeric(daysRemaining) Then
                    If CLng(daysRemaining) < 30 Then
                        MsgBox "Your license will expire in " & daysRemaining & " days.", _
                               vbInformation, "License Expiring Soon"
                    End If
                End If
                
            Case "not_activated"
                ' Show activation dialog
                ShowActivationDialog
                ValidateLicenseOnStartup = False
                
            Case "expired"
                MsgBox "Your license has expired. Please contact support.", _
                       vbCritical, "License Expired"
                ValidateLicenseOnStartup = False
                
            Case Else
                errorMsg = ExtractJsonValue(result, "error")
                MsgBox "License validation failed: " & errorMsg, _
                       vbCritical, "License Error"
                ValidateLicenseOnStartup = False
        End Select
    Else
        ' Extract error message
        errorMsg = ExtractJsonValue(result, "error")
        MsgBox "License validation failed: " & errorMsg, _
               vbCritical, "License Error"
        ValidateLicenseOnStartup = False
    End If
    
    Set licenseManager = Nothing
    Exit Function
    
ErrorHandler:
    MsgBox "Failed to validate license: " & Err.Description, _
           vbCritical, "License Manager Error"
    ValidateLicenseOnStartup = False
End Function

' Show activation dialog
Private Sub ShowActivationDialog()
    Dim licenseKey As String
    Dim licenseManager As Object
    Dim result As String
    Dim success As Boolean
    
    licenseKey = InputBox("Please enter your license key:", "License Activation")
    
    If Len(Trim(licenseKey)) = 0 Then
        Exit Sub
    End If
    
    On Error Resume Next
    Set licenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")
    
    If Err.Number <> 0 Then
        MsgBox "Failed to create license manager: " & Err.Description
        Exit Sub
    End If
    
    On Error GoTo 0
    
    ' Activate the license
    result = licenseManager.ActivateLicense(licenseKey)
    
    ' Check if activation was successful
    success = InStr(result, """success"":true") > 0
    
    If success Then
        MsgBox "License activated successfully!", vbInformation, "Activation Complete"
    Else
        Dim errorMsg As String
        errorMsg = ExtractJsonValue(result, "error")
        MsgBox "Activation failed: " & errorMsg, vbCritical, "Activation Error"
    End If
    
    Set licenseManager = Nothing
End Sub

' Simple JSON value extractor (for demonstration)
' In production, use a proper JSON parser
Private Function ExtractJsonValue(json As String, key As String) As String
    Dim startPos As Long
    Dim endPos As Long
    Dim valueStart As Long
    
    ' Find the key
    startPos = InStr(json, """" & key & """")
    If startPos = 0 Then
        ExtractJsonValue = ""
        Exit Function
    End If
    
    ' Find the colon
    startPos = InStr(startPos, json, ":")
    If startPos = 0 Then
        ExtractJsonValue = ""
        Exit Function
    End If
    
    ' Skip whitespace
    startPos = startPos + 1
    Do While Mid(json, startPos, 1) = " "
        startPos = startPos + 1
    Loop
    
    ' Check if value is string or other
    If Mid(json, startPos, 1) = """" Then
        ' String value
        valueStart = startPos + 1
        endPos = InStr(valueStart, json, """")
        If endPos > valueStart Then
            ExtractJsonValue = Mid(json, valueStart, endPos - valueStart)
        End If
    Else
        ' Number, boolean, or null
        endPos = startPos
        Do While endPos <= Len(json)
            Dim ch As String
            ch = Mid(json, endPos, 1)
            If ch = "," Or ch = "}" Or ch = "]" Then
                Exit Do
            End If
            endPos = endPos + 1
        Loop
        ExtractJsonValue = Trim(Mid(json, startPos, endPos - startPos))
    End If
End Function

' Get hardware fingerprint for support
Public Function GetHardwareFingerprint() As String
    On Error GoTo ErrorHandler
    
    Dim licenseManager As Object
    Dim result As String
    
    Set licenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")
    result = licenseManager.GetHardwareFingerprint()
    
    GetHardwareFingerprint = ExtractJsonValue(result, "fingerprint")
    
    Set licenseManager = Nothing
    Exit Function
    
ErrorHandler:
    GetHardwareFingerprint = "Error: " & Err.Description
End Function

' Example usage in main form
' Private Sub Form_Load()
'     If Not ValidateLicenseOnStartup() Then
'         ' Exit application or run in limited mode
'         End
'     End If
' End Sub