﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;



namespace Phamhilator
{
	public class MessageHandler
	{
		public readonly Dictionary<MessageInfo, int> MessageQueue = new Dictionary<MessageInfo, int>();



		public MessageHandler()
		{
			new Thread(() =>
			{
				for (var i = 1; i < 5; i++)
				{
					try
					{
						PostMessages();			
					}
					catch (Exception)
					{
						if (!GlobalInfo.Exit)
						{
							Thread.Sleep(1000);

							if (i == 4)
							{
								PostChatMessage("`Warning: 3 attempts to restart message poster thread have failed. Now shutting down...`");

								GlobalInfo.Exit = true;
							}
							else
							{
								PostChatMessage("`Warning: message poster thread has died. Attempting to restart...`");
							}
						}
					}

					if (GlobalInfo.Exit) { break; }
				}		
			}) { Priority = ThreadPriority.Lowest }.Start();
		}



		public static void EditMessage(string newMessage, int messageID)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				try
				{
					GlobalInfo.ChatWb.InvokeScript("eval", new object[]
					{
						@"$.post('http://chat.meta.stackexchange.com/messages/" + messageID + "', { text: '" + newMessage + "', fkey: fkey().fkey });"
					});
				}
				catch (Exception)
				{

				}
			});
		}

		public static bool DeleteMessage(string reportTitle, int messageID, bool checkSuccess = true)
		{
			dynamic doc = null;
			var html = "";

			Application.Current.Dispatcher.Invoke(() =>
			{
				try
				{
					GlobalInfo.ChatWb.InvokeScript("eval", new object[]
					{
						@"$.post('http://chat.meta.stackexchange.com/messages/" + messageID + "/delete" + "', { fkey: fkey().fkey });"
					});
				}
				catch (Exception)
				{

				}
			});

			if (!checkSuccess) { return true; }

			Thread.Sleep(3000); // Wait for message to be deleted.

			try
			{
				Application.Current.Dispatcher.Invoke(() => doc = GlobalInfo.ChatWb.Document);

				html = doc.documentElement.InnerHtml;
			}
			catch (Exception)
			{

			}
			
			return html.IndexOf(reportTitle, StringComparison.Ordinal) == -1;
		}		
		
		
		
		private void PostMessages(/*int consecutiveMessageCount = 0*/)
		{
			var roomID = 0;
			var error = false;
			MessageInfo message;

			do
			{
				Thread.Sleep(1000);
			} while (!GlobalInfo.BotRunning);

			while (!GlobalInfo.Exit)
			{
				Thread.Sleep(1000);

				if (GlobalInfo.ChatRoomID == 0 || GlobalInfo.AnnouncerRoomID == 0 || MessageQueue.Count == 0) { continue; }

				message = MessageQueue.Keys.First();
				roomID = MessageQueue[message];
				error = false;
				
				// Post message.

				Application.Current.Dispatcher.Invoke(() =>
				{
					try
					{
						if (roomID == GlobalInfo.ChatRoomID)
						{
							GlobalInfo.ChatWb.InvokeScript("eval", new object[]
							{
								"$.post('/chats/" + roomID + "/messages/new', { text: '" + message.Body + "', fkey: fkey().fkey });"
							});
						}
						else
						{
							GlobalInfo.AnnounceWb.InvokeScript("eval", new object[]
							{
								"$.post('/chats/" + roomID + "/messages/new', { text: '" + message.Body + "', fkey: fkey().fkey });"
							});
						}
						
					}
					catch (Exception)
					{
						error = true;
					}
				});

				MessageQueue.Remove(message);

				if (error || message.Post == null || message.Report == null) { continue; }

				// Get message ID.

				Thread.Sleep(3000);

				dynamic doc = null;
				var i = 0;
				var html = "";

				while (html.IndexOf(message.Post.Title, StringComparison.Ordinal) == -1)
				{
					if (i > 4) { return; }

					Application.Current.Dispatcher.Invoke(() => doc = roomID == GlobalInfo.ChatRoomID ? GlobalInfo.ChatWb.Document : GlobalInfo.AnnounceWb.Document);

					try
					{
						html = doc.documentElement.InnerHtml;
					}
					catch (Exception)
					{
						break;
					}

					i++;

					Thread.Sleep(2500);
				}

				var id = HTMLScraper.GetMessageIDByReportTitle(html, message.Post.Title);

				if (!GlobalInfo.PostedReports.ContainsKey(id))
				{
					GlobalInfo.PostedReports.Add(id, message);
				}				
			}
				

			//consecutiveMessageCount++;

			//var delay = (int)(4.1484 * Math.Log(consecutiveMessageCount) + 1.02242) * 1000;

			//if (consecutiveMessageCount >= 20) { return; }

			//Thread.Sleep(delay);

			//PostMessage(message, consecutiveMessageCount);
		}

		private void PostChatMessage(string message)
		{
			try
			{
				GlobalInfo.ChatWb.InvokeScript("eval", new object[]
				{
					"$.post('/chats/" + GlobalInfo.ChatRoomID + "/messages/new', { text: '" + message+ "', fkey: fkey().fkey });"
				});
			}
			catch (Exception)
			{

			}
		}
	}
}
