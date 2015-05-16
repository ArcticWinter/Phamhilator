﻿/*
 * Phamhilator. A .Net based bot network catching spam/low quality posts for Stack Exchange.
 * Copyright © 2015, ArcticEcho.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */





using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using ChatExchangeDotNet;
using Phamhilator.Yam.Core;
using Newtonsoft.Json;

namespace Phamhilator.Yam.UI
{
    using RequestType = LocalRequest.RequestType;
    using System.Net.Mail;

    public class Program
    {
        private static readonly ManualResetEvent shutdownMre = new ManualResetEvent(false);
        private static string apiKeySenderEmail;
        private static Client chatClient;
        private static Room chatRoom;
        private static RealtimePostSocket postSocket;
        private static LocalServer locServer;
        private static RemoteServer remServer;
        private static DateTime startTime;
        private static uint yamErrorCount;
        private static uint phamErrorCount;
        private static uint ghamErrorCount;



        private static void Main(string[] args)
        {
            Console.Title = "Yam v2";
            TryLogin();
            Console.Write("Joining chat room...");
            JoinRooms();
            Console.Write("done.\nInitialising log...");
            PostLogger.InitialiseLogger();
            Console.Write("done.\nStarting server...");
            InitialiseLocalServer();
            InitialiseRemoteServer();
#if DEBUG
            Console.Write("done.\nYam v2 started (debug), press Q to exit.\n");
            chatRoom.PostMessage("`Yam v2 started` (**`debug`**)`.`");
#else
            Console.Write("done.\nYam v2 started, press Q to exit.\n");
            chatRoom.PostMessage("`Yam v2 started.`");
#endif
            startTime = DateTime.UtcNow;

            Task.Run(() =>
            {
                while (true)
                {
                    if (Char.ToLowerInvariant(Console.ReadKey(true).KeyChar) == 'q')
                    {
                        shutdownMre.Set();
                        return;
                    }
                }
            });

            shutdownMre.WaitOne();

            shutdownMre.Dispose();
            postSocket.Close();
            postSocket.Dispose();
            locServer.Dispose();
            remServer.Dispose();
            PostLogger.StopLogger();
            chatRoom.PostMessage("`Yam v2 stopped.`");
            chatRoom.Leave();
            chatClient.Dispose();
        }

        private static void TryLogin()
        {
            var success = false;
            while (true)
            {
                Console.WriteLine("Please enter your Stack Exchange OpenID credentials.\n");

                Console.Write("Email: ");
                var email = Console.ReadLine();

                Console.Write("Password: ");
                var password = Console.ReadLine();

                try
                {
                    Console.Write("\nAuthenticating...");
                    chatClient = new Client(email, password);
                    apiKeySenderEmail = email;
                    Console.WriteLine("login successful!");
                    success = true;
                }
                catch (Exception)
                {
                    Console.WriteLine("failed to login.");
                }
                Thread.Sleep(3000);
                Console.Clear();
                if (success) { return; }
            }
        }

        private static void JoinRooms()
        {
            chatRoom = chatClient.JoinRoom("http://chat.meta.stackexchange.com/rooms/773/low-quality-posts-hq");
            chatRoom.IgnoreOwnEvents = true;
            chatRoom.StripMentionFromMessages = true;
            chatRoom.EventManager.ConnectListener(EventType.UserMentioned, new Action<Message>(HandleChatCommand));
        }

        private static void InitialiseRemoteServer()
        {
            remServer = new RemoteServer();
        }

        private static void InitialiseLocalServer()
        {
            locServer = new LocalServer();
            locServer.PhamEventManager.ConnectListener(RequestType.Exception, new Action<LocalRequest>(req =>
            {
                phamErrorCount++;
                chatRoom.PostMessage("Warning, error detected in Pham:\n\n" + JsonConvert.SerializeObject(req, Formatting.Indented));
            }));
            locServer.GhamEventManager.ConnectListener(RequestType.Exception, new Action<LocalRequest>(req =>
            {
                ghamErrorCount++;
                chatRoom.PostMessage("Warning, error detected in Gham:\n\n" + JsonConvert.SerializeObject(req, Formatting.Indented));
            }));
            locServer.PhamEventManager.ConnectListener(RequestType.DataManagerRequest, new Action<LocalRequest>(req =>
            {
                HandleDataManagerRequest(true, req);
            }));
            locServer.GhamEventManager.ConnectListener(RequestType.DataManagerRequest, new Action<LocalRequest>(req =>
            {
                HandleDataManagerRequest(false, req);
            }));
            //server.PhamEventManager.ConnectListener(RequestType.Info, new Action<LocalRequest>(req =>
            //{
            //    // Do something...
            //}));
            //server.GhamEventManager.ConnectListener(RequestType.Info, new Action<LocalRequest>(req =>
            //{
            //    // Do something...
            //}));
            //server.PhamEventManager.ConnectListener(RequestType.Command, new Action<LocalRequest>(req =>
            //{
            //    // Do something...
            //}));
            //server.GhamEventManager.ConnectListener(RequestType.Command, new Action<LocalRequest>(req =>
            //{
            //    // Do something...
            //}));

            postSocket = new RealtimePostSocket(true);
            postSocket.OnActiveQuestion += HandleActiveQuestion;
            postSocket.OnActiveAnswer += HandleActiveAnswer;
        }

