﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Amazon.S3.Model;
using Amazon.S3;
using System.Configuration;


namespace s3upload
{
    class Program
    {
        const string BUCKET_NAME = "mybucketname";
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

            Console.WriteLine("[{0}]From:{1}", UploadedCnt, filePath);
            Console.WriteLine("  To:{0}", s3Key);

            PutObjectRequest request = new PutObjectRequest();
            request.WithBucketName(BUCKET_NAME)
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
                Console.WriteLine("  Failed:{0}\n  Msg:{1}", s3Key, ex.Message);
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
            request.WithBucketName(BUCKET_NAME);
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
            foreach (var s3Obj in response.S3Objects)
            {
                allKeys.Add(s3Obj.Key);
            }

            Console.WriteLine("List current keys:{0}", allKeys.Count);
            return allKeys;
        }
    }
}