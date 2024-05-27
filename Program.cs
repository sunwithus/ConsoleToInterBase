using System;
using InterBaseSql.Data.InterBaseClient;
using System.Diagnostics;
using System.Text;
using ConsoleInterBaseEmbarcadero;
using System.IO;
using System.Runtime;

class Program
{
    static void Main()
    {
        string fileName = "conf.ini";
        string ffmpegExePath, path, connectionString, sourceFolderPath, destinationFolderPath, deleteAfterComplete, User, Password, Database, DataSource, tryToParse;

        ConfigLoader.LoadConfigFromFile(fileName, out ffmpegExePath, out path, out connectionString, out sourceFolderPath, out destinationFolderPath, out deleteAfterComplete, out User, out Password, out Database, out DataSource, out tryToParse);

        string[] files = Directory.GetFiles(sourceFolderPath);
        using (var connection = new IBConnection(connectionString))
        {
            connection.Open();
            Console.WriteLine("Connection to InterBase opened successfully.");

            foreach (var filePath in files)
            {
                // Получение последнего ключа в таблице SPR_SPEECH_TABLE
                int key;

                //иденитфикаторы
                string fileExtention = Path.GetExtension(filePath);

                DateTime timestampValue = DateTime.Now;
                string IMEI= "0", caller= "0", talker = "0";

                if(tryToParse == "1")
                {
                    if (fileExtention == ".wav") //09012024_225723_35751159132097_79025559157_79025562619.wav
                    {
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
                        string[] parts = fileNameNoExt.Split('_');

                        try
                        {
                            int year = int.Parse(parts[0].Substring(4, 4));
                            int month = int.Parse(parts[0].Substring(2, 2));
                            int day = int.Parse(parts[0].Substring(0, 2));
                            int hour = int.Parse(parts[1].Substring(0, 2));
                            int minute = int.Parse(parts[1].Substring(2, 2));
                            int second = int.Parse(parts[1].Substring(4, 2));
                            timestampValue = new DateTime(year, month, day, hour, minute, second);
                            IMEI = parts[2];
                            caller = "+" + parts[3];
                            talker = "+" + parts[4];
                        }
                        catch
                        {
                            Console.WriteLine("Получить данные из названия не удалось: " + fileNameNoExt);
                        }
                    }
                    if (fileExtention == ".mp3") //79147893331_79224783899_Call_Out_2023-11-20_16_14_37.mp3
                    {
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
                        string[] parts = fileNameNoExt.Split('_');

                        try
                        {
                            int year = int.Parse(parts[4].Substring(0, 4));
                            int month = int.Parse(parts[4].Substring(5, 2));
                            int day = int.Parse(parts[4].Substring(8, 2));
                            int hour = int.Parse(parts[5]);
                            int minute = int.Parse(parts[6]);
                            int second = int.Parse(parts[7]);
                            timestampValue = new DateTime(year, month, day, hour, minute, second);
                            string howCall = parts[3];
                            if (howCall == "Out") { caller = "+" + parts[0]; talker = "+" + parts[1]; }
                            else { caller = "+" + parts[1]; talker = "+" + parts[0]; }

                        }
                        catch
                        {
                            Console.WriteLine("Получить данные из названия не удалось: " + fileNameNoExt);
                        }
                    }
                }

                using (var command = new IBCommand("SELECT * FROM SPR_SPEECH_TABLE WHERE S_INCKEY=(SELECT max(S_INCKEY) FROM SPR_SPEECH_TABLE)", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        try
                        {
                            key = reader.GetInt32(reader.GetOrdinal("S_INCKEY"));
                        }
                        catch
                        {
                            key=0;
                        }
                    }
                }
                key++;

                // Вставка записей в таблицу SPR_SPEECH_TABLE и SPR_SP_DATA_1_TABLE
                using (var transaction = connection.BeginTransaction())
                {
                    if (File.Exists(filePath))
                    {
                        // ##############################################################
                        DirectoryInfo dirInfo = new DirectoryInfo(path);
                        if (!dirInfo.Exists)
                        {
                            dirInfo.Create();
                        }
                        var trustedFileName = Path.GetRandomFileName();
                        var trustedFilePath = Path.Combine(path, trustedFileName);
                        string outputFilePath = trustedFilePath + ".wav"; // путь к выходному файлу
                        Console.WriteLine("outputFilePath: " + outputFilePath);
                        Debug.WriteLine("outputFilePath: " + outputFilePath);
                        // Create the command string for FFmpeg
                        //###########################################################
                        StringBuilder sbffmpeg = new StringBuilder();
                        sbffmpeg.Append($"{ffmpegExePath} -i ");
                        sbffmpeg.Append(filePath);
                        sbffmpeg.Append(" -codec:a pcm_alaw -b:a 128k -ac 1 -ar 8000 "); // формат для Whisper mono
                        //sbffmpeg.Append(" -c:a adpcm_ima_wav -ac 2 -ar 8000 "); // работает, в постворк IMA-ADPCM 
                        //sbffmpeg.Append(" -c:a pcm_s16le -ac 2 -ar 8000 -b:a 64k "); // 
                        sbffmpeg.Append(outputFilePath);

                        // Run FFmpeg with cmd.exe
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            UseShellExecute = false,
                            RedirectStandardInput = true,
                            CreateNoWindow = true
                        };

                        using (Process process = Process.Start(startInfo))
                        {
                            using (StreamWriter sw = process.StandardInput)
                            {
                                sw.WriteLine(sbffmpeg.ToString());
                                sw.WriteLine("exit");
                                sw.Flush();
                            }
                            process.WaitForExit();
                        }
                        //###########################################################
                        Console.WriteLine("Success to convert file!");
                        Debug.WriteLine("Success to convert file!");
                        // ##############################################################
                        if (File.Exists(outputFilePath)) { 
                            byte[] fileData = File.ReadAllBytes(outputFilePath);
                            //длительность S_DURATION
                            int duration = (int)(fileData.Length / 8000);
                            string durationString = string.Format("{0:D2}:{1:D2}:{2:D2}", duration / 3600, (duration % 3600) / 60, duration % 60);

                            //using (var insertCommand = new IBCommand("insert into SPR_SPEECH_TABLE (S_INCKEY, S_TYPE, S_PRELOOKED, S_DATETIME, S_EVENTCODE, S_DEVICEID, S_SYSNUMBER, S_USERNUMBER, S_TALKER, S_DURATION) values (@S_INCKEY, @S_TYPE, @S_PRELOOKED, @S_DATETIME, @S_EVENTCODE, @S_DEVICEID, @S_SYSNUMBER, @S_USERNUMBER, @S_TALKER, @S_DURATION)", connection, transaction))
                            using (var insertCommand = new IBCommand("insert into SPR_SPEECH_TABLE (S_INCKEY, S_TYPE, S_PRELOOKED, S_DATETIME, S_EVENTCODE, S_DEVICEID, S_DURATION, S_SYSNUMBER, S_USERNUMBER, S_TALKER) values (@S_INCKEY, @S_TYPE, @S_PRELOOKED, @S_DATETIME, @S_EVENTCODE, @S_DEVICEID, @S_DURATION, @S_SYSNUMBER, @S_USERNUMBER, @S_TALKER" +
                                ")", connection, transaction))
                            {
                                insertCommand.Parameters.Add("@S_INCKEY", key);
                                insertCommand.Parameters.AddWithValue("@S_TYPE", 0);
                                insertCommand.Parameters.AddWithValue("@S_PRELOOKED", 0);
                                insertCommand.Parameters.AddWithValue("@S_DATETIME", timestampValue);
                                insertCommand.Parameters.AddWithValue("@S_SYSNUMBER", IMEI);
                                insertCommand.Parameters.AddWithValue("@S_USERNUMBER", caller);
                                insertCommand.Parameters.AddWithValue("@S_TALKER", talker);
                                insertCommand.Parameters.AddWithValue("@S_DURATION", durationString);
                                insertCommand.Parameters.AddWithValue("@S_EVENTCODE", "PCMA");
                                insertCommand.Parameters.AddWithValue("@S_DEVICEID", "APK_SUPERACCESS");
                                insertCommand.ExecuteNonQuery();
                            }
                            // добавляем данные в параметр для поля BLOB в базе данных
                            using (var insertCommand = new IBCommand("insert into SPR_SP_DATA_1_TABLE (S_INCKEY, S_ORDER, S_FSPEECH, S_RECORDTYPE) values (@S_INCKEY, @S_ORDER, @S_FSPEECH, @S_RECORDTYPE)", connection, transaction))
                            {
                                insertCommand.Parameters.Add("@S_INCKEY", key);
                                insertCommand.Parameters.Add("@S_ORDER", 1);
                                //insertCommand.Parameters.Add("@S_RECORDTYPE", "PCMA");
                                insertCommand.Parameters.Add("@S_RECORDTYPE", "PCMA");
                                insertCommand.Parameters.Add("@S_FSPEECH", IBDbType.Array, fileData.Length).Value = fileData;
                                insertCommand.ExecuteNonQuery();
                            }
                        }
                    }
                    transaction.Commit();
                }
                // ##############################################################
                // Перемещение файла после записи
                if (destinationFolderPath != "" && !string.IsNullOrEmpty(destinationFolderPath))
                {
                    DirectoryInfo directoryToMove = new DirectoryInfo(destinationFolderPath);
                    if (!directoryToMove.Exists)
                    {
                        try
                        {
                            directoryToMove.Create();
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"Failed to create directory: {ex.Message}");
                        }
                    }

                    string fileNameToMove = directoryToMove + "\\" + Path.GetFileName(filePath);
                    string destinationPath = Path.Combine(sourceFolderPath, fileNameToMove);
                    try
                    {
                        File.Move(filePath, fileNameToMove);
                        Console.WriteLine("File moved to destination folder: " + destinationFolderPath);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Failed to move: {ex.Message}");
                    }

                }
                // Удалить файл после записи
                if (destinationFolderPath == "" && (deleteAfterComplete == "true" || deleteAfterComplete == "1"))
                {
                    File.Delete(filePath);
                    Console.WriteLine("File was deleted:" + filePath) ;
                }
            }
            connection.Close();
            Console.WriteLine("Connection to InterBase closed.");
            // Очистка временной директории
            /*DirectoryInfo dirTempInfo = new DirectoryInfo(path);
            if (dirTempInfo.Exists)
            {
                dirTempInfo.Delete(true);
                Console.WriteLine("Каталог временных файлов удалён");
            }
            else
            {
                Console.WriteLine("Каталог не существует");
            }*/
            Console.WriteLine("Press a key twice to exit.");
            Console.ReadKey();
        }
    }

}