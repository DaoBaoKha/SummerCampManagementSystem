namespace SummerCampManagementSystem.Core.Enums
{
    public enum RegistrationStatus
    {
        PendingApproval = 1,    
        Approved = 2,          
        PendingPayment = 3,     
        Canceled = 4,           
        PendingCompletion = 5,  //after paid -> user complete regis by choosing activity and group
        PendingAssignGroup = 6,  //if dont choose group
        Completed = 7
    }
}
