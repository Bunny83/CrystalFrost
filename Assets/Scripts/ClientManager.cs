using OpenMetaverse;
using CrystalFrost;

public static class ClientManager
{
    public static GridClient client;
    public static TexturePipeline texturePipeline;
    public static bool active = false;
    public static AssetManager assetManager;
    public static int mainThreadId;

    // In Main method:

// If called in the non main thread, will return false;
    public static bool IsMainThread
    {
        get { return System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId; }
    }
}
