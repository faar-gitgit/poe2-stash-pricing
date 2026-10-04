using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using PoeStashPricer;

class ArchiveTest
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main(string[] args)
    {
        string dir = args[1];
        if (args[0] == "write")
        {
            var result = new TabResult { Key = "_unknown", ScannedAt = DateTime.Now };
            result.Items.Add(new SavedItem { Text = "Rarity: Rare\r\nTest Armour\r\n+80 to maximum Life\r\nUnicode: æ", X = 0.25, Count = 2 });
            string a = ScanArchive.SaveCompleted(result, "Forbidden Rites", false, dir);
            result.Items[0].Text = "Second tab";
            string b = ScanArchive.SaveCompleted(result, "Forbidden Rites", false, dir);
            Check(a != b && File.Exists(a) && File.Exists(b), "Scans overwrite each other");
            Check(ScanArchive.SaveCompleted(result, "Forbidden Rites", true, dir) == null, "Aborted scan exported");
            Check(Directory.GetFiles(dir, "*.json").Length == 2, "Aborted scan created a file");
            bool failed = false;
            try { ScanArchive.SaveCompleted(result, "Forbidden Rites", false, a); }
            catch (IOException) { failed = true; }
            Check(failed, "Write error suppressed");
            Console.WriteLine("PASS: distinct snapshots, aborted scan skipped, write error propagated");
        }
        else
        {
            bool first = false, second = false;
            foreach (string path in Directory.GetFiles(dir, "*.json"))
            {
                var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
                Check((string)data["League"] == "Forbidden Rites" && (bool)data["Complete"], "Missing metadata");
                var result = new JavaScriptSerializer().ConvertToType<TabResult>(data["Result"]);
                first |= result.Items[0].Text.Contains("+80 to maximum Life\r\nUnicode: æ");
                second |= result.Items[0].Text == "Second tab";
                Check(result.Items[0].Count == 2 && result.Items[0].X == 0.25, "Lost item details");
            }
            Check(first && second, "Snapshots did not survive separate process");
            Check(Updater.Check() == null, "Local build updater not disabled");
            Console.WriteLine("PASS: separate-process reload, full item text and metadata, local update protection");
        }
    }
}
