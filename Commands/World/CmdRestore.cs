/*
	Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
	
	Dual-licensed under the	Educational Community License, Version 2.0 and
	the GNU General Public License, Version 3 (the "Licenses"); you may
	not use this file except in compliance with the Licenses. You may
	obtain a copy of the Licenses at
	
	http://www.opensource.org/licenses/ecl2.php
	http://www.gnu.org/licenses/gpl-3.0.html
	
	Unless required by applicable law or agreed to in writing,
	software distributed under the Licenses are distributed on an "AS IS"
	BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
	or implied. See the Licenses for the specific language governing
	permissions and limitations under the Licenses.
*/
using System.IO;
namespace MCGalaxy.Commands
{
    public sealed class CmdRestore : Command
    {
        public override string name { get { return "restore"; } }
        public override string shortcut { get { return ""; } }
       public override string type { get { return CommandTypes.World; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public CmdRestore() { }

        public override void Use(Player p, string message)
        {
            //Thread CrossThread;

            if (message != "")
            {
                Level lvl;
                string[] text = new string[2];
                text[0] = "";
                text[1] = "";
                try
                {
                    text[0] = message.Split(' ')[0].ToLower();
                    text[1] = message.Split(' ')[1].ToLower();
                }
                catch
                {
                    text[1] = p.level.name;
                }
                if (message.Split(' ').Length >= 2)
                {

                    lvl = Level.Find(text[1]);
                    if (lvl == null)
                    {
                        Player.SendMessage(p, "Level not found!");
                        return;
                    }

                }
                else
                {
                    if (p != null && p.level != null) lvl = p.level;
                    else
                    {
                        Server.s.Log("u dun derped, specify the level, silly head");
                        return;
                    }
                }
                Server.s.Log(@Server.backupLocation + "/" + lvl.name + "/" + text[0] + "/" + lvl.name + ".lvl");
                if (File.Exists(@Server.backupLocation + "/" + lvl.name + "/" + text[0] + "/" + lvl.name + ".lvl"))
                {
                    try
                    {
                        File.Copy(@Server.backupLocation + "/" + lvl.name + "/" + text[0] + "/" + lvl.name + ".lvl", "levels/" + lvl.name + ".lvl", true);
                        Level temp = Level.Load(lvl.name);
                        temp.StartPhysics();
                        if (temp != null)
                        {
                            lvl.spawnx = temp.spawnx;
                            lvl.spawny = temp.spawny;
                            lvl.spawnz = temp.spawnz;

                            lvl.Length = temp.Length;
                            lvl.Width = temp.Width;
                            lvl.Height = temp.Height;

                            lvl.blocks = temp.blocks;
                            lvl.setPhysics(0);
                            lvl.ClearPhysics();

                            Command.all.Find("reveal").Use(null, "all " + text[1]);
                        }
                        else
                        {
                            Server.s.Log("Restore nulled");
                            File.Copy("levels/" + lvl.name + ".lvl.backup", "levels/" + lvl.name + ".lvl", true);
                        }

                    }
                    catch { Server.s.Log("Restore fail"); }
                }
                else { Player.SendMessage(p, "Backup " + text[0] + " does not exist."); }
            }
            else
            {
                if (Directory.Exists(@Server.backupLocation + "/" + p.level.name))
                {
                    string[] directories = Directory.GetDirectories(@Server.backupLocation + "/" + p.level.name);
                    int backupNumber = directories.Length;
                    Player.SendMessage(p, p.level.name + " has " + backupNumber + " backups .");

                    bool foundOne = false; string foundRestores = "";
                    foreach (string s in directories)
                    {
                        string directoryName = s.Substring(s.LastIndexOf('\\') + 1);
                        try
                        {
                            int.Parse(directoryName);
                        }
                        catch
                        {
                            foundOne = true;
                            foundRestores += ", " + directoryName;
                        }
                    }

                    if (foundOne)
                    {
                        Player.SendMessage(p, "Custom-named restores:");
                        Player.SendMessage(p, "> " + foundRestores.Remove(0, 2));
                    }
                }
                else
                {
                    Player.SendMessage(p, p.level.name + " has no backups yet.");
                }
            }
        }

        public override void Help(Player p)
        {
            Player.SendMessage(p, "/restore <number> - restores a previous backup of the current map");
            Player.SendMessage(p, "/restore <number> <name> - restores a previous backup of the selected map");
        }
    }
}
