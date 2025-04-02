using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Azure.AppConfiguration.Emulator.Service.LongRunningOperation
{
    static class OperationStatusConverter
    {
        public static OperationStatus ToOperationStatus(this Snapshot snapshot)
        {
            string id = GenerateId(snapshot);

            switch (snapshot.Status)
            {
                case SnapshotStatus.Provisioning:
                    return new OperationStatus
                    {
                        Id = id,
                        Status = Status.Running
                    };

                case SnapshotStatus.Ready:
                case SnapshotStatus.Archived:
                    return new OperationStatus
                    {
                        Id = id,
                        Status = Status.Succeeded
                    };

                case SnapshotStatus.Failed:
                    return new OperationStatus
                    {
                        Id = id,
                        Status = Status.Failed,
                        Error = ToErrorDetail(snapshot)
                    };

                default:
                    throw new NotImplementedException();
            }
        }

        private static string GenerateId(Snapshot snapshot)
        {
            Debug.Assert(snapshot != null);
            Debug.Assert(!string.IsNullOrEmpty(snapshot.Name));

            byte[] data = Encoding.Unicode.GetBytes(snapshot.Name);

            using (SHA256 alg = SHA256.Create())
            {
                byte[] hash = alg.ComputeHash(data);

                return Base64UrlEncoding.Encode(hash);
            }
        }

        private static ErrorDetail ToErrorDetail(Snapshot snapshot)
        {
            Debug.Assert(snapshot != null);

            switch (snapshot.StatusCode)
            {
                case (int)HttpStatusCode.TooManyRequests:
                    return new ErrorDetail
                    {
                        Code = "QuotaExceeded",
                        Message = $"The allotted quota for snapshot creation has been surpassed."
                    };

                case (int)HttpStatusCode.ServiceUnavailable:
                    return new ErrorDetail
                    {
                        Code = "Timeout",
                        Message = $"Snapshot creation timed out. Please retry the request."
                    };

                default:
                    return new ErrorDetail
                    {
                        Code = "ServerError",
                        Message = $"An error occurred. Please retry the request."
                    };
            }
        }
    }
}
