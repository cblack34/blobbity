// Not Blobbity.Game: a namespace of that name would shadow the Rules type Game for simple-name lookups inside Blobbity.Player.
namespace Blobbity.GamePackage
{
    internal static class PackageCompileProbe
    {
        internal static System.Type SessionType => typeof(Blobbity.App.ISession);
    }
}
