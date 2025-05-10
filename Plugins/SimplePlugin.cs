using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SemanticKernelPlayground.Plugins;

public sealed class SimplePlugin
{
    [KernelFunction, Description("Echoes back whatever you send in")]
    public string Echo(
        [Description("Text to echo")] string text)
    {
        return $"You said: {text}";
    }
}