﻿Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Threading
Imports Microsoft.Win32
Imports System.ComponentModel

Public Class CRD_List_Item
    Dim ZeitGesamtInteger As Integer = 0
    Dim ListOfStreams As New List(Of String)
    Dim proc As Process
    Dim ThreadList As New List(Of Thread)
    Dim timeout As DateTime
    Dim Item_ErrorTolerance As Integer
    Dim Canceld As Boolean = False
    Dim Finished As Boolean = False
    Dim Label_website_Text As String = Nothing
    Dim StatusRunning As Boolean = True
    Dim ffmpeg_command As String = Nothing
    Dim Debug2 As Boolean = False
    Dim MergeSubstoMP4 As Boolean = False
    Dim SaveLog As Boolean = False
    Dim DownloadPfad As String = Nothing
    Dim ToDispose As Boolean = False
    Dim Failed As Boolean = False
    Dim FailedCount As Integer = 0
    Dim HistoryDL_URL As String
    Dim HistoryDL_Pfad As String
    Dim HistoryFilename As String
    Dim Retry As Boolean = False
    Dim HybridMode As Boolean = False
    Dim HybridModePath As String = Nothing
    Dim HybridRunning As Boolean = False
    Dim TargetReso As Integer = 1080
#Region "Remove from list"
    Public Sub DisposeItem(ByVal Dispose As Boolean)
        If Dispose = True Then
            Me.Dispose()
        End If
    End Sub
    Public Function GetToDispose() As Boolean
        Return ToDispose
    End Function
#End Region
#Region "Set UI"
    Public Sub SetLabelWebsite(ByVal Text As String)
        Label_website.Text = Text
        Label_website_Text = Text
    End Sub
    Public Sub SetTolerance(ByVal value As Integer)
        Item_ErrorTolerance = value
    End Sub
    Public Sub SetLabelAnimeTitel(ByVal Text As String)
        Label_Anime.Text = Text
    End Sub
    Public Sub SetLabelResolution(ByVal Text As String)
        Label_Reso.Text = Text
    End Sub
    Public Sub SetLabelHardsub(ByVal Text As String)
        Label_Hardsub.Text = Text
    End Sub
    Public Sub SetLabelPercent(ByVal Text As String)
        Label_percent.Text = Text
    End Sub
    Public Sub SetThumbnailImage(ByVal Thumbnail As Image)
        PB_Thumbnail.BackgroundImage = Thumbnail
    End Sub
#End Region
#Region "Get Variables"
    Public Function GetPauseStatus() As Boolean
        Return StatusRunning
    End Function
    Public Function GetIsStatusFinished() As Boolean
        If HybridRunning = True Then
            Return False
        Else
            If proc.HasExited = True Then
                Return True
            Else
                Return False
            End If
        End If

    End Function
    Public Function GetLabelPercent()
        Try
            Return Label_percent.Text
        Catch ex As Exception
            Return 0
        End Try

    End Function
    Public Function GetPercentValue()
        Try
            Return ProgressBar1.Value
        Catch ex As Exception

            Return 0
        End Try

    End Function
    Public Function GetNameAnime()
        Try
            Return Label_Anime.Text
        Catch ex As Exception
            Return "error"
        End Try

    End Function
#End Region
#Region "Set Variables"
    Public Sub Setffmpeg_command(ByVal Value As String)
        ffmpeg_command = Value
    End Sub
    Public Sub SetMergeSubstoMP4(ByVal Value As Boolean)
        MergeSubstoMP4 = Value
    End Sub
    Public Sub SetDebug2(ByVal Value As Boolean)
        Debug2 = Value
    End Sub
    Public Sub SetSaveLog(ByVal Value As Boolean)
        SaveLog = Value
    End Sub
    Public Sub SetTargetReso(ByVal Value As Integer)
        TargetReso = Value
    End Sub
