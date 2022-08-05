using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using NLua;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AstralKeysUploader
{
    public partial class Form1 : Form
    {
        Properties.Settings Settings = Properties.Settings.Default;

        public Form1()
        {
            InitializeComponent();
        }


        private void processLua()
        {

            string url;
            RestClient client;
            IRestResponse response;

            bool result = Uri.TryCreate(URLfield.Text, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!result)
            {
                MessageBox.Show("Invalid URL!","Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using (Lua lua = new Lua())
            {
                var keysList = new List<Keystone>();
                string filePath = directoryPath.Text + @"\SavedVariables\AstralKeys.lua";
                DateTime dt = File.GetLastWriteTime(filePath);
                int timestamp = (int)((DateTimeOffset)dt).ToUnixTimeSeconds();
                Console.WriteLine("The last write time for this file was {0}.", timestamp);
                string fileContents = File.ReadAllText(filePath);
                lua.State.Encoding = Encoding.UTF8;
                lua.DoString(fileContents);
                LuaTable astralKeys = (LuaTable)lua["AstralKeys"];
                LuaTable chars = (LuaTable)lua["AstralCharacters"];
                foreach (LuaTable entry in astralKeys.Values)
                {
                    if (entry["source"].ToString() == "guild")
                    {
                        keysList.Add(new Keystone
                        {
                            character = entry["unit"].ToString(),
                            key_level = Int32.Parse(entry["key_level"].ToString()),
                            dungeon_id = Int32.Parse(entry["dungeon_id"].ToString()),
                            time_stamp = timestamp - Int32.Parse(entry["time_stamp"].ToString())
                        });
                    }
                }

                for (int index = keysList.Count-1; index > -1; index--)
                {
                    string[] character = keysList[index].character.Split('-');
                    url = $"https://raider.io/api/v1/characters/profile?region=us&realm={character[1]}&name={character[0]}&fields=guild%2Cmythic_plus_scores_by_season%3Acurrent";
                    //Console.WriteLine(url);
                    client = new RestClient(url);
                    response = client.Execute(new RestRequest(Method.GET));
                    if (response.IsSuccessful)
                    {
                        RIOProfile rioprofile = JsonConvert.DeserializeObject<RIOProfile>(response.Content);
                        keysList[index].RIOProfile = rioprofile;
                        string[] guildList = new string[] { "Mini Heroes", "Zeroes to Heroes", "Little Heroes", "Death Jesters" };
                        try
                        {
                            if (!guildList.Contains(rioprofile.guild.name))
                            {
                                Console.WriteLine($"Removed {keysList[index].character} from guild {rioprofile.guild.name}");
                                AppendText($"Removed {keysList[index].character} from guild {rioprofile.guild.name}" + Environment.NewLine);
                                keysList.RemoveAt(index);
                            }
                            if (rioprofile == null)
                            {
                                Console.WriteLine($"Removed {keysList[index].character} from guild {rioprofile.guild.name}");
                                AppendText($"Removed {keysList[index].character} from guild {rioprofile.guild.name}" + Environment.NewLine);
                                keysList.RemoveAt(index);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Removed {keysList[index].character} - No RIO profile");
                            AppendText($"Removed {keysList[index].character} - No RIO profile" + Environment.NewLine);
                            keysList.RemoveAt(index);
                            Console.WriteLine(e.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("unsuccessful");
                        Console.WriteLine($"Removed {keysList[index].character} - No RIO profile");
                        AppendText($"Removed {keysList[index].character} - No RIO profile" + Environment.NewLine);
                        keysList.RemoveAt(index);
                    }


                }

                List<Keystone> sortedList = keysList.OrderBy(k => k.dungeon_name).ThenBy(k => k.key_level).ToList();

                foreach (var entry in sortedList)
                {
                    Console.WriteLine($"{entry.character} ({entry.RIOProfile.active_spec_name} {entry.RIOProfile.@class})  {entry.RIOProfile.mythic_plus_scores_by_season.First().scores.all} - {entry.dungeon_name} {entry.key_level} ");
                    AppendText($"{entry.character} ({entry.RIOProfile.active_spec_name} {entry.RIOProfile.@class})  {entry.RIOProfile.mythic_plus_scores_by_season.First().scores.all} - {entry.dungeon_name} {entry.key_level} \r\n");
                }

                var json = JsonConvert.SerializeObject(new SubmissionData
                {
                    keystones = sortedList,
                    user = userToken.Text
                });

                client = new RestClient(URLfield.Text);
                var request = new RestRequest(Method.POST);
                request.AddJsonBody(json);
                response = client.Execute(request);
                if (!response.IsSuccessful)
                {
                    AppendText($"Upload failed with status {response.StatusCode} - {response.ErrorMessage} {Environment.NewLine} {response.Content}");
                    return;
                }

                AppendText(Environment.NewLine + "___Begin JSON Data___" + Environment.NewLine + json + Environment.NewLine);
                string curtime = DateTime.Now.ToString("dddd, dd MMMM yyyy HH: mm:ss");
                AppendText(Environment.NewLine + Environment.NewLine + $"¯¯¯ LAST UPLOAD AT {curtime} ¯¯¯" + Environment.NewLine + response.Content);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Directory.Exists(Settings.SavedVariables))
            {
                directoryPath.Text = Settings.SavedVariables;
            }
            else
            {
                string subkey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\World of Warcraft";

                RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                directoryPath.Text = localKey.OpenSubKey(subkey).GetValue("InstallLocation").ToString() + @"\_retail_\WTF\Account";
            }
            userToken.Text = Settings.DiscordTag;
            URLfield.Text = Settings.URL;
        }

        private void textBox3_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                InitialDirectory = directoryPath.Text,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                directoryPath.Text = dialog.FileName;
                Settings.SavedVariables = dialog.FileName;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            processLua();
        }

        private void directoryPath_TextChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(Settings.SavedVariables))
            {
                validateFolder();
            }
            Settings.SavedVariables = directoryPath.Text;
            Settings.Save();
            Settings.Reload();
        }

        private void discordTag_Leave(object sender, EventArgs e)
        {
            Settings.DiscordTag = userToken.Text;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Save();
        }
        private void Launch()
        {
            Settings.DiscordTag = userToken.Text;
            Settings.SavedVariables = directoryPath.Text;
            Settings.URL = URLfield.Text;
            Settings.Save();

            var watcher = new FileSystemWatcher()
            {
                Path = Settings.SavedVariables + @"\Savedvariables",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
                Filter = "*AstralKeys.lua"
            };
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;

            AppendText($"Monitoring AstralKeys.lua file for changes in {watcher.Path}" + Environment.NewLine);

            Hide();
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(50000);
            button3.Text = "Exit";   
        }

        public void AppendText (string text)
        {
            if (textBox1.InvokeRequired)
            {
                // Call this same method but append THREAD2 to the text
                Action safeWrite = delegate { AppendText(text); };
                textBox1.Invoke(safeWrite);
            }
            else
                textBox1.AppendText(text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text == "Exit")
            {
                Close();
            }
            if (validateFolder())
            {
                Launch();
            }
        }

        private bool validateFolder ()
        {
            string thisPath = directoryPath.Text + @"\SavedVariables\AstralKeys.lua";
            if (!File.Exists(thisPath))
            {
                MessageBox.Show("Could not find a SavedVariables folder with an AstralKeys.lua file in the selected account directory!" +
                    Environment.NewLine + Environment.NewLine + thisPath,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            //if the form is minimized  
            //hide it from the task bar  
            //and show the system tray icon (represented by the NotifyIcon control)  
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(50000);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void contextMenuStrip1_Click(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("By Waggz#1963", "About", MessageBoxButtons.OK);
        }
        public void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed.  
           
        }
        public void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.  
            processLua();
        }

        private void URLfield_TextChanged(object sender, EventArgs e)
        {
            Settings.URL = URLfield.Text;
            Settings.Save();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://juno.waggz.rocks:3000/users");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (checkBox1.Checked)
                rk.SetValue("AstralKeysUploader", Application.ExecutablePath);
            else
                rk.DeleteValue("AstralKeysUploader", false);
        }
    }
}
