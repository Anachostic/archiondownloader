Imports System.Drawing
Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports ArchionDownloader.Tile

Module Program
    Sub Main(args As String())
        Dim startTime As Date = Now
        Dim url As String
        Dim pages As PageCollection
        Dim t As Tile
        Dim docType As String
        Dim uid As String
        Dim m As Match

        If args.Length = 0 Then
            Console.WriteLine("Specify the url to downoad on the command line")
            Exit Sub
        End If

        url = args(0)

        ' https://www.archion.de/de/ajax?tx_sparchiondocuments_spdocumentviewer[action]=getViewerDocumentPages&uid=295770&type=churchRegister

        m = Regex.Match(url, "(\d{6,})")
        If Not m.Success Then
            Console.WriteLine("url does not contain a number for UID identification")
            Exit Sub
        End If
        uid = m.Groups(1).Value

        'uid = "268010"
        docType = "churchRegister"

        Console.WriteLine($"Gathering info for UID {uid} of type {docType}")

        pages = GetPages(uid, docType)
        Console.WriteLine($"There are {pages.Count} pages to download")

        For Each p In pages
            t = GetTile(p, uid, docType)
            Console.WriteLine($"Page {p.position} has {t.tiles.Length} tiles to download")

            BuildAndSaveImage(t, p, uid, docType)
        Next

        Console.WriteLine($"Completed in {Now.Subtract(startTime).Minutes} mins, {Now.Subtract(startTime).Seconds} secs.")

    End Sub

    Private Function GetPages(uid As String, docType As String) As PageCollection
        Dim response As String
        Dim wc As HttpClient
        Dim p() As Page
        Dim pc As New PageCollection

        wc = New HttpClient
        response = wc.GetStringAsync($"https://www.archion.de/de/ajax?tx_sparchiondocuments_spdocumentviewer[action]=getViewerDocumentPages&uid={uid}&type={docType}").Result

        wc.Dispose()

        p = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Page())(response)
        pc.AddRange(p)

        Return pc

    End Function

    Private Function GetTile(p As Page, uid As String, docType As String) As Tile
        Dim content As FormUrlEncodedContent
        Dim wc As HttpClient
        Dim msg As HttpResponseMessage
        Dim data As String

        content = New FormUrlEncodedContent(GetTilePostString(p))

        wc = New HttpClient
        msg = wc.PostAsync($"https://www.archion.de/de/ajax?tx_sparchiondocuments_spdocumentviewer[action]=getViewerDocumentPageTiles&uid={uid}&type={docType}&pageId={p.id}", content).Result
        data = msg.Content.ReadAsStringAsync.Result

        Dim t As Tile = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Tile)(data)

        Return t

    End Function

    Private Function GetTilePostString(p As Page) As KeyValuePair(Of String, String)()
        Dim keys As New List(Of KeyValuePair(Of String, String))
        Dim sb As New StringBuilder

        keys.Add(New KeyValuePair(Of String, String)("level", "13"))

        For y = 0 To CInt(Math.Ceiling(p.height \ 256))
            For x = 0 To CInt(Math.Ceiling(p.width \ 256))
                sb.Append($"{x},{y}|")
            Next
        Next

        sb.Length -= 1

        keys.Add(New KeyValuePair(Of String, String)("tiles", sb.ToString))

        Return keys.ToArray

    End Function

    Private Sub BuildAndSaveImage(t As Tile, p As Page, uid As String, docType As String)
        Dim masterBMP As Bitmap
        Dim gr As Graphics

        masterBMP = New Bitmap(p.width, p.height)
        gr = Graphics.FromImage(masterBMP)

        Console.Write($"Downloading page {p.position}... ")

        Dim r As ParallelLoopResult
        r = Parallel.ForEach(Of tileDetail)(t.tiles, Sub(td As tileDetail, state As ParallelLoopState, index As Long)
                                                         Dim tileBMP As Bitmap
                                                         Dim wc As HttpClient
                                                         Dim s As IO.Stream

                                                         wc = New HttpClient
                                                         s = wc.GetStreamAsync($"https://www.archion.de{t.baseurl}{td.src}").Result
                                                         tileBMP = DirectCast(Bitmap.FromStream(s), Drawing.Bitmap)
                                                         SyncLock (masterBMP)
                                                             gr.DrawImage(tileBMP, New Point(td.XPoint, td.YPoint))
                                                         End SyncLock

                                                         tileBMP.Dispose()
                                                         s.Dispose()
                                                         wc.Dispose()

                                                     End Sub)

        Console.WriteLine("done.")

        gr.Dispose()

        If Not IO.Directory.Exists(uid & "-" & docType) Then
            IO.Directory.CreateDirectory(uid & "-" & docType)
        End If

        masterBMP.Save(IO.Path.Combine(uid & "-" & docType, $"page-{p.position}.jpg"), Imaging.ImageFormat.Jpeg)
        masterBMP.Dispose()

    End Sub


End Module

Public Class PageCollection
    Inherits List(Of Page)
End Class

Public Class Page
    Public Property id As Integer
    Public Property position As Integer
    Public Property width As Integer
    Public Property height As Integer
End Class

Public Class Tile
    Public Property baseurl As String
    Public Property validUntil As Long
    Public Property tiles As TileDetail()

    Public Class TileDetail
        Public ReadOnly Property XPoint As Integer
            Get
                Dim m As Match
                m = Regex.Match(Me.tile, "(\d+),(\d+)")
                Return CInt(m.Groups(1).Value) * 256
            End Get
        End Property

        Public ReadOnly Property YPoint As Integer
            Get
                Dim m As Match
                m = Regex.Match(Me.tile, "(\d+),(\d+)")
                Return CInt(m.Groups(2).Value) * 256
            End Get
        End Property

        Public Property tile As String
        Public Property src As String
    End Class

End Class