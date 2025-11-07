namespace SummerCampManagementSystem.BLL.Interfaces
{
    /// <summary>
    /// singleton to load and cache prompt templates from embedded resources.
    /// </summary>
    public interface IPromptTemplateService
    {
        /// <summary>
        /// get template content by file name.
        /// </summary>
        /// <param name="templateName">file name </param>
        /// <returns>text content of prompt file</returns>
        string GetTemplate(string templateName);
    }
}