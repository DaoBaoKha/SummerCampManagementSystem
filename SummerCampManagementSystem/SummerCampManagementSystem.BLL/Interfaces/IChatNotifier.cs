namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IChatNotifier
    {
        Task SendMessageToGroupAsync(string groupName, object message);
    }
}