using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Dokan;
using System.IO;

namespace RegistryFS
{
    class RegistryFS : DokanOperations
    {
        #region DokanOperations member

        private Dictionary<string, RegistryKey> TopDirectory;

        public RegistryFS()
        {
            TopDirectory = new Dictionary<string, RegistryKey>();
            TopDirectory["HKEY_CLASSES_ROOT"] = Registry.ClassesRoot;
            TopDirectory["HKEY_CURRENT_USER"] = Registry.CurrentUser;
            TopDirectory["HKEY_CURRENT_CONFIG"] = Registry.CurrentConfig;
            TopDirectory["HKEY_LOCAL_MACHINE"] = Registry.LocalMachine;
            TopDirectory["HKEY_USERS"] = Registry.Users;
        }

        public int Cleanup(string filename, DokanFileInfo info)
        {
            return 0;
        }

        public int CloseFile(string filename, DokanFileInfo info)
        {
            return 0;
        }

        internal RegistryKey GetFolderKey(string filename)
        {
            RegistryKey k = null;

            if (filename.StartsWith(@"\HKEY_CLASSES_ROOT"))
                k = Registry.ClassesRoot;
            else if (filename.StartsWith(@"\HKEY_CURRENT_USER"))
                k = Registry.CurrentUser;
            else if (filename.StartsWith(@"\HKEY_CURRENT_CONFIG"))
                k = Registry.CurrentConfig;
            else if (filename.StartsWith(@"\HKEY_LOCAL_MACHINE"))
                k = Registry.LocalMachine;
            else if (filename.StartsWith(@"\HKEY_USERS"))
                k = Registry.Users;

            string fld = filename.Replace("\\" + k.Name + "\\", "");

            return k.OpenSubKey(fld);
        }

        

        public int CreateDirectory(string filename, DokanFileInfo info)
        {
            RegistryKey k = null;

            if (filename.StartsWith(@"\HKEY_CLASSES_ROOT"))
                k = Registry.ClassesRoot;
            else if (filename.StartsWith(@"\HKEY_CURRENT_USER"))
                k = Registry.CurrentUser;
            else if (filename.StartsWith(@"\HKEY_CURRENT_CONFIG"))
                k = Registry.CurrentConfig;
            else if (filename.StartsWith(@"\HKEY_LOCAL_MACHINE"))
                k = Registry.LocalMachine;
            else if (filename.StartsWith(@"\HKEY_USERS"))
                k = Registry.Users;

            string fld = filename.Replace("\\" + k.Name + "\\", "");
            string fl = fld.Split('\\')[fld.Split('\\').Count() - 1];
            string d = fld.Replace("\\" + fl, "");

            if (fl == d)
                k.CreateSubKey(fl);
            else
                k.OpenSubKey(d).CreateSubKey(fl);

            k.Close();

            return 0;
        }

        public int CreateFile(
            string filename,
            System.IO.FileAccess access,
            System.IO.FileShare share,
            System.IO.FileMode mode,
            System.IO.FileOptions options,
            DokanFileInfo info)
        {
            RegistryKey k = null;

            if (filename.StartsWith(@"\HKEY_CLASSES_ROOT"))
                k = Registry.ClassesRoot;
            else if (filename.StartsWith(@"\HKEY_CURRENT_USER"))
                k = Registry.CurrentUser;
            else if (filename.StartsWith(@"\HKEY_CURRENT_CONFIG"))
                k = Registry.CurrentConfig;
            else if (filename.StartsWith(@"\HKEY_LOCAL_MACHINE"))
                k = Registry.LocalMachine;
            else if (filename.StartsWith(@"\HKEY_USERS"))
                k = Registry.Users;

            string fld = filename.Replace("\\" + k.Name + "\\", "");
            string fl = fld.Split('\\')[fld.Split('\\').Count() - 1];
            string d = fld.Replace("\\" + fl, "");

            if (fl == d)
                k.CreateSubKey(fl);
            else
                k.OpenSubKey(d).CreateSubKey(fl);

            k.Close();

            return 0;
        }



