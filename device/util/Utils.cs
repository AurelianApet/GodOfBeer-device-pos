using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GodOfBeer.util
{
    public class Utils
    {
        //public static DualMoniter dualmoniter = new DualMoniter();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]

        public static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool lpSystemInfo);
        //C# 64bit OS runtime check
        public static bool Is64Bit()
        {
            bool retVal;

            IsWow64Process(Process.GetCurrentProcess().Handle, out retVal);

            return retVal;
        }
        static CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("ko-KR");
        internal static string applicationPath = /*Application.StartupPath + */@"\";
        static Dictionary<int, string> unit = new Dictionary<int, string>() {
            {0,"병" },
            {1,"ml" },
            {2,"개" }
        };
        public static string GetUnit(int? key)
        {
            if (key == null)
                return "";
            int key_ = (int)key;
            if(unit.ContainsKey(key_))
                return unit[key_];
            return "";
        }
        static Dictionary<DayOfWeek, string> dayOfWeek = new Dictionary<DayOfWeek, string>() {
            { DayOfWeek.Sunday, "일"},
            { DayOfWeek.Monday, "월"},
            { DayOfWeek.Tuesday, "화"},
            { DayOfWeek.Wednesday, "수"},
            { DayOfWeek.Thursday, "목"},
            { DayOfWeek.Friday, "금"},
            { DayOfWeek.Saturday, "토"}
        };
        public static string GetDayOfWeekString(DayOfWeek dow)
        {
            return dayOfWeek[dow];
        }
        public static DateTime Delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                //System.Windows.Forms.Application.DoEvents();
                ThisMoment = DateTime.Now;
            }

            return DateTime.Now;
        }
        public static void InitNumberInfo()
        {
            //cultureInfo.NumberFormat.CurrencyPositivePattern = 1; // 기호 후위표시
            //cultureInfo.NumberFormat.CurrencyNegativePattern = 5; // 기호 후위표시
        }
        public static string ToMoneyString(int? data)
        {
            if (data == null)
                return 0.ToString("C", cultureInfo);
            int data_ = (int)data;
            return data_.ToString("C", cultureInfo);
        }

        public static string ToMoneyString(decimal? data)
        {
            if (data == null)
                return 0.ToString("C", cultureInfo);
            int data_ = (int)data;
            return data_.ToString("C", cultureInfo);
        }
        public static string ToMoneyString(float? data)
        {
            if (data == null)
                return 0.ToString("C", cultureInfo);
            int data_ = (int)data;
            return data_.ToString("C", cultureInfo);
        }
        public static string ToMoneyStringN(int? data)
        {
            if (data == null)
                return "";
            int data_ = (int)data;
            return data_.ToString("#,###");
        }
        public static string ToMoneyStringN(decimal? data)
        {
            if (data == null)
                return "";
            int data_ = (int)data;
            return data_.ToString("#,###");
        }
        public static string ToMoneyStringN0(decimal? data)
        {
            if (data == null)
                return "0";
            int data_ = (int)data;
            return data_.ToString("#,##0");
        }
        public static string ToMoneyStringN0(int? data)
        {
            if (data == null)
                return "0";
            int data_ = (int)data;
            return data_.ToString("#,##0");
        }
        public static string ToMoneyStringN0(float? data)
        {
            if (data == null)
                return "";
            int data_ = (int)data;
            return data_.ToString("#,###.00");
        }
        public static string ToMoneyStringN(float? data)
        {
            if (data == null)
                return "";
            int data_ = (int)data;
            return data_.ToString("#,##0.00");
        }
        public static decimal FromMoneyString(string data)
        {
            decimal result = 0;

            if (!decimal.TryParse(data, NumberStyles.Currency, cultureInfo, out result))
            {
                result = 0;
            }

            return result;
        }

        public static bool IsHHMM(string time)
        {
            return Regex.IsMatch(time, @"^(0[0-9]|1[0-9]|2[0123]):([0-5][0-9])$");
        }

        public static string GetMysqlDatetimeString(DateTime? datetime)
        {
            if (datetime == null)
                return null;

            return GetMysqlDatetimeString((DateTime)datetime);
        }

        public static string GetMysqlDatetimeString(DateTime datetime)
        {
            return datetime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string GetHHMMByDateTime(DateTime datetime)
        {
            return datetime.ToString("t", DateTimeFormatInfo.InvariantInfo);
        }

        public static string GetHHMMByDateTime(DateTime? datetime)
        {
            if(datetime == null)
                return  "";

            return ((DateTime)datetime).ToString("t", DateTimeFormatInfo.InvariantInfo);
        }

        public static DateTime GetDateTimeByHHMM(string time)
        {
            return DateTime.ParseExact(time, "H:mm", null, DateTimeStyles.None);
        }

        public static bool IsDigit(char ch)
        {
            return char.IsDigit(ch);
        }

        public static bool IsAlpha(char ch)
        {
            string str = ch.ToString().ToLower();
            switch (str[0])
            {
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                    return true;
                default:
                    return false;
            }
        }

             
        
        public static byte[] ToByteArray(object source)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, source);
                return stream.ToArray();
            }
        }
        
        public static object ToObject(byte[] source)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(source))
            {
                return formatter.Deserialize(stream);
                //return stream.ToArray();
            }
        }
        /*
        public static string ToByte64String(object source)
        {
            return Convert.ToBase64String(ToByteArray(source));
        }

        public static object FromByte64String(string source)
        {
            return ToObject(Convert.FromBase64String(source));
        }
        //*/
        public static string GetString(object obj)
        {
            StringBuilder sb = new StringBuilder();

            var properties = obj.GetType().GetProperties();
            foreach(var property in properties)
            {
                string name = property.Name;
                string value = property.GetValue(obj)?.ToString() ?? "null";

                sb.Append(name);
                sb.Append(" : ");
                sb.Append(value);
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        public static string Compress(string source)
        {
            byte[] sourceArray = Encoding.UTF8.GetBytes(source);
            MemoryStream memoryStream = new MemoryStream();
            using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(sourceArray, 0, sourceArray.Length);
            }
            memoryStream.Position = 0;
            byte[] temporaryArray = new byte[memoryStream.Length];
            memoryStream.Read(temporaryArray, 0, temporaryArray.Length);
            byte[] targetArray = new byte[temporaryArray.Length + 4];
            Buffer.BlockCopy(temporaryArray, 0, targetArray, 4, temporaryArray.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(sourceArray.Length), 0, targetArray, 0, 4);
            return Convert.ToBase64String(targetArray);
        }

        public static string Decompress(string source)
        {
            byte[] sourceArray = Convert.FromBase64String(source);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(sourceArray, 0);
                memoryStream.Write(sourceArray, 4, sourceArray.Length - 4);
                byte[] targetArray = new byte[dataLength];
                memoryStream.Position = 0;

                using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gZipStream.Read(targetArray, 0, targetArray.Length);
                }
                return Encoding.UTF8.GetString(targetArray);
            }
            //return (source);
        }

        public static string ReplaceEx(string str, string oriStr, string newStr)
        {
            string temp = str;
            while (temp.IndexOf(oriStr) > -1)
            {
                int index = temp.IndexOf(oriStr);
                temp = temp.Remove(index, oriStr.Length);
                temp = temp.Insert(index, newStr);
            }
            return temp;
        }
    }
}
