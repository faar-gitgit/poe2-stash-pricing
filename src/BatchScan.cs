using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PoeStashPricer
{
    public partial class MainForm
    {
        bool batchRunning, batchCancelled, lastScanCompleted;
        Rectangle lastScanRegion;
        string batchDirectory;
        NumericUpDown batchCount;
        Button batchButton;

        void StopBatch()
        {
            batchCancelled = true;
            if (scanner != null) scanner.CancelRequested = true;
        }

        bool BatchStopped(IntPtr game)
        {
            return batchCancelled || IsDisposed || Native.IsKeyDown(Native.VK_ESCAPE)
                || Native.GetForegroundWindow() != game;
        }

        async Task<bool> BatchDelay(IntPtr game, int milliseconds)
        {
            for (int elapsed = 0; elapsed < milliseconds; elapsed += 50)
            {
                if (BatchStopped(game)) return false;
                await Task.Delay(50);
            }
            return !BatchStopped(game);
        }

        async void StartBatch(bool fromHotkey)
        {
            if (batchRunning) { StopBatch(); return; }
            if (busy || table == null) { SetStatus("Wait for the current operation and prices to finish loading."); return; }
            // Reserve the operation before yielding so repeated hotkeys cannot start another run.
            batchRunning = true;
            batchCancelled = false;
            int completed = 0, count = (int)batchCount.Value;
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scan-exports",
                "batch-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
            try
            {
                IntPtr game = await GetGame(fromHotkey);
                if (game == IntPtr.Zero) throw new InvalidOperationException("Open the stash on its first tab, then press F6.");
                batchButton.Text = "Stop batch (F6/F7)";
                batchCount.Enabled = false;
                chkHover.Enabled = false;
                btnPreview.Enabled = false;
                btnScanKey.Enabled = false;
                btnOverlayKey.Enabled = false;
                cbLeague.Enabled = false;
                // Let the starting hotkey be released before the scanner begins.
                if (!await BatchDelay(game, 400)) return;
                for (int i = 1; i <= count; i++)
                {
                    if (BatchStopped(game)) break;
                    batchDirectory = Path.Combine(root, "tab-" + i.ToString("D3"));
                    Log.Write("batch: scanning tab " + i + " of " + count);
                    await ScanOnce(true);
                    if (!lastScanCompleted || BatchStopped(game)) break;
                    completed++;
                    if (i == count) break;

                    // Header changes distinguish identical/empty tabs. Contents also distinguish tabs
                    // whose similarly styled headers do not change enough to be recognised.
                    Rectangle region = lastScanRegion;
                    int height = Math.Max(20, (int)(region.Width * 0.065));
                    Rectangle header = new Rectangle(region.X, Math.Max(0, region.Y - height), region.Width, height);
                    Native.MoveMouse(region.X + region.Width / 2, header.Y);
                    if (!await BatchDelay(game, 200)) break;
                    using (Bitmap before = Grid.Capture(header))
                    using (Bitmap contentsBefore = Grid.Capture(region))
                    {
                        if (BatchStopped(game)) break;
                        Native.NextStashTab();
                        bool changed = false;
                        int confirmations = 0;
                        for (int retry = 0; retry < 20 && !changed; retry++)
                        {
                            if (!await BatchDelay(game, 150)) break;
                            using (Bitmap after = Grid.Capture(header))
                            using (Bitmap contentsAfter = Grid.Capture(region))
                            {
                                bool headerChanged = BatchHeader.Changed(before, after);
                                bool contentsChanged = BatchHeader.ContentsChanged(contentsBefore, contentsAfter);
                                confirmations = headerChanged || contentsChanged ? confirmations + 1 : 0;
                                changed = confirmations >= 2;
                                if (changed) Log.Write("batch: next tab confirmed (header=" + headerChanged + ", contents=" + contentsChanged + ")");
                            }
                        }
                        if (BatchStopped(game)) break;
                        if (!changed)
                        {
                            // Preserve evidence so a real navigation failure can be distinguished from
                            // a visual-detection failure without asking for a precisely timed screenshot.
                            before.Save(Path.Combine(root, "switch-header-before.png"));
                            contentsBefore.Save(Path.Combine(root, "switch-contents-before.png"));
                            using (Bitmap after = Grid.Capture(header)) after.Save(Path.Combine(root, "switch-header-after.png"));
                            using (Bitmap after = Grid.Capture(region)) after.Save(Path.Combine(root, "switch-contents-after.png"));
                            throw new InvalidOperationException("Could not confirm the next tab after 3 seconds. Diagnostic images and completed scans were saved. Check tab count or folders.");
                        }
                    }
                    if (!await BatchDelay(game, 350)) break;
                }
            }
            catch (Exception ex) { Log.Write("batch error: " + ex); SetStatus(ex.Message); batchFailure = ex.Message; }
            finally
            {
                batchRunning = false;
                batchDirectory = null;
                if (!IsDisposed)
                {
                    batchButton.Text = "Scan all tabs (F6)";
                    batchCount.Enabled = chkHover.Enabled = btnPreview.Enabled = btnScanKey.Enabled = btnOverlayKey.Enabled = cbLeague.Enabled = true;
                    string status = string.Format("Batch {0}: {1}/{2} tabs saved. {3}", completed == count ? "complete" : "stopped", completed, count, root);
                    if (batchFailure != null) status += " " + batchFailure;
                    Log.Write(status);
                    SetStatus(status);
                }
                batchFailure = null;
            }
        }
        string batchFailure;
    }

    public static class BatchHeader
    {
        public static bool Changed(Bitmap before, Bitmap after)
        {
            return ChangedFraction(before, after) > 0.015;
        }

        public static bool ContentsChanged(Bitmap before, Bitmap after)
        {
            // Require a broad change, not a blinking stack count or a small animated icon.
            return ChangedFraction(before, after) > 0.08;
        }

        static double ChangedFraction(Bitmap before, Bitmap after)
        {
            if (before.Size != after.Size) return 0;
            int changed = 0, samples = 0;
            for (int y = 0; y < before.Height; y += 3)
                for (int x = 0; x < before.Width; x += 3)
                {
                    Color a = before.GetPixel(x, y), b = after.GetPixel(x, y);
                    if (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) > 90) changed++;
                    samples++;
                }
            return samples > 0 ? (double)changed / samples : 0;
        }
    }
}
