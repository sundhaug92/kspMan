using HtmlAgilityPack;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace kspMan
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            do
            {
                if (!Directory.Exists("tmp")) Directory.CreateDirectory("tmp");
                Console.WriteLine("Enter path to GameData");
                GameDataDirectory = Console.ReadLine();
            } while (GameDataDirectory == "");
            int modNr = 0;
            while (true)
            {
                Console.WriteLine("Enter addon ID number (0 to exit), spaceport-URL or URL");
                var modStr = Console.ReadLine();
                try
                {
                    if (modStr.Contains("kerbalspaceport"))
                    {
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(new WebClient().DownloadString(modStr));
                        modNr = doc.GetElementbyId("addonid").GetAttributeValue("value", 0);
                    }
                    else if (modStr.Contains("://"))
                    {
                        new WebClient().DownloadFile(modStr, "tmp/" + modStr.Substring(modStr.LastIndexOf('/') + 1));
                        InstallFile("tmp/" + modStr.Substring(modStr.LastIndexOf('/') + 1));
                        continue;
                    }
                    else modNr = int.Parse(modStr);
                    if (modNr <= 0) break;
                    Install(modNr);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Not a number or not an URL");
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("Addon ID not found");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            if (Directory.Exists("tmp"))
            {
                Console.WriteLine("Cleaning tmp");
                Directory.Delete("tmp", true);
            }
        }

        private static void Install(int p)
        {
            var file = Download(p);
            InstallFile(file);
        }

        private static void InstallFile(string file)
        {
            if (!file.EndsWith(".zip")) throw new NotImplementedException();

            ZipArchive za = new ZipArchive(File.OpenRead(file));
            var tmpFolder = "tmp/" + DateTime.Now.Ticks;
            za.ExtractToDirectory(tmpFolder);

            var entries = za.Entries.ToArray();
            if (entries.Where((zae =>
            {
                return zae.FullName.Count((c) => { return c == '/'; }) == 1;
            })).Count() == 1)
            {
                entries = (from zae in entries where zae.FullName.Count((c) => { return c == '/'; }) != 1 || !zae.FullName.EndsWith("/") select zae).ToArray();
            }
            var InstallEntries = (from entry in entries where entry.FullName.ToLower().Contains("/plugins/") || entry.FullName.ToLower().Contains("/textures/") || entry.FullName.ToLower().Contains("/ships/") || entry.FullName.ToLower().Contains("/saves/") || entry.FullName.ToLower().Contains("/plugins/") || entry.FullName.ToLower().Contains("/plugindata/") || entry.FullName.ToLower().Contains("/parts/") || entry.FullName.ToLower().Contains("/props/") || entry.FullName.ToLower().Contains("/resources/") || entry.FullName.ToLower().Contains("/spaces/") || entry.FullName.ToLower().Contains("/flags/") || entry.FullName.ToLower().Contains("/fx/") || entry.FullName.ToLower().Contains("gamedata/") select entry.FullName).ToArray();

            foreach (var entry in entries.Where((zae) => { return !(InstallEntries.Contains(zae.FullName) || InstallEntries.Any((str) => { return str.StartsWith(zae.FullName); })); }))
            {
                Console.WriteLine("Unknown file/folder: " + entry.FullName);
            }

            if (InstallEntries.Length == 0) Console.WriteLine("Could not install, no valid directory found");

            foreach (var instEntry in InstallEntries)
            {
                var instEStr = instEntry.StartsWith("/") ? instEntry : "/" + instEntry;
                var instEStr2 = instEStr.Contains("GameData") ? instEStr.Substring(instEStr.IndexOf("GameData/") + "GameData/".Length) : instEStr;

                if ((!GameDataDirectory.EndsWith("/")) && !GameDataDirectory.EndsWith("\\"))
                {
                    if (!instEStr.StartsWith("/")) instEStr = "/" + instEStr;
                    if (!instEStr2.StartsWith("/")) instEStr2 = "/" + instEStr2;
                }
                Console.WriteLine("Installing " + instEStr + " to " + GameDataDirectory + instEStr2);
                if (instEStr.EndsWith("/")) Directory.CreateDirectory(GameDataDirectory + instEStr2);
                else
                {
                    if (!File.Exists(GameDataDirectory + instEStr)) File.Copy(tmpFolder + instEStr, GameDataDirectory + instEStr2, true);
                    else
                    {
                        if (File.GetCreationTimeUtc(GameDataDirectory + instEStr2) > File.GetCreationTimeUtc(tmpFolder + instEStr))
                        {
                            File.Copy(tmpFolder + instEStr, GameDataDirectory + instEStr2, true);
                        }
                    }
                }
            }
        }

        private static string Download(int p)
        {
            var dlUrl = GetDownloadUrl(p).ToString();
            var fullFileName = dlUrl.Substring(dlUrl.IndexOf("f=") + 1);
            var fileName = "tmp/" + fullFileName.Substring(fullFileName.LastIndexOf('/') + 1);
            new WebClient().DownloadFile(dlUrl, fileName);
            return fileName;
        }

        private static Uri GetDownloadUrl(int p)
        {
            var postStr = "addonid=" + p + "&send=Download+Now!&action=downloadfileaddon";
            var wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            var str = wc.UploadString("http://kerbalspaceport.com/wp/wp-admin/admin-ajax.php", postStr);
            return new Uri(str);
        }

        public static string GameDataDirectory { get; set; }
    }
}