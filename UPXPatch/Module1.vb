Imports System.IO
Imports System.Text

Module Module1
    Public rnd As New Random()

    Public Sub Main()
        Console.ForegroundColor = ConsoleColor.Red
        Console.Write(vbLf & "UPX AntiDepack + Protect || Version 1.0")
        Console.WriteLine("")

        Console.ResetColor()

        Dim args = Environment.GetCommandLineArgs()

        If args.Length = 1 Then
            StdOut.Write("Syntax: " & AppDomain.CurrentDomain.FriendlyName & " <file_path>", True)
            Environment.Exit(0)
        End If

        Dim fileName As String = args(1)

        If IO.File.Exists(fileName) Then
            Dim bytesReplacer As New Patcher

            Try
                Using fs As New FileStream(fileName, FileMode.Open, FileAccess.Read)
                    If Not New BinaryReader(fs).ReadBytes(2).SequenceEqual(New Byte() {&H4D, &H5A}) Then
                        Throw New Exception("Input file is not a valid executable!")
                    End If
                End Using

                Console.ForegroundColor = ConsoleColor.Yellow
                StdOut.Log("Randomizing Section Names...")


                bytesReplacer.PatchBytes(fileName, {&H55, &H50, &H58, &H30, &H0}, GenerateRandomSectionName())
                bytesReplacer.PatchBytes(fileName, {&H55, &H50, &H58, &H31, &H0}, GenerateRandomSectionName())
                bytesReplacer.PatchBytes(fileName, {&H55, &H50, &H58, &H32, &H0}, GenerateRandomSectionName())

                StdOut.Log("Randomizing Version...")

                Dim offset As Long = bytesReplacer.FindStringOffset(fileName, "UPX!") ' version identifier

                If offset <> -1 Then
                    Dim bytesToReplace As Int16 = 15
                    Dim randomVersion(bytesToReplace) As Byte
                    rnd.NextBytes(randomVersion)

                    bytesReplacer.PatchBytesByOffset(fileName, offset - bytesToReplace + 4, randomVersion)
                Else
                    Throw New Exception("UPX version unreachable? Is it modified?")
                End If

                StdOut.Log("Randomizing Standard DOS Message...")


                bytesReplacer.PatchBytes(fileName, Encoding.ASCII.GetBytes("This program cannot be run in DOS mode."),
                Encoding.ASCII.GetBytes(GenerateRandomMessage()))

                StdOut.Log("EntryPoint patching...")

                Dim isBuild64 As Boolean = PE.Is64(fileName)

                If isBuild64 Then
                    bytesReplacer.PatchBytes(fileName, New Byte() {&H0, &H53, &H56}, New Byte() {&H90, &H90, &H90}) ' x86_64
                Else
                    bytesReplacer.PatchBytes(fileName, New Byte() {&H0, &H60, &HBE}, New Byte() {&H90, &H90, &H90}) ' i386
                End If

                StdOut.Log("Adding randomized padding...")

                AddRandomPadding(fileName)

                StdOut.Log("Altering UPX Headers...")
                bytesReplacer.AlterUPXHeaders(fileName)

                StdOut.Log("Modifying import table...")
                bytesReplacer.ModifyImportTable(fileName)

                StdOut.Log("Removing UPX-specific strings...")
                bytesReplacer.RemoveUPXStrings(fileName)

                StdOut.Log("Adding dummy imports...")
                bytesReplacer.AddDummyImports(fileName)



            Catch ex As Exception
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine(ex.Message)
                Console.ResetColor()
                Environment.Exit(1)
            End Try

            Console.ForegroundColor = ConsoleColor.Green
            StdOut.Log("Successfully patched!")
        End If
    End Sub
    Private Function GenerateRandomSectionName() As Byte()
        Dim nameLength As Integer = 5
        Dim randomSectionName(nameLength - 1) As Byte
        For i As Integer = 0 To nameLength - 1
            randomSectionName(i) = CByte(rnd.Next(&H61, &H7A)) ' ASCII a-z
        Next
        Return randomSectionName
    End Function

    Private Function GenerateRandomMessage() As String
        Dim rn As New Random
        Return rn.Next(9999, 999999).ToString()
    End Function

    Private Sub AddRandomPadding(fileName As String)
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)
        Dim paddingSize As Integer = rnd.Next(16, 64) ' Random padding length between 16 and 64 bytes
        Dim randomPadding(paddingSize - 1) As Byte
        rnd.NextBytes(randomPadding)


        Dim newFileBytes As Byte() = New Byte(fileBytes.Length + randomPadding.Length - 1) {}
        Array.Copy(fileBytes, newFileBytes, fileBytes.Length)
        Array.Copy(randomPadding, 0, newFileBytes, fileBytes.Length, randomPadding.Length)

        File.WriteAllBytes(fileName, newFileBytes)
    End Sub
