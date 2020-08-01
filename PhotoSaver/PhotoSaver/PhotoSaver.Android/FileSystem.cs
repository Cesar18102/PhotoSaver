using Android.OS;

using Xamarin.Forms;

using PhotoSaver.Droid;

[assembly : Dependency(typeof(FileSystem))]
namespace PhotoSaver.Droid
{
    public class FileSystem : IFileSystem
    {
        public string GetExternalStoragePath()
        {
            return Environment.ExternalStorageDirectory.AbsolutePath;
        }
    }
}