        public int DeleteDirectory(string filename, DokanFileInfo info)
        {
            RegistryKey k = GetFolderKey(filename);

            string fld = filename.Replace("\\" + k.Name + "\\", "");
            string fl = fld.Split('\\')[fld.Split('\\').Count() - 1];
            string d = fld.Replace("\\" + fl, "");

            if (fl == d)
                k.DeleteSubKeyTree(fl);
            else
                k.OpenSubKey(d).DeleteSubKeyTree(fl);

            return 0;
        }

        public int DeleteFile(string filename, DokanFileInfo info)
        {
            return -1;
        }


        private RegistryKey GetRegistoryEntry(string name)
        {
            int top = name.IndexOf('\\', 1) - 1;
            if (top < 0)
                top = name.Length - 1;

            string topname = name.Substring(1, top);
            int sub = name.IndexOf('\\', 1);

            if (TopDirectory.ContainsKey(topname))
            {
                if (sub == -1)
                    return TopDirectory[topname];
                else
                    return TopDirectory[topname].OpenSubKey(name.Substring(sub + 1));
            }
            return null;
        }

        public int FlushFileBuffers(
            string filename,
            DokanFileInfo info)
        {
            return -1;
        }

        public int FindFiles(
            string filename,
            System.Collections.ArrayList files,
            DokanFileInfo info)
        {
            if (filename == "\\")
            {
                foreach (string name in TopDirectory.Keys)
                {
                    FileInformation finfo = new FileInformation();
                    finfo.FileName = name;
                    finfo.Attributes = System.IO.FileAttributes.Directory;
                    finfo.LastAccessTime = DateTime.Now;
                    finfo.LastWriteTime = DateTime.Now;
                    finfo.CreationTime = DateTime.Now;
                    files.Add(finfo);
                }
                return 0;
            }
            else
            {
                RegistryKey key = GetRegistoryEntry(filename);
                if (key == null)
                    return -1;
                foreach (string name in key.GetSubKeyNames())
                {
                    FileInformation finfo = new FileInformation();

                    finfo.FileName = name;
                    finfo.Attributes = System.IO.FileAttributes.Directory;
                    finfo.LastAccessTime = DateTime.Now;
                    finfo.LastWriteTime = DateTime.Now;
                    finfo.CreationTime = DateTime.Now;
                    files.Add(finfo);
                }
                foreach (string name in key.GetValueNames())
                {
                    FileInformation finfo = new FileInformation();
                    var fname = name == "" ? "(Default).txt" : name;
                    string ext;
                    switch(key.GetValueKind(name))
                    {
                        case RegistryValueKind.String:
                        case RegistryValueKind.DWord:
                        case RegistryValueKind.QWord:
                        case RegistryValueKind.ExpandString:
                        case RegistryValueKind.MultiString:
                        case RegistryValueKind.None:
                            ext = ".txt";
                            break;
                        case RegistryValueKind.Binary:
                            ext = ".bin";
                            break;
                        case RegistryValueKind.Unknown:
                            ext = ".unknown";
                            break;
                        default:
                            ext = ".txt";
                            break;
                    }
                    finfo.FileName = fname + ext;
                    finfo.Length = key.GetValue(name).ToString().Length;
                    finfo.Attributes = System.IO.FileAttributes.Normal;
                    finfo.LastAccessTime = DateTime.Now;
                    finfo.LastWriteTime = DateTime.Now;
                    finfo.CreationTime = DateTime.Now;
                    files.Add(finfo);
                }
                return 0;
            }
        }


        public int GetFileInformation(
            string filename,
            FileInformation fileinfo,
            DokanFileInfo info)
        {
            if (filename == "\\")
            {
                fileinfo.Attributes = System.IO.FileAttributes.Directory;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                fileinfo.CreationTime = DateTime.Now;

                return 0;
            }

            RegistryKey key = GetRegistoryEntry(filename);
            if (key == null)
                return -1;

            fileinfo.Attributes = System.IO.FileAttributes.Directory;
            fileinfo.LastAccessTime = DateTime.Now;
            fileinfo.LastWriteTime = DateTime.Now;
            fileinfo.CreationTime = DateTime.Now;

            return 0;
        }

