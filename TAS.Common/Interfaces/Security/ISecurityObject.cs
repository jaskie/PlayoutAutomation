namespace TAS.Common.Interfaces.Security
{
    /// <summary>
    /// base interface for IUser and IGroup
    /// </summary>
    public interface ISecurityObject: IPersistent
    {
        SecurityObjectType SecurityObjectTypeType { get; }
    }
}
