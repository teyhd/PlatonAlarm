using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatonAlarm
{
    public class Lesson
    {
        private DateTime Timestart = DateTime.Now;
        private DateTime Timestop = DateTime.Now;
        private List<DateTime> TimestartArr = new List<DateTime>();
        public List<DateTime> TimestopArr = new List<DateTime>();
        public Lesson()
        {
            Console.WriteLine(1);
        }
        public void ReloadTime()
        {
            Timestart = DateTime.Now;
            Timestop = DateTime.Now;
            TimestartArr.Clear();
            TimestopArr.Clear();
        }
        public void AddTime(string Start, string Stop)
        {
            try
            {
                Timestart = DateTime.Parse(Start);
                Timestop = DateTime.Parse(Stop);
            }
            catch (Exception e)
            {
                //LogError(e, "Shape processing failed.");
              //  throw;
            }
            TimestartArr.Add(Timestart);
            TimestopArr.Add(Timestop);
        }
        public int GetLessNum()
        {
            int ans = 0;
            for (int i = 0; i < TimestartArr.Count; i++)
            {
                DateTime Test = DateTime.Now;
               // Test = DateTime.Parse("09:00");
               // Test = DateTime.Parse("11:22");//перемена
                int StartComp = DateTime.Compare(Test, TimestartArr[i]);
                int StopComp = -1;
                if (i > 0)
                {
                     StopComp = DateTime.Compare(Test, TimestopArr[i - 1]);
                }
                // -1 1 Перемена
                //-1 -1 Урок
                if (StartComp <= 0)
                {
                    Console.WriteLine("начало " + StartComp);
                    Console.WriteLine("Конец " + StopComp);
                    ans = i;
                    break;
                }
            }
            return ans;
        }
        public string[] GetText()
        {
            string[] ans = new string[3];
            DateTime Test = DateTime.Now;
            for (int i = 0; i < TimestartArr.Count; i++)
            {
              //  Console.WriteLine(Test);
                // Test = DateTime.Parse("14:00");
                // Test = DateTime.Parse("11:22");//перемена
                int StartComp = DateTime.Compare(Test, TimestartArr[i]);
                int StopComp = DateTime.Compare(Test, TimestopArr[i]);
                //Console.WriteLine($"Урок №{i + 1}" );
             //   Console.WriteLine("Начало " + StartComp);
               //Console.WriteLine("Конец " + StopComp);
                TimeSpan Ost;
                Ost = TimestartArr[i].AddDays(1).Subtract(Test);
                //Console.WriteLine(Ost.TotalSeconds);
                ans[0] = $"До начала рабочего дня";
                ans[1] = Ost.ToString(@"hh\:mm\:ss");
                ans[2] = ((int)((Ost.TotalSeconds * 100) / 83940)).ToString();
                
                if (StartComp < 0)
                {
                    Ost = TimestartArr[i].Subtract(Test);
                    if (i < 5)
                    {
                        ans[0] = $"До начала урока №{i + 1}";
                    }
                    else
                    {
                        ans[0] = $"До начала урока №{i}";
                    }
                    
                    ans[2] = ((int)((Ost.TotalSeconds * 100) / 32400)).ToString();
                    ans[1] = Ost.ToString(@"hh\:mm\:ss");
                    break;
                }

                if (StartComp >= 0 && StopComp <= 0)
                {
                    Ost = TimestopArr[i].Subtract(Test);
                    if (i < 5)
                    {
                        ans[0] = $"До конца урока №{i + 1}";
                    }
                    else
                    {
                        ans[0] = $"До конца урока №{i }";
                    }
                    ans[2] = ((int)((Ost.TotalSeconds * 100) / 2400)).ToString();
                    ans[1] = Ost.ToString(@"hh\:mm\:ss");
                    break;
                }
            }

           //Console.WriteLine(ans[0]);
            //Console.WriteLine(ans[1]);
            //Console.WriteLine(ans[2]);
            //Console.WriteLine(ans[3]);
            return ans;
        }
        public string[] UntilEnd()
        {
            string[] ans = new string[2];
            DateTime dateTime = TimestopArr[0];
            Console.WriteLine(GetLessNum());
            if (GetLessNum() - 1 >= 0)
            {
                dateTime = TimestopArr[GetLessNum() - 1];
            }
            if (GetLessNum() == 0)
            {
                ans[0] = "До начала урока №1";
                ans[1] = TimestartArr[0].Subtract(DateTime.Now).ToString(@"hh\:mm\:ss");
            }
            //  Console.WriteLine(TimestopArr[GetLessNum() - 1]);
            // return DateTime.Now.Subtract(dateTime);
            DateTime Test = DateTime.Now;
            Test = DateTime.Parse("09:05");
            //return dateTime.Subtract(Test);
            return ans;
        }
    }
}
