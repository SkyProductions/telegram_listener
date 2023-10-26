using Microsoft.VisualBasic;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using TL;

namespace WTelegramClientTestWF
{
	public partial class MainForm : Form
	{
		private WTelegram.Client _client;

		public MainForm()
		{
			InitializeComponent();
			WTelegram.Helpers.Log = (l, s) => Debug.WriteLine(s);
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			_client?.Dispose();
			Properties.Settings.Default.Save();
		}

		private void linkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start(((LinkLabel)sender).Tag as string);
		}

		private async void buttonLogin_Click(object sender, EventArgs e)
		{
			buttonLogin.Enabled = false;
			listBox.Items.Add($"Connecting & login into Telegram servers...");
			_client = new WTelegram.Client(int.Parse(textBoxApiID.Text), textBoxApiHash.Text);
			await DoLogin(textBoxPhone.Text);
		}

		private async Task DoLogin(string loginInfo)
		{
			string what = await _client.Login(loginInfo);
			if (what != null)
			{
				listBox.Items.Add($"A {what} is required...");
				labelCode.Text = what + ':';
				textBoxCode.Text = "";
				labelCode.Visible = textBoxCode.Visible = buttonSendCode.Visible = true;
				textBoxCode.Focus();
				return;
			}
			panelActions.Visible = true;
			listBox.Items.Add($"We are now connected as {_client.User}");
		}

		private async void buttonSendCode_Click(object sender, EventArgs e)
		{
			labelCode.Visible = textBoxCode.Visible = buttonSendCode.Visible = false;
			await DoLogin(textBoxCode.Text);
		}

		private async void buttonGetChats_Click(object sender, EventArgs e)
		{
			if (_client.User == null)
			{
				MessageBox.Show("You must login first.");
				return;
			}

            _client.OnUpdate += _client_OnUpdate;
			//var chats = await _client.Messages_GetAllChats();
			//listBox.Items.Clear();
			//foreach (var chat in chats.chats.Values)
			//	if (chat.IsActive)
			//		listBox.Items.Add(chat);
		}

		
        private async Task _client_OnUpdate(UpdatesBase arg)
        {
            if(arg.UpdateList.Length > 0)
			{
               
                foreach (var item in arg.UpdateList)
				{
					if((item is UpdateNewChannelMessage))
					{
                        String _content = string.Empty;
                        UpdateNewChannelMessage msg = item as UpdateNewChannelMessage;
						if(msg.message.Peer is PeerChannel)
						{
                            PeerChannel channel = msg.message.Peer as PeerChannel;
                            _content += $"{channel.channel_id} {channel}:";
						}
                        _content += msg.message.ToString();

                        listBox.Items.Add(_content);
                    }
                }
			}
        }

        private async void buttonGetMembers_Click(object sender, EventArgs e)
		{
			if (listBox.SelectedItem is not ChatBase chat)
			{
				MessageBox.Show("You must select a chat in the list first");
				return;
			}
			var users = chat is Channel channel
				? (await _client.Channels_GetAllParticipants(channel)).users
				: (await _client.Messages_GetFullChat(chat.ID)).users;
			listBox.Items.Clear();
			foreach (var user in users.Values)
				listBox.Items.Add(user);
		}

		private async void buttonGetDialogs_Click(object sender, EventArgs e)
		{
			if (_client.User == null)
			{
				MessageBox.Show("You must login first.");
				return;
			}
			var dialogs = await _client.Messages_GetAllDialogs(null);
			listBox.Items.Clear();
			foreach (Dialog dialog in dialogs.dialogs)
			{
				var peer = dialogs.UserOrChat(dialog);
				if (peer.IsActive)
					listBox.Items.Add(peer);
			}
		}

		private async void buttonSendMsg_Click(object sender, EventArgs e)
		{
			var msg = Interaction.InputBox("Type some text to send to ourselves\n(Saved Messages)", "Send to self");
			if (!string.IsNullOrEmpty(msg))
			{
				msg = "_Here is your *saved message*:_\n" + Markdown.Escape(msg);
				var entities = _client.MarkdownToEntities(ref msg);
				await _client.SendMessageAsync(InputPeer.Self, msg, entities: entities);
			}
		}
	}
}
