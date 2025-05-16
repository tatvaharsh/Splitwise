namespace SplitWise.Service.Interface
{
    public interface IAppContextService
    {
        string GetBaseURL();
        Guid? GetUserId();
    }
}