        public int LockFile(
            string filename,
            long offset,
            long length,
            DokanFileInfo info)
        {
            return 0;
        }

        public int MoveFile(
            string filename,
            string newname,
            bool replace,
            DokanFileInfo info)
        {
            //RegistryKey

            return -1;
        }

        public int OpenDirectory(string filename, DokanFileInfo info)
        {
            return 0;
        }

        private string clearf(string fname)
        {
            string a = fname.Replace("\\" + fname.Split('\\')[fname.Split('\\').Count() - 1], "");
            return fname.Replace(a + "\\", "");
        }

        public int ReadFile(
            string filename,
            byte[] buffer,
            ref uint readBytes,
            long offset,
            DokanFileInfo info)
        {
            string a = filename.Replace("\\" + filename.Split('\\')[filename.Split('\\').Count() - 1], "");
            RegistryKey k = GetRegistoryEntry(a);

            try
            {
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(k.GetValue(clearf(filename).Replace(Path.GetExtension(clearf(filename)), "")).ToString()));
                if (ms == null)
                    return 0;

                ms.Seek(offset, SeekOrigin.Begin);
                readBytes = (uint)ms.Read(buffer, 0, buffer.Length);
                return 0;
            }
            catch(Exception e)
            {
                return 0;
            }
        }

        public int SetEndOfFile(string filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int SetFileAttributes(
            string filename,
            System.IO.FileAttributes attr,
            DokanFileInfo info)
        {
            return -1;
        }

        public int SetFileTime(
            string filename,
            DateTime ctime,
            DateTime atime,
            DateTime mtime,
            DokanFileInfo info)
        {
            return -1;
        }

        public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int Unmount(DokanFileInfo info)
        {
            return 0;
        }

        public int GetDiskFreeSpace(
           ref ulong freeBytesAvailable,
           ref ulong totalBytes,
           ref ulong totalFreeBytes,
           DokanFileInfo info)
        {
            freeBytesAvailable = 512 * 1024 * 1024;
            totalBytes = 1024 * 1024 * 1024;
            totalFreeBytes = 512 * 1024 * 1024;
            return 0;
        }

        public int WriteFile(
            string filename,
            byte[] buffer,
            ref uint writtenBytes,
            long offset,
            DokanFileInfo info)
        {
            return -1;
        }

        #endregion
    }

    class Program
    {
        static void Main(string[] args)
        {
            string p = "r:\\";
            if(args.Count() == 1)
            {
                if (char.IsLetter(args[0][0]) && args[0][1] == ':' && args[0][2] == '\\')
                    p = args[0];
            }
            DokanOptions opt = new DokanOptions
            {
                MountPoint = p,
                DebugMode = true,
                UseStdErr = true,
                NetworkDrive = false,
                RemovableDrive = true,     // provides an "eject"-menu to unmount
                UseKeepAlive = true,  // auto-unmount
                ThreadCount = 0,      // 0 for default, 1 for debugging
                VolumeLabel = "Registry"
            };

            


            int status = DokanNet.DokanMain(opt, new RegistryFS());
            switch (status)
            {
                case DokanNet.DOKAN_DRIVE_LETTER_ERROR:
                    Console.WriteLine("Incorrect drive letter");
                    break;
                case DokanNet.DOKAN_DRIVER_INSTALL_ERROR:
                    Console.WriteLine("Driver install error");
                    break;
                case DokanNet.DOKAN_MOUNT_ERROR:
                    Console.WriteLine("Mount error");
                    break;
                case DokanNet.DOKAN_START_ERROR:
                    Console.WriteLine("Start error");
                    break;
                case DokanNet.DOKAN_ERROR:
                    Console.WriteLine("Unknown error");
                    break;
                case DokanNet.DOKAN_SUCCESS:
                    Console.WriteLine("Success");
                    break;
                default:
                    Console.WriteLine("Unknown status: %d", status);
                    break;

            }
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
