using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using IniParser;
using UsersCollector.Domain;
using VkApiNet;
using System.Windows.Forms;
using VkApiNet.Vk;

namespace UsersCollector
{
    internal class Program
    {
        private static DbManger s_db;
        private static int s_appId;
        private const int RES_COUNT = 1000;

        [STAThread]
        private static void Main(string[] args)
        {
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            Console.CursorVisible = false;
            Console.Title = "VK";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            log4net.Config.XmlConfigurator.Configure();

            Status("Загрузка...");
            try
            {
                s_db = DbManger.GetInstance();
                ParseArguments(args);
                ReadConfiguration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));
            }
            catch (Exception e)
            {
                Error(e.Message);
                return;
            }

            Status("Авторизация...");
            var af = new AuthForm(s_appId);
            if (af.ShowDialog() != DialogResult.OK)
            {
                Error("Для работы нужно авторизоваться в сети!");
                return;
            }

            var usrManager = new UsersManager(af.GetAccessToken());
            try
            {
                var currentuser = usrManager.GetUserById(af.GetTokenUserId());
                Console.Title = String.Format("AppID: {0} - {1}", s_appId, currentuser.GetFullName());
            }
            catch (Exception e)
            {
                Error(e.Message);
                return;
            }

            var offset = 0;
            var random = new Random();
            var query = new VkUsersSearchQuery();
            query.SetParameter(UserSearchParam.HomeTown, "Снежинск");
            query.SetParameter(UserSearchParam.Count, RES_COUNT);
            query.SetParameter(UserSearchParam.Fields,
                               new List<VkProfile.EntryType> {VkProfile.EntryType.Sex, VkProfile.EntryType.Contacts});

            Status("Выполнение...");

            for (int age = 10; age < 60; age++)
            {
                query.SetParameter(UserSearchParam.AgeFrom, age);
                query.SetParameter(UserSearchParam.AgeTo, age);
                offset = 0;
                while (true)
                {
                    query.SetParameter(UserSearchParam.Offset, offset);
                    List<VkUser> result;
                    try
                    {
                        result = usrManager.Search(query);
                    }
                    catch (Exception e)
                    {
                        Error(e.Message);
                        break;
                    }
                    if (result.Count == 0) break;

                    foreach (VkUser user in result)
                    {
                        Status("({0}) {1}... ", user.Id, user.GetFullName());
                        using (var session = s_db.OpenSession())
                        {
                            using (var t = session.BeginTransaction())
                            {
                                try
                                {
                                    session.Save(new User
                                        {
                                            FirstName = user.FirstName,
                                            LastName = user.LastName,
                                            VkId = user.Id,
                                            Sex = user.Sex,
                                            HomePhone = user.HomePhone,
                                            MobilePhone = user.MobilePhone,
                                            BirthYear = user.BirthYear
                                        });
                                }
                                catch (Exception e)
                                {
                                    if (e.InnerException != null &&
                                        e.InnerException is System.Data.SqlClient.SqlException)
                                    {
                                        if (
                                            !(e.InnerException as System.Data.SqlClient.SqlException).Message.Contains(
                                                "Violation of UNIQUE KEY"))
                                        {
                                            Error(e.Message);
                                        }
                                        else
                                        {
                                            Error("({0}) {1}: Уже сохранено!", user.Id, user.GetFullName());
                                        }
                                    }
                                    continue;
                                }
                                t.Commit();
                                Print("({0}) {1}: Сохранено", user.Id, user.GetFullName());
                            }
                        }
                    }
                    offset += result.Count;

                    // sleep 1 - 20 seconds
                    var secs = random.Next(1000, 20000);
                    Status("Ожидание {0} сек.", secs/1000);
                    Thread.Sleep(secs);
                }

                var asecs = random.Next(1000, 20000);
                Status("Ожидание {0} сек.", asecs/1000);
                Thread.Sleep(asecs);
            }

            Status("Готово");
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs consoleCancelEventArgs)
        {
            Application.Exit();
        }

        private static void ReadConfiguration(string cfgFile)
        {
            var ini = new FileIniDataParser();
            var data = ini.LoadFile(cfgFile);

            try
            {
                s_appId = Int32.Parse(data["VKApplication"]["id"]);
            }
            catch (Exception)
            {
                throw new AppConfigurationException("VKApplication.id");
            }
        }

        static void ParseArguments(string[] args)
        {
            foreach (var s in args)
            {
                if (String.Compare(s, "-setup", true) == 0)
                {
                    try
                    {
                        s_db.SetupContext();
                        Print("Completed!");
                    }
                    catch (Exception e)
                    {
                        Error(e.Message);
                    }
                    throw new ApplicationException("Need restart!");
                }
            }
        }
        static void Print(string msg, params object[] data)
        {
           // if (Console.CursorTop == 0) Console.CursorTop = 2;

            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(msg, data);
            Console.ForegroundColor = oldColor;
        }
        static void Status(string msg, params object[] data)
        {
            //var oldTop = Console.CursorTop;
            //var oldLeft = Console.CursorLeft;

            //Console.CursorTop = 0;
            //Console.CursorLeft = 0;

            Console.WriteLine(msg, data);

            //Console.CursorTop = oldTop;
            //Console.CursorLeft = oldLeft;
        }
        static void Error(string msg, params object[] data)
        {
            //if (Console.CursorTop == 0) Console.CursorTop = 2;

            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(msg, data);
            Console.ForegroundColor = oldColor;
        }
    }
    

    internal class AppConfigurationException : Exception
    {
        public AppConfigurationException(string paramName)
            : base("Invalid or missing configuration parameter: " + paramName)
        {

        }
    }
}
