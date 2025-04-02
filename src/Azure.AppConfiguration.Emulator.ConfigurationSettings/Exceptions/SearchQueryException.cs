using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class SearchQueryException : ArgumentException
    {
        private string _paramName;
        private string _message;

        public SearchQueryException(string paramName, string message, Exception inner = null)
            : base(message, paramName, inner)
        {
            _paramName = paramName;
            _message = message;
        }

        public void SetParamName(string name)
        {
            _paramName = name;
        }

        public override string ParamName => _paramName;

        public override string Message => $"{ParamName}({Position}): {_message}";

        public int Position { get; set; }

        public string Query { get; set; }
    }
}
