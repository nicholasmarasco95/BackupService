using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Backup_Service_Console_Edition_1._0
{
    class Program
    {
        static void Main(string[] args)
        {
            welcome_message();
            Stopwatch total_timer = new Stopwatch();
            total_timer.Start();

            string file_name = DateTime.Now.ToString("backup_MM-dd-yyy");
            file_name += ".zip";
            string file_to_backup = @"C:\Backup Service Console Edition 1.0\Service files\backup_path.txt";
            string dir_to_zip = @"C:\Backup Service Console Edition 1.0\Service files\temp_backup_files\to_zip";
            string dir_to_upload_copy = @"C:\Backup Service Console Edition 1.0\Service files\temp_backup_files\to_upload_copy";
            string dir_zip_zipfun = "C:\\\"Backup Service Console Edition 1.0\"\\\"Service files\"\\temp_backup_files\\to_zip";  //zip function doesn't allows spaces in address, so the folder with a "space name" you have to add the quotations marks "".
            string dir_up_copy_zip_fun = "C:\\\"Backup Service Console Edition 1.0\"\\\"Service files\"\\temp_backup_files\\to_upload_copy";  //zip function doesn't allows spaces in address, so the folder with a "space name" you have to add the quotations marks "".
            string file_server_info = @"C:\Backup Service Console Edition 1.0\Service files\server_info.txt";
            string file_del_days = @"C:\Backup Service Console Edition 1.0\Service files\del_days.txt";
            string file_to_upload_path = dir_to_upload_copy + @"\" + file_name;
            string file_csv_history = @"C:\Backup Service Console Edition 1.0\Service files\Backup Service 1.0 History.csv";
            string file_csv_copy = @"C:\Backup Service Console Edition 1.0\Service files\csv_copy_path.txt";
            string file_device_on_off = @"C:\Backup Service Console Edition 1.0\Service files\device_backup_on_off.txt";
            string file_ftp_on_off = @"C:\Backup Service Console Edition 1.0\Service files\ftp_backup_on_off.txt";
            string file_mount = @"C:\Backup Service Console Edition 1.0\Service files\mount_unmount_files\mount_admin.bat";
            string file_unmount = @"C:\Backup Service Console Edition 1.0\Service files\mount_unmount_files\unmount_admin.bat";
            string file_device_dest_path = @"C:\Backup Service Console Edition 1.0\Service files\device_dest_path.txt";

            bool autopilot = autopilot_check(file_to_backup, file_server_info, file_del_days, file_device_on_off, file_ftp_on_off, file_mount, file_unmount, file_device_dest_path);
            if (autopilot)
                settings_change(file_to_backup, file_csv_copy, file_del_days, file_server_info, file_device_on_off, file_ftp_on_off, file_mount, file_unmount, file_device_dest_path);

            folder_cleaner(dir_to_upload_copy);
            folder_cleaner(dir_to_zip);
            List<string> dir_address = new List<string>(File.ReadAllLines(file_to_backup));
            Stopwatch copy_timer = new Stopwatch();
            copy_timer.Start();
            copy_fun(dir_address, dir_to_zip);
            copy_timer.Stop();
            Stopwatch zip_timer = new Stopwatch();
            zip_timer.Start();
            zip_fun(dir_zip_zipfun, dir_up_copy_zip_fun, file_name);
            zip_timer.Stop();
            folder_cleaner(dir_to_zip);

            if (File.ReadAllText(file_device_on_off) == "1")
            {
                string device_dest = File.ReadAllText(file_device_dest_path);
                mount_device(file_mount);
                int days;
                string days_read = File.ReadAllText(file_del_days);
                if (int.TryParse(days_read, out days))
                {
                    if (days > 0)
                    {
                        del_old_file_device(file_device_dest_path, days);
                    }
                }
                File.Copy(dir_to_upload_copy + @"\" + file_name, device_dest + @"\" + file_name, true);
                unmount_device(file_unmount);
            }

            Stopwatch upload_timer = new Stopwatch();
            if (File.ReadAllText(file_ftp_on_off) == "1")
            {
                int days;
                string days_read = File.ReadAllText(file_del_days);
                if (int.TryParse(days_read, out days))
                {
                    if (days > 0)
                    {
                        del_old_file_ftp(file_server_info, days);
                    }
                }
                upload_timer.Start();
                ftp_upload(file_server_info, file_to_upload_path);
                upload_timer.Stop();
            }

            string upload_size= folder_dimension(dir_to_upload_copy);

            folder_cleaner(dir_to_upload_copy);
            folder_cleaner(dir_to_zip);

            total_timer.Stop();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("\n\n");
            line_printer();
            TimeSpan copy_t = TimeSpan.FromMinutes(copy_timer.Elapsed.TotalMinutes);
            TimeSpan zip_t = TimeSpan.FromMinutes(zip_timer.Elapsed.TotalMinutes);
            TimeSpan upload_t = TimeSpan.FromMinutes(upload_timer.Elapsed.TotalMinutes);
            TimeSpan total_t = TimeSpan.FromMinutes(total_timer.Elapsed.TotalMinutes);
            Console.WriteLine("\"COPY\" Elapsed time= {0:D2}H : {1:D2}M : {2:D2}s : {3:D3}ms", copy_t.Hours, copy_t.Minutes, copy_t.Seconds, copy_t.Milliseconds);
            Console.WriteLine("\"ZIP\" Elapsed time= {0:D2}H : {1:D2}M : {2:D2}s : {3:D3}ms", zip_t.Hours, zip_t.Minutes, zip_t.Seconds, zip_t.Milliseconds);
            Console.WriteLine("\"UPLOAD\" Elapsed time= {0:D2}H : {1:D2}M : {2:D2}s : {3:D3}ms", upload_t.Hours, upload_t.Minutes, upload_t.Seconds, upload_t.Milliseconds);
            Console.WriteLine("\"TOTAL\" Elapsed time= {0:D2}H : {1:D2}M : {2:D2}s : {3:D3}ms", total_t.Hours, total_t.Minutes, total_t.Seconds, total_t.Milliseconds);
            line_printer();
            Console.ResetColor();

            CSV_update(file_csv_history, file_name, file_device_on_off, file_ftp_on_off, upload_t, total_t, copy_t, upload_size, file_csv_copy);

            goodbye_message();
            Console.Write("Enter to exit ");
            Console.ReadLine();
        }

        private static void welcome_message()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            int width = Console.WindowWidth;
            for (int line = 0; line < 2; line++)
            {
                for (int asterisk = 0; asterisk < width; asterisk++)
                {
                    Console.Write("*");
                }
            }
            string welcome = "Welcome to Backup Service Console Edition 1.0";
            for (int wel = 0; wel < (width - welcome.Length) / 2; wel++)
            {
                Console.Write("*");
            }
            Console.Write(welcome);
            for (int wel = 0; wel < (width - welcome.Length) / 2; wel++)
            {
                Console.Write("*");
            }
            for (int line = 0; line < 2; line++)
            {
                for (int asterisk = 0; asterisk < width; asterisk++)
                {
                    Console.Write("*");
                }
            }
            Console.ResetColor();
            Console.WriteLine("\n");
        }

        private static void goodbye_message()
        {
            Console.WriteLine("\n");
            Console.ForegroundColor = ConsoleColor.Cyan;
            int width = Console.WindowWidth;
            for (int line = 0; line < 2; line++)
            {
                for (int asterisk = 0; asterisk < width; asterisk++)
                {
                    Console.Write("*");
                }
            }
            string uploaded = "Goodbye";
            for (int wel = 0; wel < (width - uploaded.Length) / 2; wel++)
            {
                Console.Write("*");
            }
            Console.Write(uploaded);
            for (int wel = 0; wel < (width - uploaded.Length) / 2; wel++)
            {
                Console.Write("*");
            }
            for (int line = 0; line < 2; line++)
            {
                for (int asterisk = 0; asterisk < width; asterisk++)
                {
                    Console.Write("*");
                }
            }
            Console.ResetColor();
            Console.WriteLine("\n");
        }

        private static void line_printer()
        {
            int width = Console.WindowWidth;
            for (int line = 0; line < width; line++)
            {
                Console.Write("-");
            }
        }

        private static bool autopilot_check(string file_to_backup, string file_server_info, string file_del_days, string file_device_on_off, string file_ftp_on_off, string file_mount, string file_unmount, string file_device_dest_path)
        {
            //valuate to enter a TimeOut
            settings_check(file_to_backup, file_server_info, file_del_days, file_device_on_off, file_ftp_on_off, file_mount, file_unmount, file_device_dest_path); //valuate to return a bool value
            Console.Write("If you want to change or check settings, press Enter within 10 secs ");
            bool autopilot = Timeout.ReadLine(10000);
            if (autopilot)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                new SoundPlayer(@"C:\Backup Service Console Edition 1.0\Service files\sounds\Autopilot Disconnect sound.wav").Play();
                Console.WriteLine("\n\t\"AUTOPILOT\" DISCONNECT\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n\t\"AUTOPILOT\" ON\n");
                Console.ResetColor();
            }
            return autopilot;
        }

        private static void autopilot_deactivate_error_warning()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            new SoundPlayer(@"C:\Backup Service Console Edition 1.0\Service files\sounds\Autopilot Disconnect sound.wav").Play();
            Console.WriteLine("\n\t\"AUTOPILOT\" DISCONNECT\n");
            Console.ResetColor();
        }

        private static void settings_change(string file_to_backup, string file_csv_copy, string file_del_days, string file_server_info, string file_device_on_off, string file_ftp_on_off, string file_mount, string file_unmount, string file_device_dest_path)
        {
            bool exit_check = false;
            while (!exit_check)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("\nSettings control panel\n");
                Console.ResetColor();
                Console.Write("To check or change ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("\"GENERAL\" ");
                Console.ResetColor();
                Console.Write("settings press ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("\"G\" ");
                Console.ResetColor();
                Console.Write(", ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("\"F\" ");
                Console.ResetColor();
                Console.Write("for ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("\"FTP\" ");
                Console.ResetColor();
                Console.Write(", ");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("\"D\" ");
                Console.ResetColor();
                Console.Write("for ");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("\"DEVICES\"");
                Console.ResetColor();
                Console.Write(", ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\"E\" ");
                Console.ResetColor();
                Console.Write("to ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("EXIT");
                Console.ResetColor();
                Console.Write(": ");
                string ans = Console.ReadLine();
                if (ans.ToLower() == "g")
                {
                    string gen_ans;
                    do
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("\n\nGeneral Settings panel");
                        Console.ResetColor();
                        Console.Write("\nBackup Type (");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("Press \"B\"");
                        Console.ResetColor();
                        Console.WriteLine(")");
                        Console.Write("\tBackup Device: ");
                        if (File.ReadAllText(file_device_on_off) == "1")
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("ON");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("OFF");
                            Console.ResetColor();
                        }
                        Console.Write("\tFTP Backup: ");
                        if (File.ReadAllText(file_ftp_on_off) == "1")
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("ON");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("OFF");
                            Console.ResetColor();
                        }
                        Console.Write("\nFolders to Backup (");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("Press \"F\"");
                        Console.ResetColor();
                        Console.WriteLine(")");
                        Console.Write("\tFolder set: ");
                        if (new FileInfo(file_to_backup).Length <= 2)
                        {
                            Console.WriteLine("No folder set!");
                        }
                        else
                        {
                            List<string> to_print_list = new List<string>(File.ReadAllLines(file_to_backup));
                            Console.WriteLine();
                            foreach (string line in to_print_list)
                            {
                                Console.WriteLine("\t" + line);
                            }
                        }
                        Console.WriteLine();
                        Console.Write("Backup History copy destination (");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("Press \"C\"");
                        Console.ResetColor();
                        Console.WriteLine(" to change destination or for more info)");
                        Console.Write("\tFolder set: ");
                        if (new FileInfo(file_csv_copy).Length <= 2)
                        {
                            Console.WriteLine("No folder set!");
                        }
                        else
                        {
                            Console.WriteLine(File.ReadAllText(file_csv_copy));
                        }
                        Console.Write("\nDays limit (");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("Press \"D\"");
                        Console.ResetColor();
                        Console.WriteLine(" to change days or for more info)");
                        Console.Write("\tDays set: ");
                        if (new FileInfo(file_del_days).Length == 0)
                        {
                            Console.Write("No Days set!");
                        }
                        else
                        {
                            Console.WriteLine(File.ReadAllText(file_del_days));
                        }
                        Console.WriteLine("\n");
                        Console.Write("Choose setting to change, otherwise Press \"E\" to Exit: ");
                        gen_ans = Console.ReadLine().ToLower();
                        if (gen_ans.ToLower() == "f")
                        {
                            Console.Beep();
                            change_file_backup(file_to_backup);
                        }
                        if (gen_ans.ToLower() == "c")
                        {
                            Console.Beep();
                            change_file_csv_copy(file_csv_copy);
                        }
                        if (gen_ans.ToLower() == "d")
                        {
                            Console.Beep();
                            change_days_limit(file_del_days);
                        }
                        if (gen_ans.ToLower() == "b")
                        {
                            Console.Beep();
                            change_bakcup_type(file_device_on_off, file_ftp_on_off);
                        }
                        if (new FileInfo(file_to_backup).Length <= 2)
                        {
                            change_file_backup(file_to_backup);
                        }
                    }
                    while (gen_ans != "e");
                }
                if (ans.ToLower() == "f")
                {
                    change_server_info(file_server_info, file_ftp_on_off);
                }
                if (ans.ToLower() == "d")
                {
                    string dev_ans="";
                    while (dev_ans != "e")
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("\n\nDevice Settings panel");
                        Console.ResetColor();
                        Console.Write("\nMount/Unmount settings (");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("Press \"M\"");
                        Console.ResetColor();
                        Console.WriteLine(")");
                        StreamReader file = new StreamReader(file_mount);
                        file.ReadLine();
                        string device_letter = file.ReadLine();
                        string volume = file.ReadLine();
                        volume = volume.Replace("set volume=", "");
                        device_letter = device_letter.Replace("set drive=", "");
                        file.Close();
                        Console.WriteLine("\tMount:");
                        Console.WriteLine("\t\tDriver Letter Mount: " + device_letter);
                        Console.WriteLine("\t\tVolume Name: " + volume);
                        Console.WriteLine("\tUnmount:");
                        StreamReader file_un_check = new StreamReader(file_unmount);
                        file_un_check.ReadLine();
                        string device_letter_unmount_check = file_un_check.ReadLine();
                        device_letter_unmount_check = device_letter_unmount_check.Replace("set drive=", "");
                        file_un_check.Close();
                        Console.WriteLine("\t\tDriver Letter Unmount: " + device_letter_unmount_check);
                        Console.Write("\nDestination Setting (");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("Press \"D\"");
                        Console.ResetColor();
                        Console.WriteLine(")");
                        string dest = File.ReadAllText(file_device_dest_path);
                        Console.Write("\tDestination Folder: ");
                        if (dest.Length < 3)
                        {
                            Console.WriteLine("\tNo folder set");
                        }
                        else
                        {
                            Console.WriteLine(dest);
                        }
                        Console.WriteLine();
                        Console.Write("Choose setting to change, otherwise Press \"E\" to Exit: ");

                        dev_ans = Console.ReadLine();
                        if (dev_ans.ToLower() == "m")
                        {
                            change_mount_unmount(file_mount, file_unmount);
                        }
                        if (dev_ans.ToLower() == "d")
                        {
                            change_destination_device(file_device_dest_path, file_device_on_off);
                        }
                        
                    }

                    //change_mount_unmount(file_mount, file_unmount);
                }
                if (ans.ToLower() == "e")
                {
                    exit_check = true;
                }
            }
        }

        private static void change_file_backup(string file_to_backup)
        {
            Console.WriteLine();
            line_printer();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Backup Settings");
            Console.ResetColor();
            List<string> dir_address = new List<string>(File.ReadAllLines(file_to_backup));
            Console.Write("\tFolder set: ");
            if (new FileInfo(file_to_backup).Length <= 2)
            {
                Console.WriteLine("No folder set!");
            }
            else
            {
                Console.WriteLine();
                foreach (string line in dir_address)
                {
                    Console.WriteLine("\t" + line);
                }
            }
            Console.WriteLine();
            string change_input;
            if (new FileInfo(file_to_backup).Length <= 2)
            {
                while (new FileInfo(file_to_backup).Length <= 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\a\nWARNING: No folder to backup, this will cause a fatal error\n");
                    Console.ResetColor();
                    do
                    {
                        Console.Write("Press \"A\" to Add folder, \"D\" to Del folder, \"C\" to erase file, \"E\" to stop: ");
                        change_input = Console.ReadLine().ToLower();
                        if (change_input.ToLower() == "a")
                        {
                            Console.Write("Enter folder path: ");
                            dir_address.Add(Console.ReadLine());
                        }
                        if (change_input.ToLower() == "d")
                        {
                            Console.Write("Enter folder path: ");
                            dir_address.Remove(Console.ReadLine());
                        }
                        if (change_input.ToLower() == "c")
                        {
                            dir_address.Clear();
                            Console.WriteLine("File Successfully erased");
                        }
                    }
                    while (change_input != "e");
                    File.WriteAllLines(file_to_backup, dir_address);
                }
            }
            else
            {
                do
                {
                    Console.Write("Press \"A\" to Add folder, \"D\" to Del folder, \"C\" to erase file, \"E\" to stop: ");
                    change_input = Console.ReadLine().ToLower();
                    if (change_input.ToLower() == "a")
                    {
                        Console.Write("Enter folder path: ");
                        dir_address.Add(Console.ReadLine());
                    }
                    if (change_input.ToLower() == "d")
                    {
                        Console.Write("Enter folder path: ");
                        dir_address.Remove(Console.ReadLine());
                    }
                    if (change_input.ToLower() == "c")
                    {
                        dir_address.Clear();
                        Console.WriteLine("File Successfully erased");
                    }
                }
                while (change_input != "e");
                File.WriteAllLines(file_to_backup, dir_address);
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Folders to backup");
            Console.ResetColor();
            List<string> to_print_list = new List<string>(File.ReadAllLines(file_to_backup));
            Console.Write("\tFolder set: ");
            Console.WriteLine();
            foreach (string line in to_print_list)
            {
                Console.WriteLine("\t" + line);
            }
            line_printer();
        }

        private static void change_file_csv_copy(string file_csv_copy)
        {
            Console.WriteLine("\n");
            line_printer();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Backup History settings");
            Console.ResetColor();
            Console.WriteLine("\nIf you enter a folder path, the program will copy the updated Backup History in that folder. It could be useful if you want to remotely check it by a Cloud Service. Whatever is your decision, the program will update the Backup History in his default folder.");
            Console.Write("\nFolder set: ");
            if(new FileInfo(file_csv_copy).Length <= 2)
            {
                Console.Write("No folder set!");
            }
            else
            {
                Console.WriteLine(File.ReadAllText(file_csv_copy));
            }
            Console.WriteLine();
            Console.Write("\nTo change copy path ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Press \"C\"");
            Console.ResetColor();
            Console.Write(", to erase file");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(" Press \"E\"");
            Console.ResetColor();
            Console.Write(", otherwise ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Press ENTER ");
            Console.ResetColor();
            Console.Write(": ");
            string check = Console.ReadLine();
            if (check.ToLower() == "c")
            {
                Console.Write("\nEnter folder path: ");
                File.WriteAllText(file_csv_copy, Console.ReadLine());
                Console.Write("\nFolder set: ");
                Console.WriteLine(File.ReadAllText(file_csv_copy));
            }
            if (check.ToLower() == "e")
            {
                File.WriteAllText(file_csv_copy, "");
                Console.WriteLine("File Successfully erased");
            }
            line_printer();
        }

        private static void change_days_limit(string file_del_days)
        {
            Console.Beep();
            Console.WriteLine();
            line_printer();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Days limit Settings");
            Console.ResetColor();
            Console.Write("\tDays set: ");
            if (new FileInfo(file_del_days).Length == 0)
            {
                Console.Write("No Days set!");
            }
            else
            {
                Console.WriteLine(File.ReadAllText(file_del_days));
            }
            Console.WriteLine("\n");
            Console.WriteLine("This setting helps you to keep memory free, the files older than X days will be deleted.\nNote: the limit works for Devices and Server settings.\nThe input must be an integer.\n");
            Console.Write("To change Days limit ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Press \"C\"");
            Console.ResetColor();
            Console.Write(", to erase limit press ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Press \"E\"");
            Console.ResetColor();
            Console.Write(", otherwise ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("press ENTER");
            Console.ResetColor();
            Console.Write(": ");
            string check = Console.ReadLine();
            if (check.ToLower() == "c")
            {
                bool check_out = false;
                string input;
                int limit;
                while (!check_out)
                {
                    Console.Write("\aEnter limit: ");
                    input = Console.ReadLine();
                    if (int.TryParse(input, out limit))
                    {
                        File.WriteAllText(file_del_days, input);
                        check_out = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("ERROR: input must be an integer");
                        Console.ResetColor();
                    }
                }
            }
            if (check.ToLower() == "e")
            {
                File.WriteAllText(file_del_days, "");
            }

            Console.Write("\nDays set: ");
            if (new FileInfo(file_del_days).Length == 0)
            {
                Console.Write("No Days set!");
            }
            else
            {
                Console.WriteLine(File.ReadAllText(file_del_days));
            }
            Console.WriteLine();
            line_printer();
            Console.WriteLine();
        }

        private static void change_server_info(string file_server_info, string file_ftp_on_off)
        {
            //bool enable: TRUE: If server info are required, FALSE: if FTP upload is deactivated
            string enable_check = File.ReadAllText(file_ftp_on_off);
            bool enable;
            if (enable_check == "1")
            {
                enable = true;
            }
            else
            {
                enable = false;
            }
            Console.Beep();
            Console.WriteLine();
            line_printer();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Server Settings");
            Console.ResetColor();
            StreamReader file = new StreamReader(file_server_info);
            string ftp_address = file.ReadLine();
            string user = file.ReadLine();
            string pass = file.ReadLine();
            string dest_path = file.ReadLine();
            file.Close();
            if (ftp_address== null)
            {
                Console.WriteLine("Address: No Address Set");
            }
            else
            {
                Console.WriteLine("Address: {0}", ftp_address);
            }
            if (user == null)
            {
                Console.WriteLine("User: No User Set");
            }
            else
            {
                Console.WriteLine("User: {0}", user);
            }
            if (pass == null)
            {
                Console.WriteLine("Password: No Password Set");
            }
            else
            {
                Console.Write("Password: ");
                for (int count = 0; count < pass.Length; count++)
                {
                    Console.Write("*");
                }
            }
            Console.WriteLine();
            if (dest_path == null)
            {
                Console.WriteLine("Server Destination folder: No Folder Set");
            }
            else
            {
                Console.WriteLine("Server Destination folder: {0}", dest_path);
            }
            Console.WriteLine();
            if (enable)
            {
                while (ftp_address == null || ftp_address.Length<=2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\aERROR: No Address Set");
                    Console.ResetColor();
                    Console.Write("Enter FTP Address: ");
                    ftp_address = Console.ReadLine();
                }
                while (user == null || user.Length<=2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\aERROR: No User Set");
                    Console.ResetColor();
                    Console.Write("Enter User: ");
                    user = Console.ReadLine();
                }
                while (pass == null || pass.Length<=2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\aERROR: No Password Set");
                    Console.ResetColor();
                    Console.Write("Enter Password: ");
                    pass = Console.ReadLine();
                }
                while (dest_path == null || dest_path.Length<=2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\aERROR: No Destination path Set");
                    Console.ResetColor();
                    Console.Write("Enter Destination path: ");
                    dest_path = Console.ReadLine();
                }
            }
                bool exit_check = false;
                bool erase = false;
                while (!exit_check)
                {
                    Console.Write("To change FTP Address ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"A\"");
                    Console.ResetColor();
                    Console.Write(", User ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"U\"");
                    Console.ResetColor();
                    Console.Write(", Password ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"P\"");
                    Console.ResetColor();
                    Console.Write(", Destination path ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"D\"");
                    Console.ResetColor();
                    Console.Write(", for Help ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"H\"");
                    Console.ResetColor();
                    Console.Write(", to erase settings ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"C\"");
                    Console.ResetColor();
                    Console.Write(", to Exit ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"E\"");
                    Console.ResetColor();
                    Console.Write(": ");
                    string ans = Console.ReadLine();
                    if (ans.ToLower() == "h")
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Server settings example:");
                        Console.WriteLine("Address: ftp://www.example.com");
                        Console.WriteLine("User: 123456@server.com");
                        Console.WriteLine("Password: qwerty12");
                        Console.WriteLine("Folder: /www.example.com/destination_folder \n");
                        Console.WriteLine("This infos were provided by your hosting, you can find them on your account, or contact the provider.");
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                    if (ans.ToLower() == "a")
                    {
                        Console.Write("Enter Address: ");
                        ftp_address = Console.ReadLine();
                        erase = false;
                    }
                    if (ans.ToLower() == "u")
                    {
                        Console.Write("Enter User: ");
                        user = Console.ReadLine();
                        erase = false;
                    }
                    if (ans.ToLower() == "p")
                    {
                        Console.Write("Enter Password: ");
                        pass = Console.ReadLine();
                        erase = false;
                    }
                    if (ans.ToLower() == "d")
                    {
                        Console.Write("Enter Destination path: ");
                        dest_path = Console.ReadLine();
                        erase = false;
                    }
                    if (ans.ToLower() == "c")
                    {
                        erase = true;
                        ftp_address = "";
                        user = "";
                        pass = "";
                        dest_path = "";
                        File.WriteAllText(file_server_info, "");
                        Console.Beep();
                        Console.WriteLine("Settings successfully erased");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\aWARNING: if FTP backup is enabled and there are some empty settings, the program will stop.\n");
                        Console.ResetColor();
                    }
                    if (ans.ToLower() == "e")
                    {
                        exit_check = true;
                    }
                }
                if (!erase)
                {
                    StreamWriter file_write = new StreamWriter(file_server_info);
                    file_write.WriteLine(ftp_address);
                    file_write.WriteLine(user);
                    file_write.WriteLine(pass);
                    file_write.WriteLine(dest_path);
                    file_write.Close();
                }
            Console.WriteLine();
        }

        private static void change_bakcup_type(string file_device_on_off, string file_ftp_on_off)
        {
            Console.WriteLine();
            line_printer();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Backup Type Settings");
            Console.ResetColor();
            string device = File.ReadAllText(file_device_on_off);
            string ftp = File.ReadAllText(file_ftp_on_off);
            Console.Write("\tBackup Device: ");
            if (device == "1")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ON");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("OFF");
                Console.ResetColor();
            }
            Console.Write("\tFTP Backup: ");
            if (ftp=="1")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ON");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("OFF");
                Console.ResetColor();
            }
            bool exit_check = false;
            string ans;
            while (!exit_check)
            {
                Console.Write("\nTo Activate/Deactivate Backup Device ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Press \"D\"");
                Console.ResetColor();
                Console.Write(", to Activate/Deactivate Backup FTP ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Press \"F\"");
                Console.ResetColor();
                Console.Write(", to Exit ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Press \"E\"");
                Console.ResetColor();
                Console.Write(": ");
                ans = Console.ReadLine();
                if (ans.ToLower() == "d")
                {
                    Console.Write("\nDevice Backup: To Activate ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"A\"");
                    Console.ResetColor();
                    Console.Write(", to Deactivate ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"D\"");
                    Console.ResetColor();
                    Console.Write(": ");
                    string ans_d = Console.ReadLine();
                    if (ans_d.ToLower() == "a")
                    {
                        File.WriteAllText(file_device_on_off, "1");
                        Console.Beep();
                        Console.WriteLine("\tBackup Device successfully Activated.");
                    }
                    if (ans_d.ToLower() == "d")
                    {
                        File.WriteAllText(file_device_on_off, "0");
                        Console.Beep();
                        Console.WriteLine("\tBackup Device successfully Deactivated.");
                    }
                }
                if (ans.ToLower() == "f")
                {
                    Console.Write("\nFTP Backup: To Activate ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"A\"");
                    Console.ResetColor();
                    Console.Write(", to Deactivate ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"D\"");
                    Console.ResetColor();
                    Console.Write(": ");
                    string ans_d = Console.ReadLine();
                    if (ans_d.ToLower() == "a")
                    {
                        File.WriteAllText(file_ftp_on_off, "1");
                        Console.Beep();
                        Console.WriteLine("\tFTP Backup successfully Activated.");
                    }
                    if (ans_d.ToLower() == "d")
                    {
                        File.WriteAllText(file_ftp_on_off, "0");
                        Console.Beep();
                        Console.WriteLine("\tFTP Backup successfully Deactivated.");
                    }
                }
                if (ans.ToLower() == "e")
                {
                    exit_check = true;
                }
            }
            line_printer();
        }

        private static void change_mount_unmount(string file_mount, string file_unmount)
        {
            bool exit_check = false;
            Console.WriteLine();
            line_printer();
            while (!exit_check)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Device Backup Settings");
                Console.ResetColor();
                Console.WriteLine("\nMount settings:");
                StreamReader file = new StreamReader(file_mount);
                file.ReadLine();
                string device_letter = file.ReadLine();
                string volume = file.ReadLine();
                volume = volume.Replace("set volume=", "");
                device_letter = device_letter.Replace("set drive=", "");
                file.Close();
                Console.WriteLine("\tDriver Letter Mount: " + device_letter);
                Console.WriteLine("\tVolume Name: " + volume);
                Console.WriteLine("\nUnmount settings:");
                StreamReader file_un_check = new StreamReader(file_unmount);
                file_un_check.ReadLine();
                string device_letter_unmount_check = file_un_check.ReadLine();
                device_letter_unmount_check = device_letter_unmount_check.Replace("set drive=", "");
                file_un_check.Close();
                Console.WriteLine("\tDriver Letter Unmount: " + device_letter_unmount_check);
                Console.Write("\nTo change Mount Settings ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Press \"M\"");
                Console.ResetColor();
                Console.Write(", to change Unmount Settings ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Press \"U\"");
                Console.ResetColor();
                Console.Write(", to Exit ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Press \"E\"");
                Console.ResetColor();
                Console.Write(": ");
                string ans_check = Console.ReadLine();
                if (ans_check.ToLower() == "m")
                {
                    Console.Write("\nTo change letter mount ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"L\"");
                    Console.ResetColor();
                    Console.Write(", to change volume name ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"N\"");
                    Console.ResetColor();
                    Console.Write(", for Help ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"H\"");
                    Console.ResetColor();
                    Console.Write(", to erase settings ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"C\"");
                    Console.ResetColor();
                    Console.Write(": ");
                    string ans_m = Console.ReadLine();
                    if (ans_m.ToLower() == "l")
                    {
                        Console.Write("Enter device letter (without colon): ");
                        device_letter = Console.ReadLine();
                        device_letter.ToUpper();
                    }
                    if (ans_m.ToLower() == "n")
                    {
                        Console.Write("Enter the volume name: ");
                        volume = Console.ReadLine();
                    }
                    if (ans_m.ToLower() == "h")
                    {
                        Console.WriteLine("\nHELP");
                        Console.WriteLine("\"L\" is the driver letter.");
                        Console.Write("To show the volume name open CMD (");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("Press \"C\"");
                        Console.ResetColor();
                        Console.Write(") and type \"mountvol\". The parameter that starts with \"\\\\?\\\\Volume{\" is the \"volume name\", copy and paste it in settings: ");
                        string cmd_check = Console.ReadLine();
                        if (cmd_check.ToLower() == "c")
                        {
                            Process.Start("cmd");
                        }
                        ans_m = "";
                    }
                    if (ans_m.ToLower() == "c")
                    {
                        device_letter = "NO DRIVE LETTER";
                        volume = "NO VOLUME NAME";
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\a\nWARNING: Mount and Unmount backup device prevent to compromise the backup in case of virus attack.\n");
                        Console.ResetColor();
                    }
                    string[] lines = { "@echo off", "set drive=" + device_letter.ToUpper(), "set volume=" + volume, "mountvol %drive%: %volume%" };
                    File.WriteAllText(file_mount, string.Empty);
                    File.WriteAllLines(file_mount, lines);
                    file.Close();
                }
                if (ans_check.ToLower() == "u")
                {
                    StreamReader file_un = new StreamReader(file_unmount);
                    file_un.ReadLine();
                    string device_letter_unmount = file_un.ReadLine();
                    file_un.Close();
                    Console.Write("\nTo change Unmount letter ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"L\"");
                    Console.ResetColor();
                    Console.Write(", to erase settings ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Press \"C\"");
                    Console.ResetColor();
                    Console.Write(": ");
                    string ans_un = Console.ReadLine();
                    if (ans_un.ToLower() == "l")
                    {
                        Console.Write("Enter Letter to \"Unmount\" (without colon): ");
                        device_letter_unmount = Console.ReadLine();
                        string[] lines_un = { "@echo off", "set drive=" + device_letter_unmount.ToUpper() + ":", "mountvol %drive% /p" };
                        File.WriteAllLines(file_unmount, lines_un);
                    }
                    if (ans_un.ToLower() == "c")
                    {
                        device_letter_unmount = "NO DRIVE LETTER";
                        string[] lines_un = { "@echo off", "set drive=" + device_letter_unmount.ToUpper() + ":", "mountvol %drive% /p" };
                        File.WriteAllLines(file_unmount, lines_un);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\a\nWARNING: Mount and Unmount backup device prevent to compromise the backup in case of virus attack.\n");
                        Console.ResetColor();
                    }
                }
                if (ans_check.ToLower() == "e")
                {
                    exit_check = true;
                }
            }
            line_printer();
        }

        private static void change_destination_device(string file_device_dest_path, string file_device_on_off)
        {
            Console.WriteLine();
            line_printer();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Device Destination Settings");
            Console.ResetColor();
            string dest = File.ReadAllText(file_device_dest_path);
            Console.Write("\tDestination Folder: ");
            if (dest.Length < 3)
            {
                Console.WriteLine("\tNo folder set");
            }
            else
            {
                Console.WriteLine(dest);
            }
            Console.Write("To change folder path ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Press \"F\"");
            Console.ResetColor();
            Console.Write(", to erase setting ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Press \"C\"");
            Console.ResetColor();
            Console.Write(": ");
            string ans = Console.ReadLine();
            if (ans.ToLower() == "f")
            {
                Console.Write("Enter folder path: ");
                File.WriteAllText(file_device_dest_path, Console.ReadLine());
            }
            if (ans.ToLower() == "c")
            {
                File.WriteAllText(file_device_dest_path, "");
                Console.Write("\tSettings successfully erased");
            }
        }

        private static void settings_check(string file_to_backup, string file_server_info, string file_del_days, string file_device_on_off, string file_ftp_on_off, string file_mount, string file_unmount, string file_device_dest_path)
        {
            Console.WriteLine();
            line_printer();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Settings check\n");
            Console.ResetColor();
            Console.Write("\tChecking folder to backup");
            if (new FileInfo(file_to_backup).Length <= 2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\a\tERROR: no files to backup");
                Console.ResetColor();
                change_file_backup(file_to_backup);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tOK");
                Console.ResetColor();
            }
            Console.WriteLine();
            Console.Write("\tChecking backup type");
            if (File.ReadAllText(file_device_on_off)=="0" && File.ReadAllText(file_ftp_on_off) == "0")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\a\tERROR: at least one backup type must be enabled");
                Console.ResetColor();
                change_bakcup_type(file_device_on_off, file_ftp_on_off);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tOK");
                Console.ResetColor();
            }
            Console.WriteLine();
            Console.Write("\tChecking FTP backup settings");
            if (File.ReadAllText(file_ftp_on_off) == "1")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tEnabled");
                Console.ResetColor();
                bool flag = false;
                StreamReader server_check = new StreamReader(file_server_info);
                try
                {
                    if (server_check.ReadLine().Length < 2 || server_check.ReadLine().Length < 2 || server_check.ReadLine().Length < 2 || server_check.ReadLine().Length < 2)
                    {
                        flag = true;
                    }
                }
                catch
                {
                    flag = true;
                }
                server_check.Close();
                if (flag)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\a\t\tERROR: some empty fields in Server Info");
                    Console.ResetColor();
                    change_server_info(file_server_info, file_ftp_on_off);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\tNot enabled");
                Console.ResetColor();
            }
            Console.WriteLine();
            Console.Write("\tChecking Device backup settings");
            if (File.ReadAllText(file_device_on_off) == "1")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tEnabled");
                Console.ResetColor();
                StreamReader file = new StreamReader(file_mount);
                file.ReadLine();
                string device_letter = file.ReadLine();
                string volume = file.ReadLine();
                volume = volume.Replace("set volume=", "");
                device_letter = device_letter.Replace("set drive=", "");
                file.Close();
                StreamReader file_un_check = new StreamReader(file_unmount);
                file_un_check.ReadLine();
                string device_letter_unmount_check = file_un_check.ReadLine();
                device_letter_unmount_check = device_letter_unmount_check.Replace("set drive=", "");
                file_un_check.Close();
                if (device_letter == "NO DRIVE LETTER" || volume == "NO VOLUME NAME" || device_letter_unmount_check == "NO DRIVE LETTER:")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\t\tWARNING: some empty values in Mount/Unmount settings.");
                    Console.ResetColor();
                }
                Console.Write("\t\tDestination folder: ");
                if (File.ReadAllText(file_device_dest_path).Length < 3)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\a\tERROR: no destination folder");
                    Console.ResetColor();
                    change_destination_device(file_device_dest_path, file_device_on_off);
                }
                else
                {
                    Console.Write(File.ReadAllText(file_device_dest_path));
                }
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\tNot enabled");
                Console.ResetColor();
            }
            Console.WriteLine();
            Console.Write("\tChecking Days Limit settings");
            string del_limit_check = File.ReadAllText(file_del_days);
            if (del_limit_check.Length < 1 || File.ReadAllText(file_del_days)=="0")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n\t\tWARNING: No Days Limit set, it's useful to keep memory free");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tOK");
                Console.ResetColor();
            }
            Console.WriteLine();
            line_printer();
            Console.WriteLine();
        }

        static void copy_fun(List<string> dir_address, string dir_to_zip)
        {
            foreach (string fold in dir_address)
            {
                string fold_name = fold.Split('\\').Last();
                string dest_path = Path.Combine(dir_to_zip, fold_name);
                Directory.CreateDirectory(dest_path);
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("\nCopying \"{0}\"\n", fold_name);
                Console.ResetColor();

                copy_par_2(fold, dest_path);
            }
        }

        private static void copy_par_2(string src, string dest)
        {
            DirectoryInfo dir = new DirectoryInfo(src);
            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }
            FileInfo[] files = dir.GetFiles();
            Console.WriteLine("\t" + dir.Name);
            foreach (FileInfo file in files)
            {
                try
                {
                    {
                        string temppath = Path.Combine(dest, file.Name);
                        file.CopyTo(temppath, false);
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("An error occurred: '{0}'", e);
                    Console.ResetColor();

                }
            }
            foreach (DirectoryInfo subdir in dirs)
            {
                try
                {
                    string temppath = Path.Combine(dest, subdir.Name);
                    copy_par_2(subdir.FullName, temppath);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("An error occurred: '{0}'", e);
                    Console.ResetColor();
                }
            }
        }

        private static void folder_cleaner(string path_folder)
        {
            foreach (string Folder in Directory.GetDirectories(path_folder))
            {
                folder_cleaner(Folder);
                Directory.Delete(Folder, true);
            }
            foreach (string file in Directory.GetFiles(path_folder))
            {
                var pPath = Path.Combine(path_folder, file);
                FileInfo fi = new FileInfo(pPath);
                File.SetAttributes(pPath, FileAttributes.Normal);
                File.Delete(file);
            }
        }

        private static void zip_fun(string dir_zip_zipfun, string dir_up_copy_zip_fun, string file_name)
        {
            Console.Write("\nZipping");
            Process zip = new Process();
            zip.StartInfo.FileName = @"C:\Program Files\WinRAR\Rar.exe";
            string name = dir_up_copy_zip_fun + "\\" + file_name;
            zip.StartInfo.Arguments = "a " + name + " " + dir_zip_zipfun;
            zip.Start();
            zip.WaitForExit();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\t OK\n");
            Console.ResetColor();
        }

        private static void mount_device(string mount_file)
        {
            //REMIND TO RUN EXE AS ADMIN
            Console.WriteLine();
            Console.Write("Mounting device");
            try
            {
                ProcessStartInfo procInfo = new ProcessStartInfo();
                procInfo.UseShellExecute = true;
                procInfo.FileName = Path.GetFileName(mount_file);  //The file in that DIR.
                procInfo.WorkingDirectory = mount_file.Replace(Path.GetFileName(mount_file), ""); //The working DIR.
                procInfo.Verb = "runas";
                var process= Process.Start(procInfo);  //Start that process.
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\t OK\n");
            Console.ResetColor();
        }

        private static void unmount_device(string unmount_file)
        {
            //REMIND TO RUN EXE AS ADMIN

            Console.Write("Unmounting device");
            try
            {
                ProcessStartInfo procInfo = new ProcessStartInfo();
                procInfo.UseShellExecute = true;
                procInfo.FileName = Path.GetFileName(unmount_file);  //The file in that DIR.
                procInfo.WorkingDirectory = unmount_file.Replace(Path.GetFileName(unmount_file), ""); //The working DIR.
                procInfo.Verb = "runas";
                Process.Start(procInfo);  //Start that process.
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\t OK\n");
            Console.ResetColor();
        }

        private static bool ftp_upload(string server_info, string file_to_upload_path)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            int width = Console.WindowWidth;
            for (int asterisk = 0; asterisk < width; asterisk++)
            {
                Console.Write("*");
            }
            string message = "FTP UPLOAD";
            for (int wel = 0; wel < (width - message.Length) / 2; wel++)
            {
                Console.Write("*");
            }
            Console.Write(message);
            for (int wel = 0; wel < (width - message.Length) / 2; wel++)
            {
                Console.Write("*");
            }
            for (int asterisk = 0; asterisk < width; asterisk++)
            {
                Console.Write("*");
            }
            Console.ResetColor();

            System.IO.StreamReader info_file = new System.IO.StreamReader(server_info);
            string ftp_server = info_file.ReadLine();
            string user = info_file.ReadLine();
            string pass = info_file.ReadLine();
            string server_folder_path = info_file.ReadLine();

            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                Console.Write("Contacting the server");
                client.Credentials = new System.Net.NetworkCredential(user, pass);
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("\tOK");
                Console.ResetColor();
                Console.Write("Uploading file");

                try
                {
                    client.UploadFile(ftp_server + server_folder_path + new FileInfo(file_to_upload_path).Name, "STOR", file_to_upload_path);
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("\tOK");
                    Console.ResetColor();
                }
                catch (Exception exc)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n\a");
                    line_printer(); line_printer();
                    Console.WriteLine("UPLOAD FAIL");
                    Console.WriteLine("Check Internet connection, check if IP is enabled to upload and check the destination path.");
                    Console.Write("\n");
                    Console.WriteLine("Error details:");
                    Console.WriteLine(exc);
                    line_printer(); line_printer();
                    Console.ResetColor();
                    return false;
                }
                Console.ResetColor();
                return true;
            }
        }

        private static String Convert_BytesToString(long byteCount)
        {
            string[] suf = { " B", " KB", " MB", " GB", " TB", " PB", " EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        private static string folder_dimension(string dir_path)
        {
            DirectoryInfo dir_info = new DirectoryInfo(dir_path);
            Int64 size = 0;
            foreach (FileInfo file_info in dir_info.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                size += file_info.Length;
            }

            return Convert_BytesToString(size);
        }

        private static void CSV_update(string csv_file, string file_name, string device_backup_on_off, string ftp_backup_on_off, TimeSpan upload_time, TimeSpan tot_timer, TimeSpan copy_timer, string upload_size, string CSV_copy)
        {
            var str_type = new StringBuilder();
            string name = file_name;
            string date = DateTime.Today.ToShortDateString();
            string upload_t = string.Format("{0:D2}H {1:D2}M {2:D2}s", upload_time.Hours, upload_time.Minutes, upload_time.Seconds);
            string tot_t = string.Format("{0:D2}H {1:D2}M {2:D2}s", tot_timer.Hours, tot_timer.Minutes, tot_timer.Seconds);
            string copy_t= string.Format("{0:D2}H {1:D2}M {2:D2}s", copy_timer.Hours, copy_timer.Minutes, copy_timer.Seconds);
            string machine_name = Environment.MachineName;
            string dev_check = File.ReadAllText(device_backup_on_off);
            string ftp_check = File.ReadAllText(ftp_backup_on_off);
            string backup_type="";
            if (dev_check == "1" && ftp_check == "1")
            {
                backup_type = "DEV & FTP";
            }
            if (dev_check=="1" && ftp_check == "0")
            {
                backup_type = "DEV";
            }
            if (dev_check=="0" && ftp_check == "1")
            {
                backup_type = "FTP";
            }
            string new_line = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}\n", name, date, upload_size, backup_type, machine_name, copy_timer, upload_t, tot_t);
            str_type.Append(new_line);
            File.AppendAllText(csv_file, str_type.ToString());

            string copy_file = File.ReadAllText(CSV_copy);
            if (!string.IsNullOrEmpty(copy_file))
            {
                try
                {
                    string file_to_copy = csv_file.Substring(csv_file.LastIndexOf(@"\") + 1);
                    File.Copy(csv_file, copy_file + @"\" + file_to_copy, true); //"true" to overwrite
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\aERROR WHILE COPYING \"backup_history\" FILE, error description:\n\t{0}", e);
                    Console.ResetColor();
                }
            }
        }

        private static void del_old_file_ftp(string server_info, int days)
        {
            StreamReader file = new StreamReader(server_info);
            string ftp_address = file.ReadLine();
            string user = file.ReadLine();
            string pass = file.ReadLine();
            string dest = file.ReadLine();
            file.Close();

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftp_address + dest);
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                request.Credentials = new NetworkCredential(user, pass);
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream response_stream = response.GetResponseStream();

                StreamReader reader = new StreamReader(response_stream);

                string folder_reader = "check";
                string file_name; string file_date_str; DateTime file_date;
                while (!string.IsNullOrEmpty(folder_reader))
                {
                    folder_reader = reader.ReadLine();
                    if (!string.IsNullOrEmpty(folder_reader))
                    {
                        file_name = folder_reader.Substring(folder_reader.LastIndexOf(" ") + 1);
                        folder_reader = folder_reader.Remove(folder_reader.LastIndexOf(" ")); //del name
                        folder_reader = folder_reader.Remove(folder_reader.LastIndexOf(" ")); //del hour
                        if (file_name.Length > 4)
                        {
                            file_date_str = folder_reader.Substring(folder_reader.LastIndexOf(" ") + 1); //add day
                            folder_reader = folder_reader.Remove(folder_reader.LastIndexOf(" ")); //del day
                            if (file_date_str.Length == 1)
                            {
                                folder_reader = folder_reader.Remove(folder_reader.LastIndexOf(" ")); //del space
                                file_date_str += " " + folder_reader.Substring(folder_reader.LastIndexOf(" ") + 1); 
                                //add month if date has one number
                            }
                            else
                            {
                                file_date_str += " " + folder_reader.Substring(folder_reader.LastIndexOf(" ") + 1);
                                //add month if date has >=2 numbers
                            }
                            file_date = Convert.ToDateTime(file_date_str); //convert to Date

                            if (file_date < DateTime.Now.AddDays(days * -1).Date)
                            {
                                request = (FtpWebRequest)WebRequest.Create(ftp_address + dest + file_name);
                                request.Method = WebRequestMethods.Ftp.DeleteFile;
                                request.Credentials = new NetworkCredential(user, pass);

                                response = (FtpWebResponse)request.GetResponse();
                                Console.WriteLine("\nDeleted: {0}\t{1}", file_name, file_date);
                            }
                        }
                    }
                }
                reader.Close();
                response.Close();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\a");
                Console.WriteLine("An error occurred while deleting old files\nError description: ");
                Console.WriteLine(e);
                Console.ResetColor();
            }
        }

        private static void del_old_file_device(string device_destination, int days)
        {
            string path = File.ReadAllText(device_destination);
            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.LastAccessTime < DateTime.Now.AddDays(days * -1))
                    fi.Delete();
            }
        }
    }

    class Timeout
    {
        private static Thread inputThread;
        private static AutoResetEvent getInput, gotInput;
        private static string input;

        static Timeout()
        {
            getInput = new AutoResetEvent(false);
            gotInput = new AutoResetEvent(false);
            inputThread = new Thread(reader);
            inputThread.IsBackground = true;
            inputThread.Start();
        }

        private static void reader()
        {
            while (true)
            {
                getInput.WaitOne();
                input = Console.ReadLine();
                gotInput.Set();
            }
        }

        public static bool ReadLine(int timeOutMillisecs)
        {
            getInput.Set();
            bool success = gotInput.WaitOne(timeOutMillisecs);
            if (success)
                return true;
            else
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\t No input");
            Console.ResetColor();
            return false;
        }
    }
}
