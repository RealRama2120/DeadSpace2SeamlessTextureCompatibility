using System;

namespace DeadSpaceTextureLauncher
{
    internal sealed class LauncherException : Exception
    {
        public LauncherException(string message) : base(message) { }
        public LauncherException(string message, Exception inner) : base(message, inner) { }
    }
}
