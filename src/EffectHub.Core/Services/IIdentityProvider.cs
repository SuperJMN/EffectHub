namespace EffectHub.Core.Services;

public interface IIdentityProvider
{
    string CurrentAlias { get; }
    string CurrentId { get; }
}

public interface IIdentitySigner
{
    Task<string> SignAsync(string message);
}

public class LocalIdentityProvider : IIdentityProvider
{
    public string CurrentAlias { get; set; } = "Local User";
    public string CurrentId { get; } = Environment.MachineName;
}
