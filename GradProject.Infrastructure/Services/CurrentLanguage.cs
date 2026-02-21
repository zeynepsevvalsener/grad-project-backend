using GradProject.Application.Interfaces;

namespace GradProject.Infrastructure.Services
{
    public class CurrentLanguage : ICurrentLanguage
    {
        public string Value { get; set; } = "en";
    }
}