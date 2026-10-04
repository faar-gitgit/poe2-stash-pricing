using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace PoeStashPricer
{
    // Independent snapshots: ordinary tabs cannot be reliably identified across scans.
    public static class ScanArchive
    {
        public static string SaveCompleted(TabResult result, string league, bool aborted, string directory)
        {
            if (aborted) return null;
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "scan-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")
                + "-" + Guid.NewGuid().ToString("N") + ".json");
            var snapshot = new { SchemaVersion = 1, AppVersion = MainForm.Version,
                League = league, Complete = true, Result = result };
            string json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(snapshot);
            // CreateNew prevents accidental overwrite; failures reach the existing scan-error dialog.
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(json);
            return path;
        }
    }
}
