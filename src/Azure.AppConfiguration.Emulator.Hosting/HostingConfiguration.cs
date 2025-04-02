using System;
using System.Security.Cryptography.X509Certificates;

namespace Azure.AppConfiguration.Emulator.Hosting
{
    public class HostingConfiguration
    {
        private X509Certificate2 _cert;

        /// <summary>
        /// Port to be used for listening to HTTP requests
        /// </summary>
        public int Port { get; set; } = 8483;

        /// <summary>
        /// Base 64 encoded PFX file
        /// </summary>
        public string PFX { get; set; }

        /// <summary>
        /// Password to PFX file
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Certificate constructed from provided certificate data
        /// </summary>
        public X509Certificate2 Certificate
        {

            get
            {

                if (_cert == null && !string.IsNullOrEmpty(PFX))
                {
                    _cert = new X509Certificate2(Convert.FromBase64String(PFX), Password);
                }

                return _cert;
            }
        }
    }
}
