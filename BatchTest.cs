using System;
using System.Drawing;
using System.IO;
using System.Web.Script.Serialization;
using PoeStashPricer;
class BatchTest
{
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
    static void Main(string[] args)
    {
        using (var first = new Bitmap(800, 50))
        using (var same = new Bitmap(800, 50))
        using (var next = new Bitmap(800, 50))
        {
            Check(!BatchHeader.Changed(first, same), "Identical tab accepted as changed");
            using (var g = Graphics.FromImage(next)) g.FillRectangle(Brushes.Gold, 120, 5, 80, 35);
            Check(BatchHeader.Changed(first, next), "Selected tab change missed");
            same.SetPixel(20, 20, Color.White);
            Check(!BatchHeader.Changed(first, same), "Single-pixel noise accepted");
        }
        var result = new TabResult { Key = "currency", ScannedAt = DateTime.Now };
        using (var a = new Bitmap(300, 300))
        using (var b = new Bitmap(300, 300))
        {
            Check(!BatchHeader.ContentsChanged(a, b), "Unchanged contents accepted");
            using (var g = Graphics.FromImage(b)) g.FillRectangle(Brushes.Gold, 0, 0, 15, 15);
            Check(!BatchHeader.ContentsChanged(a, b), "Small animation accepted");
            using (var g = Graphics.FromImage(b)) g.FillRectangle(Brushes.Gold, 0, 0, 150, 150);
            Check(BatchHeader.ContentsChanged(a, b), "Changed stash contents missed");
        }
        result.Items.Add(new SavedItem { Text = "Same item text in distinct tabs" });
        string one = ScanArchive.SaveCompleted(result, "Forbidden Rites", false, Path.Combine(args[0], "tab-001"));
        string two = ScanArchive.SaveCompleted(result, "Forbidden Rites", false, Path.Combine(args[0], "tab-002"));
        Check(File.Exists(one) && File.Exists(two), "Same-type tabs overwritten");
        string aborted = Path.Combine(args[0], "tab-003");
        Check(ScanArchive.SaveCompleted(result, "Forbidden Rites", true, aborted) == null && !Directory.Exists(aborted), "Cancelled tab exported");
        var cfg = new JavaScriptSerializer().Deserialize<AppSettings>("{\"HoverDelay\":40}");
        Check(cfg.HoverDelay == 40 && cfg.BatchTabCount == 0, "Old settings incompatible");
        cfg.BatchTabCount = 22;
        Check(new JavaScriptSerializer().Deserialize<AppSettings>(new JavaScriptSerializer().Serialize(cfg)).BatchTabCount == 22, "Count not preserved");
        Console.WriteLine("PASS: header change/no-change/noise; separate same-type tabs; cancellation; settings compatibility and count persistence");
    }
}
