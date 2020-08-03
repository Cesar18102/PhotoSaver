using System;
using System.IO;
using System.Linq;
using System.ComponentModel;

using Xamarin.Forms;

using Newtonsoft.Json;

namespace PhotoSaver
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private Camera Camera = new Camera();
        private IFileSystem FileSystem = DependencyService.Get<IFileSystem>();

        public MainPage()
        {
            InitializeComponent();
        }

        private bool IsStorageTypeSpecified()
        {
            return StorageType.SelectedIndex != -1;
        }

        private bool IsInternalStorage()
        {
            return StorageType.Items[StorageType.SelectedIndex] == Signs.InternalStorageType;
        }

        private void StorageType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirs.Children.Clear();

            if (IsInternalStorage())
                InternalStorageType.IsVisible = true;
            else
            {
                InternalStorageType.IsVisible = false;
                PushPicker();
            }
        }

        private void InternalStorageType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirs.Children.Clear();
            PushPicker();
        }

        private void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            Guid senderId = (sender as Element).Id;
            while (Dirs.Children.Count > 0 && !Dirs.Children.Last().Id.Equals(senderId))
                Dirs.Children.RemoveAt(Dirs.Children.Count - 1);
            PushPicker();
            UpdateFilename();
        }

        private void PushPicker()
        {
            Picker picker = new Picker();
            picker.SelectedIndexChanged += Picker_SelectedIndexChanged;
            Dirs.Children.Add(picker);
            UpdateLastPicker();
        }

        private async void UpdateLastPicker()
        {
            string path = GetPath();

            if (!Directory.Exists(path))
            {
                await DisplayAlert("Помилка", "Путь не існує", "ОК");
                Dirs.Children.RemoveAt(Dirs.Children.Count - 1);
                return;
            }

            string[] dirs = Directory.GetDirectories(path);

            Picker lastPicker = Dirs.Children.Last() as Picker;
            lastPicker.Items.Clear();

            foreach (string dir in dirs)
            {
                string folder = dir.Substring(dir.LastIndexOf('/') + 1);
                lastPicker.Items.Add(folder);
            }
        }

        private void UpdateFilename()
        {
            string path = GetPath();

            if (path == null || !Directory.Exists(path))
                return;

            int filesCount = Directory.GetFiles(path).Length;
            Filename.Text = $"{filesCount + 1}.png";
        }

        private async void CreateFolder_Clicked(object sender, EventArgs e)
        {
            string path = GetPath();

            string dir = await DisplayPromptAsync(
                "Створити нову папку",
                "Введіть ім'я папки"
            );

            if (dir == null)
                return;

            string fullPath = Path.Combine(path, dir);
            Directory.CreateDirectory(fullPath);

            UpdateLastPicker();
            (Dirs.Children.Last() as Picker).SelectedItem = dir;
        }

        private string GetPath()
        {
            if (!IsStorageTypeSpecified())
                return null;

            string path = null;

            if (IsInternalStorage())
            {
                Environment.SpecialFolder? folder = GetSpecialFolder();

                if (!folder.HasValue)
                    return null;

                path = Environment.GetFolderPath(folder.Value);
            }
            else
                path = FileSystem.GetExternalStoragePath();

            foreach(Picker picker in Dirs.Children)
            {
                if (picker.SelectedIndex == -1)
                    continue;

                path = Path.Combine(path, picker.Items[picker.SelectedIndex]);
            }

            return path;
        }

        private Environment.SpecialFolder? GetSpecialFolder()
        {
            if (InternalStorageType.SelectedIndex == -1)
                return null;

            string selected = InternalStorageType.Items[InternalStorageType.SelectedIndex];
            object parsed = Enum.Parse(typeof(Environment.SpecialFolder), selected);

            return (Environment.SpecialFolder)parsed;
        }

        private async void MakePhotoButton_Clicked(object sender, EventArgs e)
        {
            string path = GetPath();

            if (path == null)
            {
                await DisplayAlert("Помилка", "Некоректно задана путь", "ОК");
                return;
            }

            if (!Directory.Exists(path))
            {
                await DisplayAlert("Помилка", "Путь не існує", "ОК");
                return;
            }

            if (string.IsNullOrEmpty(Filename.Text))
            {
                await DisplayAlert("Помилка", "Некоректно задане ім'я файла", "ОК");
                return;
            }

            int count = 0;

            while (true)
            {
                using (Stream stream = await Camera.TakePhoto(Camera.CameraLocation.REAR))
                {
                    if (stream == null)
                        break;

                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);

                    string fullPath = Path.Combine(path, Filename.Text);
                    using (FileStream fs = File.Create(fullPath))
                        fs.Write(data, 0, data.Length);
                }

                ++count;
                UpdateFilename();
            }

            await DisplayAlert("Збережено", $"Збережено {count} файлів", "ОК");
        }

        public class State
        {
            public string Prefix { get; set; }
            public string Path { get; set; }
        }

        private static readonly string SAVE_PATH = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            "save.json"
        );

        public void SaveState()
        {
            string path = GetPath();

            if (path == null)
                return;

            State state = new State();

            if (IsInternalStorage())
            {
                Environment.SpecialFolder specialFolder = GetSpecialFolder().Value;
                state.Prefix = specialFolder.ToString();
                state.Path = path.Replace(Environment.GetFolderPath(specialFolder), "").Trim('/');
            }
            else
            {
                state.Prefix = FileSystem.GetExternalStoragePath();
                state.Path = path.Replace(state.Prefix, "").Trim('/');
            }

            string json = JsonConvert.SerializeObject(state);
            using (StreamWriter strw = File.CreateText(SAVE_PATH))
                strw.WriteLine(json);
        }

        public void LoadState()
        {
            if (!File.Exists(SAVE_PATH))
                return;

            string json = null;
            using (StreamReader str = new StreamReader(SAVE_PATH))
                json = str.ReadToEnd();

            if (json == null)
                return;

            State state = JsonConvert.DeserializeObject<State>(json);

            if(state.Prefix == FileSystem.GetExternalStoragePath())
                StorageType.SelectedItem = Signs.ExternalStorageType; //pushes pickers itself
            else
            {
                StorageType.SelectedItem = Signs.InternalStorageType;
                InternalStorageType.SelectedItem = state.Prefix; //pushes pickers itself
            }

            string[] dirs = state.Path.Split('/');
            foreach(string dir in dirs)
                (Dirs.Children.Last() as Picker).SelectedItem = dir; //pushes pickers itself
        }
    }
}