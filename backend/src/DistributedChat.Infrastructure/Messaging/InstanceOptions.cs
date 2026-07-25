using DistributedChat.Domain.Users;

namespace DistributedChat.Infrastructure.Messaging;

public sealed class InstanceOptions
{
    public const string SectionName = "Instance";

    public string InstanceId { get; set; } = Environment.MachineName;

    public static bool IsValid(InstanceOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.InstanceId)
            && options.InstanceId.Length <= UserPresence.MaximumInstanceIdLength;
    }
}
