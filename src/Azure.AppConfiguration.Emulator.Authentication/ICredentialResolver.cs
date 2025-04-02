namespace Azure.AppConfiguration.Emulator.Authentication
{
    public interface ICredentialResolver
    {
        Credential GetCredential();
    }
}
