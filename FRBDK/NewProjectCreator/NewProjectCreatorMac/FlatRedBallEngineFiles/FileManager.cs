#if WINDOWS_PHONE  || (XBOX360 && XNA4) || MONODROID || WINDOWS_8 || SILVERLIGHT
#define USE_ISOLATED_STORAGE
#endif

#if XBOX360 || SILVERLIGHT || WINDOWS_PHONE || MONODROID || WINDOWS_8 || IOS
#define USES_DOT_SLASH_ABOLUTE_FILES
#endif

#region Using Statements
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

using System.Xml;
using System.Xml.Serialization;

using System.Text;

#if !FRB_MDX
using System.Xml.Linq;
using System.Linq;

#endif

#if FRB_MDX

#elif FRB_XNA || WINDOWS_PHONE
using Microsoft.Xna.Framework;

#if USE_ISOLATED_STORAGE && !WINDOWS_8
using System.IO.IsolatedStorage;
#endif





#elif SILVERLIGHT
using System.Windows;
using System.Windows.Resources;
using System.IO.IsolatedStorage;
#endif

#if !WINDOWS_8
using File = System.IO.File;
#endif

using System.Collections;


using System.Reflection;

#if !WINDOWS_PHONE
using System.Net;

#endif


#if !FRB_RAW
//using FlatRedBall.Instructions.Reflection;

using System.Runtime.Serialization;
using System.Threading;
using System.Diagnostics;

#endif

#endregion

namespace FlatRedBall.IO
{
    public enum RelativeType
    {
        Relative,
        Absolute
    }

    public static partial class FileManager
    {
        #region Fields

        static bool mHasUserFolderBeenInitialized = false;

#if XBOX360

        static StorageDevice mStorageDevice = null;
        static StorageContainer mLastStorageContainer = null;
#endif

#if SILVERLIGHT || WINDOWS_PHONE || (XBOX360 && XNA4) || MONOGAME
        static string DefaultRelativeDirectory = "./";
#else
        // Vic says - this used to be:
        //static string mRelativeDirectory = (System.IO.Directory.GetCurrentDirectory() + "/").ToLower().Replace("\\", "/");
        // But the current directory is the directory that launched the application, not the directory of the .exe.
        // We want to make sure that we use the .exe so that the game/tool can reference the proper path when loading
        // content.
        // Update: Made this per-thread so we can do multi-threaded loading.
        // static string mRelativeDirectory = (System.Windows.Forms.Application.StartupPath + "/").ToLower().Replace("\\", "/");
        // Update October 22, 2012 - Projects like Glue may be multi-threaded, but they want the default directory to be preset to
        // something specific.  But I think we only want this for tools (on the PC).
        public static string DefaultRelativeDirectory = (System.Windows.Forms.Application.StartupPath + "/").ToLowerInvariant().Replace("\\", "/");

#endif

        static Dictionary<int, string> mRelativeDirectoryDictionary = new Dictionary<int, string>();



        static Dictionary<string, object> mFileCache = new Dictionary<string, object>();

        static Dictionary<Type, XmlSerializer> mXmlSerializers = new Dictionary<Type, XmlSerializer>();

        static XmlReaderSettings mXmlReaderSettings = new XmlReaderSettings();

        #endregion

        #region Properties

        public static string CurrentDirectory
        {
            get
            {
#if WINDOWS_8
                throw new NotImplementedException();
#else
                return (System.IO.Directory.GetCurrentDirectory() + "/").Replace("\\", "/");

#endif

            }
            set
            {
#if WINDOWS_8
                throw new NotImplementedException();
#else
                System.IO.Directory.SetCurrentDirectory(value);
#endif
            }
        }

        public static bool PreserveCase
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The directory that FlatRedBall will use when loading assets.  Defaults to the application's directory.
        /// </summary>
        #endregion
        static public string RelativeDirectory
        {
            get
            {
#if WINDOWS_8
                int threadID = Environment.CurrentManagedThreadId;
#else
                int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif                
                if (mRelativeDirectoryDictionary.ContainsKey(threadID))
                {
                    return mRelativeDirectoryDictionary[threadID];
                }
                else
                {
                    return DefaultRelativeDirectory;
                }

            }
            set
            {

                if (FileManager.IsRelative(value))
                {
                    throw new InvalidOperationException("Relative Directory must be an absolute path");
                }

                string valueToSet = value;

#if USES_DOT_SLASH_ABOLUTE_FILES
                // On the Xbox 360 the way to specify absolute is to put a '/' before
                // a file name.
                if (value.Length > 1 && (value[0] != '.' || value[1] != '/'))
                {
                    valueToSet = Standardize("./" + value.Replace("\\", "/"));
                }
                else if (value.Length == 0)
                {
                    valueToSet = "./";
                }
                else
                {
                    valueToSet = Standardize(value.Replace("\\", "/"));
                }

#else
                ReplaceSlashes(ref valueToSet);
                valueToSet = Standardize(valueToSet, "", false);

#endif

                if (!string.IsNullOrEmpty(valueToSet) && !valueToSet.EndsWith("/"))
                    valueToSet += "/";



#if WINDOWS_8
                int threadID = Environment.CurrentManagedThreadId;
#else

                int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif

                lock (mRelativeDirectoryDictionary)
                {
                    if (valueToSet == DefaultRelativeDirectory)
                    {
                        if (mRelativeDirectoryDictionary.ContainsKey(threadID))
                        {
                            mRelativeDirectoryDictionary.Remove(threadID);
                        }
                    }
                    else
                    {
                        if (mRelativeDirectoryDictionary.ContainsKey(threadID))
                        {
                            mRelativeDirectoryDictionary[threadID] = valueToSet;
                        }
                        else
                        {
                            mRelativeDirectoryDictionary.Add(threadID, valueToSet);
                        }
                    }
                }
            }
        }

#if !XBOX360 && !SILVERLIGHT && !WINDOWS_PHONE && !MONODROID && !WINDOWS_8


        public static string StartupPath
        {
            get
            {
#if IOS
				return "./";
#else


                return (System.Windows.Forms.Application.StartupPath + "/");
#endif
			}
        }

        public static string MyDocuments
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\"; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the path to the user specific application data directory.
        /// </summary>
        /// <remarks>If your game/application will be writing anything to the file system, you will want 
        /// to do so somewhere in this directory.  The reason for this is because you cannot anticipate
        /// whether the user will have the needed permissions to write to the directory where the 
        /// executable lives.</remarks>
        /// <example>C:\Documents and Settings\&lt;username&gt;\Application Data</example> 
        #endregion
        public static string UserApplicationData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\"; }
        }

        public static string UserApplicationDataForThisApplication
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();

                string applicationDataName = assembly == null ? "" : Assembly.GetEntryAssembly().FullName;
				if(string.IsNullOrEmpty (applicationDataName))
				{
					applicationDataName = @"FRBDefault";
				}
				else
				{
					applicationDataName = applicationDataName.Substring(0, applicationDataName.IndexOf(','));
				}

#if IOS
				var documents = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
				string folder = Path.Combine (documents, "..", "Library");

                folder = FileManager.RemoveDotDotSlash(folder);

                // Make it absolute:
                folder = "." + folder;
#else
				string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif
				folder =  folder + @"\" + applicationDataName + @"\";

#if IOS
                folder = folder.Replace('\\', '/');
#endif
                return folder;

            }
        }

