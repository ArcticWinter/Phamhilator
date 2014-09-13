﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;



namespace Phamhilator
{
	public partial class MainWindow
	{
		private int roomId;
		private bool startMonitoring;
		private bool exit;
		private bool catchOff = true;
		private bool catchSpam = true;
		private bool catchLQ = true;
		private bool catchBadTag = true;
		private bool refreshBadTags;
		private bool quietMode;
		private string previouslyPostMessagesPath =  Path.Combine(Directory.GetParent(Directory.GetParent(Environment.CurrentDirectory).FullName).FullName, "Previously Post Messages.txt");
		private readonly HashSet<Post> postedMessages = new HashSet<Post>();
		private readonly DateTime twentyTen = new DateTime(2010, 01, 01);



		public MainWindow()
		{
			InitializeComponent();

			HideScriptErrors(realtimeWb, true);
			HideScriptErrors(chatWb, true);

			PopulatePostedMessages();

			new Thread(() =>
			{
				do
				{
					Thread.Sleep(12000);
				} while (!startMonitoring);

				while (!exit)
				{
					Dispatcher.Invoke(() => realtimeWb.Refresh());

					do
					{
						Thread.Sleep(12000);
					} while (!startMonitoring);
				}
			}).Start();

			new Thread(() =>
			{
				while (!startMonitoring)
				{
					Thread.Sleep(6000);
				}

				while (!exit)
				{
					dynamic doc = null;
					string html;

					Dispatcher.Invoke(() => doc = realtimeWb.Document);

					try
					{
						html = doc.documentElement.InnerHtml;
					}
					catch (Exception)
					{
						continue;
					}

					if (!html.Contains("DIV class=metaInfo")) { continue; }	

					var posts = GetAllPosts(html);

					CheckPosts(posts);

					do
					{
						Thread.Sleep(6000);
					} while (!startMonitoring);
				}
			}).Start();
		}



		private IEnumerable<Post> GetAllPosts(string html)
		{
			var posts = new List<Post>();

			html = html.Substring(html.IndexOf("<BR class=cbo>", StringComparison.Ordinal));

			while (html.Length > 10000)
			{
				var postURL = HTMLScrapper.GetURL(html);

				posts.Add(new Post
				{
					URL = postURL,
					Title = HTMLScrapper.GetTitle(html),
					AuthorLink = HTMLScrapper.GetAuthorLink(html),
					AuthorName = HTMLScrapper.GetAuthorName(html),
					Site = HTMLScrapper.GetSite(postURL),
					Tags = HTMLScrapper.GetTags(html)
				});

				var startIndex = html.IndexOf("question-container realtime-question", 100, StringComparison.Ordinal);

				html = html.Substring(startIndex);
			}

			return posts;
		}

