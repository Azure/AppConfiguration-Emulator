using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class KeyLockedException : Exception
    {
        public KeyLockedException(string name, Exception inner = null) : base(string.Empty, inner)
        {
            ParamName = name;
        }

        public string ParamName { get; private set; }
    }
}