#endif
        #endregion

        #region Methods

        #region Constructor

        static FileManager()
        {
        }

        #endregion

        #region Public Methods

        #region Caching Methods
        public static void CacheObject(object objectToCache, string fileName)
        {
            mFileCache.Add(Standardize(fileName), objectToCache);
        }


        public static object GetCachedObject(string fileName)
        {
            return mFileCache[Standardize(fileName)];
        }


        public static bool IsCached(string fileName)
        {
            return mFileCache.ContainsKey(Standardize(fileName));
        }




        #endregion

        public static T CloneObject<T>(T objectToClone)
        {
            string container;

            XmlSerialize(objectToClone, out container);

            XmlSerializer serializer = GetXmlSerializer(typeof(T));// new XmlSerializer(type);

            return (T)serializer.Deserialize(new StringReader(container));
        }
#if !SILVERLIGHT && !WINDOWS_8
        public static void CopyDirectory(string sourceDirectory, string destDirectory, bool deletePrevious, List<string> excludeFiles, List<string> excludeDirectories)
        {
            if (excludeDirectories != null)
            {
                string currentDir;

                for (int i = 0; i < excludeDirectories.Count; ++i)
                {
                    currentDir = excludeDirectories[i];

                    currentDir = RemovePath(currentDir);
                    currentDir = currentDir.TrimEnd(new char[] { '\\', '/' });
                    excludeDirectories[i] = currentDir;
                }
            }

            if (excludeFiles != null)
            {
                string currentFile;

                for (int i = 0; i < excludeFiles.Count; ++i)
                {
                    currentFile = excludeFiles[i];

                    currentFile = RemovePath(currentFile);
                    currentFile = Standardize(currentFile);
                    excludeFiles[i] = currentFile;
                }
            }

            CopyDirectoryHelper(sourceDirectory, destDirectory, deletePrevious, excludeFiles, excludeDirectories);
        }

        public static void CopyDirectory(string sourceDirectory, string destDirectory, bool deletePrevious)
        {
            CopyDirectoryHelper(sourceDirectory, destDirectory, deletePrevious, null, null);
        }


        public static void DeleteDirectory(string directory)
        {
#if WINDOWS_8
            throw new NotImplementedException();
#else
            string[] dirList = Directory.GetDirectories(directory);
            foreach (string dir in dirList)
            {
                DeleteDirectory(dir);
            }

            string[] fileList = Directory.GetFiles(directory);
            foreach (string file in fileList)
            {
                File.Delete(file);
            }

            Directory.Delete(directory);
#endif
        }
#endif


        public static void DeleteFile(string fileName)
        {
#if USE_ISOLATED_STORAGE
            DeleteFileFromIsolatedStorage(fileName);

#else
            System.IO.File.Delete(fileName);
#endif
        }


#if XBOX360
        public static void DisposeLastStorageContainer()
        {
            if (mLastStorageContainer != null)
            {
                mLastStorageContainer.Dispose();
                mLastStorageContainer = null;
            }
        }

