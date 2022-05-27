using System.Configuration;

using Bluegrams.Application;

namespace SFP
{
    public class SettingsModel : ApplicationSettingsBase
    {
        public SettingsModel()
        {
            PortableJsonSettingsProvider.SettingsFileName = "SFP.config";
            PortableJsonSettingsProvider.ApplyProvider(SFP.Properties.Settings.Default);
        }
    }
}
