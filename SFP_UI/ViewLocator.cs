#region

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SFP_UI.ViewModels;

#endregion

namespace SFP_UI;

public class ViewLocator : IDataTemplate
{
    public Control Build(object data)
    {
        string name = data.GetType().FullName!.Replace("ViewModel", "View");
        Type? type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