        private static void HandleChatCommand(Message command)
        {
            if (!UserAccess.Owners.Select(u => u.ID).Contains(command.AuthorID)) { return; }

            var cmd = command.Content.Trim().ToUpperInvariant();

            if (cmd.StartsWith("ADD REMOTE CLIENT"))
            {
                var emailKeyPair = command.Content.Trim().Remove(17).Split(' ');
                var owner = emailKeyPair[0];
                var email = emailKeyPair[1];
                var key = Guid.NewGuid().ToString();

                remServer.ApiKeys[key] = owner;
                DataManager.SaveData("Yam", RemoteServer.ApiKeysDataKey, owner + ":" + key);
                SendApiKeyEmail(command.AuthorName, email, key);

                chatRoom.PostReply(command, "`Client successfully added; an email has been sent with the API key.`");
                return;
            }

            switch (cmd)
            {
                case "STOP":
                {
                    chatRoom.PostReply(command, "`Stopping...`");
                    shutdownMre.Set();
                    return;
                }
                case "STATUS":
                {
                    var hoursAlive = (DateTime.UtcNow - startTime).TotalHours;
                    var getStatus = new Func<int, string>(er => er == 0 ? "Good" : er <= 2 ? "Ok" : "Bad");
                    var getErrorRate = new Func<uint, int>(ec => (int)Math.Round(ec / hoursAlive));
                    var yamErrorRate = getErrorRate(yamErrorCount);   var yamStatus = getStatus(yamErrorRate);
                    var phamErrorRate = getErrorRate(phamErrorCount); var phamStatus = getStatus(phamErrorRate);
                    var ghamErrorRate = getErrorRate(ghamErrorCount); var ghamStatus = getStatus(ghamErrorRate);
                    var statusReport = "    Status report:\n    \n" +
                                       "    Yam:  " + yamStatus + " (" + yamErrorCount + " @ " + yamErrorRate + "/h)\n" +
                                       "    Pham: " + phamStatus + " (" + phamErrorCount + " @ " + phamErrorRate + "/h)\n" +
                                       "    Gham: " + ghamStatus + " (" + ghamErrorCount + " @ " + ghamErrorRate + "/h)";
                    chatRoom.PostMessage(statusReport);
                    return;
                }
                case "LOG DATA":
                {
                    var items = PostLogger.Log.Count;
                    var uncomp = PostLogger.LogSizeUncompressed / 1024.0 / 1024;
                    var comp = PostLogger.LogSizeCompressed / 1024.0 / 1024;
                    var compRatio = Math.Round((uncomp / comp) * 100);
                    var dataReport = "    Log report:\n    \n" +
                                     "    Items: " + items + "\n" +
                                     "    Update interval: " + PostLogger.UpdateInterval + " seconds \n" +
                                     "    Size (uncompressed): " + Math.Round(uncomp) + " MiB\n" +
                                     "    Size (compressed): " + Math.Round(comp) + " MiB\n" +
                                     "    Compression %: " + compRatio + "\n";
                    chatRoom.PostMessage(dataReport);
                    return;
                }
                case "LOCAL DATA":
                {
                    var secsAlive = (DateTime.UtcNow - startTime).TotalSeconds;
                    var phamRecTotal = locServer.DataReceivedPham / 1024.0; var phamRecPerSec = phamRecTotal / secsAlive;
                    var phamSentTotal = locServer.DataSentPham / 1024.0;    var phamSentPerSec = phamSentTotal / secsAlive;
                    var ghamRecTotal = locServer.DataReceivedGham / 1024.0; var ghamRecPerSec = ghamRecTotal / secsAlive;
                    var ghamSentTotal = locServer.DataSentGham / 1024.0;    var ghamSentPerSec = ghamSentTotal / secsAlive;
                    var overallTotal = phamRecTotal + phamSentTotal + ghamRecTotal + ghamSentTotal;
                    var overallPerSec = overallTotal / secsAlive;
                    var dataReport = "    Local Yam data report (in KiB):\n" +
                                     "    Total transferred:  " + Math.Round(overallTotal) + " (~" + Math.Round(overallPerSec, 1) + "/s)\n    \n" +
                                     "    Sent to Pham:       " + Math.Round(phamSentTotal) + " (~" + Math.Round(phamSentPerSec, 1) + "/s)\n" +
                                     "    Received from Pham: " + Math.Round(phamRecTotal) + " (~" + Math.Round(phamRecPerSec, 1) + "/s)\n    \n" +
                                     "    Sent to Gham:       " + Math.Round(ghamSentTotal) + " (~" + Math.Round(ghamSentPerSec, 1) + "/s)\n" +
                                     "    Received from Gham: " + Math.Round(ghamRecTotal) + " (~" + Math.Round(ghamRecPerSec, 1) + "/s)";
                    chatRoom.PostMessage(dataReport);
                    return;
                }
                case "REMOTE DATA":
                {
                    var secsAlive = (DateTime.UtcNow - startTime).TotalSeconds;
                    var clientCount = remServer.Clients.Count;
                    var overallTotal = remServer.TotalDataSent / 1024.0;
                    var overallPerSec = Math.Round(overallTotal / secsAlive, 1);
                    var dataReport = "    Remote Yam data report (in KiB):\n" +
                                     "    Total transferred: " + Math.Round(overallTotal) + " (~" + overallPerSec + "/s)\n" +
                                     "    Clients:           " + clientCount;
                    chatRoom.PostMessage(dataReport);
                    return;
                }
                default:
                {
                    chatRoom.PostReply(command, "`Command not recognised.`");
                    return;
                }
            }
        }

