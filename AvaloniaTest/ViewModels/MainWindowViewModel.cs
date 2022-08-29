using Avalonia.Threading;
using AvaloniaTest.Views;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ReactiveUI;
using ScreenShotAvalonia.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AvaloniaTest.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static readonly HttpClient client = new HttpClient();
        private string _startDate;

        public string StartDate
        {
            get => _startDate;
            set => Set(ref _startDate, value);
        }
        private string _endDate;

        public string EndDate
        {
            get => _endDate;
            set => Set(ref _endDate, value);
        }

        private Avalonia.Media.Imaging.Bitmap _image;

        public Avalonia.Media.Imaging.Bitmap Images 
        {
            get => _image;
            set => this.RaiseAndSetIfChanged(ref _image, value);
        }

        private List<Avalonia.Media.Imaging.Bitmap> _getImage;

        public List<Avalonia.Media.Imaging.Bitmap> GetImage
        {
            get => _getImage;
            set => this.RaiseAndSetIfChanged(ref _getImage, value);
        }

        AppDbContext db = new AppDbContext();

        public MainWindowViewModel()
        {
            db.Database.EnsureCreated();
            OpenWindowCommand = ReactiveCommand.Create(OpenScreenWindow);
            ScreenShotCommand = ReactiveCommand.Create(ScreenShot);
        }

        public ReactiveCommand<Unit, Unit> OpenWindowCommand { get; private set; }

        private void OpenScreenWindow()
        {
            GetScreenShotAsync(Convert.ToDateTime(_startDate), Convert.ToDateTime(_endDate));
            
            GetImage = GetListImage();
        }


        public ReactiveCommand<Unit, Unit> ScreenShotCommand { get; private set; }

        private void ScreenShot()
        {
            Images = CaptureScreen(); 
        }


        private List<Avalonia.Media.Imaging.Bitmap> GetListImage()
        {
            var ListImage = new List<Avalonia.Media.Imaging.Bitmap>();
            foreach (var img in db.Images)
            {
                var ConvertStringToByte = Convert.FromBase64String(img.ScreenShot.Trim(new char[] {'"'}));
                ListImage.Add(Byte64ToImg(ConvertStringToByte));
            }
            return ListImage;
        }

        


        public async void GetScreenShotAsync(DateTime startDate, DateTime endDate)
        {
            await Dispatcher.UIThread.InvokeAsync(async() =>
            {
                if (db.Images == null)
                {
                    using var response = await client.GetAsync($@"http://45.84.226.180/GetScreenshots?{startDate}&{endDate}").ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    var s = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var img = JsonConvert.DeserializeObject<List<ScreenShotAvalonia.Model.Image>>(s);
                    await db.AddRangeAsync(img);
                    await db.SaveChangesAsync();
                }
                
            });
            
        }

        private static async void PostScreenShot(byte[] file)
        {
            using (var content = new ByteArrayContent(file))
            {
                using (MultipartFormDataContent mpfdc = new MultipartFormDataContent())
                {
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    mpfdc.Add(content, "file", "image");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                    using var response = await client.PostAsync("http://45.84.226.180/UploadScreenshot/", mpfdc).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                }
            }
        }


        public static Avalonia.Media.Imaging.Bitmap CaptureScreen()
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(new System.Drawing.Point(0, 0),
                new System.Drawing.Size(Screen.PrimaryScreen.Bounds.Width,
                    Screen.PrimaryScreen.Bounds.Height));

            return CaptureRect(rect, System.Drawing.Imaging.ImageFormat.Png);
        }

        public static Avalonia.Media.Imaging.Bitmap CaptureRect(System.Drawing.Rectangle rect, ImageFormat format)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(rect.Width, rect.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                {
                    using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, System.Drawing.CopyPixelOperation.SourceCopy);
                    }
                    bitmap.Save(ms, format);
                }
                var convertToBase64 = Convert.ToBase64String(ms.GetBuffer());
                PostScreenShot(ms.GetBuffer());
                return Byte64ToImg(ms.GetBuffer());
            }
        }

        private static Avalonia.Media.Imaging.Bitmap ConvertToAvaloniaBitmap(System.Drawing.Image bitmap)
        {
            if (bitmap == null)
                return null;
            System.Drawing.Bitmap bitmapTmp = new System.Drawing.Bitmap(bitmap);
            var bitmapdata = bitmapTmp.LockBits(new Rectangle(0, 0, bitmapTmp.Width, bitmapTmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Avalonia.Media.Imaging.Bitmap bitmap1 = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul,
                bitmapdata.Scan0,
                new Avalonia.PixelSize(bitmapdata.Width, bitmapdata.Height),
                new Avalonia.Vector(96, 96),
                bitmapdata.Stride);
            bitmapTmp.UnlockBits(bitmapdata);
            bitmapTmp.Dispose();
            return bitmap1;
        }

        private static Avalonia.Media.Imaging.Bitmap Byte64ToImg(byte[] img)
        {
            System.Drawing.Image image;
            using (MemoryStream ms = new MemoryStream(img))
            {
                ms.Write(img);
                image = System.Drawing.Image.FromStream(ms);
            }
            return ConvertToAvaloniaBitmap(image);
        }
    }
}
