namespace TAS.Common.Interfaces.Security
{
    public interface IGroup: ISecurityObject
    {
        string Name { get; set; }
    }
}
