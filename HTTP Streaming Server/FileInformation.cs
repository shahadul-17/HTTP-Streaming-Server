using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace HTTP_Streaming_Server
{
    public class FileInformation
    {
        private long _contentLength;
        private string _fileName, _lastModified, _contentType;

        public string fileName      // "fileName" is unused...
        {
            get
            {
                return _fileName;
            }
        }

        public string lastModified
        {
            get
            {
                return _lastModified;
            }
        }

        public long contentLength
        {
            get
            {
                return _contentLength;
            }
        }

        public string contentType
        {
            get
            {
                return _contentType;
            }
        }

        private static Dictionary<string, string> contentTypes = null;

        public FileInformation(string fileName)
        {
            this._fileName = fileName;

            if (contentTypes == null)
            {
                try
                {
                    LoadContentTypes();
                }
                catch (Exception exception)
                {
                    throw exception;
                }
            }

            try
            {
                RetrieveFileInformation();
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        private void RetrieveFileInformation()
        {
            try
            {
                FileInfo fileInfo = new FileInfo(_fileName);
                _lastModified = fileInfo.LastWriteTime.ToString("r");
                _contentLength = fileInfo.Length;
                _contentType = contentTypes[fileInfo.Extension.Substring(1).ToLower()];
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        private static void LoadContentTypes()
        {
            string line = "";
            string[] substrings;

            contentTypes = new Dictionary<string, string>();

            try
            {
                using (StreamReader streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("HTTP_Streaming_Server.Resources.content-types.txt")))
                {
                    try
                    {
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            substrings = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                            contentTypes.Add(substrings[0], substrings[1]);
                        }
                    }
                    catch (Exception exception)
                    {
                        throw exception;
                    }
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }
    }
}