#endif

        #region XML Docs
        /// <summary>
        /// Returns whether the file exists considering the relative directory.
        /// </summary>
        /// <param name="fileName">The file to search for.</param>
        /// <returns>Whether the argument file exists.</returns>
        #endregion
        public static bool FileExists(string fileName)
        {
            if (IsRelative(fileName))
            {
                return FileExists(MakeAbsolute(fileName));
            }
            else
            {
#if SILVERLIGHT || USE_ISOLATED_STORAGE
                bool isIsolatedStorageFile = IsInIsolatedStorage(fileName);

                if (isIsolatedStorageFile)
                {
                    return FileExistsInIsolatedStorage(fileName);
                }
                else
                {

                    if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
                        fileName = fileName.Substring(2);
                    fileName = fileName.Replace("\\", "/");
#if SILVERLIGHT


                    Uri uri = new Uri(fileName, UriKind.Relative);

                    StreamResourceInfo sri = Application.GetResourceStream(uri);

                    return sri != null;
#else
                    Stream stream = null;
                    // This method tells us if a file exists.  I hate that we have 
                    // to do it this way - the TitleContainer should have a FileExists
                    // property to avoid having to do logic off of exceptions.  <sigh>
                    try
                    {
                        stream = TitleContainer.OpenStream(fileName);
                    }
#if MONODROID
                    catch (Java.IO.FileNotFoundException fnfe)
                    {
                        return false;
                    }
#else
                    catch (FileNotFoundException fnfe)
                    {
                        return false;
                    }
#endif

                    if (stream != null)
                    {
                        stream.Dispose();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
#endif // SILVERLIGHT
                }


#elif XBOX360
                if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
                    fileName = fileName.Substring(2);


                if (IsFileNameInUserFolder(fileName))
                {
                    using (StorageContainer sc = GetStorageContainer())
                    {
                        return System.IO.File.Exists(fileName);
                    }
                }
                else
                {
                    
                    return System.IO.File.Exists(fileName);

                }

#else
            #if IOS
				// Wait, it seems like File.Exist *does* want a '.' at the beginning
				//if(fileName.StartsWith("./"))
				//{
                    // can't start with '.'
				//fileName = fileName.Substring(1);
				//}

            #endif


				if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
					fileName = fileName.Substring(2);

                return System.IO.File.Exists(fileName);
#endif
            }
        }
        

#if !XBOX360 && !SILVERLIGHT && !WINDOWS_8
        #region XML Docs
        /// <summary>
        /// Searches the passed directory and all subdirectories for the passed file.
        /// </summary>
        /// <param name="fileToFind">The name of the file including extension.</param>
        /// <param name="directory">The directory to search in, including all subdirectories.</param>
        /// <returns>The full path of the first file found matching the name, or an empty string if none is found.</returns>
        #endregion
        public static string FindFileInDirectory(string fileToFind, string directory)
        {

            string[] files = System.IO.Directory.GetFiles(directory);
            string[] directories = System.IO.Directory.GetDirectories(directory);

            fileToFind = RemovePath(fileToFind);

            foreach (string file in files)
            {
                if (RemovePath(file).ToLowerInvariant() == fileToFind.ToLowerInvariant())
                    return directory + "/" + fileToFind;
            }

            foreach (string directoryChecking in directories)
            {
                string fileFound = FindFileInDirectory(fileToFind, directoryChecking);
                if (fileFound != "")
                    return fileFound;

            }

            return "";
        }

        #region XML Docs
        /// <summary>
        /// Searches the executable's director and all subdirectories for the passed file.
        /// </summary>
        /// <param name="fileToFind">The name of the file which may or may not include an extension.</param>
        /// <returns>The full path of the first file found matching the name, or an empty string if none is found</returns>
        #endregion
        public static string FindFileInDirectory(string fileToFind)
        {
            return FindFileInDirectory(FileManager.RelativeDirectory);
        }
#endif


        

        public static string FromFileText(string fileName)
        {
#if SILVERLIGHT
            string containedText;

            Uri uri = new Uri(fileName, UriKind.Relative);

            StreamResourceInfo sri = Application.GetResourceStream(uri);
            Stream stream = sri.Stream;
            StreamReader reader = new StreamReader(stream);

            containedText = reader.ReadToEnd();

            stream.Close();
            reader.Close();
            
            return containedText;

#else

            if (IsRelative(fileName))
            {
                fileName = MakeAbsolute(fileName);
            }
            //NM: 14/08/11
            //I changed this to do a standardize as my tilemap files had backwards slashes in them,
            //the RemoveDotDotSlash method on it's own was not working correctly with these filepaths.
            //Standardize on the other hand repalces the slashes and then calls RemoveDotDotSlash.
            //fileName = FileManager.RemoveDotDotSlash(fileName);
            fileName = FileManager.Standardize(fileName);

            string containedText = "";

            // Creating a filestream then using that enables us to open files that are open by other apps.

            Stream fileStream = null;
            try
            {
                // We used to do it this way because it got around the file already being open...but this causes
                // problems on WP7.  Maybe we'll need to branch if the GetStream doesn't work for us.
                //using (FileStream fileStream = new FileStream( new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                //{
                // Update June , 2011
                // We do need to branch
                // because opening a CSV
                // that is already open in 
                // Excel causes a crash otherwise
#if WINDOWS
                fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
#else
                fileStream = GetStreamForFile(fileName);

#endif
                using (System.IO.StreamReader sr = new StreamReader(fileStream))
                {
                    containedText = sr.ReadToEnd();
                    Close(sr);
                }

            }
            finally
            {
                Close(fileStream);

            }


            return containedText;
#endif
        }


        public static byte[] GetByteArrayFromEmbeddedResource(Assembly assembly, string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                throw new NullReferenceException("ResourceName must not be null - can't get the byte array for resource");
            }

            if (assembly == null)
            {
                throw new NullReferenceException("Assembly is null, so can't find the resource\n" + resourceName);
            }

            Stream resourceStream = assembly.GetManifestResourceStream(resourceName);

            if (resourceStream == null)
            {
                string message = "Could not find a resource stream for\n" + resourceName + "\n but found " +
                    "the following names:\n\n";

                foreach (string containedResource in assembly.GetManifestResourceNames())
                {
                    message += containedResource + "\n";
                }


                throw new NullReferenceException(message);
            }

            byte[] buffer = new byte[resourceStream.Length];
            resourceStream.Read(buffer, 0, buffer.Length);
            Close(resourceStream);

            resourceStream.Dispose();
            return buffer;
        }

        #region XML Docs
        /// <summary>
        /// Returns the extension in a filename.
        /// </summary>
        /// <remarks>
        /// The extension returned will not contain a period.
        /// 
        /// <para>
        /// <code>
        /// // this code will return a string containing "png", not ".png"
        /// FileManager.GetExtension(@"FolderName/myImage.png");
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="fileName">The filename.</param>
        /// <returns>Returns the extension or an empty string if no period is found in the filename.</returns>
        #endregion
        public static string GetExtension(string fileName)
        {
            if (fileName == null)
            {
                return "";
            }


            int i = fileName.LastIndexOf('.');
            if (i != -1)
            {
                bool hasDotSlash = false;
                if (i < fileName.Length + 1 && (fileName[i + 1] == '/' || fileName[i + 1] == '\\'))
                {
                    hasDotSlash = true;
                }

                if (hasDotSlash)
                {
                    return "";
                }
                else
                {
                    return fileName.Substring(i + 1, fileName.Length - (i + 1)).ToLowerInvariant();
                }
            }
            else
            {
                return ""; // This returns "" because calling the method with a string like "redball" should return no extension
            }
        }


        public static string GetDirectory(string fileName)
        {
            return GetDirectory(fileName, RelativeType.Absolute);
        }

        public static string GetDirectory(string fileName, RelativeType relativeType)
        {
            int lastIndex = System.Math.Max(
                fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));

            if (lastIndex == fileName.Length - 1)
            {
                // If this happens then fileName is actually a directory.
                // So we should return the parent directory of the argument.



                lastIndex = System.Math.Max(
                    fileName.LastIndexOf('/', fileName.Length - 2),
                    fileName.LastIndexOf('\\', fileName.Length - 2));
            }

            if (lastIndex != -1)
            {
                bool isFtp = false;

                if (FileManager.IsUrl(fileName) || isFtp)
                {
                    // don't standardize URLs - they're case sensitive!!!
                    return fileName.Substring(0, lastIndex + 1);

                }
                else
                {
                    if (relativeType == RelativeType.Absolute)
                    {
                        return FileManager.Standardize(fileName.Substring(0, lastIndex + 1));
                    }
                    else
                    {
                        return FileManager.Standardize(fileName.Substring(0, lastIndex + 1), "", false);
                    }
                }
            }
            else
                return ""; // there was no directory found.

        }

        public static string GetDirectoryKeepRelative(string fileName)
        {
            int lastIndex = System.Math.Max(
                fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));

            if (lastIndex == fileName.Length - 1)
            {
                // If this happens then fileName is actually a directory.
                // So we should return the parent directory of the argument.



                lastIndex = System.Math.Max(
                    fileName.LastIndexOf('/', fileName.Length - 2),
                    fileName.LastIndexOf('\\', fileName.Length - 2));
            }
            if (lastIndex != -1)
            {
                return fileName.Substring(0, lastIndex + 1);
            }
            else
            {
                return "";
            }
        }


        #region GetAllFilesInDirectory


        #region XML Docs
        /// <summary>
        /// Returns a List containing all of the files found in a particular directory and its subdirectories.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <returns></returns>
        #endregion
        public static List<string> GetAllFilesInDirectory(string directory)
        {
#if SILVERLIGHT || USE_ISOLATED_STORAGE

            return GetAllFilesInDirectoryIsolatedStorage(directory);
#else
            List<string> arrayToReturn = new List<string>();

            if (directory == "")
                directory = RelativeDirectory;


            if (directory.EndsWith(@"\") == false && directory.EndsWith("/") == false)
                directory += @"\";


            string[] files = System.IO.Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

            arrayToReturn.AddRange(files);

            
            return arrayToReturn;
#endif
        }

        #region XML Docs
        /// <summary>
        /// Returns a List containing all files which match the fileType argument which are 
        /// in the directory argument or a subfolder.  This recurs, returning all files.
        /// </summary>
        /// <param name="directory">String representing the directory to search.  If an empty
        /// string is passed, the method will search starting in the directory holding the .exe.</param>
        /// <param name="fileType">The file type to search for specified as an extension.  The extension
        /// can either have a period or not.  That is ".jpg" and "jpg" are both valid fileType arguments.  An empty
        /// or null value for this parameter will return all files regardless of file type.</param>
        /// <returns>A list containing all of the files found which match the fileType.</returns>
        #endregion
        public static List<string> GetAllFilesInDirectory(string directory, string fileType)
        {
            return GetAllFilesInDirectory(directory, fileType, int.MaxValue);

        }


        #region XML Docs
        /// <summary>
        /// Returns a List containing all files which match the fileType argument which are within
        /// the depthToSearch folder range relative to the directory argument.
        /// </summary>
        /// <param name="directory">String representing the directory to search.  If an empty
        /// string is passed, the method will search starting in the directory holding the .exe.</param>
        /// <param name="fileType">The file type to search for specified as an extension.  The extension
        /// can either have a period or not.  That is ".jpg" and "jpg" are both valid fileType arguments.  An empty
        /// or null value for this parameter will return all files regardless of file type.</param>
        /// <param name="depthToSearch">The depth to search through.  If the depthToSearch
        /// is 0, only the argument directory will be searched.</param>
        /// <returns>A list containing all of the files found which match the fileType.</returns>
        #endregion
        public static List<string> GetAllFilesInDirectory(string directory, string fileType, int depthToSearch)
        {
            List<string> arrayToReturn = new List<string>();

            GetAllFilesInDirectory(directory, fileType, depthToSearch, arrayToReturn);

            return arrayToReturn;
        }



        public static void GetAllFilesInDirectory(string directory, string fileType, int depthToSearch, List<string> arrayToReturn)
        {
#if SILVERLIGHT || WINDOWS_8
            throw new NotImplementedException();
#else
            if (!Directory.Exists(directory))
            {
                return;
            }

            if (directory == "")
                directory = RelativeDirectory;

            if (directory.EndsWith(@"\") == false && directory.EndsWith("/") == false)
                directory += @"\";

            // if they passed in a fileType which begins with a period (like ".jpg"), then
            // remove the period so only the extension remains.  That is, convert
            // ".jpg" to "jpg"
            if (fileType != null && fileType.Length > 0 && fileType[0] == '.')
                fileType = fileType.Substring(1);

            string[] files = System.IO.Directory.GetFiles(directory);
            string[] directories = System.IO.Directory.GetDirectories(directory);

            if (string.IsNullOrEmpty(fileType))
            {
                arrayToReturn.AddRange(files);
            }
            else
            {
                int fileCount = files.Length;

                for (int i = 0; i < fileCount; i++)
                {
                    string file = files[i];
                    if (GetExtension(file) == fileType)
                    {
                        arrayToReturn.Add(file);
                    }
                }
            }


            if (depthToSearch > 0)
            {
                int directoryCount = directories.Length;
                for (int i = 0; i < directoryCount; i++)
                {
                    string directoryChecking = directories[i];

                    GetAllFilesInDirectory(directoryChecking, fileType, depthToSearch - 1, arrayToReturn);
                }
            }
#endif
        }
        #endregion




        public static bool IsCurrentStorageDeviceConnected()
        {
#if XBOX360
            if (mStorageDevice == null)
            {
                return false;
            }
            else if (!mStorageDevice.IsConnected)
            {
                return false;
            }
#endif


            return true;
        }

        public static string GetUserFolder(string userName)
        {

            if (!mHasUserFolderBeenInitialized)
            {
                throw new InvalidOperationException("The user folder has not been initialized yet.  Please call FileManager.InitializeUserFolder first");
            }

            string stringToReturn = "";

#if USE_ISOLATED_STORAGE
            stringToReturn = IsolatedStoragePrefix + @"\" + userName + @"\";
#elif XBOX360
            mLastUserName = userName;

            if (mStorageDevice == null)
            {
                throw new InvalidOperationException("There is no storage device.  Call InitializeUserFolder");
            }
            else if (!mStorageDevice.IsConnected)
            {
                throw new InvalidOperationException("The storage device is not connected");
            }
            else
            {

                return IsolatedStoragePrefix + @"\" + userName + @"\";
            }
#else
            stringToReturn = FileManager.UserApplicationDataForThisApplication + userName + @"\";
#endif

#if IOS
            stringToReturn = stringToReturn.Replace('\\', '/');

            // let's make sure this thing is absolute
			if(!stringToReturn.StartsWith("./"))
            {
                if(stringToReturn.StartsWith("/"))
                {
                    stringToReturn = "." + stringToReturn;
                }
                else
                {
					stringToReturn = "./" + stringToReturn;
                }
            }

#endif

            return stringToReturn;
        }

#if XBOX360
        // always always always dispose this after getting it!  Or use a using statement
        private static StorageContainer GetStorageContainer()
        {

            var result = mStorageDevice.BeginOpenContainer(mAssemblyName +
                    "___" + mLastUserName, null, null);

            result.AsyncWaitHandle.WaitOne(TimeSpan.FromMinutes(5));

            mLastStorageContainer = mStorageDevice.EndOpenContainer(result);

            return mLastStorageContainer;
        }

        public static void InitializeUserFolder(StorageDevice storageDevice, string applicationName)
        {
            mStorageDevice = storageDevice;
            mHasUserFolderBeenInitialized = true;
            mAssemblyName = applicationName;
        }

#else
        public static void InitializeUserFolder(string userName)
        {
#if SILVERLIGHT || USE_ISOLATED_STORAGE

    #if WINDOWS_8 || IOS
            // I don't know if we need to get anything here
    #else
            mIsolatedStorageFile = IsolatedStorageFile.GetUserStoreForApplication();
    #endif
            mHasUserFolderBeenInitialized = true;

#else
            string directory = FileManager.UserApplicationDataForThisApplication + userName + @"\";

            // iOS doesn't like backslashes:
            directory = directory.Replace("\\", "/");

        #if IOS
			if(directory.StartsWith("./"))
            {
                directory = directory.Substring(1);

            }
        #endif

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            mHasUserFolderBeenInitialized = true;
#endif







        }





#endif


        #region XML Docs
        /// <summary>
        /// Determines whether a particular file is a graphical file that can be loaded by the FRB Engine.
        /// </summary>
        /// <remarks>
        /// This method does conducts the simple test of looking at the extension of the filename.  If the extension inaccurately
        /// represents the actual format of the file, the method may also inaccurately report whether the file is graphical.
        /// </remarks>
        /// <param name="fileToTest">The file name to test.</param>
        /// <returns>Whether the file is a graphic file.</returns>
        #endregion
        public static bool IsGraphicFile(string fileToTest)
        {

            string extension = GetExtension(fileToTest).ToLowerInvariant();

            return (extension == "bmp" || extension == "jpg" || extension == "png" || extension == "tga");
        }


        public static bool IsRelative(string fileName)
        {
            if (fileName == null)
            {
                throw new System.ArgumentException("Cannot check if a null file name is relative.");
            }



#if USES_DOT_SLASH_ABOLUTE_FILES
            if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
            {
                return false;
            }
            // If it's isolated storage, then it's not relative:
            else if (fileName.Contains(IsolatedStoragePrefix))
            {
                return false;
            }
            else
            {
                return true;
            }
#elif MAC
			return !(fileName.Length > 1 && fileName[0] == '/') && 
			  fileName.StartsWith("\\\\") == false;
#else

            // a non-relative directory will have a letter than a : at the beginning.
            // for example c:/file.bmp.  If other cases arise, this may need to be changed.
            // Aha!  If it starts with \\ then it's a network file.  Thsi is absolute.
            return !(fileName.Length > 1 && fileName[1] == ':') && fileName.StartsWith("\\\\") == false;
#endif
        }


        public static bool IsRelativeTo(string fileName, string directory)
        {
            if (!IsRelative(fileName))
            {
                // the filename is an absolute path

                if (!IsRelative(directory))
                {
                    // just have to make sure that the filename includes the path
                    fileName = fileName.ToLowerInvariant().Replace('\\', '/');
                    directory = directory.ToLowerInvariant().Replace('\\', '/');

                    return fileName.IndexOf(directory) == 0;
                }
            }
            else // fileName is relative
            {
                if (IsRelative(directory))
                {
                    // both are relative, so let's make em full and see what happens
                    string fullFileName = FileManager.MakeAbsolute(fileName);
                    string fullDirectory = FileManager.MakeAbsolute(directory);

                    return IsRelativeTo(fullFileName, fullDirectory);

                }
            }
            return false;
        }


        public static bool IsUrl(string fileName)
        {
            return fileName.IndexOf("http:") == 0;
        }


        #region Make Absolute/Make Relative

        public static string MakeAbsolute(string pathToMakeAbsolute)
        {
            if (IsRelative(pathToMakeAbsolute) == false)
            {
                throw new ArgumentException("The path is already absolute: " + pathToMakeAbsolute);
            }

            return Standardize(pathToMakeAbsolute);// RelativeDirectory + pathToMakeAbsolute;
        }


        public static string MakeRelative(string pathToMakeRelative)
        {
            return MakeRelative(pathToMakeRelative, RelativeDirectory);
        }


        public static string MakeRelative(string pathToMakeRelative, string pathToMakeRelativeTo)
        {
            if (string.IsNullOrEmpty(pathToMakeRelative) == false)
            {
                pathToMakeRelative = FileManager.Standardize(pathToMakeRelative);
                pathToMakeRelativeTo = FileManager.Standardize(pathToMakeRelativeTo);
                if (!pathToMakeRelativeTo.EndsWith("/"))
                {
                    pathToMakeRelativeTo += "/";
                }

                // Use the old method if we can
                if (pathToMakeRelative.ToLowerInvariant().StartsWith(pathToMakeRelativeTo.ToLowerInvariant()))
                {
                    pathToMakeRelative = pathToMakeRelative.Substring(pathToMakeRelativeTo.Length);
                }
                else
                {
                    // Otherwise, we have to use the new method to identify the common root

                    // Split the path strings
                    string[] path = pathToMakeRelative.Split('/');
                    string[] relpath = pathToMakeRelativeTo.Split('/');

                    string relativepath = string.Empty;

                    // build the new path
                    int start = 0;
                    // November 1, 2011
                    // Do we want to do this:
                    // March 26, 2012
                    // Yes!  Found a bug
                    // while working on wahoo's
                    // tools that we need to check
                    // "start" against the length of
                    // the string arrays.
                    //while (start < path.Length && start < relpath.Length && path[start] == relpath[start])
                    //while (path[start] == relpath[start])
                    while (start < path.Length && start < relpath.Length && path[start].ToLower() == relpath[start].ToLower())
                    {
                        start++;
                    }

                    // If start is 0, they aren't on the same drive, so there is no way to make the path relative without it being absolute
                    if (start != 0)
                    {
                        // add .. for every directory left in the relative path, this is the shared root
                        for (int i = start; i < relpath.Length; i++)
                        {
                            if (relpath[i] != string.Empty)
                                relativepath += @"../";
                        }

                        // if the current relative path is still empty, and there are more than one entries left in the path,
                        // the file is in a subdirectory.  Start with ./
                        if (relativepath == string.Empty && path.Length - start > 0)
                        {
                            relativepath += @"./";
                        }

                        // add the rest of the path
                        for (int i = start; i < path.Length; i++)
                        {
                            relativepath += path[i];
                            if (i < path.Length - 1) relativepath += "/";
                        }

                        pathToMakeRelative = relativepath;
                    }
                }
            }
            return pathToMakeRelative;

        }

        #endregion


        #region XML Docs
        /// <summary>
        /// Returns the fileName without an extension, or makes no changes if fileName has no extension.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The file name with extension removed if an extension existed.</returns>
        #endregion
        public static string RemoveExtension(string fileName)
        {
            int extensionLength = GetExtension(fileName).Length;

            if (extensionLength == 0)
                return fileName;

            if (fileName.Length > extensionLength && fileName[fileName.Length - (extensionLength + 1)] == '.')
                return fileName.Substring(0, fileName.Length - (extensionLength + 1));
            else
                return fileName;

        }

        #region XML Docs
        /// <summary>
        /// Modifies the fileName by removing its path, or makes no changes if the fileName has no path.
        /// </summary>
        /// <param name="fileName">The file name to change</param>
        #endregion
        public static void RemovePath(ref string fileName)
        {
            int indexOf1 = fileName.LastIndexOf('/', fileName.Length - 1, fileName.Length);
            if (indexOf1 == fileName.Length - 1 && fileName.Length > 1)
            {
                indexOf1 = fileName.LastIndexOf('/', fileName.Length - 2, fileName.Length - 1);
            }

            int indexOf2 = fileName.LastIndexOf('\\', fileName.Length - 1, fileName.Length);
            if (indexOf2 == fileName.Length - 1 && fileName.Length > 1)
            {
                indexOf2 = fileName.LastIndexOf('\\', fileName.Length - 2, fileName.Length - 1);
            }


            if (indexOf1 > indexOf2)
                fileName = fileName.Remove(0, indexOf1 + 1);
            else if (indexOf2 != -1)
                fileName = fileName.Remove(0, indexOf2 + 1);
        }

        #region XML Docs
        /// <summary>
        /// Returns the fileName without a path, or makes no changes if the fileName has no path.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The modified fileName if a path is found.</returns>
        #endregion
        public static string RemovePath(string fileName)
        {
            RemovePath(ref fileName);

            return fileName;
        }

        #region XML Docs
        /// <summary>
        /// Sets the relative directory to the current directory.
        /// </summary>
        /// <remarks>
        /// The current directory is not necessarily the same as the directory of the .exe.  If the 
        /// .exe is called from a different location (such as the command line in a different folder),
        /// the current directory will differ.
        /// </remarks>
        #endregion
        public static void ResetRelativeToCurrentDirectory()
        {
#if WINDOWS_8
            throw new NotImplementedException();
#else
            RelativeDirectory = (System.IO.Directory.GetCurrentDirectory() + "/").Replace("\\", "/");
#endif
        }


        public static void ThrowExceptionIfFileDoesntExist(string fileName)
        {
            // In Silverlight there is no access to System.IO.File.Exists

            if (FileManager.FileExists(fileName) == false)
            {


#if SILVERLIGHT || WINDOWS_8
                throw new FileNotFoundException("Could not find the file " + fileName);
#else

                // Help diagnose the problem
                string directory = GetDirectory(fileName);

                if (FileManager.IsRelative(directory))
                {
                    directory = FileManager.MakeAbsolute(directory);
                }

                if (System.IO.Directory.Exists(directory))
                {


                    // See if the file without extension exists
                    string fileNameAsXnb = RemoveExtension(fileName) + ".xnb";

                    if (System.IO.File.Exists(fileNameAsXnb))
                    {
                        throw new FileNotFoundException("Could not find the " +
                            "file \n" + fileName + "\nbut found the XNB file\n" + fileNameAsXnb +
                            ".\nIs the file loaded through the content pipeline?  If so, " +
                            "try loading the file without an extension.");
                    }
                    else
                    {
	#if XBOX360 || SILVERLIGHT || WINDOWS_PHONE || MONODROID

                        FileNotFoundException fnfe = new FileNotFoundException("Could not find the " +
                            "file " + fileName + " but found the directory " + directory +
                            "  Did you type in the name of the file wrong?");
	#else

                        FileNotFoundException fnfe = new FileNotFoundException("Could not find the " +
                            "file " + fileName + " but found the directory " + directory +
                            "  Did you type in the name of the file wrong?", fileName);
	#endif
                        throw fnfe;
                    }
                }
                else
                {
	#if XBOX360 || SILVERLIGHT || WINDOWS_PHONE || MONODROID

                    throw new FileNotFoundException("Could not find the " +
                        "file " + fileName + " or the directory " + directory);
	#else
                    throw new FileNotFoundException("Could not find the " +
                        "file " + fileName + " or the directory " + directory, fileName);
	#endif
                }

#endif
            }
        }


        public static void SaveEmbeddedResource(Assembly assembly, string resourceName, string targetFileName)
        {
#if WINDOWS_8
            throw new NotImplementedException();
#else
            System.IO.Directory.CreateDirectory(FileManager.GetDirectory(targetFileName));

            byte[] buffer = GetByteArrayFromEmbeddedResource(assembly, resourceName);

            bool succeeded = true;

            if (File.Exists(targetFileName))
            {
                File.Delete(targetFileName);
            }
            WriteStreamToFile(targetFileName, buffer, succeeded);
#endif
        }

        private static void WriteStreamToFile(string targetFileName, byte[] buffer, bool succeeded)
        {
            if (succeeded)
            {
#if WINDOWS_8
                throw new NotImplementedException();
#else
                using (FileStream fs = new FileStream(targetFileName, FileMode.Create))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        bw.Write(buffer);
                        bw.Close();
                        fs.Close();
                    }
                }
#endif
            }
        }

        public static void SaveText(string stringToSave, string fileName)
        {
            SaveText(stringToSave, fileName, System.Text.Encoding.UTF8);

        }


        private static void SaveText(string stringToSave, string fileName, System.Text.Encoding encoding)
        {
            // encoding is currently unused
            fileName = fileName.Replace("/", "\\");
            
            ////////////Early Out///////////////////////
#if WINDOWS
            if (!string.IsNullOrEmpty(FileManager.GetDirectory(fileName)) &&
                !Directory.Exists(FileManager.GetDirectory(fileName)))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(fileName));
            }

            System.IO.File.WriteAllText(fileName, stringToSave);
            return;
#endif
            ////////////End Early Out///////////////////////////






            StreamWriter writer = null;

#if WINDOWS_PHONE || MONOGAME


            if (!fileName.Contains(IsolatedStoragePrefix))
            {
                throw new ArgumentException("You must use isolated storage.  Use FileManager.GetUserFolder.");
            }

            fileName = FileManager.GetIsolatedStorageFileName(fileName);

#if WINDOWS_8 || IOS
            throw new NotImplementedException();
#else
            IsolatedStorageFileStream isfs = null;

            isfs = new IsolatedStorageFileStream(
                fileName, FileMode.Create, mIsolatedStorageFile);

            writer = new StreamWriter(isfs);
#endif

#elif !XBOX360
            if (!string.IsNullOrEmpty(FileManager.GetDirectory(fileName)) &&
                !Directory.Exists(FileManager.GetDirectory(fileName)))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(fileName));
            }


            FileInfo fileInfo = new FileInfo(fileName);
            // We used to first delete the file to try to prevent the
            // OS from reporting 2 file accesses.  But I don't think this
            // solved the problem *and* it has the nasty side effect of possibly
            // deleting the entire file , but not being able to save it if there is
            // some weird access issue.  This would result in Glue deleting some files
            // like the user's Game1 or plugins not properly saving files 
            //if (System.IO.File.Exists(fileName))
            //{
            //    System.IO.File.Delete(fileName);
            //}
            writer = fileInfo.CreateText();


