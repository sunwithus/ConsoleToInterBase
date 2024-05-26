using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleInterBaseEmbarcadero
{
    internal class ConfigLoader
    {
        public static void LoadConfigFromFile(string fileName, out string ffmpegExePath, out string path, out string connectionString, out string sourceFolderPath, out string destinationFolderPath, out string deleteAfterComplete, out string User, out string Password, out string Database, out string DataSource, out string tryToParse)
        {
            ffmpegExePath = path = connectionString = sourceFolderPath = destinationFolderPath = deleteAfterComplete = tryToParse = "";
            User = Password = Database = DataSource = "";

            try
            {
                string[] lines = File.ReadAllLines(fileName);

                foreach (string line in lines)
                {
                    if (line.StartsWith("//") || string.IsNullOrWhiteSpace(line))
                    {
                        continue; // Пропускаем комментарии и пустые строки
                    }

                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        switch (key)
                        {
                            case "ffmpegExePath": ffmpegExePath = value; break;
                            case "path": path = value; break;
                            case "connectionString": connectionString = value; break;
                            case "sourceFolderPath": sourceFolderPath = value; break;
                            case "destinationFolderPath": destinationFolderPath = value; break;
                            case "User": User = value; break;
                            case "Password": Password = value; break;
                            case "Database": Database = value; break;
                            case "DataSource": DataSource = value; break;
                            case "deleteAfterComplete": deleteAfterComplete = value; break;
                            case "tryToParse": tryToParse = value; break;
                            default: // Обработка неизвестного ключа
                                break;
                        }

                        connectionString = $"User={User};Password={Password};Database={Database};DataSource={DataSource};" +
                        "Port=3050;Dialect=3;Charset=NONE;Role=;Connection lifetime=15;Pooling=true;MinPoolSize=0;MaxPoolSize=50;Packet Size=8192;ServerType=0";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading config file: {ex.Message}");
            }
        }
    }
}
