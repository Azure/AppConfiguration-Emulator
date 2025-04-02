namespace Microsoft.AppConfig.Service.Authentication.Anonymous
{
    public class AnonymousOptions
    {
        /// <summary>
        /// The role name that is assigned to anonymous users.
        /// Ex: "Reader", "Owner"
        /// </summary>
        public string AnonymousUserRole { get; init; }

        /// <summary>
        /// Anonymous User security identifier (SID)
        /// </summary>
        public string AnonymousUserSid { get; init; } = "AnonymousUser@Azure_AppConfiguration_Emulator";
    }
}
