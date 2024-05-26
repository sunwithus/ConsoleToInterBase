# interbaseConsoleC#

Консольное приложение, записывает в БД Interbase версии 2009  аудио-данные из указанной папки. Преобразует через ffmpeg в нужный формат (в моём случае - "{ffmpegExePath} -i  -codec:a pcm_alaw -b:a 128k -ac 1 -ar 8000 "). Пытается парсить данные из названия файла. Настройки доступны в файле conf.ini 

The console application writes audio data from the specified folder to the Interbase database version 2009. Converts via ffmpeg to the desired format (in my case - "{ffmpegExePath} -i -codec:a pcm_alaw -b:a 128k -ac 1 -ar 8000 "). Tries to parse data from the file name. The settings are available in the conf.ini file

