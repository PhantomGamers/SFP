using System.Configuration;

using Bluegrams.Application;

namespace SFP.Models
{
    public class Settings : ApplicationSettingsBase
    {
        public Settings()
        {
            PortableJsonSettingsProvider.SettingsFileName = "SFP.config";
            PortableJsonSettingsProvider.ApplyProvider(SFP.Properties.Settings.Default);
        }
    }
}
