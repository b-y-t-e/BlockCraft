namespace Blokolo.Core;

/// <summary>Po zmianie nazwy z BlockCraft na Blokolo: stare katalogi danych (światy i ustawienia w %APPDATA%\.blockcraft,
/// pobrane zasoby i sieć w %LOCALAPPDATA%\BlockCraft) przenoszone raz do nowych, jeśli nowych jeszcze nie ma.</summary>
public static class DataDirs
{
    public static void MigrateFromBlockCraft()
    {
        Move(Environment.SpecialFolder.ApplicationData, ".blockcraft", ".blokolo");
        Move(Environment.SpecialFolder.LocalApplicationData, "BlockCraft", "Blokolo");
    }

    private static void Move(Environment.SpecialFolder folder, string oldName, string newName)
    {
        try
        {
            string root = Environment.GetFolderPath(folder);
            string from = Path.Combine(root, oldName), to = Path.Combine(root, newName);
            if (!Directory.Exists(from) || Directory.Exists(to)) return;
            Directory.Move(from, to);
            Log.Info($"Dane przeniesione: {from} -> {to}");
        }
        catch (Exception ex) { Log.Error($"Przenoszenie danych {oldName} -> {newName}: {ex.Message}"); }
    }
}
