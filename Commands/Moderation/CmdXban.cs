/*
	Copyright 2011 MCGalaxy
		
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
    public sealed class CmdXban : Command
    {
        public override string name { get { return "xban"; } }
        public override string shortcut { get { return ""; } }
       public override string type { get { return CommandTypes.Moderation; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public CmdXban() { }
        public override void Use(Player p, string message)
        {

            if (message == "") { Help(p); return; }

            Player who = Player.Find(message.Split(' ')[0]);
            string msg = message.Split(' ')[0];
            if (p != null)
            {
                p.ignorePermission = true;
            }
            try
            {
                if (who != null)
                {
                    Command.all.Find("xundo").Use(p, msg);
                    Command.all.Find("ban").Use(p, msg);
                    Command.all.Find("banip").Use(p, "@" + msg);
                    Command.all.Find("kick").Use(p, message);
                    Command.all.Find("xundo").Use(p, msg);

                }
                else
                {
                    Command.all.Find("ban").Use(p, msg);
                    Command.all.Find("banip").Use(p, "@" + msg);
                    Command.all.Find("xundo").Use(p, msg);
                }

            }
            finally
            {
                if (p != null) p.ignorePermission = false;
            }
        }


        public override void Help(Player p)
        {
            Player.SendMessage(p, "/xban [name] [message]- Bans, banips, undoes, and kicks [name] with [message], if specified.");
        }
    }
}
