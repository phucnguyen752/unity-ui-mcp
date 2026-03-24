using System.Threading.Tasks;
using UnityEngine;

namespace UnityMCP
{
    public interface IAiApiClient
    {
        Task<string> AnalyzeImage(Texture2D texture, string prompt);
        Task<string> Chat(string userMessage);
    }
}
