﻿Imports System.Drawing.Imaging

Public Class Tileset

    Public images(&HFF) As Bitmap
    Public priorityImages(&HFF) As Bitmap
    Public address As Integer
    Public collisionAddr As Integer
    Public paletteAddr As Integer
    Public gfxAddr As Integer
    Public pltAnimAddr As Integer

    Public map16 As Byte()
    Public LinGFX(511)(,) As Byte

    'Testing
    Public collision As Byte()

    Public Sub New(ByVal s As IO.Stream)
        Dim levelStartPos As Long = s.Position
        address = Pointers.ReadPointer(s)
        If address = -1 Then Throw New Exception("Invalid tiles pointer.")
        s.Seek(4, IO.SeekOrigin.Current)
        collisionAddr = Pointers.ReadPointer(s)
        If collisionAddr = -1 Then Throw New Exception("Invalid collision pointer.")
        gfxAddr = Pointers.ReadPointer(s)
        If gfxAddr = -1 Then Throw New Exception("Invalid graphics pointer.")
        paletteAddr = Pointers.ReadPointer(s)
        If paletteAddr = -1 Then Throw New Exception("Invalid palette pointer.")
        s.Seek(4, IO.SeekOrigin.Current)
        pltAnimAddr = Pointers.ReadPointer(s)
        Reload(s)
        s.Seek(levelStartPos, IO.SeekOrigin.Begin)
    End Sub

    Public Sub New(ByVal s As IO.Stream, ByVal tilesAddr As Integer, ByVal colAddr As Integer, ByVal gfxAddr As Integer, ByVal palAddr As Integer, ByVal sprPal As Integer, ByVal pltAnimAddr As Integer)
        Me.address = tilesAddr
        Me.collisionAddr = colAddr
        Me.gfxAddr = gfxAddr
        Me.paletteAddr = palAddr
        Me.pltAnimAddr = pltAnimAddr
        Reload(s)
    End Sub

    Public Sub Reload(ByVal s As IO.Stream)
        'Load palette
        s.Seek(paletteAddr, IO.SeekOrigin.Begin)
        Dim plt As Color() = SNESGFX.ReadPalette(s, &H80, False)

        'Load graphics data
        Dim gfx(&H3FFF) As Byte
        s.Seek(gfxAddr, IO.SeekOrigin.Begin)
        s.Read(gfx, 0, &H4000)
        'Load map16 data
        s.Seek(address, IO.SeekOrigin.Begin)
        map16 = ZAMNCompress.Decompress(s)
        'Load collision data
        s.Seek(collisionAddr, IO.SeekOrigin.Begin)
        Dim collision(&H3FF) As Byte
        s.Read(collision, 0, &H400)

        Me.collision = collision

        'Palette animation (currently not used)
        If pltAnimAddr > 0 Then
            'Shrd.GoToPointer(s)
        End If
        'Convert GFX to linear format
        For l As Integer = 0 To &H1FF
            LinGFX(l) = SNESGFX.PlanarToLinear(gfx, l * &H20)
        Next
        'Copy to bitmaps
        For i As Integer = 0 To &HFF
            Dim CurBmp As New Bitmap(64, 64, PixelFormat.Format8bppIndexed), CurPrBmp As New Bitmap(64, 64, PixelFormat.Format8bppIndexed)
            SNESGFX.FillPalette(CurBmp, plt)
            SNESGFX.FillPalette(CurPrBmp, plt)
            'Shrd.MakePltTransparent(CurPrBmp)
            Dim data As BitmapData = CurBmp.LockBits(New Rectangle(0, 0, 64, 64), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed)
            Dim dataPr As BitmapData = CurPrBmp.LockBits(New Rectangle(0, 0, 64, 64), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed)
            Dim x As Integer = 0, y As Integer = 0
            For m As Integer = i * &H80 To i * &H80 + &H7F Step 2
                Dim g As Integer = map16(m)
                If (map16(m + 1) And 1) = 1 Then
                    g += &H100
                End If
                SNESGFX.DrawTile(data, x, y, map16(m + 1), map16(m), LinGFX)

                If (collision(g * 2) And 1) > 0 And (collision(g * 2 + 1) And 1) = 0 Then
                    SNESGFX.DrawTile(dataPr, x, y, map16(m + 1), map16(m), LinGFX)
                End If

                x += 8
                If x = 64 Then
                    x = 0
                    y += 8
                End If
            Next
            CurBmp.UnlockBits(data)
            CurPrBmp.UnlockBits(dataPr)
            images(i) = CurBmp
            priorityImages(i) = CurPrBmp
        Next
    End Sub
End Class