        private static void HandleActiveQuestion(Question q)
        {
            PostLogger.EnqueuePost(true, q.Base);

            var locReq = new LocalRequest { Type = LocalRequest.RequestType.Question, Data = q };
            locServer.SendData(true, locReq);
            locServer.SendData(false, locReq);

            remServer.SendDataAll(q);
        }

        private static void HandleActiveAnswer(Answer a)
        {
            PostLogger.EnqueuePost(false, a);

            var locReq = new LocalRequest { Type = LocalRequest.RequestType.Answer, Data = a };
            locServer.SendData(true, locReq);
            locServer.SendData(false, locReq);

            remServer.SendDataAll(a);
        }

        private static void HandleDataManagerRequest(bool fromPham, LocalRequest req)
        {
            try
            {
                var owner = (string)req.Options["Owner"];
                var key = (string)req.Options["Key"];
                var data = (string)req.Data;

                switch ((string)req.Options["DMReqType"])
                {
                    case "GET":
                    {
                        try
                        {
                            var requestedData = DataManager.LoadData(owner, key);
                            var response = new LocalRequest
                            {
                                ID = LocalRequest.GetNewID(),
                                Type = LocalRequest.RequestType.DataManagerRequest,
                                Options = new Dictionary<string, object>
                                {
                                    { "FullFillReqID", req.ID },
                                    { "Owner", owner },
                                    { "Key", key }
                                },
                                Data = requestedData
                            };

                            locServer.SendData(fromPham, response);
                        }
                        catch (Exception ex)
                        {
                            chatRoom.PostMessage("Detected error in Yam:\n\n" + ex.ToString());
                            yamErrorCount++;

                            // Post back to the listener (prevent the calling thread from hanging).
                            var response = new LocalRequest
                            {
                                ID = LocalRequest.GetNewID(),
                                Type = LocalRequest.RequestType.DataManagerRequest,
                                Options = new Dictionary<string, object>
                                {
                                    { "FullFillReqID", req.ID }
                                }
                            };
                            locServer.SendData(fromPham, response);
                        }
                        return;
                    }
                    case "UPD":
                    {
                        DataManager.SaveData(owner, key, data);
                        return;
                    }
                    case "DEL":
                    {
                        DataManager.DeleteData(owner, key);
                        return;
                    }
                    default:
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            catch (Exception ex)
            {
                SendEx(fromPham, ex, new Dictionary<string, object> { { "ReceivedRequest", req } });
            }
        }

        private static void SendApiKeyEmail(string acceptedBy, string recipient, string key)
        {
            using (var client = new SmtpClient())
            {
                client.Port = 587;
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Timeout = 10000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(apiKeySenderEmail, "password");

                using (var mm = new MailMessage(apiKeySenderEmail, recipient)
                {
                    Subject = "Phamhilator API Key",
                    Body = "This is an automated message sent from the Phamhilator Network (upon request from yourself).\n" +
                            "Your application for an API key has been successfully received & accepted by " + acceptedBy + "!\n\n" +
                            "Your API key is: " + key + "\n\n" +
                            "Regards, The Pham Team."
                })
                {
                    client.Send(mm);
                }
            }
        }

        private static void SendEx(bool toPham, Exception ex, Dictionary<string, object> additionalInfo = null)
        {
            try
            {
                locServer.SendData(toPham, new LocalRequest
                {
                    Type = RequestType.Exception,
                    Options = additionalInfo,
                    Data = ex
                });
            }
            catch (Exception e)
            {
                yamErrorCount++;
                chatRoom.PostMessage("Detected error in Yam:\n\n" + e.ToString());
            }
        }
    }
}
