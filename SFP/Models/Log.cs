#region

using NLog;

#endregion

namespace SFP.Models;

public static class Log
{
    public static readonly Logger Logger = LogManager.GetCurrentClassLogger();
}