module TVTropes.Test.Backup

open TVTropes
open NUnit.Framework
open FsUnit

let path = @"C:\Users\Ming\Documents\TVTropesBackup"

[<TestFixtureSetUp>]
let setup() =
  Backup.setBackupPath path

[<Test>]
let ``getBackupPath should produce the backup path as called by initialize``() =
  Backup.getBackupPath()
  |> should equal path

[<Test>]
let ``fullPath should produce the full path from a relative path``() =
  Backup.fullPath @"NamespacesA-K\Determinator.Music@action=source"
  |> should equal (path + @"\NamespacesA-K\Determinator.Music@action=source")

[<Test>]
let ``relativePath should produce the relative path from a full path``() =
  Backup.relativePath (path + @"\NamespacesA-K\Determinator.Music@action=source")
  |> should equal @"NamespacesA-K\Determinator.Music@action=source"

[<Test>]
let ``fileExists should produce true if the referenced backup file exists``() =
  Backup.fileExists @"NamespacesA-K\Determinator.Music@action=source"
  |> should be True

[<Test>]
let ``fileExists should produce true if the referenced backup file does not exist``() =
  Backup.fileExists @"Main\Main.NotAPage@action=source"
  |> should be False

[<Test>]
let ``fileExists should produce false if the path references to a dir rather than a file``() =
  Backup.fileExists @"Main"
  |> should be False

[<Test>]
let ``dirExists should produce true for an existing dir``() =
  Backup.dirExists @"NamespacesA-K"
  |> should be True

[<Test>]
let ``dirExists should produce false if dir does not exist``() =
  Backup.dirExists @"DirDoesNotExist"
  |> should be False

[<Test>]
let ``dirExists should produce false even if the file exists``() =
  Backup.dirExists @"NamespacesA-K\Determinator.Music@action=source"
  |> should be False

[<Test>]
let ``pageName should produce the corresponding page name of a path``() =
  Backup.pageName @"NamespacesA-K\Administrivia\Administrivia.TheGoalsOfTVTropes@action=source"
  |> should equal ("Administrivia", "TheGoalsOfTVTropes")

[<Test>]
let ``filenameOf should produce the filename of a page``() =
  Backup.filenameOf("Administrivia", "NoTropeIsTooCommon")
  |> should equal "Administrivia.NoTropeIsTooCommon@action=source"

[<Test>]
let ``getContentsFromPath should produce the contents of a page referred by path and nothing else``() =
  let c = Backup.getContentsFromPath @"NamespacesA-K\Administrivia\Administrivia.TheGoalsOfTVTropes@action=source"
  c |> should startWith "Here are the TV Tropes goals."
  c |> should endWith "<br/>"

[<Test>]
let ``enumerateFiles should produce a sequence the paths of the backup files, starting from Main``() =
  let first4 = Backup.enumerateFiles()
               |> Seq.take 4
               |> List.ofSeq
  first4 |> should equal [ @"Main\Main.1\Main.3DMovie@action=source"
                           @"Main\Main.A\Main.A-1Pictures@action=source"
                           @"Main\Main.A\Main.A-CupAngst@action=source"
                           @"Main\Main.A\Main.A-ha@action=source" ]