#else


            fileName = FileManager.GetIsolatedStorageFileName(fileName);

            var sc = GetStorageContainer();
            string dir = FileManager.GetDirectory(fileName);
            if (!string.IsNullOrEmpty(dir) && !sc.DirectoryExists(dir))
            {
                sc.CreateDirectory(dir);
            }
            writer = new StreamWriter(sc.CreateFile(fileName));
            using(sc)
#endif

            using (writer)
            {
                writer.Write(stringToSave);

                Close(writer);
            }

#if WINDOWS_PHONE || MONODROID
            isfs.Close();
            isfs.Dispose();
#endif
        }


        public static string Standardize(string fileNameToFix)
        {
            return Standardize(fileNameToFix, RelativeDirectory);
        }


        public static string Standardize(string fileNameToFix, string relativePath)
        {
            return Standardize(fileNameToFix, relativePath, true);
        }

        /// <summary>
        /// Replaces back slashes with forward slashes, but
        /// doesn't break network addresses.
        /// </summary>
        /// <param name="stringToReplace">The string to replace slashes in.</param>
        static void ReplaceSlashes(ref string stringToReplace)
        {
            bool isNetwork = false;
            if (stringToReplace.StartsWith("\\\\"))
            {
                stringToReplace = stringToReplace.Substring(2);
                isNetwork = true;
            }

            stringToReplace = stringToReplace.Replace("\\", "/");

            if (isNetwork)
            {
                stringToReplace = "\\\\" + stringToReplace;
            }
        }

        public static string Standardize(string fileNameToFix, string relativePath, bool makeAbsolute)
        {
            if (fileNameToFix == null)
                return null;

            bool isNetwork = fileNameToFix.StartsWith("\\\\");

            ReplaceSlashes(ref fileNameToFix);

            if (makeAbsolute && !isNetwork)
            {
                // Not sure what this is all about, but everything should be standardized:
                //#if SILVERLIGHT
                //                if (IsRelative(fileNameToFix) && mRelativeDirectory.Length > 1)
                //                    fileNameToFix = mRelativeDirectory + fileNameToFix;

                //#else

                if (IsRelative(fileNameToFix))
                {
                    fileNameToFix = (relativePath + fileNameToFix);
                    ReplaceSlashes(ref fileNameToFix);
                }

                //#endif
            }

#if !XBOX360
            fileNameToFix = RemoveDotDotSlash(fileNameToFix);
 
            if (fileNameToFix.StartsWith("..") && makeAbsolute)
            {
                throw new InvalidOperationException("Tried to remove all ../ but ended up with this: " + fileNameToFix);
            }

#endif
            // It's possible that there will be double forward slashes.
            fileNameToFix = fileNameToFix.Replace("//", "/");

#if !MONODROID && !IOS
            if (!PreserveCase)
            {
                fileNameToFix = fileNameToFix.ToLowerInvariant();
            }
#endif

            return fileNameToFix;
        }

        public static string RemoveDotDotSlash(string fileNameToFix)
        {
            if (fileNameToFix.Contains(".."))
            {
                // First let's get rid of any ..'s that are in the middle
                // for example:
                //
                // "content/zones/area1/../../background/outdoorsanim/outdoorsanim.achx"
                //
                // would become
                // 
                // "content/background/outdoorsanim/outdoorsanim.achx"

                fileNameToFix = fileNameToFix.Replace("\\", "/");

                //int indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");
                //bool shouldLoop = indexOfNextDotDotSlash > 0;

                //while (shouldLoop)
                //{
                //    int indexOfPreviousDirectory = fileNameToFix.LastIndexOf('/', indexOfNextDotDotSlash - 2, indexOfNextDotDotSlash - 2);

                //    fileNameToFix = fileNameToFix.Remove(indexOfPreviousDirectory + 1, indexOfNextDotDotSlash - indexOfPreviousDirectory + 2);

                //    indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");

                //    shouldLoop = indexOfNextDotDotSlash > 0;


                //}

                int indexOfNextDotDotSlash = GetDotDotSlashIndex(fileNameToFix);


                bool shouldLoop = indexOfNextDotDotSlash > 0;

                while (shouldLoop)
                {
                    // add one to go from "/../" to "../"
                    indexOfNextDotDotSlash++;

                    int indexOfPreviousDirectory = fileNameToFix.LastIndexOf('/', indexOfNextDotDotSlash - 2, indexOfNextDotDotSlash - 2);

                    fileNameToFix = fileNameToFix.Remove(indexOfPreviousDirectory + 1, indexOfNextDotDotSlash - indexOfPreviousDirectory + 2);

                    indexOfNextDotDotSlash = GetDotDotSlashIndex(fileNameToFix);

                    shouldLoop = indexOfNextDotDotSlash > 0;


                }
            }

            if(fileNameToFix.Contains("/./"))
            {
                fileNameToFix = fileNameToFix.Replace("/./", "/");
            }

            if(fileNameToFix.Contains("\\.\\"))
            {
                fileNameToFix = fileNameToFix.Replace("\\.\\", "\\");

            }
            // Let's not force the user to a certain type of slashes
            if (fileNameToFix.Contains("/.\\"))
            {
                fileNameToFix = fileNameToFix.Replace("/.\\", "/");
            }

            if (fileNameToFix.Contains("\\./"))
            {
                fileNameToFix = fileNameToFix.Replace("\\./", "\\");

            }


            return fileNameToFix;
        }

        private static int GetDotDotSlashIndex(string fileNameToFix)
        {
            int indexOfNextDotDotSlash = fileNameToFix.LastIndexOf("/../");

            while (indexOfNextDotDotSlash > 0 && fileNameToFix[indexOfNextDotDotSlash - 1] == '.')
            {
                indexOfNextDotDotSlash = fileNameToFix.LastIndexOf("/../", indexOfNextDotDotSlash);
            }
            return indexOfNextDotDotSlash;
        }


        #region XML Methods

        public static T XmlDeserialize<T>(string fileName)
        {
            T objectToReturn = default(T);

#if SILVERLIGHT || WINDOWS_PHONE || (XBOX360 && XNA4) || MONODROID
            if (fileName.Contains(FileManager.IsolatedStoragePrefix) && mHasUserFolderBeenInitialized == false)
            {
                throw new InvalidOperationException("The user folder hasn't been initialized.  Call FileManager.InitializeUserFolder first");
            }
#endif

            if (FileManager.IsRelative(fileName))
                fileName = FileManager.RelativeDirectory + fileName;

            // Do this check before removing the ./ at the end of the file name
#if !XBOX360 || XNA4
            ThrowExceptionIfFileDoesntExist(fileName);
#endif


#if XBOX360 || SILVERLIGHT || WINDOWS_PHONE || MONODROID
            // Silverlight and 360 don't like ./ at the start of the file name, but that's what we use to identify an absolute path
            if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
                fileName = fileName.Substring(2);

#endif

            bool handled = false;

#if WINDOWS_8
            handled = XmlDeserializeWindows8IfIsolatedStorage<T>(fileName, out objectToReturn);
#endif


            if (!handled)
            {
                using (Stream stream = GetStreamForFile(fileName))
                {
                    objectToReturn = XmlDeserialize<T>(stream);
                }
            }
#if XBOX360 //&& !XNA4
            if (IsFileNameInUserFolder(fileName))
            {
                FileManager.DisposeLastStorageContainer();
            }
#endif

            return objectToReturn;
        }

        public static T XmlDeserialize<T>(Stream stream)
        {

            if (stream == null)
            {
                return default(T); // this happens if the file can't be found
            }
            else
            {
                XmlSerializer serializer = GetXmlSerializer<T>();
                T objectToReturn;
                objectToReturn = (T)serializer.Deserialize(stream);

                Close(stream);

                return objectToReturn;
            }
        }

        public static T XmlDeserializeFromString<T>(string stringToDeserialize)
        {
            if (string.IsNullOrEmpty(stringToDeserialize))
            {
                return default(T);
            }
            else
            {
                XmlSerializer serializer = GetXmlSerializer<T>();
                TextReader textReader = new StringReader(stringToDeserialize);

                T objectToReturn = (T)serializer.Deserialize(textReader);

                Close(textReader);
                textReader.Dispose();
                return objectToReturn;

            }
        }

        public static void XmlDeserialize<T>(string fileName, out T objectToDeserializeTo)
        {
            objectToDeserializeTo = XmlDeserialize<T>(fileName);
        }

        public static Stream GetStreamForFile(string fileName)
        {
            return GetStreamForFile(fileName, FileMode.Open);
        }

        public static Stream GetStreamForFile(string fileName, FileMode mode)
        {
            // This used to
            // not be here but
            // there is a branch
            // below which was making
            // this absolute if it already
            // wasn't.  I suppose we should
            // always do this...
            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.RelativeDirectory + fileName;
            }


            if (fileName.StartsWith("./"))
            {
                fileName = fileName.Substring(2);
            }
            Stream stream = null;
