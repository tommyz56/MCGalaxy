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
using System;
using System.Collections.Generic;
using System.IO;
namespace MCGalaxy.Commands
{
    public sealed class CmdRules : Command
    {
        public override string name { get { return "rules"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.Information; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }
        public CmdRules() { }

        public override void Use(Player p, string message)
        {
            if (!File.Exists("text/rules.txt")) {
                CP437Writer.WriteAllText("text/rules.txt", "No rules entered yet!");
            }
            List<string> rules = CP437Reader.ReadAllLines("text/rules.txt");

            Player who = null;
            if (message != "")
            {
                if (p == null || (int)p.group.Permission < CommandOtherPerms.GetPerm(this))
                {
                    Server.s.Log(((int)p.group.Permission).ToString());
                    Server.s.Log(CommandOtherPerms.GetPerm(this).ToString());
                    Player.SendMessage(p, "You can't send /rules to another player!");
                    return;
                }
                who = Player.Find(message);
            }
            else
            {
                who = p;
            }
            
            if (who != null)
            {
                who.hasreadrules = true;
                if (who.level == Server.mainLevel && Server.mainLevel.permissionbuild == LevelPermission.Guest) { who.SendMessage("You are currently on the guest map where anyone can build"); }
                who.SendMessage("Server Rules:");
                if (who.name != p.name) {Player.SendMessage(p, "Sent the rules to " + who.color + who.DisplayName + Server.DefaultColor + ".");}
                foreach (string s in rules)
                    who.SendMessage(s);
                if (who.name != p.name) {who.SendMessage(p.color + p.DisplayName + Server.DefaultColor + " sent you the rules.");}
            }
            else if (p == null && String.IsNullOrEmpty(message))
            {
                Player.SendMessage(null, "Server Rules:");
                foreach (string s in rules)
                    Player.SendMessage(null, s);
            }
            else
            {
                Player.SendMessage(p, "There is no player \"" + message + "\"!");
            }
        }

        public override void Help(Player p)
        {
        	if ((int)p.group.Permission >= CommandOtherPerms.GetPerm(this))
        	{
        		Player.SendMessage(p, "/rules [player]- Displays server rules to a player.");
        		Player.SendMessage(p, "If no [player] is given, the rules will be sent to you.");
        	}
        	else
        	{
            	Player.SendMessage(p, "/rules - Displays the server rules.");
        	}
        }
    }
}
