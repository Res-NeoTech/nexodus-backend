namespace NexodusAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class BypassProxyValidationAttribute : Attribute
    {

    }
}