#End Region
    Public Sub KillRunningTask()
        If HybridRunning = True Then
            Canceld = True
        Else
            Try
                If proc.HasExited Then
                Else
                    proc.Kill()
                    proc.WaitForExit(500)
                    Label_percent.Text = "canceled -%"
                End If
            Catch ex As Exception
            End Try
        End If
    End Sub

    Private Sub bt_del_MouseEnter(sender As Object, e As EventArgs) Handles bt_del.MouseEnter
        Dim p As PictureBox = sender
        p.BackgroundImage = My.Resources.main_del_hover
    End Sub

    Private Sub bt_del_MouseLeave(sender As Object, e As EventArgs) Handles bt_del.MouseLeave
        Dim p As PictureBox = sender
        p.BackgroundImage = My.Resources.main_del
    End Sub
    Private Sub bt_pause_MouseEnter(sender As Object, e As EventArgs) Handles bt_pause.MouseEnter
        Dim p As PictureBox = sender
        If StatusRunning = True Then
            p.BackgroundImage = My.Resources.main_pause_hover
        Else
            p.BackgroundImage = My.Resources.main_pause_play_hover
        End If
    End Sub

    Private Sub bt_pause_MouseLeave(sender As Object, e As EventArgs) Handles bt_pause.MouseLeave
        Dim p As PictureBox = sender
        If StatusRunning = True Then
            p.BackgroundImage = My.Resources.main_pause
        Else
            p.BackgroundImage = My.Resources.main_pause_play
        End If
    End Sub

    Private Sub bt_pause_Click(sender As Object, e As EventArgs) Handles bt_pause.Click
        If HybridRunning = True Then
            If StatusRunning = True Then
                StatusRunning = False
                bt_pause.BackgroundImage = My.Resources.main_pause_play

            Else
                StatusRunning = True
                bt_pause.BackgroundImage = My.Resources.main_pause
            End If
        Else
            If proc.HasExited = True Then
                If ProgressBar1.Value < 100 Then
                    If Retry = True Then
                        If Main.RunningDownloads < Main.MaxDL Then

                        Else
                            If MessageBox.Show("You have currtenly on your set Download limit." + vbNewLine + " You can Press OK to ignore it.", "Download maximum reached", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
                                Exit Sub
                            End If
                        End If
                        If My.Computer.FileSystem.FileExists(HistoryDL_Pfad.Replace(Chr(34), "")) Then 'Pfad = Kompeltter Pfad mit Dateinamen + ENdung
                            Try
                                My.Computer.FileSystem.DeleteFile(HistoryDL_Pfad.Replace(Chr(34), ""))
                            Catch ex As Exception
                            End Try
                        End If
                        StartDownload(HistoryDL_URL, HistoryDL_Pfad, HistoryFilename, HybridMode)
                        StatusRunning = True
                        Label_website.Text = Label_website_Text
                    Else
                        MsgBox("The download process seems to have crashed", MsgBoxStyle.Exclamation)
                        Label_percent.Text = "Press the play button again to retry."
                        ProgressBar1.Value = 0
                        Retry = True
                        StatusRunning = False
                    End If

                Else
                End If
                Exit Sub
            End If
            If StatusRunning = True Then
                StatusRunning = False
                bt_pause.BackgroundImage = My.Resources.main_pause_play
                SuspendProcess(proc)
            Else
                If Failed = True Then
                    Dim Result As DialogResult = MessageBox.Show("The download has " + FailedCount.ToString + " failded segments" + vbNewLine + "Press 'Ignore' to continue", "Download Error", MessageBoxButtons.AbortRetryIgnore) '= DialogResult.Ignore Then

                    If Result = DialogResult.Ignore Then
                        Failed = False
                        StatusRunning = True
                        bt_pause.BackgroundImage = My.Resources.main_pause
                        ResumeProcess(proc)
                    ElseIf Result = DialogResult.Retry Then
                        Try
                            proc.Kill()
                            proc.WaitForExit(500)
                            Label_percent.Text = "retrying -%"
                            Label_website.Text = Label_website_Text
                        Catch ex As Exception
                        End Try

                        If proc.HasExited Then
                            StartDownload(HistoryDL_URL, HistoryDL_Pfad, HistoryFilename, HybridMode)
                            StatusRunning = True
                            Label_website.Text = Label_website_Text
                            bt_pause.BackgroundImage = My.Resources.main_pause
                        End If
                    ElseIf Result = DialogResult.Abort Then
                        Try
                            proc.Kill()
                            proc.WaitForExit(500)
                            Label_percent.Text = "canceled -%"
                            Label_website.Text = Label_website_Text
                        Catch ex As Exception
                        End Try
                    End If
                Else
                    If StatusRunning = True Then
                        StatusRunning = False
                        bt_pause.BackgroundImage = My.Resources.main_pause_play
                        SuspendProcess(proc)
                    Else
                        StatusRunning = True
                        bt_pause.BackgroundImage = My.Resources.main_pause
                        ResumeProcess(proc)
                    End If
                End If

            End If
        End If

    End Sub
    Public Sub SetToolTip(ByVal Text As String)
        ToolTip1.SetToolTip(Me, Text)
    End Sub
    Private Sub Item_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.ContextMenuStrip = ContextMenuStrip1 '.ContextMenu
        Dim locationY As Integer = 0
        bt_del.SetBounds(775, locationY + 10, 35, 29)
        bt_pause.SetBounds(740, locationY + 15, 25, 20)
        PB_Thumbnail.SetBounds(11, 20, 168, 95)
        PB_Thumbnail.BringToFront()
        Label_website.Location = New Point(195, locationY + 15)
        Label_Anime.Location = New Point(195, locationY + 42)
        Label_Reso.Location = New Point(195, locationY + 101)
        Label_Hardsub.Location = New Point(300, locationY + 101)
        Label_percent.SetBounds(432, locationY + 101, 378, 19)
        Label_percent.AutoSize = False
        ProgressBar1.SetBounds(195, locationY + 70, 601, 20)
        PictureBox5.Location = New Point(0, 136)
        PictureBox5.Height = 6
    End Sub

    Public Function GetTextBound()
        Return Label_website.Location.Y
    End Function


#Region "Download + Update UI"

    Public Sub StartDownload(ByVal DL_URL As String, ByVal DL_Pfad As String, ByVal Filename As String, ByVal DownloadHybridMode As Boolean)
        'MsgBox(DL_URL)
        DownloadPfad = DL_Pfad
        HistoryDL_URL = DL_URL
        HistoryDL_Pfad = DL_Pfad
        HistoryFilename = Filename

        If DownloadHybridMode = True Then
            Dim Evaluator = New Thread(Sub() DownloadHybrid(DL_URL, DL_Pfad, Filename))
            Evaluator.Start()
            HybridMode = True
            HybridRunning = True
        Else
            DownloadFFMPEG(DL_URL, DL_Pfad, Filename)
        End If

    End Sub

#Region "Download Cache"
    Private Sub tsDownloadAsync(ByVal DL_URL As String, ByVal DL_Pfad As String)
        Try
            Dim wc_ts As New WebClient
            wc_ts.DownloadFile(New Uri(DL_URL), DL_Pfad)
        Catch ex As Exception
            Try
                Dim wc_ts As New WebClient
                wc_ts.DownloadFile(New Uri(DL_URL), DL_Pfad)
            Catch ex2 As Exception
                Debug.WriteLine("Download error #2: " + DL_Pfad + vbNewLine + ex.ToString + vbNewLine + DL_URL)
            End Try
            Debug.WriteLine("Download error #1: " + DL_Pfad)
        End Try

    End Sub
    Private Function tsStatusAsync(ByVal prozent As Integer, ByVal di As IO.DirectoryInfo, ByVal Filename As String, ByVal pausetime As Integer)
        Dim Now As Date = Date.Now

        Dim FinishedSize As Double = 0
        Dim AproxFinalSize As Double = 0

        Try

            Dim aryFi As IO.FileInfo() = di.GetFiles("*.ts")
            Dim fi As IO.FileInfo
            For Each fi In aryFi
                FinishedSize = FinishedSize + Math.Round(fi.Length / 1048576, 2, MidpointRounding.AwayFromZero).ToString()
            Next
        Catch ex As Exception
        End Try
        'Thread.Sleep(1000)
        'Pause(1)

        If prozent > 0 Then
            AproxFinalSize = Math.Round(FinishedSize * 100 / prozent, 2, MidpointRounding.AwayFromZero).ToString() ' Math.Round( / 1048576, 2, MidpointRounding.AwayFromZero).ToString()
        End If
        Dim duration As TimeSpan = Date.Now - di.CreationTime
        Dim TimeinSeconds As Integer = duration.Hours * 3600 + duration.Minutes * 60 + duration.Seconds
        TimeinSeconds = TimeinSeconds - pausetime
        Dim DataRate As Double = FinishedSize / TimeinSeconds
        Dim DataRateString As String = Math.Round(DataRate, 2, MidpointRounding.AwayFromZero).ToString()
        If prozent > 100 Then
            prozent = 100
        ElseIf prozent < 0 Then
            prozent = 0
        End If
        Me.Invoke(New Action(Function()
                                 ProgressBar1.Value = prozent
                                 Label_percent.Text = DataRateString + "MB\s " + Math.Round(FinishedSize, 2, MidpointRounding.AwayFromZero).ToString + "MB/" + Math.Round(AproxFinalSize, 2, MidpointRounding.AwayFromZero).ToString + "MB " + prozent.ToString + "%"
                                 Return Nothing
                             End Function))
        'RaiseEvent UpdateUI(Filename, prozent, FinishedSize, AproxFinalSize, Color.FromArgb(247, 140, 37), DataRateString + "MB\s")

        Return Nothing

    End Function

    Public Function DownloadHybrid(ByVal DL_URL As String, ByVal DL_Pfad As String, ByVal Filename As String) As String
        'MsgBox(DL_URL)
        Dim Folder As String = einstellungen.GeräteID()
        Dim Pfad2 As String = Path.GetDirectoryName(DL_Pfad.Replace(Chr(34), "")) + "\" + Folder + "\"
        If Not Directory.Exists(Path.GetDirectoryName(Pfad2)) Then
            ' Nein! Jetzt erstellen...
            Try
                Directory.CreateDirectory(Path.GetDirectoryName(Pfad2))
            Catch ex As Exception
                MsgBox("Temp folder creation failed")
                Return Nothing
                Exit Function
                ' Ordner wurde nich erstellt
                'Pfad2 = Pfad + "\" + CR_FilenName_Backup + ".mp4"
            End Try
        End If
        Dim MergeSub As String() = DL_URL.Split(New String() {"-i " + Chr(34)}, System.StringSplitOptions.RemoveEmptyEntries)
        If MergeSub.Count > 1 Then
            Me.Invoke(New Action(Function()
                                     Label_percent.Text = "Downloading Subtitles..."
                                     Return Nothing
                                 End Function))

            For i As Integer = 1 To MergeSub.Count - 1
                    Dim SubsURL As String() = MergeSub(i).Split(New [Char]() {Chr(34)})
                    Dim SubsClient As New WebClient
                    SubsClient.Encoding = Encoding.UTF8
                    If Main.WebbrowserCookie = Nothing Then
                    Else
                        SubsClient.Headers.Add(HttpRequestHeader.Cookie, Main.WebbrowserCookie)
                    End If
                    Dim SubsFile As String = einstellungen.GeräteID() + ".txt"

                    Dim retry As Boolean = True
                    Dim retryCount As Integer = 3
                    While retry
                        Try
                            SubsClient.DownloadFile(SubsURL(0), Pfad2 + "\" + SubsFile)
                            retry = False
                        Catch ex As Exception
                            If retryCount > 0 Then
                                retryCount = retryCount - 1
                                Me.Invoke(New Action(Function()
                                                         Label_percent.Text = "Error Downloading Subtitles - retrying"
                                                         Return Nothing
                                                     End Function))

                            Else
                                Dim utf8WithoutBom2 As New System.Text.UTF8Encoding(False)
                                Using sink As New StreamWriter(SubsFile, False, utf8WithoutBom2)
                                    sink.WriteLine(My.Resources.ass_template)
                                End Using
                                retry = False
                            End If
                        End Try
                    End While
                    DL_URL = DL_URL.Replace(SubsURL(0), Pfad2 + "\" + SubsFile)
                Next

        End If
        Dim m3u8_url As String() = DL_URL.Split(New [Char]() {Chr(34)})
        Dim m3u8_url_1 As String = Nothing
        Dim m3u8_url_3 As String = m3u8_url(1)
        If Debug2 = True Then
            MsgBox(m3u8_url(1) + vbNewLine + DL_Pfad + vbNewLine + Filename)
        End If
        Dim client0 As New WebClient
        client0.Encoding = Encoding.UTF8
        Dim text As String = client0.DownloadString(m3u8_url(1))
        If InStr(text, "RESOLUTION=") Then 'master m3u8 no fragments 
            Dim new_m3u8_2() As String = text.Split(New String() {vbLf}, System.StringSplitOptions.RemoveEmptyEntries)
            If TargetReso = 42 Then
                TargetReso = 1080
            End If

            For i As Integer = 0 To new_m3u8_2.Count - 1
                    'MsgBox("x" + Main.Resu.ToString)
                    If CBool(InStr(new_m3u8_2(i), "x" + TargetReso.ToString)) = True Then
                        m3u8_url_1 = new_m3u8_2(i + 1)
                        Exit For
                    End If
                Next
                If InStr(m3u8_url_1, "https://") Then
                    text = client0.DownloadString(m3u8_url_1)
                Else
                    Dim c() As String = New Uri(m3u8_url_3).Segments
                    Dim path As String = "https://" + New Uri(m3u8_url_3).Host
                    For i3 As Integer = 0 To c.Count - 2
                        path = path + c(i3)
                    Next
                    m3u8_url_3 = path + m3u8_url_1
                    'MsgBox(m3u8_url_1)
                    text = client0.DownloadString(m3u8_url_3)
                End If



            End If
        Dim LoadedKeys As New List(Of String)
        LoadedKeys.Add("Nothing")
        Dim KeyFileCache As String = Nothing
        Dim textLenght() As String = text.Split(New String() {vbLf}, System.StringSplitOptions.RemoveEmptyEntries)
        Dim Fragments() As String = text.Split(New String() {".ts"}, System.StringSplitOptions.RemoveEmptyEntries)
        Dim FragmentsInt As Integer = Fragments.Count - 2
        Dim nummerint As Integer = 0 '-1
        Dim m3u8FFmpeg As String = Nothing
        Dim ts_dl As String = Nothing

        HybridModePath = Pfad2
        'MsgBox(HybridModePath)
        If Debug2 = True Then
            MsgBox(Pfad2)
        End If
        Dim PauseTime As Integer = 0
        Dim Threads As Integer = Environment.ProcessorCount / 2 - 1
        If Threads < 2 Then
            Threads = 2
        End If
        Dim di As New IO.DirectoryInfo(Pfad2)
        For i As Integer = 0 To textLenght.Length - 1
            If InStr(textLenght(i), ".ts") Then
                For w As Integer = 0 To Integer.MaxValue

                    If StatusRunning = False Then
                        'MsgBox(True.ToString)
                        Thread.Sleep(5000)
                        PauseTime = PauseTime + 5
                    ElseIf ThreadList.Count > Threads Then
                        Thread.Sleep(125)
                    ElseIf Canceld = True Then
                        For www As Integer = 0 To Integer.MaxValue
                            If ThreadList.Count > 0 Then
                                Thread.Sleep(250)
                            Else
                                Try
                                    System.IO.Directory.Delete(HybridModePath, True)
                                Catch ex As Exception
                                End Try
                                Exit For
                            End If
                        Next
                        Return Nothing
                        Exit Function
                    Else

                        Exit For
                    End If
                Next

                nummerint = nummerint + 1
                Dim nummer4D As String = String.Format("{0:0000}", nummerint)
                Dim curi As String = textLenght(i)
                If InStr(curi, "https://") Then
                ElseIf InStr(curi, "../") Then
                    Dim countDot() As String = curi.Split(New String() {"./"}, System.StringSplitOptions.RemoveEmptyEntries)

                    Dim c() As String = New Uri(m3u8_url_3).Segments
                    Dim path As String = "https://" + New Uri(m3u8_url_3).Host
                    For i3 As Integer = 0 To c.Count - (2 + countDot.Count - 1)
                        path = path + c(i3)
                    Next
                    curi = path + countDot(countDot.Count - 1)
                Else
                    Dim c() As String = New Uri(m3u8_url_3).Segments
                    Dim path As String = "https://" + New Uri(m3u8_url_3).Host
                    For i3 As Integer = 0 To c.Count - 2
                        path = path + c(i3)
                    Next
                    curi = path + textLenght(i)
                End If

                Dim Evaluator = New Thread(Sub() Me.tsDownloadAsync(curi, Pfad2 + nummer4D + ".ts"))
                Evaluator.Start()
                ThreadList.Add(Evaluator)
                m3u8FFmpeg = m3u8FFmpeg + Pfad2 + nummer4D + ".ts" + vbLf
                Dim FragmentsFinised = (ThreadList.Count + nummerint) / FragmentsInt * 100
                tsStatusAsync(FragmentsFinised, di, Filename, PauseTime)


            ElseIf textLenght(i) = "#EXT-X-PLAYLIST-TYPE:VOD" Then
            ElseIf InStr(textLenght(i), "URI=" + Chr(34)) Then
                Dim KeyLine As String = textLenght(i)
                If InStr(KeyLine, "https://") Then

                    Dim KeyFile() As String = KeyLine.Split(New String() {"URI=" + Chr(34)}, System.StringSplitOptions.RemoveEmptyEntries)
                    Dim KeyFile2() As String = KeyFile(1).Split(New String() {Chr(34)}, System.StringSplitOptions.RemoveEmptyEntries)
                    If LoadedKeys.Item(LoadedKeys.Count - 1) = KeyFile2(0) Then
                    Else
                        Dim KeyClient As New WebClient
                        KeyClient.Encoding = Encoding.UTF8
                        If Main.WebbrowserCookie = Nothing Then
                        Else
                            KeyClient.Headers.Add(HttpRequestHeader.Cookie, Main.WebbrowserCookie)
                        End If
                        Dim KeyFile3 As String = einstellungen.GeräteID() + ".key"
                        KeyFileCache = KeyFile3

                        Dim retry As Boolean = True
                        Dim retryCount As Integer = 3

                        Try
                            KeyClient.DownloadFile(KeyFile2(0), Application.StartupPath + "\" + KeyFile3)
                            Retry = False
                        Catch ex As Exception
                            If retryCount > 0 Then
                                retryCount = retryCount - 1
                                Me.Invoke(New Action(Function()
                                                         Label_percent.Text = "Access Error - retrying"
                                                         Return Nothing
                                                     End Function))

                            Else
                                Me.Invoke(New Action(Function()
                                                         Label_percent.Text = "Access Error - download canceled"
                                                         Return Nothing
                                                     End Function))
                                                         Return Nothing
                                                         Exit Function
                                'Dim utf8WithoutBom2 As New System.Text.UTF8Encoding(False)
                                'Using sink As New StreamWriter(SubsFile, False, utf8WithoutBom2)
                                '    sink.WriteLine(My.Resources.ass_template)
                                'End Using
                                'Retry = False
                            End If
                        End Try

                        LoadedKeys.Add(KeyFile2(0))
                    End If
                    If KeyFile2.Count > 1 Then
                        KeyLine = KeyFile(0) + "URI=" + Chr(34) + KeyFileCache + Chr(34) + KeyFile2(1)
                    Else
                        KeyLine = KeyFile(0) + "URI=" + Chr(34) + KeyFileCache + Chr(34)
                    End If
                    'ElseIf InStr(KeyLine, "../") Then
                    '    Dim countDot() As String = KeyLine.Split(New String() {"./"}, System.StringSplitOptions.RemoveEmptyEntries)

                    '    Dim c() As String = New Uri(m3u8_url_3).Segments
                    '    Dim path As String = "https://" + New Uri(m3u8_url_3).Host
                    '    For i3 As Integer = 0 To c.Count - (2 + countDot.Count - 1)
                    '        path = path + c(i3)
                    '    Next
                    '    KeyLine = path + countDot(countDot.Count - 1)

                Else
                    Dim c() As String = New Uri(m3u8_url_3).Segments
                    Dim path As String = "https://" + New Uri(m3u8_url_3).Host
                    For i3 As Integer = 0 To c.Count - 2
                        path = path + c(i3)
                    Next
                    KeyLine = KeyLine.Replace("URI=" + Chr(34), "URI=" + Chr(34) + path) 'path + textLenght(i)
                    Dim KeyFile() As String = KeyLine.Split(New String() {"URI=" + Chr(34)}, System.StringSplitOptions.RemoveEmptyEntries)
                    Dim KeyFile2() As String = KeyFile(1).Split(New String() {Chr(34)}, System.StringSplitOptions.RemoveEmptyEntries)
                    If LoadedKeys.Item(LoadedKeys.Count - 1) = KeyFile2(0) Then
                    Else
                        Dim KeyClient As New WebClient
                        KeyClient.Encoding = Encoding.UTF8
                        If Main.WebbrowserCookie = Nothing Then
                        Else
                            KeyClient.Headers.Add(HttpRequestHeader.Cookie, Main.WebbrowserCookie)
                        End If
                        Dim KeyFile3 As String = einstellungen.GeräteID() + ".key"
                        KeyFileCache = KeyFile3

                        Dim retry As Boolean = True
                        Dim retryCount As Integer = 3

                        Try
                            KeyClient.DownloadFile(KeyFile2(0), Application.StartupPath + "\" + KeyFile3)
                            Retry = False
                        Catch ex As Exception
                            If retryCount > 0 Then
                                retryCount = retryCount - 1
                                Me.Invoke(New Action(Function()
                                                         Label_percent.Text = "Access Error - retrying"
                                                         Return Nothing
                                                     End Function))

                            Else
                                Me.Invoke(New Action(Function()
                                                         Label_percent.Text = "Access Error - download canceled"
                                                         Return Nothing
                                                     End Function))
                                Return Nothing
                                Exit Function
                                'Dim utf8WithoutBom2 As New System.Text.UTF8Encoding(False)
                                'Using sink As New StreamWriter(SubsFile, False, utf8WithoutBom2)
                                '    sink.WriteLine(My.Resources.ass_template)
                                'End Using
                                'Retry = False
                            End If
                        End Try
                        'KeyClient.DownloadFile(KeyFile2(0), Application.StartupPath + "\" + KeyFile3)
                        LoadedKeys.Add(KeyFile2(0))
                    End If
                    If KeyFile2.Count > 1 Then
                        KeyLine = KeyFile(0) + "URI=" + Chr(34) + KeyFileCache + Chr(34) + KeyFile2(1)
                    Else
                        KeyLine = KeyFile(0) + "URI=" + Chr(34) + KeyFileCache + Chr(34)
                    End If
                End If
                m3u8FFmpeg = m3u8FFmpeg + KeyLine + vbLf
            Else
                m3u8FFmpeg = m3u8FFmpeg + textLenght(i) + vbLf
            End If
        Next
        Dim utf8WithoutBom As New System.Text.UTF8Encoding(False)
        Using sink As New StreamWriter(Pfad2 + "\index" + Folder + ".m3u8", False, utf8WithoutBom)
            sink.WriteLine(m3u8FFmpeg)
        End Using
        For w As Integer = 0 To Integer.MaxValue
            If ThreadList.Count > 0 Then
                Thread.Sleep(250)
            Else
                Thread.Sleep(250)
                Exit For
            End If
        Next
        tsStatusAsync(100, di, Filename, PauseTime)

        DL_URL = DL_URL.Replace(m3u8_url(1), Pfad2 + "index" + Folder + ".m3u8")


        'MsgBox(DL_URL)
        Dim exepath As String = Application.StartupPath + "\ffmpeg.exe"
        Dim startinfo As New System.Diagnostics.ProcessStartInfo

        Dim cmd As String = "-allowed_extensions ALL " + DL_URL + " " + DL_Pfad '+ " " + ffmpeg_command + " " + DL_Pfad 'start ffmpeg with command strFFCMD string
        ' MsgBox(cmd) -headers " + Chr(34) + "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0" + Chr(34) + 
        If Debug2 = True Then
            MsgBox(cmd)
        End If


        'all parameters required to run the process
        startinfo.FileName = exepath
        startinfo.Arguments = cmd
        startinfo.UseShellExecute = False
        startinfo.WindowStyle = ProcessWindowStyle.Normal
        startinfo.RedirectStandardError = True
        startinfo.RedirectStandardInput = True
        startinfo.RedirectStandardOutput = True
        startinfo.CreateNoWindow = True
        proc = New Process
        proc.EnableRaisingEvents = True
        AddHandler proc.ErrorDataReceived, AddressOf ffmpegOutput
        AddHandler proc.OutputDataReceived, AddressOf ffmpegOutput
        AddHandler proc.Exited, AddressOf ProcessClosed
        proc.StartInfo = startinfo
        proc.Start() ' start the process
        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()
        HybridRunning = False
        Return Nothing
    End Function



#End Region



    Public Function DownloadFFMPEG(ByVal DLCommand As String, ByVal DL_Pfad As String, ByVal Filename As String) As String


        Dim exepath As String = Application.StartupPath + "\ffmpeg.exe"
        Dim startinfo As New System.Diagnostics.ProcessStartInfo
        Dim cmd As String = "-headers " + Chr(34) + "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0" + Chr(34) + " " + DLCommand + " " + DL_Pfad 'start ffmpeg with command strFFCMD string

        If Debug2 = True Then
            MsgBox(cmd)
        End If
        'all parameters required to run the process
        startinfo.FileName = exepath
        startinfo.Arguments = cmd
        startinfo.UseShellExecute = False
        startinfo.WindowStyle = ProcessWindowStyle.Normal
        startinfo.RedirectStandardError = True
        startinfo.RedirectStandardInput = True
        startinfo.RedirectStandardOutput = True
        startinfo.CreateNoWindow = True
        proc = New Process
        proc.EnableRaisingEvents = True
        AddHandler proc.ErrorDataReceived, AddressOf ffmpegOutput
        AddHandler proc.OutputDataReceived, AddressOf ffmpegOutput
        AddHandler proc.Exited, AddressOf ProcessClosed
        proc.StartInfo = startinfo
        proc.Start() ' start the process
        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()
        Return Nothing
    End Function

    Sub ProcessClosed(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Pause(5)
            If Finished = False Then
                If Canceld = False Then
                    Label_website.Text = "The download process seems to have crashed"
                    Label_percent.Text = "Press the play button again to retry."
                    ProgressBar1.Value = 100
                    Retry = True
                    StatusRunning = False
                End If
            End If
        Catch ex As Exception

        End Try
        'Me.Invoke(New Action(Function()
        '                         Label_percent.Text = "Finished - event"
        '                         Return Nothing
        '                     End Function))
    End Sub

    Sub ffmpegOutput(ByVal sender As Object, ByVal e As DataReceivedEventArgs)
        'timeout = DateTime.Now
        'MsgBox(timeout)
        Try
            Dim logfile As String = DownloadPfad.Replace(".mp4", ".log").Replace(Chr(34), "")
            If SaveLog = True Then
                If File.Exists(logfile) Then
                    Using sw As StreamWriter = File.AppendText(logfile)
                        sw.Write(vbNewLine)
                        sw.Write(Date.Now + e.Data)
                    End Using
                Else
                    File.WriteAllText(logfile, Date.Now + " " + e.Data)
                End If
            End If
        Catch ex As Exception
        End Try

#Region "Detect Auto resolution"
        If MergeSubstoMP4 = False Then
            If CBool(InStr(e.Data, "Stream #")) And CBool(InStr(e.Data, "Video")) = True Then
                'MsgBox(True.ToString + vbNewLine + e.Data)
                'MsgBox(InStr(e.Data, "Stream #").ToString + vbNewLine + InStr(e.Data, "Video").ToString)

                'MsgBox("with CBool" + vbNewLine + CBool(InStr(e.Data, "Stream #")).ToString + vbNewLine + CBool(InStr(e.Data, "Video")).ToString)

                ListOfStreams.Add(e.Data)
            End If
            If InStr(e.Data, "Stream #") And InStr(e.Data, " -> ") Then
                'UsesStreams.Add(e.Data)
                'MsgBox(e.Data)
                Dim StreamSearch() As String = e.Data.Split(New String() {" -> "}, System.StringSplitOptions.RemoveEmptyEntries)
                Dim StreamSearch2 As String = StreamSearch(0) + ":"
                For i As Integer = 0 To ListOfStreams.Count - 1
                    If CBool(InStr(ListOfStreams(i), StreamSearch2)) Then 'And CBool(InStr(ListOfStreams(i), " Video:")) Then
                        'MsgBox(ListOfStreams(i))
                        Dim ResoSearch() As String = ListOfStreams(i).Split(New String() {"x"}, System.StringSplitOptions.RemoveEmptyEntries)
                        'MsgBox(ResoSearch(1))
                        If CBool(InStr(ResoSearch(2), " [")) = True Then
                            Dim ResoSearch2() As String = ResoSearch(2).Split(New String() {" ["}, System.StringSplitOptions.RemoveEmptyEntries)
                            Me.Invoke(New Action(Function()
                                                     Label_Reso.Text = ResoSearch2(0) + "p"
                                                     Return Nothing
                                                 End Function))
                        End If
                    End If
                Next
            End If
        End If
#End Region

        If InStr(e.Data, "Duration: N/A, bitrate: N/A") Then

        ElseIf InStr(e.Data, "Duration: ") Then
            Dim ZeitGesamt As String() = e.Data.Split(New String() {"Duration: "}, System.StringSplitOptions.RemoveEmptyEntries)
            Dim ZeitGesamt2 As String() = ZeitGesamt(1).Split(New [Char]() {System.Convert.ToChar(".")})
            Dim ZeitGesamtSplit() As String = ZeitGesamt2(0).Split(New [Char]() {System.Convert.ToChar(":")})
            'MsgBox(ZeitGesamt2(0))
            ZeitGesamtInteger = CInt(ZeitGesamtSplit(0)) * 3600 + CInt(ZeitGesamtSplit(1)) * 60 + CInt(ZeitGesamtSplit(2))



        ElseIf InStr(e.Data, " time=") Then
            'MsgBox(e.Data)
            Dim ZeitFertig As String() = e.Data.Split(New String() {" time="}, System.StringSplitOptions.RemoveEmptyEntries)
            Dim ZeitFertig2 As String() = ZeitFertig(1).Split(New [Char]() {System.Convert.ToChar(".")})
            Dim ZeitFertigSplit() As String = ZeitFertig2(0).Split(New [Char]() {System.Convert.ToChar(":")})
            Dim ZeitFertigInteger As Integer = CInt(ZeitFertigSplit(0)) * 3600 + CInt(ZeitFertigSplit(1)) * 60 + CInt(ZeitFertigSplit(2))
            Dim bitrate3 As String = 0
            If InStr(e.Data, "bitrate=") Then
                Dim bitrate As String() = e.Data.Split(New String() {"bitrate="}, System.StringSplitOptions.RemoveEmptyEntries)
                Dim bitrate2 As String() = bitrate(1).Split(New String() {"kbits/s"}, System.StringSplitOptions.RemoveEmptyEntries)

                If InStr(bitrate2(0), ".") Then
                    Dim bitrateTemo As String() = bitrate2(0).Split(New String() {"."}, System.StringSplitOptions.RemoveEmptyEntries)
                    bitrate3 = bitrateTemo(0)
                ElseIf InStr(bitrate2(0), ",") Then
                    Dim bitrateTemo As String() = bitrate2(0).Split(New String() {","}, System.StringSplitOptions.RemoveEmptyEntries)
                    bitrate3 = bitrateTemo(0)
                End If
            End If
            Dim bitrateInt As Double = CInt(bitrate3) / 1024
            Dim FileSize As Double = ZeitGesamtInteger * bitrateInt / 8
            Dim DownloadFinished As Double = ZeitFertigInteger * bitrateInt / 8
            Dim percent As Integer = ZeitFertigInteger / ZeitGesamtInteger * 100
            Me.Invoke(New Action(Function()
                                     If percent > 100 Then
                                         percent = 100
                                     End If
                                     ProgressBar1.Value = percent
                                     Label_percent.Text = Math.Round(DownloadFinished, 2, MidpointRounding.AwayFromZero).ToString + "MB/" + Math.Round(FileSize, 2, MidpointRounding.AwayFromZero).ToString + "MB " + percent.ToString + "%"
                                     Return Nothing
                                 End Function))
        ElseIf InStr(e.Data, "Failed to open segment") Then
            FailedCount = FailedCount + 1
            If Item_ErrorTolerance = 0 Then

            ElseIf FailedCount >= Item_ErrorTolerance Then
                Failed = True
                StatusRunning = False
                bt_pause.BackgroundImage = My.Resources.main_pause_play
                SuspendProcess(proc)
                Me.Invoke(New Action(Function()

                                         Label_percent.Text = "Missing segment detected, retry or resume with the play button"
                                         Return Nothing
                                     End Function))
            End If

        ElseIf InStr(e.Data, "muxing overhead:") Then
            Finished = True
            Me.Invoke(New Action(Function()
                                     Dim Done As String() = Label_percent.Text.Split(New String() {"MB"}, System.StringSplitOptions.RemoveEmptyEntries)
                                     Label_percent.Text = "Finished - " + Done(0) + "MB"
                                     Return Nothing
                                 End Function))
            If HybridMode = True Then
                Thread.Sleep(5000)
                Try
                    System.IO.Directory.Delete(HybridModePath, True)
                Catch ex As Exception
                End Try
            End If
        End If


    End Sub

#Region "Manga DL"


    Public Sub DownloadMangaPages(ByVal Pfad As String, ByVal BaseURL As String, ByVal SiteList As List(Of String), ByVal FolderName As String)

        Dim Pfad_DL As String = Pfad + "\" + FolderName
        If Debug2 = True Then
            MsgBox(BaseURL + SiteList(0))
        End If


        Try
            Directory.CreateDirectory(Pfad_DL)
            'MsgBox(True.ToString)
        Catch ex As Exception
        End Try

        For i As Integer = 0 To SiteList.Count - 1
            'MsgBox(BaseURL + SiteList(i) + vbNewLine + Pfad_DL + "\" + SiteList(i))
            Dim iWert As Integer = i
            Using client As New WebClient()
                client.Headers.Add("User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0")
                client.DownloadFile(BaseURL + SiteList(i), Pfad_DL + "\" + SiteList(i))
                Pause(1)
            End Using
            Me.Invoke(New Action(Function()
                                     iWert = iWert + 1
                                     Dim Prozent As Integer = iWert / SiteList.Count * 100
                                     Label_percent.Text = iWert.ToString + "/" + SiteList.Count.ToString + " " + Prozent.ToString + "%"
                                     ProgressBar1.Value = Prozent
                                     Return Nothing
                                 End Function))

        Next

    End Sub
#End Region

    Private Sub bt_del_Click(sender As Object, e As EventArgs) Handles bt_del.Click
        If HybridRunning = True Then
            If MessageBox.Show("Are you sure you want to cancel the Download?", "Cancel Download!", MessageBoxButtons.YesNo) = DialogResult.No Then
                Exit Sub
            End If
            Canceld = True
            'KillRunningTask()
        Else
            If proc.HasExited Then
                If MessageBox.Show("The Download is not running anymore, press ok to remove it from the list.", "Remove from list!", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
                    Exit Sub
                End If
                ToDispose = True
            Else
                If MessageBox.Show("Are you sure you want to cancel the Download?", "Cancel Download!", MessageBoxButtons.YesNo) = DialogResult.No Then
                    Exit Sub
                End If
                Canceld = True
                KillRunningTask()
            End If
        End If

    End Sub

#End Region


    Private Sub SuspendProcess(ByVal process As System.Diagnostics.Process)
        For Each t As ProcessThread In process.Threads
            Dim th As IntPtr
            th = OpenThread(ThreadAccess.SUSPEND_RESUME, False, t.Id)
            If th <> IntPtr.Zero Then
                SuspendThread(th)
                CloseHandle(th)
            End If
        Next
    End Sub


    Private Sub ResumeProcess(ByVal process As System.Diagnostics.Process)
        For Each t As ProcessThread In process.Threads
            Dim th As IntPtr
            th = OpenThread(ThreadAccess.SUSPEND_RESUME, False, t.Id)
            If th <> IntPtr.Zero Then
                ResumeThread(th)
                CloseHandle(th)
            End If
        Next
    End Sub



    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        Try
            For tlc As Integer = 0 To ThreadList.Count - 1
                If ThreadList.Item(tlc).IsAlive Then
                Else
                    ThreadList.Remove(ThreadList.Item(tlc))
                End If
            Next
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Label_Anime_Click(sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Label_Anime.Click, PB_Thumbnail.Click, Label_Reso.Click, Label_percent.Click, ProgressBar1.Click, Label_website.Click, Me.Click
        If e.Button = MouseButtons.Right Then
            ' MsgBox("Right Button Clicked")

            ContextMenuStrip1.ContextMenu.Show(Me, MousePosition)
        End If
    End Sub

    Private Sub ViewInExplorerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ViewInExplorerToolStripMenuItem.Click
        Process.Start(Path.GetDirectoryName(DownloadPfad.Replace(Chr(34), "")))
    End Sub

    Private Sub PlaybackVideoFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PlaybackVideoFileToolStripMenuItem.Click
        If GetIsStatusFinished() = True Then
            PlaybackVideoFileToolStripMenuItem.Enabled = True
        Else
            PlaybackVideoFileToolStripMenuItem.Enabled = False
        End If
        Process.Start(DownloadPfad.Replace(Chr(34), ""))
    End Sub

    Private Sub CRD_List_Item_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        PictureBox5.Width = Me.Width - 40

        bt_del.Location = New Point(Me.Width - 63, 10)
        bt_pause.Location = New Point(Me.Width - 98, 15)

        ProgressBar1.Width = Me.Width - 223
    End Sub
End Class