		private void CheckPosts(IEnumerable<Post> posts)
		{
			foreach (var post in posts.Where(p => postedMessages.All(pp => pp.Title != p.Title)))
			{
				var info = PostChecker.CheckPost(post, refreshBadTags);
				var message = (!info.InaccuracyPossible ? "" : " (possible)") + ": [" + post.Title + "](" + post.URL + "), by [" + post.AuthorName + "](" + post.AuthorLink + "), on `" + post.Site + "`.";
				
				switch (info.Type)
				{
					case PostType.Offensive:
					{
						if (!catchOff) { break; }

						if (quietMode)
						{
							Task.Factory.StartNew(() => PostMessage("[Offensive](" + post.URL + ")" + (info.InaccuracyPossible ? "." : " possible.")));
							AddPost(post);
						}
						else
						{
							Task.Factory.StartNew(() => PostMessage("**Offensive**" + message));
							AddPost(post);
						}

						break;
					}

					case PostType.BadUsername:
					{
						if (!catchOff) { break; }

						if (quietMode)
						{
							Task.Factory.StartNew(() => PostMessage("[Bad Username](" + post.URL + ")" + (info.InaccuracyPossible ? "." : " possible.")));
							AddPost(post);
						}
						else
						{
							Task.Factory.StartNew(() => PostMessage("**Bad Username**" + message));
							AddPost(post);
						}

						break;
					}

					case PostType.BadTagUsed:
					{
						if (!catchBadTag) { break; }

						if (quietMode)
						{
							Task.Factory.StartNew(() => PostMessage("[Bad Tag Used](" + post.URL + ")" + (info.InaccuracyPossible ? "." : " possible.")));
							AddPost(post);
						}
						else
						{
							Task.Factory.StartNew(() => PostMessage("**Bad Tag Used**" + message));
							AddPost(post);
						}

						break;
					}

					case PostType.LowQuality:
					{
						if (!catchLQ) { break; }

						if (quietMode)
						{
							Task.Factory.StartNew(() => PostMessage("[Low quality](" + post.URL + ")" + (info.InaccuracyPossible ? "." : " possible.")));
							AddPost(post);
						}
						else
						{
							Task.Factory.StartNew(() => PostMessage("**Low quality**" + message));
							AddPost(post);
						}

						break;
					}

					case PostType.Spam:
					{
						if (!catchSpam) { break; }

						if (quietMode)
						{
							Task.Factory.StartNew(() => PostMessage("[Spam](" + post.URL + ")" + (info.InaccuracyPossible ? "." : " possible.")));
							AddPost(post);
						}
						else
						{
							Task.Factory.StartNew(() => PostMessage("**Spam**" + message));
							AddPost(post);
						}

						break;
					}
				}
			}

			if (refreshBadTags) { refreshBadTags = false; }
		}

		private void PostMessage(string message, int consecutiveMessageCount = 0)
		{
			if (roomId == 0)
			{
				GetRoomId();
			}

			var error = false;
			
			Dispatcher.Invoke(() =>
			{
				try
				{
					chatWb.InvokeScript("eval", new object[]
					{
						"$.post('/chats/" + roomId + "/messages/new', { text: '" + message + "', fkey: fkey().fkey });"
					});
				}
				catch (Exception)
				{
					error = true;
				}		
			});

			if (!error) { return; }

			consecutiveMessageCount++;

			var delay = (int)Math.Min((4.1484 * Math.Log(consecutiveMessageCount) + 1.02242), 20) * 1000;

			if (delay >= 20) { return; }

			Thread.Sleep(delay);

			PostMessage(message, consecutiveMessageCount);
		}

		private void HideScriptErrors(WebBrowser wb, bool hide)
		{
			var fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
			if (fiComWebBrowser == null) return;
			var objComWebBrowser = fiComWebBrowser.GetValue(wb);
			if (objComWebBrowser == null)
			{
				wb.Loaded += (o, s) => HideScriptErrors(wb, hide);
				return;
			}
			objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
		}

		private void GetRoomId()
		{
			Dispatcher.Invoke(() =>
			{
				try
				{
					var startIndex = chatWb.Source.AbsolutePath.IndexOf("rooms/", StringComparison.Ordinal) + 6;
					var endIndex = chatWb.Source.AbsolutePath.IndexOf("/", startIndex + 1, StringComparison.Ordinal);

					var t = chatWb.Source.AbsolutePath.Substring(startIndex, endIndex - startIndex);

					if (!t.All(Char.IsDigit)) { return; }

					roomId = int.Parse(t);
				}
				catch (Exception)
				{

				}			
			});
		}

		private void AddPost(Post post)
		{
			AddPost(post);

			File.AppendAllText(previouslyPostMessagesPath, (DateTime.Now - new DateTime(2010, 01, 01)).TotalMinutes + "]" + post.Title + "\n");
		}

