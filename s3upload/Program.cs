using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Amazon.S3.Model;
using Amazon.S3;
using System.Configuration;
using System.Threading;


namespace s3upload
{
    class Program
    {
        #region S3 Client
        static string _bucketName = "";
        static string BucketName
        {
            get 
            {
                if (_bucketName == "")
                {
                    _bucketName = ConfigurationManager.AppSettings["BucketName"];
                }
                return _bucketName;
            }
        }
        static AmazonS3Client _client = null;
        static AmazonS3Client Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new AmazonS3Client(ConfigurationManager.AppSettings["AWSAccessKey"], ConfigurationManager.AppSettings["AWSSecretKey"]);
                }
                return _client;
            }
        }
        #endregion

        static int UploadedCnt = 0;
        static List<string> ExistKeys = new List<string>();

        static void Main(string[] args)
        {
            if (args.Length != 1 || (!Directory.Exists(args[0])))
            {
                Usage();
                return;
            }
            string uploadDirectory = args[0];
            IEnumerable<string> files = GetAllFilesInDirectory(uploadDirectory);

            ExistKeys = GetExistObjectsKey(uploadDirectory);
            foreach (var file in files)
            {
                UploadFile2S3(file,GetContentType(file));
            }

        }

        private static IEnumerable<string> GetAllFilesInDirectory(string uploadDirectory)
        {
            IEnumerable<string> files = new List<string>();
            try
            {
                files = Directory.EnumerateFiles(uploadDirectory, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return files;
            }
            return files;
        }

        private static void Usage()
        {
            Console.WriteLine("s3upload directory-to-upload\n\tupload all files in a directory with lower key name");
        }

        private static void UploadFile2S3(string filePath,string contentType)
        {
            string s3Key = filePath.Replace('\\','/').ToLower();
            if (ExistKeys.Contains(s3Key))
            {
                Console.WriteLine("Key Exist:{0}", s3Key);
                return;
            }

            Console.WriteLine("{0}>From:{1}", UploadedCnt.ToString().PadLeft(6,' '), filePath);
            Console.WriteLine("         To:{0}", s3Key);

            PutObjectRequest request = new PutObjectRequest();
            request.WithBucketName(BucketName)
                .WithFilePath(filePath)
                .WithKey(s3Key);
                
            if (contentType != "")
            {
                request.WithContentType(contentType);
                UploadedCnt++;
            }

            try
            {
                Client.PutObject(request);
            }
            catch (AmazonS3Exception ex)
            {
                string eMsg = string.Format("  *Failed:{0}\n  Msg:{1}", s3Key, ex.Message);
                Console.WriteLine(eMsg);
                Log(eMsg);
            }
        }

        private static string GetContentType(string path)
        {
            string fileExt = System.IO.Path.GetExtension(path).ToLower();
            if (fileExt == ".html" || fileExt == ".htm")
                return "text/html";
            if (fileExt == ".js")
                return "text/javascript";
            if (fileExt == ".css")
                return "text/css";
            if (fileExt == ".bmp" || fileExt == ".jpg" || fileExt == ".jpeg" || fileExt == ".png" || fileExt == ".gif")
                return "image/" + fileExt.Remove(0, 1);
            if (fileExt == ".swf")
                return "application/x-shockwave-flash";

            //default
            return string.Empty;
        }

        private static List<string> GetExistObjectsKey(string prefix)
        {
            prefix = prefix.Replace('\\','/').ToLower();
            var request = new ListObjectsRequest();
            request.WithBucketName(BucketName);
            request.Prefix = prefix;


            ListObjectsResponse response = new ListObjectsResponse();
            try
            {
               response = Client.ListObjects(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Get current keys error:{0}", ex.Message);
                return new List<string>();
            }


            List<string> allKeys = new List<string>();
            allKeys.AddRange(response.S3Objects.Select(s3Obj => s3Obj.Key));

            //iterate the paging(aws can only return 1000 keys in a request)
            while (response.IsTruncated)
            {
                request.Marker = response.NextMarker;
                response = new ListObjectsResponse();
                try
                {
                    response = Client.ListObjects(request);
                }
                catch (Exception ex) 
                {
                    break;
                }

                allKeys.AddRange(response.S3Objects.Select(s3Obj => s3Obj.Key));
            }


            Console.WriteLine("List current keys:{0}", allKeys.Count);
            Thread.Sleep(2000);
            return allKeys;
        }

        private static void Log(string eMsg)
        {
            string logFileName = "s3upload.log";

            StreamWriter logWriter = null;
            if (!File.Exists(logFileName))
            {
                logWriter = File.CreateText(logFileName);
            }
            else
            {
                logWriter = new StreamWriter(File.OpenWrite(logFileName));
            }

            try
            {
                logWriter.WriteLine("[0]>{1}", DateTime.Now, eMsg);
            }
            catch (Exception)
            {
                Console.WriteLine("error to write log");
            }
        }
    }
}