#if USES_DOT_SLASH_ABOLUTE_FILES && !IOS
            // Silverlight and 360 don't like ./ at the start of the file name, but that's what we use to identify an absolute path
            if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
                fileName = fileName.Substring(2);



            if (fileName.Contains(IsolatedStoragePrefix))
            {
                fileName = GetIsolatedStorageFileName(fileName);

#if XBOX
                var storageContainer = GetStorageContainer();
                stream = storageContainer.OpenFile(fileName, mode);
#else

#if WINDOWS_8
                throw new NotImplementedException();
#else
                IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(fileName, mode, mIsolatedStorageFile);

                stream = isfs;
#endif
#endif
            }
            else
            {


#if WINDOWS_PHONE || (XBOX360 && XNA4) || MONODROID || WINDOWS_8 || IOS
                stream = TitleContainer.OpenStream(fileName);
#else

                fileName = fileName.Replace("\\", "/");
                Uri uri = new Uri(fileName, UriKind.Relative);

                StreamResourceInfo sri = Application.GetResourceStream(uri);

                if (sri == null)
                {

                    throw new Exception("Could not find the file " +
                        fileName + ".  Did you add " + fileName + " to " +
                        "your project and set its 'Build Action' to 'Content'?");
                }

                stream = sri.Stream;
#endif
            }
