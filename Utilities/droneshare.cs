﻿using fastJSON;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace MissionPlanner.Utilities
{


    public class droneshare
    {
        public class APIConstants
        {
            /**
             * The default world wide drone broker
             */
            public static String DEFAULT_SERVER = "api.3drobotics.com";


            public static String URL_BASE = "http://" + DEFAULT_SERVER;


            /**
             * If using a raw TCP link to the server, use this port number
             */
            public static int DEFAULT_TCP_PORT = 5555;


            public static String ZMQ_URL = "tcp://" + DEFAULT_SERVER + ":5556";


            public static String TLOG_MIME_TYPE = "application/vnd.mavlink.tlog";

            // Do not use this key in your own applications - please register your own.
            // https://developer.3drobotics.com/
            public static String apiKey = "614ca8bd.4d084b822a53c6eccb642271db04c937";
        }

        private static string ToQueryString(NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys
                         from value in nvc.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            return string.Join("&", array);
        }

        public static string doUpload(string file, string userId, string userPass, string vehicleId, string apiKey)
        {
            String baseUrl = APIConstants.URL_BASE;
            NameValueCollection @params = new NameValueCollection();
            @params.Add("api_key", apiKey);
            @params.Add("login", userId);
            @params.Add("password", userPass);
            @params.Add("autoCreate", "true");
            String queryParams = ToQueryString(@params);
            String webAppUploadUrl = String.Format("{0}/api/v1/mission/upload/{1}?{1}", baseUrl, vehicleId, queryParams); // ?{2}



            try
            {
                // http post
                string JSONresp = UploadFilesToRemoteUrl(webAppUploadUrl,file,"droneshare.log",@params);

                var JSONnobj = JSON.Instance.ToObject<object>(JSONresp);

                object[] data = (object[])JSONnobj;

                var item2 = ((Dictionary<string, object>)data[0]);

                string answer = item2["viewURL"].ToString();

                return answer;

                // http port with query string
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(webAppUploadUrl);
                request.ContentType = APIConstants.TLOG_MIME_TYPE;
                request.Method = "POST";
                request.KeepAlive = true;
                request.Credentials = System.Net.CredentialCache.DefaultCredentials;
                request.Accept = "application/json";

                request.ContentLength = new FileInfo(file).Length;

                using (var stream = request.GetRequestStream())
                {
                    using (var filebin = new BinaryReader(File.OpenRead(file)))
                    {
                        byte[] buffer = new byte[1024 * 4];
                        while (filebin.BaseStream.Position < filebin.BaseStream.Length)
                        {
                            int read = filebin.Read(buffer, 0, buffer.Length);
                            stream.Write(buffer, 0, read);
                        }
                    }
                }

                try
                {

                    var response = (HttpWebResponse)request.GetResponse();

                    var JSONresp2 = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    var JSONnobj2 = JSON.Instance.ToObject(JSONresp2);

                    return JSONnobj2.ToString();
                }
                catch (Exception ex) { Console.WriteLine(ex); }

                /*

                JSONArray missions = new JSONArray(resp);
                if (missions.length() != 1)
                    throw new IOException("The server rejected this log file");


                JSONObject mission = missions.getJSONObject(0);
                String viewURL = mission.getString("viewURL");


                System.out.println("View URL is " + viewURL);
                */
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }

            return "";
        }
    

        public static string UploadFilesToRemoteUrl(string url, string file, string logpath, NameValueCollection nvc)
        {

            long length = 0;
            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");


            HttpWebRequest httpWebRequest2 = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest2.ContentType = "multipart/form-data; boundary=" + boundary;
            httpWebRequest2.Method = "POST";
            httpWebRequest2.KeepAlive = true;
            httpWebRequest2.Credentials = System.Net.CredentialCache.DefaultCredentials;
            httpWebRequest2.Accept = "application/json";



            Stream memStream = new System.IO.MemoryStream();

            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            byte[] boundarybytes2 = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary);


            string formdataTemplate = "\r\n--" + boundary +
            "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";

            foreach (string key in nvc.Keys)
            {
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                memStream.Write(formitembytes, 0, formitembytes.Length);
            }


            memStream.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: " + APIConstants.TLOG_MIME_TYPE + "\r\n\r\n";

           // for (int i = 0; i < files.Length; i++)
            {

                //string header = string.Format(headerTemplate, "file" + i, files[i]);
                string header = string.Format(headerTemplate, "uplTheFile.tlog", file);

                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);

                memStream.Write(headerbytes, 0, headerbytes.Length);


                FileStream fileStream = new FileStream(file, FileMode.Open,
                FileAccess.Read);
                byte[] buffer = new byte[1024];

                int bytesRead = 0;

                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    memStream.Write(buffer, 0, bytesRead);

                }

                fileStream.Close();
            }

            // write last boundry
            memStream.Write(boundarybytes2, 0, boundarybytes2.Length);
            // write last -- to last boundry
            memStream.Write(new byte[] {(byte)'-',(byte)'-'},0,2);

            httpWebRequest2.ContentLength = memStream.Length;

            Stream requestStream = httpWebRequest2.GetRequestStream();

            memStream.Position = 0;
            byte[] tempBuffer = new byte[memStream.Length];
            memStream.Read(tempBuffer, 0, tempBuffer.Length);
            memStream.Close();
            requestStream.Write(tempBuffer, 0, tempBuffer.Length);
            requestStream.Close();


            WebResponse webResponse2 = httpWebRequest2.GetResponse();

            Stream stream2 = webResponse2.GetResponseStream();
            StreamReader reader2 = new StreamReader(stream2);


            string answer = reader2.ReadToEnd();

            webResponse2.Close();
            httpWebRequest2 = null;
            webResponse2 = null;

            return answer;
        }
    }
}
