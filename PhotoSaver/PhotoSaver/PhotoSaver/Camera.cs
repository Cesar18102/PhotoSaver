using System;
using System.IO;
using System.Threading.Tasks;

using Plugin.Media;
using Plugin.Media.Abstractions;

namespace PhotoSaver
{
    public class Camera
    {
        public enum CameraLocation
        {
            FRONT,
            REAR
        };

        public async Task<Stream> TakePhoto(CameraLocation cameraLocation, int quality = 50)
        {
            try
            {
                StoreCameraMediaOptions cameraOptions = new StoreCameraMediaOptions();

                cameraOptions.AllowCropping = true;
                cameraOptions.PhotoSize = PhotoSize.MaxWidthHeight;
                cameraOptions.CompressionQuality = Math.Min(Math.Max(quality, 0), 100);
                cameraOptions.DefaultCamera = cameraLocation == CameraLocation.FRONT ? CameraDevice.Front : CameraDevice.Rear;

                MediaFile file = await CrossMedia.Current.TakePhotoAsync(cameraOptions);

                return file == null ? null : file.GetStream();
            }
            catch (MediaPermissionException ex) { return null; }
        }
    }
}