		private void PopulatePostedMessages()
		{
			if (!File.Exists(previouslyPostMessagesPath)) { return; }

			var titles = new List<string>(File.ReadAllText(previouslyPostMessagesPath).Split('\n'));
			var date = 0;

			for (var i = 0; i < titles.Count; i++)
			{
				date = int.Parse(titles[i].Split(']')[0].Trim());

				if ((DateTime.Now - twentyTen).TotalMinutes - date > 2880)
				{
					titles.Remove(titles[i]);

					continue;
				}

				postedMessages.Add(new Post { Title = titles[i].Split(']')[1].Trim() });
			}

			File.WriteAllText(previouslyPostMessagesPath, "");

			foreach (var post in titles)
			{
				File.AppendAllText(previouslyPostMessagesPath, post + "\n");
			}
		}



		// ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ UI Events ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ 



		private void Button_Click(object sender, RoutedEventArgs e)
		{
			chatWb.Source = new Uri("http://chat.meta.stackexchange.com/rooms/773/room-for-low-quality-posts");

			GetRoomId();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			chatWb.Source = new Uri("http://chat.meta.stackexchange.com/rooms/89/tavern-on-the-meta");

			GetRoomId();
		}

		private void Button_Click_2(object sender, RoutedEventArgs e)
		{
			var b = ((Button)sender);

			if ((string)b.Content == "Start Monitoring")
			{
				startMonitoring = true;

				b.Content = "Pause Monitoring";

				PostMessage("`Phamhilator™ started...`");
			}
			else
			{
				startMonitoring = false;

				b.Content = "Start Monitoring";

				PostMessage("`Phamhilator™ paused...`");
			}
		}

		private void catchOffCb_Checked(object sender, RoutedEventArgs e)
		{
			catchOff = true;

			if (roomId != 0)
			{
				PostMessage("`Offensive reports enabled.`");
			}
		}

		private void catchOffCb_Unchecked(object sender, RoutedEventArgs e)
		{
			catchOff = false;

			if (roomId != 0)
			{
				PostMessage("`Offensive reports disabled.`");
			}
		}

		private void catchLQCb_Checked(object sender, RoutedEventArgs e)
		{
			catchLQ = true;

			if (roomId != 0)
			{
				PostMessage("`Low Quality reports enabled.`");
			}
		}

		private void catchLQCb_Unchecked(object sender, RoutedEventArgs e)
		{
			catchLQ = false;

			if (roomId != 0)
			{
				PostMessage("`Low Quality reports disabled.`");
			}
		}

		private void catchSpamCb_Checked(object sender, RoutedEventArgs e)
		{
			catchSpam = true;

			if (roomId != 0)
			{
				PostMessage("`Spam reports enabled.`");
			}
		}

		private void catchSpamCb_Unchecked(object sender, RoutedEventArgs e)
		{
			catchSpam = false;

			if (roomId != 0)
			{
				PostMessage("`Spam reports disabled.`");
			}
		}

		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			catchBadTag = true;

			if (roomId != 0)
			{
				PostMessage("`Bad Tag Used reports enabled.`");
			}
		}

		private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			catchBadTag = false;

			if (roomId != 0)
			{
				PostMessage("`Bad Tags Used reports disabled.`");
			}
		}

		private void Button_Click_3(object sender, RoutedEventArgs e)
		{
			var b = (Button)sender;

			if ((string)b.Content == "Enable Quiet Mode")
			{
				quietMode = true;
				b.Content = "Disable Quiet Mode";

				PostMessage("`Quiet mode enabled.`");
			}
			else
			{
				quietMode = false;
				b.Content = "Enable Quiet Mode";

				PostMessage("`Quiet mode disabled.`");
			}
		}

		private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (roomId == 0) { return; }

			e.Cancel = true;

			PostMessage("`Phamhilator™ stopped.`");

			exit = true;

			Task.Factory.StartNew(() =>
			{
				Thread.Sleep(5000);

				Dispatcher.Invoke(() => Application.Current.Shutdown());
			});
		}

		private void Button_Click_4(object sender, RoutedEventArgs e)
		{
			refreshBadTags = true;

			PostMessage("`Bad Tag definitions updated.`");
		}
	}
}
