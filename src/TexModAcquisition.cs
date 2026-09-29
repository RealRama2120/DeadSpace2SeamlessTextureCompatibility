using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;

namespace DeadSpaceTextureLauncher
{
    internal static class TexModAcquisition
    {
        public const string OriginalArchiveUrl = "https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/texmod/texmod.zip";
        public const string ArchiveSha1 = "c05a59ef20c5cb682230de2be9973945562ab86d";
        public const string ExecutableSha256 = "f662be61eee7c3d2849e1c734b3b83e9e25fa4e873c7352852130f0c0ceb98af";

        public static string DownloadVerified(Action<string> progress)
        {
            Directory.CreateDirectory(AppInfo.ToolsDirectory);
            string archivePath = Path.Combine(AppInfo.ToolsDirectory, "texmod.download.zip");
            string temporaryExe = AppInfo.ManagedTexModPath + ".download";
            TryDelete(archivePath);
            TryDelete(temporaryExe);

            try
            {
                Report(progress, "Downloading TexMod 0.9b from the archived original project...");
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                using (WebClient client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = AppInfo.Name + "/" + AppInfo.Version;
                    client.DownloadFile(OriginalArchiveUrl, archivePath);
                }

                Report(progress, "Verifying the official archive checksum...");
                string archiveHash = HashFile(archivePath, SHA1.Create());
                if (!archiveHash.Equals(ArchiveSha1, StringComparison.OrdinalIgnoreCase))
                    throw new LauncherException("The downloaded TexMod archive did not match the published checksum. It was rejected and deleted.");

                using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                {
                    ZipArchiveEntry executable = null;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (Path.GetFileName(entry.FullName).Equals("TexMod.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            executable = entry;
                            break;
                        }
                    }
                    if (executable == null) throw new LauncherException("The verified archive did not contain TexMod.exe.");
                    using (Stream input = executable.Open())
                    using (FileStream output = new FileStream(temporaryExe, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        input.CopyTo(output);
                }

                string executableHash = HashFile(temporaryExe, SHA256.Create());
                if (!executableHash.Equals(ExecutableSha256, StringComparison.OrdinalIgnoreCase))
                    throw new LauncherException("TexMod.exe did not match the pinned original checksum. It was rejected and deleted.");

                TryDelete(AppInfo.ManagedTexModPath);
                File.Move(temporaryExe, AppInfo.ManagedTexModPath);
                Report(progress, "Verified TexMod 0.9b is ready.");
                return AppInfo.ManagedTexModPath;
            }
            finally
            {
                TryDelete(archivePath);
                TryDelete(temporaryExe);
            }
        }

        public static bool IsVerifiedOriginal(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            try { return HashFile(filePath, SHA256.Create()).Equals(ExecutableSha256, StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        private static string HashFile(string path, HashAlgorithm algorithm)
        {
            using (algorithm)
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] hash = algorithm.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void Report(Action<string> progress, string message)
        {
            Log.Info(message);
            if (progress != null) progress(message);
        }
    }
}
