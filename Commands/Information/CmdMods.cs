/*
	Copyright 2010 MCLawl Team - Written by Valek (Modified for use with MCGalaxy)
 
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
namespace MCGalaxy.Commands
{
    public sealed class CmdMods : Command
    {
        public override string name { get { return "mods"; } }
        public override string shortcut { get { return "mod"; } }
        public override string type { get { return CommandTypes.Information; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }
        public CmdMods() { }

        public override void Use(Player p, string message)
        {
            if (message != "") { Help(p); return; }
            string modlist = "";
            string tempmods;
            foreach (string mod in Server.Mods)
            {
                tempmods = mod.Substring(0, 1);
                tempmods = tempmods.ToUpper() + mod.Remove(0, 1);
                modlist += tempmods + ", ";
            }
            modlist = modlist.Remove(modlist.Length - 2);
            Player.SendMessage(p, "&9MCGalaxy Moderation Team: " + Server.DefaultColor + modlist + "&e.");
        }

        public override void Help(Player p)
        {
            Player.SendMessage(p, "/mods - Displays the list of MCGalaxy moderators.");
        }
    }
}