#else

            stream = File.OpenRead(fileName);
#endif

            return stream;
        }


#if !SILVERLIGHT && !WINDOWS_PHONE && !XBOX360 && !MONODROID && !MONOGAME
        private static void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            System.Xml.XmlAttribute attr = e.Attr;
            Console.WriteLine("Unknown attribute " +
            attr.Name + "='" + attr.Value + "'");
        }

        private static void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            Console.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
        }
#endif

        public static object BinaryDeserialize(Type type, string fileName)
        {
            object objectToReturn = null;

			throw new NotImplementedException ("Look to original source for this");

            return objectToReturn;

        }

        public static object XmlDeserialize(Type type, string fileName)
        {
            object objectToReturn = null;

            if (FileManager.IsRelative(fileName))
                fileName = FileManager.RelativeDirectory + fileName;


            ThrowExceptionIfFileDoesntExist(fileName);

#if XBOX360
            // Cute, the 360 doesn't like ./ at the start of the file name.
            if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
                fileName = fileName.Substring(2);
            
#endif


            using (Stream stream = GetStreamForFile(fileName))
            {
                XmlSerializer serializer = GetXmlSerializer(type);
                objectToReturn = serializer.Deserialize(stream);
                Close(stream);
            }

            return objectToReturn;
        }







        public static void BinarySerialize<T>(T objectToSerialize, string fileName)
        {
            BinarySerialize(typeof(T), objectToSerialize, fileName);
        }

        public static void BinarySerialize(Type type, object objectToSerialize, string fileName)
        {
			throw new NotImplementedException ("See original source for implementation");

        }


        public static void XmlSerialize(Type type, object objectToSerialize, string fileName)
        {
            if (FileManager.IsRelative(fileName))
                fileName = FileManager.RelativeDirectory + fileName;

#if USE_ISOLATED_STORAGE
            if (!fileName.Contains(IsolatedStoragePrefix))
            {
                throw new ArgumentException("You must use isolated storage.  Use FileManager.GetUserFolder.");
            }

            fileName = GetIsolatedStorageFileName(fileName);

#endif

#if WINDOWS_8
            XmlSerializeWindows8(type, objectToSerialize, fileName);
#else

            XmlSerializeAllOtherPlatforms(type, objectToSerialize, fileName);
#endif
        }

        
        private static void XmlSerializeAllOtherPlatforms(Type type, object objectToSerialize, string fileName)
        {

            Stream fs = null;
            XmlWriter writer = null;

#if SILVERLIGHT || WINDOWS_PHONE || MONODROID
            IsolatedStorageFileStream isfs = null;

#endif

            #if IOS
            fileName = fileName.Replace('\\', '/');
            #endif



#if !XBOX360 && !WINDOWS_PHONE && !MONODROID
            string directory = FileManager.GetDirectory(fileName);

#if WINDOWS_8
            // We use the isolated storage triple underscore
            // so no folders are necessary
            //throw new NotImplementedException();
#else

        #if IOS
            if(directory.StartsWith("."))
            {
                directory = directory.Substring(1);
            }
        #endif

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
#endif

#endif
#if XBOX360
            StorageContainer sc = null;
#endif
            try
            {
                XmlSerializer serializer = GetXmlSerializer(type);


#if USE_ISOLATED_STORAGE && !XBOX360 && !IOS

#if WINDOWS_8 
                throw new NotImplementedException();
#else

                isfs = new IsolatedStorageFileStream(
                   fileName, FileMode.Create, mIsolatedStorageFile);

                XmlWriterSettings xms = new XmlWriterSettings();
                xms.Encoding = System.Text.Encoding.UTF8;
                xms.Indent = true;
                writer = XmlWriter.Create(isfs, xms);
#endif

#else

#if XBOX360
                if (fileName.StartsWith("./"))
                {
                    fileName = fileName.Substring(2);
                }



                sc = GetStorageContainer();

                fs = sc.CreateFile(fileName);
#else

            #if IOS
                if(fileName.StartsWith("./"))
                {
                    // can't start with '.'
                    fileName = fileName.Substring(1);
                }

            #endif


                // I used to call File.Open with the FileMode.Truncate
                // but that caused the file to be modified twice and this
                // was bad in Glue.  So now we delete instead of truncate
                // to prevent file systems from reporting 2 changes when a file
                // has really only changed once.
                if (System.IO.File.Exists(fileName))
                {
                    System.IO.File.Delete(fileName);
                }
                 
                fs = System.IO.File.Open(fileName, FileMode.OpenOrCreate);

#endif

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                writer = XmlWriter.Create(fs, settings);

#endif

                serializer.Serialize(writer, objectToSerialize);
            }
            finally
            {
                if (fs != null) Close(fs);

#if SILVERLIGHT || WINDOWS_PHONE || MONODROID
                if (isfs != null)
                {
                    isfs.Close();
                }
#elif XBOX360
                FileManager.DisposeLastStorageContainer();
#endif
            }
        }


        public static void XmlSerialize<T>(T objectToSerialize, string fileName)
        {

            XmlSerialize(typeof(T), objectToSerialize, fileName);
        }

        public static void XmlSerialize<T>(T objectToSerialize, out string stringToSerializeTo)
        {
            XmlSerialize(typeof(T), objectToSerialize, out stringToSerializeTo);
        }

        public static void XmlSerialize(Type type, object objectToSerialize, out string stringToSerializeTo)
        {
            MemoryStream memoryStream = new MemoryStream();

            XmlSerializer serializer = GetXmlSerializer(type);

            serializer.Serialize(memoryStream, objectToSerialize);


#if SILVERLIGHT || WINDOWS_PHONE  || (XBOX360 && XNA4) || MONOGAME

            byte[] asBytes = memoryStream.ToArray();

            stringToSerializeTo = System.Text.Encoding.UTF8.GetString(asBytes, 0, asBytes.Length);
#elif XBOX360
            
            throw new NotImplementedException("XmlSerialization to string is not supported yet");



#else

            stringToSerializeTo = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
#endif

        }

