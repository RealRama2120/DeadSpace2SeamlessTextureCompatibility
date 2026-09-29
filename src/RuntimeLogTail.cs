using System;
using System.IO;
using System.Text;

namespace DeadSpaceTextureLauncher
{
    // Runtime records are ASCII and may be appended while the companion reads.
    // Keep only one bounded partial record, never the entire session history.
    internal sealed class RuntimeLogTail
    {
        internal const int ReadBudget = 4 * 1024 * 1024;
        private long position;
        private readonly byte[] buffer = new byte[8192];
        private readonly StringBuilder pending = new StringBuilder();
        private bool oversized;
        internal long Position { get { return position; } }

        internal void Poll(string path, Action<string> consume)
        {
            if (!File.Exists(path)) return;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (stream.Length < position) { position = 0; pending.Clear(); oversized = false; }
                stream.Position = position;
                int remaining = ReadBudget;
                while (remaining > 0)
                {
                    int read = stream.Read(buffer, 0, Math.Min(buffer.Length, remaining));
                    if (read == 0) break;
                    position += read; remaining -= read;
                    for (int i = 0; i < read; i++)
                    {
                        char value = (char)buffer[i];
                        if (value == '\n')
                        {
                            if (!oversized) consume(pending.ToString().TrimEnd('\r'));
                            pending.Clear(); oversized = false;
                        }
                        else if (!oversized)
                        {
                            if (pending.Length < 4096) pending.Append(value);
                            else { pending.Clear(); oversized = true; }
                        }
                    }
                }
            }
        }
    }
}
