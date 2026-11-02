// Исправленная версия кода для C# 7.3 и устранения ошибок компиляции

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace PlatonAlarm
{
    public partial class Form1 : Form
    {
        static readonly bool InSchool = true;
        static readonly string AppPath = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string MusicFolder = Path.Combine(AppPath, "1");
        static readonly string ScheduleFile = Path.Combine(AppPath, "0.txt");
        static readonly string WallpaperFolder = (System.Environment.GetEnvironmentVariable("PLATONALARM_PATH_1") ?? throw new System.InvalidOperationException("Set PLATONALARM_PATH_1"));
        static readonly string MemWallpaperFolder = (System.Environment.GetEnvironmentVariable("PLATONALARM_PATH_2") ?? throw new System.InvalidOperationException("Set PLATONALARM_PATH_2"));
        static readonly Color DefaultLabelColor = ColorTranslator.FromHtml("#1ABC9C");

        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;
        const int WM_CLOSE = 0x0010;

        public static List<string> ShMus = new List<string>();
        public static List<string> LongMus = new List<string>();

        static int wallpIndex = -1;
        static int memIndex = 0;

        public static Lesson Less = new Lesson();
        private static bool isPlaying = false;

        int clicked = 0;
        Random rng = new Random();
        int CountShort = 0;
        int CountLong = 0;

        public Form1()
        {
            InitializeComponent();
            InitForm();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
            this.Resize += Form1_Resize;
            this.Shown += (s, e) => AdjustLayout();
            button1.Click += button1_Click;
            label1.Click += label1_Click;
            label2.Click += label2_Click;
            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            listBox2.SelectedIndexChanged += listBox2_SelectedIndexChanged;
            progressBar1.Click += progressBar1_Click;
            timer1.Tick += timer1_Tick;
            timer2.Tick += timer2_Tick;
        }

        private void InitForm()
        {
            this.MaximizeBox = false;
            this.TransparencyKey = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = true;
            this.TopMost = true;

            Screen screen = Screen.FromPoint(Cursor.Position);
            this.Width = screen.Bounds.Width;
            this.Height = screen.Bounds.Height;

            AdjustLayout();

            if (InSchool)
            {
                progressBar1.Hide();
                button1.Hide();
            }
            progressBar1.Hide();
        }

        private void AdjustLayout()
        {
            Screen screen = Screen.FromPoint(Cursor.Position);
            this.Width = screen.Bounds.Width;
            this.Height = screen.Bounds.Height;

            progressBar1.Width = this.Width;
            progressBar1.Location = new Point(0, this.Height - progressBar1.Height - 50);

            int bottomPadding = IsWindows11() ? 7 : 4;
            float wid1 = IsWindows11() ? 0.15f : 0.20f;
            label1.Location = new Point((int)(this.Width * wid1), this.Height - label1.Height - bottomPadding);
            label2.Location = new Point((int)(this.Width * 0.7), this.Height - label2.Height - bottomPadding);
            button1.Location = new Point((int)(this.Width * 0.05), this.Height - button1.Height - bottomPadding);
        }

        private static bool IsWindows11()
        {
            try
            {
                var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                var buildStr = (string)reg.GetValue("CurrentBuild");
                int build = int.Parse(buildStr);
                return build >= 22000;
            }
            catch
            {
                return false;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            AdjustLayout();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                cp.ExStyle &= ~0x40000;
                return cp;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape || keyData == (Keys.Alt | Keys.F4))
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TopMost = true;
            InitTime();
            InitMelody();
            label1.ForeColor = DefaultLabelColor;
            label2.ForeColor = DefaultLabelColor;

            NativeMethods.SetWindowPos(
                this.Handle,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            NativeMethods.SetWindowPos(
                this.Handle,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
        }

        private static void InitTime()
        {
            if (!File.Exists(ScheduleFile)) return;
            var json = File.ReadAllText(ScheduleFile);
            dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            foreach (var item in data)
                Less.AddTime((string)item.start, (string)item.stop);
            timer1.Enabled = true;
        }

        private void InitMelody()
        {
            if (!Directory.Exists(MusicFolder)) return;
            foreach (var file in Directory.GetFiles(MusicFolder))
            {
                using (var audio = new AudioFileReader(file))
                {
                    var name = Path.GetFileName(file);
                    if (audio.TotalTime.TotalSeconds < 8)
                    {
                        ShMus.Add(name);
                        listBox1.Items.Add(name);
                    }
                    else
                    {
                        LongMus.Add(name);
                        listBox2.Items.Add(name);
                    }
                }
            }
            Console.WriteLine(LongMus.ToString());
        }

        private async Task PlayAsync(string path = "Salarm.mp3", float volume = 0.9f)
        {
            if (InSchool) { timer1.Enabled = true; return; }
            if (isPlaying) return;
            isPlaying = true;

            string fullPath = Path.Combine(MusicFolder, path);
            if (!File.Exists(fullPath)) { timer1.Enabled = true; return; }

            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                using (var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                {
                    float originalVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                    device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;

                    using (var audioFile = new AudioFileReader(fullPath))
                    using (var outputDevice = new WaveOutEvent())
                    {
                        var tcs = new TaskCompletionSource<object>();

                        outputDevice.Init(audioFile);
                        outputDevice.Volume = volume;
                        outputDevice.PlaybackStopped += (s, e) => tcs.TrySetResult(null);
                        outputDevice.Play();

                        // Ждём завершения воспроизведения
                        await tcs.Task;

                        // Восстанавливаем громкость
                        device.AudioEndpointVolume.MasterVolumeLevelScalar = originalVolume;

                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Ошибка воспроизведения: " + ex.Message); }
            finally { timer1.Enabled = true; isPlaying = false; }
        }
        public class AutoClosingMessageBox
        {
            System.Threading.Timer _timeoutTimer;
            string _caption;
            AutoClosingMessageBox(string text, string caption, int timeout)
            {
                _caption = caption;
                _timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
                    null, timeout, System.Threading.Timeout.Infinite);
                using (_timeoutTimer)
                    System.Windows.Forms.MessageBox.Show(text, caption);
            }
            public static void Show(string text, string caption, int timeout)
            {
                new AutoClosingMessageBox(text, caption, timeout);
            }
            void OnTimerElapsed(object state)
            {
                IntPtr mbWnd = FindWindow("#32770", _caption); // lpClassName is #32770 for MessageBox
                if (mbWnd != IntPtr.Zero)
                    SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _timeoutTimer.Dispose();
            }
            const int WM_CLOSE = 0x0010;
            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                var data = Less.GetText();
                if (data == null || data.Length < 4) return;

                label1.Text = data[0];
                label2.Text = data[1];
                int parsedVal;
                if (int.TryParse(data[2], out parsedVal))
                {
                    int value = Math.Max(0, Math.Min(100, 100 - parsedVal));
                    progressBar1.Value = value;
                }
                progressBar1.Text = data[1];

                string time = data[1];
                string type = data[3];

                if (time.Split(':')[1] == "20") RereadTime();
                else if (time == "00:10:00" && type == "t" && !InSchool)
                    NotifyUser("До конца урока осталось 10 минут!", "#4A90E2");
                else if (time == "00:05:00" && type == "t")
                    NotifyUser("Осталось 5 минут! Сохраните свои работы!", "#FFB74D");
                else if (time == "00:01:00")
                {
                    NotifyUser("Через минуту конец урока", "#1ABC9C");
                    Longsong();
                }
            }
            catch (Exception ex) { Console.WriteLine("Ошибка таймера: " + ex.Message); }
        }

        private void NotifyUser(string message, string color)
        {
            timer1.Enabled = false;
            AutoClosingMessageBox.Show(message, "Внимание!", 3000);
            label1.ForeColor = ColorTranslator.FromHtml(color);
            label2.ForeColor = ColorTranslator.FromHtml(color);
            timer1.Enabled = true;
        }

        public static void RereadTime() { timer1.Enabled = false; Less.ReloadTime(); InitTime(); }
        private void ShortSong() { if (ShMus.Count == 0) return; if (CountShort >= ShMus.Count) CountShort = 0; _ = PlayAsync(ShMus[CountShort++]); }
        private void Longsong() { if (LongMus.Count == 0) return; if (CountLong >= LongMus.Count) CountLong = 0; _ = PlayAsync(LongMus[CountLong++]); }

        private void label1_Click(object sender, EventArgs e) { if (CountShort > 0) CountShort--; if (CountLong > 0) CountLong--; }
        private void label2_Click(object sender, EventArgs e) => Longsong();
        private void button1_Click(object sender, EventArgs e)
        {
            clicked++;
            button1.Text = clicked.ToString();
            int bottomPadding = IsWindows11() ? 7 : 5;
            button1.Location = new Point(rng.Next(10, this.Width - 100), this.Height - button1.Height - bottomPadding);
            if (clicked % 10 == 0) { SetWallpaper(true); Task.Delay(3300).ContinueWith(_ => SetWallpaper(false)); }
            if (clicked == 1005) AutoClosingMessageBox.Show("Ты победил!!! Покажи сообщение учителю, получи 5 по информатике", "Победа!", 3000);
        }
        private void progressBar1_Click(object sender, EventArgs e) => ShortSong();
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) { if (listBox1.SelectedItem != null) _ = PlayAsync(listBox1.SelectedItem.ToString()); }
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e) { if (listBox2.SelectedItem != null) _ = PlayAsync(listBox2.SelectedItem.ToString()); }

        public static void SetWallpaper(bool useMem = false)
        {
            try
            {
                string dir = useMem ? MemWallpaperFolder : WallpaperFolder;
                if (!Directory.Exists(dir)) { Console.WriteLine("Нет доступа к " + dir); return; }
                var files = Directory.GetFiles(dir, "*.jpg");
                if (files.Length == 0) return;
                string file = useMem ? files[memIndex++ % files.Length] : files[++wallpIndex % files.Length];
                RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true);
                key.SetValue("WallpaperStyle", "2");
                key.SetValue("TileWallpaper", "0");
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, file, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
            }
            catch (Exception ex) { Console.WriteLine("Ошибка смены обоев: " + ex.Message); }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
    }
}