End Module

Public Class Patcher
    Public Function IsPatternPresent(fileName As String, pattern As Byte()) As Boolean
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)
        Return FindSequence(fileBytes, pattern) <> -1
    End Function

    Public Sub PatchBytes(fileName As String, searchPattern As Byte(), replacementPattern As Byte())
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)
        Dim offset As Integer = FindSequence(fileBytes, searchPattern)
        If offset <> -1 Then
            Array.Copy(replacementPattern, 0, fileBytes, offset, replacementPattern.Length)
            File.WriteAllBytes(fileName, fileBytes)
        End If
    End Sub

    Public Sub PatchBytesByOffset(fileName As String, offset As Long, replacementPattern As Byte())
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)
        Array.Copy(replacementPattern, 0, fileBytes, offset, replacementPattern.Length)
        File.WriteAllBytes(fileName, fileBytes)
    End Sub

    Public Function FindStringOffset(fileName As String, searchString As String) As Long
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)
        Dim searchPattern As Byte() = Encoding.ASCII.GetBytes(searchString)
        Return FindSequence(fileBytes, searchPattern)
    End Function

    Public Sub AlterUPXHeaders(fileName As String)
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)
        Dim upxHeaderPattern As Byte() = {&H55, &H50, &H58, &H21} ' UPX!
        Dim offset As Integer = FindSequence(fileBytes, upxHeaderPattern)
        If offset <> -1 Then

            Dim randomBytes(3) As Byte
            rnd.NextBytes(randomBytes)
            Array.Copy(randomBytes, 0, fileBytes, offset, randomBytes.Length)
            File.WriteAllBytes(fileName, fileBytes)
        End If
    End Sub

    Public Sub ModifyImportTable(fileName As String)
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)

        Dim importTableOffset As Integer = FindImportTableOffset(fileBytes)
        If importTableOffset <> -1 Then
            Dim importTableLength As Integer = &H40
            Dim randomBytes(importTableLength - 1) As Byte
            rnd.NextBytes(randomBytes)
            Array.Copy(randomBytes, 0, fileBytes, importTableOffset, randomBytes.Length)
            File.WriteAllBytes(fileName, fileBytes)
        End If
    End Sub

    Public Sub ModifyPEHeaderAndSections(fileName As String)
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)
        Dim peHeaderOffset As Integer = BitConverter.ToInt32(fileBytes, &H3C)

        Dim timestampOffset As Integer = peHeaderOffset + &H8
        Dim checksumOffset As Integer = peHeaderOffset + &H58
        Dim newTimestamp As Integer = rnd.Next()
        Dim newChecksum As Integer = rnd.Next()
        Array.Copy(BitConverter.GetBytes(newTimestamp), 0, fileBytes, timestampOffset, 4)
        Array.Copy(BitConverter.GetBytes(newChecksum), 0, fileBytes, checksumOffset, 4)


        Dim numberOfSections As Integer = BitConverter.ToInt16(fileBytes, peHeaderOffset + &H6)
        Dim sectionHeaderOffset As Integer = peHeaderOffset + &HF8
        For i As Integer = 0 To numberOfSections - 1
            Dim sectionNameOffset As Integer = sectionHeaderOffset + (i * &H28)
            Dim randomSectionName As Byte() = GenerateRandomSectionName()
            Array.Copy(randomSectionName, 0, fileBytes, sectionNameOffset, randomSectionName.Length)
        Next

        File.WriteAllBytes(fileName, fileBytes)
    End Sub

    Public Sub RemoveUPXStrings(fileName As String)
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)
        Dim upxStrings As String() = {"$Id: UPX ", "UPX!"}
        For Each upxString In upxStrings
            Dim offset As Integer = FindSequence(fileBytes, Encoding.ASCII.GetBytes(upxString))
            If offset <> -1 Then
                Dim replacement(Encoding.ASCII.GetBytes(upxString).Length - 1) As Byte
                rnd.NextBytes(replacement)
                Array.Copy(replacement, 0, fileBytes, offset, replacement.Length)
            End If
        Next
        File.WriteAllBytes(fileName, fileBytes)
    End Sub

    Public Sub AddDummyImports(fileName As String)
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)

        Dim dummyImports As String() = {"hgvnb84e975", "fjd239483", "dij23e9802"}
        Dim importTableOffset As Integer = FindImportTableOffset(fileBytes)
        If importTableOffset <> -1 Then
            Dim importTable As New List(Of Byte)
            For Each dummyImport In dummyImports
                importTable.AddRange(Encoding.ASCII.GetBytes(dummyImport))
                importTable.Add(0)
            Next
            File.WriteAllBytes(fileName, fileBytes.Concat(importTable).ToArray())
        End If
    End Sub

    Public Sub ModifySectionCharacteristics(fileName As String)
        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)
        Dim peHeaderOffset As Integer = BitConverter.ToInt32(fileBytes, &H3C)
        Dim sectionHeaderOffset As Integer = peHeaderOffset + &HF8
        Dim numberOfSections As Integer = BitConverter.ToInt16(fileBytes, peHeaderOffset + &H6)
        For i As Integer = 0 To numberOfSections - 1
            Dim sectionOffset As Integer = sectionHeaderOffset + (i * &H28)

            Dim newCharacteristics As Integer = rnd.Next()
            Array.Copy(BitConverter.GetBytes(newCharacteristics), 0, fileBytes, sectionOffset + &H24, 4)
        Next
        File.WriteAllBytes(fileName, fileBytes)
    End Sub

    Public Function FindSequence(haystack As Byte(), needle As Byte()) As Integer
        For i As Integer = 0 To haystack.Length - needle.Length
            Dim match As Boolean = True
            For j As Integer = 0 To needle.Length - 1
                If haystack(i + j) <> needle(j) Then
                    match = False
                    Exit For
                End If
            Next
            If match Then
                Return i
            End If
        Next
        Return -1
    End Function

    Private Function FindImportTableOffset(fileBytes As Byte()) As Integer
        Dim peHeaderOffset As Integer = BitConverter.ToInt32(fileBytes, &H3C)
        Dim importTableRVAOffset As Integer = peHeaderOffset + &H80
        Dim importTableRVA As Integer = BitConverter.ToInt32(fileBytes, importTableRVAOffset)
        If importTableRVA = 0 Then Return -1

        Return RVAtoOffset(fileBytes, importTableRVA)
    End Function

    Private Function RVAtoOffset(fileBytes As Byte(), rva As Integer) As Integer
        Dim peHeaderOffset As Integer = BitConverter.ToInt32(fileBytes, &H3C)
        Dim sectionHeaderOffset As Integer = peHeaderOffset + &HF8
        Dim numberOfSections As Integer = BitConverter.ToInt16(fileBytes, peHeaderOffset + &H6)

        For i As Integer = 0 To numberOfSections - 1
            Dim sectionOffset As Integer = sectionHeaderOffset + (i * &H28)
            Dim virtualAddress As Integer = BitConverter.ToInt32(fileBytes, sectionOffset + &HC)
            Dim sizeOfRawData As Integer = BitConverter.ToInt32(fileBytes, sectionOffset + &H10)
            Dim pointerToRawData As Integer = BitConverter.ToInt32(fileBytes, sectionOffset + &H14)

            If rva >= virtualAddress And rva < virtualAddress + sizeOfRawData Then
                Return pointerToRawData + (rva - virtualAddress)
            End If
        Next

        Return -1
    End Function

    Private Function GenerateRandomSectionName() As Byte()
        Dim nameLength As Integer = 5
        Dim randomSectionName(nameLength - 1) As Byte
        For i As Integer = 0 To nameLength - 1
            randomSectionName(i) = CByte(rnd.Next(&H61, &H7A)) ' ASCII a-z
        Next
        Return randomSectionName
    End Function
End Class

Public Class PE
    Public Shared Function Is64(fileName As String) As Boolean

        Dim fileBytes As Byte() = File.ReadAllBytes(fileName)
        Dim peHeaderOffset As Integer = BitConverter.ToInt32(fileBytes, &H3C)
        Dim machine As UInt16 = BitConverter.ToUInt16(fileBytes, peHeaderOffset + 4)
        Return machine = &H8664 ' IMAGE_FILE_MACHINE_AMD64
    End Function
End Class

Public Class StdOut
    Public Shared Sub Write(message As String, Optional newLine As Boolean = True)
        If newLine Then
            Console.WriteLine(message)
        Else
            Console.Write(message)
        End If
    End Sub

    Public Shared Sub Log(message As String)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}")
    End Sub
End Class