#if !FRB_MDX
        //Implemented based on interface, not part of algorithm
        public static string RemoveAllNamespaces(string xmlDocument)
        {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));

            return xmlDocumentWithoutNs.ToString();
        }

        //Core recursion function
        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                foreach (XAttribute attribute in xmlDocument.Attributes())
                    xElement.Add(attribute);

                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }
#endif


        #endregion

        #endregion

        #region Internal Methods

        internal static XmlSerializer GetXmlSerializer<T>()
        {
            return GetXmlSerializer(typeof(T));
        }


        internal static XmlSerializer GetXmlSerializer(Type type)
        {
            lock (mXmlSerializers)
            {
                if (mXmlSerializers.ContainsKey(type))
                {
                    return mXmlSerializers[type];
                }
                else
                {
      
                    // For info on this block, see:
                    // http://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor
#if DEBUG
                    XmlSerializer newSerializer = XmlSerializer.FromTypes(new[] { type })[0];
#else
                    XmlSerializer newSerializer = null;

                    newSerializer = new XmlSerializer(type);
#endif

#if !SILVERLIGHT && !WINDOWS_PHONE && !XBOX360 && !MONOGAME
                        newSerializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
                    newSerializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);
#endif

                    mXmlSerializers.Add(type, newSerializer);
                    return newSerializer;
                }
            }
        }


        #endregion

        #region Private Methods

