using System.Diagnostics.CodeAnalysis;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

using SFP_UI.ViewModels;

namespace SFP_UI
{
    public class ViewLocator : IDataTemplate
    {
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "From template")]
        public IControl Build(object data)
        {
            string? name = data.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            return type != null ? (Control)Activator.CreateInstance(type)! : (IControl)new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object data) => data is ViewModelBase;
    }
}
