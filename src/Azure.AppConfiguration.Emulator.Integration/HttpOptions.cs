namespace Azure.AppConfiguration.Emulator.Integration
{
    public class HttpOptions
    {
        /// <summary>
        /// The value used for HttpClientHandler.CheckCertificateRevocationList. If this is set to false, then
        /// the certificate revocation list for certificates received is checked. If true, the check is not made.
        /// This needs to be true for environments where certificate revocation list is not supported.
        /// </summary>
        public bool DisableCertificateRevocationListChecking { get; set; }
    }
}