#if !SILVERLIGHT && !WINDOWS_8

        private static void CopyDirectoryHelper(string sourceDirectory, string destDirectory, bool deletePrevious, List<string> excludeFiles, List<string> excludeDirectories)
        {
            destDirectory = FileManager.Standardize(destDirectory);

            if (!destDirectory.EndsWith(@"\") && !destDirectory.EndsWith(@"/"))
            {
                destDirectory += @"\";
            }

            if (Directory.Exists(destDirectory) && deletePrevious)
            {
                DeleteDirectory(destDirectory);
            }

            if (!Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            string[] fileList = Directory.GetFiles(sourceDirectory);
            foreach (string file in fileList)
            {
                if (excludeFiles == null || !excludeFiles.Contains(file))
                    File.Copy(file, destDirectory + RemovePath(file), true);
            }

            string dirName;
            string[] dirList = Directory.GetDirectories(sourceDirectory);
            foreach (string dir in dirList)
            {
                dirName = RemovePath(dir);

                if (excludeDirectories == null || !excludeDirectories.Contains(dirName))
                    CopyDirectoryHelper(dir, destDirectory + dirName, deletePrevious, excludeFiles, excludeDirectories);
            }

        }
#endif
        
        public static void Close(Stream stream)
        {
#if WINDOWS_8
            // Close was removed - no need to do anything
#else
            stream.Close();
#endif
        }

        public static void Close(StreamReader streamReader)
        {
#if WINDOWS_8
            // Close was removed - no need to do anything
#else
            streamReader.Close();
#endif
        }

        private static void Close(BinaryWriter writer)
        {
#if WINDOWS_8
            // Close was removed - no need to do anything
#else
            writer.Close();
#endif
        }

        private static void Close(StreamWriter writer)
        {
#if WINDOWS_8
            // Close was removed - no need to do anything
#else
            writer.Close();
#endif
        }

        public static void Close(TextReader writer)
        {
#if WINDOWS_8
            // Close was removed - no need to do anything
#else
            writer.Close();
#endif
        }

        #endregion

        #endregion
    }
}
