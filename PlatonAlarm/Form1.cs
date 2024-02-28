using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using System.Drawing;
using System.Windows;
using Microsoft.Win32;
using System.Collections.Generic;

namespace PlatonAlarm
{
    public partial class Form1 : Form
    {
        static string AppPath = @"C:\Windows\secur\1";
        [DllImport("winmm.dll")]
        static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);
        public Lesson Less = new Lesson();
        public Form1()
        {
            InitializeComponent();
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.TransparencyKey = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
            Screen screen = Screen.FromPoint(Cursor.Position); // Получаем текущий экран
            //this.StartPosition = FormStartPosition.Manual; // Устанавливаем ручное позиционирование формы
            //this.Location = new Point(100, screen.WorkingArea.Height- label1.Height); // Устанавливаем позицию формы в правом верхнем углу экрана
            Console.WriteLine((screen.WorkingArea.Width - this.Width - 110).ToString());
            Console.WriteLine( screen.WorkingArea.Width);
            TopMost = true;
           //this.Height = label1.Height + label2.Height + progressBar1.Height;
            this.Height = screen.Bounds.Height;
            this.Width = screen.Bounds.Width;
            progressBar1.Width = screen.WorkingArea.Width;
            if (IsWindows11())
            {
                progressBar1.Location = new Point(0, this.Height - progressBar1.Height - 45);
                label1.Location = new Point(130, this.Height - label1.Height - 7);
                label2.Location = new Point(this.Width - 450, this.Height - label2.Height);
            }
            else
            {
                progressBar1.Location = new Point(0, this.Height - progressBar1.Height - 40);
                label1.Location = new Point(this.Width - label2.Width - 570, this.Height - label1.Height - 6);
                label2.Location = new Point(this.Width - 450, this.Height - label2.Height);
            }

            Console.WriteLine(IsWindows11());
            // label1.Width = progressBar1.Width;
            //label2.Width = progressBar1.Width;
        }
        public static bool IsWindows11()
        {
            var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

            var currentBuildStr = (string)reg.GetValue("CurrentBuild");
            var currentBuild = int.Parse(currentBuildStr);

            return currentBuild >= 22000;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            TopMost = true;
            InitTime();
            InitMelody();
           // AutoClosingMessageBox.Show("До конца урока осталось 10 минут!", "Внимание!", 10000);
            Play("gary-meow meloboom.mp3");
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        public static List<string> ShMus = new List<string>();
        public static List<string> LongMus = new List<string>();

        void InitMelody()
        {
            string Path = $"{AppPath}\\1";
            foreach (string fileName in Directory.GetFiles(Path))
            {
                
                using (var audioFile = new AudioFileReader(fileName))
                {
                    // Console.WriteLine(audioFile.Length);
                    string[] temp = fileName.Split('\\');
                   if (audioFile.TotalTime.TotalSeconds < 8)
                    {
                        Console.WriteLine(temp[temp.Length-1]);
                        Console.WriteLine(audioFile.TotalTime);
                        ShMus.Add(temp[temp.Length-1]);
                        listBox1.Items.Add(temp[temp.Length - 1]);
                    } else
                    {
                        LongMus.Add(temp[temp.Length - 1]);
                        Console.WriteLine(temp[temp.Length - 1]);
                        Console.WriteLine(audioFile.TotalTime);
                        listBox2.Items.Add(temp[temp.Length - 1]);
                    }
                    
                };
            }

        }
        Boolean flagmus = true;
        async void Play(string Path = "alarm.mp3",float Newval = 1.0f)
        {
            flagmus = false;
            Path = $"{AppPath}\\1\\{Path}";
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                float volumeLevel = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                device.AudioEndpointVolume.MasterVolumeLevelScalar = Newval;
                keybd_event((byte)Keys.VolumeUp, 0, 0, 0); // increase volume
                using (var audioFile = new AudioFileReader(Path))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Stop();
                    outputDevice.Init(audioFile);
                   // outputDevice.Volume = 1;
                    outputDevice.Play();
                    await Task.Delay(5);
                    flagmus = true;
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        if (flagmus==false) outputDevice.Stop();
                        //Console.WriteLine(outputDevice.GetPosition());
                        await Task.Delay(1);
                    }
                    
                }
                device.AudioEndpointVolume.MasterVolumeLevelScalar = volumeLevel;
                timer1.Enabled = true;
            }
            catch (Exception e)
            {
                timer1.Enabled = true;
                //LogError(e, "Shape processing failed.");
                //throw;
            }
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {
            // timer1.Enabled = true;
            //PlayMelody();
            ShortSong();

        }

        static void PlayMelody()
        {
            int[,] melody = new int[,]
            {
            { 659, 200 }, // E
            { 659, 200 }, // E
            { 659, 200 }, // E
            { 523, 200 }, // C
            { 659, 200 }, // E
            { 783, 400 }, // G
            { 392, 400 }, // G
            };

            // Воспроизведение мелодии
            for (int i = 0; i < melody.GetLength(0); i++)
            {
                Console.Beep(melody[i, 0], melody[i, 1]+150);
             //   Thread.Sleep(melody[i, 1]);
                Console.WriteLine(i);
            }
        }

        private void InitTime(string filePath = "0.txt")
        {
            filePath = $"{AppPath}\\{filePath}";
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                dynamic jsonObject = JsonConvert.DeserializeObject(jsonContent);
                foreach (var item in jsonObject)
                {
                   //Console.WriteLine((string)item.start);
                    Less.AddTime((string)item.start, (string)item.stop);
                }
                timer1.Enabled = true;
            }
            catch (Exception e)
            {
                //LogError(e, "Shape processing failed.");
                //throw;
            }

        }
        int CountShort = 0;
        int CountLong = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            TopMost = true;
            try
            {
                label1.Text = Less.GetText()[0];
                label2.Text = Less.GetText()[1];
                progressBar1.Text = Less.GetText()[1];
                progressBar1.Value = 100 - int.Parse(Less.GetText()[2]);
                if (Less.GetText()[1].Split(':')[1] == "20")
                {
                    timer1.Enabled = false;
                    Less.ReloadTime();
                    InitTime();
                }
                if (Less.GetText()[1] == "00:10:00")
                {
                    //PlayMelody();
                    timer1.Enabled = false;
                    Play("ay-ya-ya meloboom.mp3");
                    AutoClosingMessageBox.Show("До конца урока осталось 10 минут!", "Внимание!", 10000);
                    //timer1.Enabled = false;
                }
                if (Less.GetText()[1] == "00:05:00")
                {
                    timer1.Enabled = false;
                    ShortSong();
                    AutoClosingMessageBox.Show("Осталось 5 минут! Сохраните свои работы!", "Внимание!", 10000);
                    //timer1.Enabled = true;
                }
                if (Less.GetText()[1] == "00:01:00")
                {
                    timer1.Enabled = false;
                    Longsong();
                    AutoClosingMessageBox.Show("Конец урока! Комьютеры заблокируются через 10 секнуд", "Внимание!", 10000);
                    //timer1.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                //LogError(e, "Shape processing failed.");
               // throw;
            }           
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
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

        private void label1_Click(object sender, EventArgs e)
        {
            if (CountShort >= 0) {
                CountShort--;
            }
            if (CountLong >= 0)
            {
                CountLong--;
            }
         
        }

        void Longsong()
        {
            if (CountLong >= LongMus.Count)
            {
                CountLong = 0;
                Play("Salarm.mp3");
            }
            else
            {
                Play(LongMus[CountLong]);
                CountLong++;
            }
        } 

        void ShortSong()
        {
            if (CountShort >= ShMus.Count)
            {
                CountShort = 0;
                Play("Salarm.mp3");
            }
            else
            {
                Play(ShMus[CountShort]);
                CountShort++;
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
            Longsong();
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioEndpointVolume.MasterVolumeLevelScalar = 0.2f;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string curItem = listBox1.SelectedItem.ToString();
            Play(curItem);
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string curItem = listBox2.SelectedItem.ToString();
            Play(curItem,0.3f);
        }
    }
}
