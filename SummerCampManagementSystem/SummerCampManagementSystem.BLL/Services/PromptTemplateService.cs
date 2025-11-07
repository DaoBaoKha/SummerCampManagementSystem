using SummerCampManagementSystem.BLL.Interfaces;
using System.Collections.Concurrent;
using System.Reflection;

namespace SummerCampManagementSystem.BLL.Services
{
    /// <summary>
    /// singleton to load and cache prompt templates from embedded resources.
    /// into memory for fast access.
    /// </summary>
    public class PromptTemplateService : IPromptTemplateService
    {
        // use ConcurrentDictionary for thread-safe access
        private readonly ConcurrentDictionary<string, string> _templateCache = new ConcurrentDictionary<string, string>();

        public PromptTemplateService()
        {
            LoadTemplatesFromResources();
        }

        private void LoadTemplatesFromResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            // folder namespace 
            var resourceNamespace = "SummerCampManagementSystem.BLL.PromptTemplates";

            // get all file resource in the namespace
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(resourceNamespace) && r.EndsWith(".txt"));

            foreach (var resourceName in resourceNames)
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) continue;
                    using (var reader = new StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        // get file name 
                        var templateName = resourceName
                            .Replace(resourceNamespace + ".", "")
                            .Replace(".txt", "");

                        _templateCache[templateName] = content;
                    }
                }
            }
        }

        public string GetTemplate(string templateName)
        {
            if (_templateCache.TryGetValue(templateName, out var templateContent))
            {
                return templateContent;
            }
            throw new KeyNotFoundException($"Không tìm thấy Prompt Template tên là '{templateName}'.");
        }
    }
}