/*
    Copyright 2011 MCGalaxy
        
    Dual-licensed under the    Educational Community License, Version 2.0 and
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

namespace MCGalaxy.Commands {
    
    public sealed class CmdEnvironment : Command {
        
        public override string name { get { return "environment"; } }
        public override string shortcut { get { return "env"; } }
        public override string type { get { return CommandTypes.World; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public CmdEnvironment() { }

        public override void Use(Player p, string message) {
        	message = message.ToLower();
        	if (message == "l preset" || message == "level preset") {
        		SendPresetsMessage(p);
        		return;
        	}
            string[] args = null;
            if (message == "" || (args = message.Split(' ')).Length < 3) {
                Help(p); return;
            } else {
                string group = args[0];
                string target = null;
                
                if (group == "player" || group == "p") {
                    target = "p";
                } else if (group == "level" || group == "l") {
                    target = "l";
                } else {
                    p.SendMessage(string.Format("Env: Unrecognised target \"{0}\".", group));
                    return;
                }
                Handle(p, target, args[1], args[2]);
            }
        }
        
        void Handle(Player p, string group, string variable, string value) {
            bool level = group == "l";
            switch (variable) {
                case "fog":
                    SetEnvColour(p, value, 2, "fog", ref p.level.FogColor, level);
                    break;
                case "cloud":
                case "clouds":
                    SetEnvColour(p, value, 1, "cloud", ref p.level.CloudColor, level);
                    break;
                case "sky":
                    SetEnvColour(p, value, 0, "sky", ref p.level.SkyColor, level);
                    break;
                case "dark":
                case "shadow":
                    SetEnvColour(p, value, 3, "shadow", ref p.level.ShadowColor, level);
                    break;
                case "sun":
                case "light":
                case "sunlight":
                    SetEnvColour(p, value, 4, "sunlight", ref p.level.LightColor, level);
                    break;
                case "weather":
                    SetEnvWeather(p, value, ref p.level.weather, level);
                    break;
                    
                case "level":
                    if (!level) {
                        p.SendMessage("This feature is not available for 'player' target");
                        return;
                    }
                    byte ignored = 0;
                    SetEnvMapAppearance(p, value, "water level", false, 0, ref ignored );
                    break;
                case "horizon":
                case "edge":
                case "water":
                    if (!level) {
                        p.SendMessage("This feature is not available for 'player' target"); return;
                    }
                    SetEnvMapAppearance(p, value, "edge block", true, Block.waterstill, ref p.level.HorizonBlock);
                    break;
                case "side":
                case "border":
                case "bedrock":
                    if (!level) {
                        p.SendMessage("This feature is not available for 'player' target"); return;
                    }
                    SetEnvMapAppearance(p, value, "sides block", true, Block.blackrock, ref p.level.EdgeBlock);
                    break;
                case "preset":
                    if (!SetPreset(p, value, level))
                        return;
                    break;
                default:
                    Help(p);
                    return;
            }
            
            if (level)
                p.level.Save(true);
        }
        
        void SetEnvColour(Player p, string value, byte envType, string envTypeName,
                          ref string modify, bool level) {
            string target = level ? p.level.name : "you";
            if (IsResetString(value)) {
                p.SendMessage(string.Format("Reset {0} color for {1}&S to normal", envTypeName, target));
                if( level )
                    modify = null;
            } else {
                if (value.Length > 0 && value[0] == '#')
                    value = value.Substring(1);
                if (value.Length != 6 || !IsValidHex(value)) {
                    p.SendMessage(string.Format("Env: \"#{0}\" is not a valid HEX color code.", value));
                    return;
                }
                
                p.SendMessage(string.Format("Set {0} color for {1}&S to #{2}", envTypeName, target, value));
                if( level )
                    modify = value;
            }
            SendEnvColorPacket(p, level, envType, value);
        }
        
        void SendEnvColorPacket(Player p, bool level, byte envType, string value) {
            if (level) {
                foreach (Player pl in Player.players) {
                    if (pl.level == p.level)
                        SendEnvColorPacket(pl, envType, value);
                }
            } else {
                SendEnvColorPacket(p, envType, value);
            }
        }
        
        void SendEnvColorPacket(Player p, byte envType, string value) {
            if (p.HasCpeExt(CpeExt.EnvColors)) {
                try {
                    System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml("#" + value.ToUpper());
                    p.SendEnvColor(envType, col.R, col.G, col.B);
                } catch {
                    p.SendEnvColor(envType, -1, -1, -1);
                }
            }
        }
        
        void SetEnvWeather(Player p, string value, ref byte modify, bool level) {
            byte weather = 255;
            string target = level ? p.level.name : "you";
            if (IsResetString(value)) {
                p.SendMessage(string.Format("Reset weather for {0}&S to 0 (sun)", target));
                if( level )
                    p.level.weather = 0;
            } else {
                if (byte.TryParse(value, out weather)) {
                } else if (CaselessEquals(value, "sun")) {
                    weather = 0;
                } else if (CaselessEquals(value, "rain")) {
                    weather = 1;
                }  else if (CaselessEquals(value, "snow")) {
                    weather = 2;
                }
                
                if (weather > 2) {
                    p.SendMessage("Please use a valid integer (0,1,2) or string (sun,rain,snow)");
                    return;
                }
            }
            
            if( level )
                p.level.weather = weather;
            string weatherType = weather == 0 ? "&sSun" : (weather == 1 ? "&1Rain" : "&fSnow");
            p.SendMessage(string.Format("&aSet weather for {0}&a to {1} ({2}&a)", target, weather, weatherType));
            
            // Send the changed colour to all players affected by the command.
            if (level) {
                foreach (Player pl in Player.players) {
                    if (pl.level == p.level && pl.HasCpeExt(CpeExt.EnvWeatherType)) {
                        pl.SendSetMapWeather(weather);
                    }
                }
            } else if (p.HasCpeExt(CpeExt.EnvWeatherType)) {
                p.SendSetMapWeather(weather);
            }
        }
        
        void SetEnvMapAppearance(Player p, string value, string variable,
                                 bool block, byte defValue, ref byte modifyBlock) {
            if (IsResetString(value)) {
                p.SendMessage(string.Format("Reset {0} for {0}&S to normal", variable, p.level.name));
                if( block )
                    modifyBlock = defValue;
                else
                    p.level.EdgeLevel = (short)(p.level.Height / 2);
            } else {
                if (block && !CheckBlock(p, value, variable, ref modifyBlock))
                    return;
                if( !block && !CheckShort(p, value, variable, ref p.level.EdgeLevel))
                    return;
            }
            
            foreach (Player pl in Player.players) {
                if (pl.HasCpeExt(CpeExt.EnvMapAppearance) && pl.level == p.level)
        			pl.SendCurrentMapAppearance();
            }
        }
        
        bool CheckBlock(Player p, string value, string variable, ref byte modify) {
            byte block = Block.Byte(value);
            if (block == Block.Zero) {
                Help(p);
            } else if (block == Block.shrub || block == Block.yellowflower || block == Block.redflower || 
                       block == Block.mushroom || block == Block.redmushroom || block == Block.rope || block == Block.fire) {
                p.SendMessage(string.Format("Env: Cannot use {0} for {1}.", block, variable));
            } else {
                modify = block;
                p.SendMessage(string.Format("Set {0} for {1}&S to {2}", variable, p.level.name, block));
                return true;
            }
            return false;
        }
        
        bool CheckShort(Player p, string value, string variable, ref short modify) {
            short level;
            if (!short.TryParse(value, out level)) {
                p.SendMessage(string.Format("Env: \"{0}\" is not a valid integer.", value));
                return false;
            } else {
                modify = level;
                p.SendMessage(string.Format("Set {0} for {1}&S to {2}", variable, p.level.name, level));
                return true;
            }
        }
        
        bool SetPreset(Player p,  string value, bool level) {
            EnvPreset preset = null; // fog, sky, clouds, sun, shadow
            if (CaselessEquals(value, "midnight")) {
                preset = new EnvPreset("8b8989", "191970", "000080", "0000cd", "918A3B");
            } else if (CaselessEquals(value, "cartoon")) {
                preset = new EnvPreset("00ffff", "1e90ff", "00bfff", "f5deb3", "f4a460");
            } else if (CaselessEquals(value, "noir")) {
                preset = new EnvPreset("000000", "1f1f1f", "000000", "696969", "1f1f1f");
            } else if (CaselessEquals(value, "trippy")) {
                preset = new EnvPreset("4B0082", "FFD700", "006400", "7CFC00", "B22222");
            } else if (CaselessEquals(value, "watery")) {
                preset = new EnvPreset("5f9ea0", "008080", "008B8B", "E0FFFF", "008B8B");
            } else if (CaselessEquals(value, "normal")) {
                preset = new EnvPreset("-1", "-1", "-1", "-1", "-1");
            } else if (CaselessEquals(value, "gloomy")) {
                preset = new EnvPreset("6A80A5", "405875", "405875", "444466", "3B3B59");
            } else if (CaselessEquals(value, "cloudy")) {
                preset = new EnvPreset("AFAFAF", "8E8E8E", "8E8E8E", "9b9b9b", "8C8C8C");
            } else if (CaselessEquals(value, "sunset")) {
                preset = new EnvPreset("FFA322", "836668", "9A6551", "7F6C60", "46444C");
            } else if (CaselessEquals(value, "midnight2")) {
                preset = new EnvPreset("131947", "070A23", "1E223A", "181828", "0F0F19");
            }
            
            if( preset != null ) {
                SendEnvColorPacket(p, level, 0, preset.Sky);
                if (level) p.level.SkyColor = preset.Sky;
                SendEnvColorPacket(p, level, 1, preset.Clouds);
                if (level) p.level.CloudColor = preset.Clouds;
                SendEnvColorPacket(p, level, 2, preset.Fog);
                if (level) p.level.FogColor = preset.Fog;
                SendEnvColorPacket(p, level, 3, preset.Shadow);
                if (level) p.level.ShadowColor = preset.Shadow;
                SendEnvColorPacket(p, level, 4, preset.Sun);
                if (level) p.level.LightColor = preset.Sun;
                
                if (CaselessEquals( value, "normal") && level) {
                    Command.all.Find("env").Use(p, "l weather 0");
                    Command.all.Find("env").Use(p, "l water normal");
                    Command.all.Find("env").Use(p, "l bedrock normal");
                    Command.all.Find("env").Use(p, "l level normal");
                }
                return true;
            } else {
            	SendPresetsMessage(p);
                return false;
            }
        }
        
        class EnvPreset {
            public string Fog, Sky, Clouds, Sun, Shadow;
            
            public EnvPreset( string fog, string sky, string clouds,
                             string sun, string shadow ) {
                Fog = fog; Sky = sky; Clouds = clouds;
                Sun = sun; Shadow = shadow;
            }
        }
        
        static bool IsValidHex(string hex) {
            for (int i = 0; i < hex.Length; i++) {
                char ch = hex[i];
                if (ch < '0' || ch > '9' && ch < 'A' || ch > 'F' &&
                    ch < 'a' || ch > 'f') {
                    return false;
                }
            }
            return true;
        }
        
        bool IsResetString(string value) {
            return CaselessEquals(value, "-1") || CaselessEquals(value, "normal") ||
                CaselessEquals(value, "reset") || CaselessEquals(value, "default");
        }
        
        bool CaselessEquals(string a, string b) {
            return a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }
        
        static void SendPresetsMessage(Player p) {
        	 p.SendMessage("/env l preset [type] -- Uses an env preset on your map");
             p.SendMessage("Valid types: Cartoon/Midnight/Midnight2/Noir/Normal/Trippy/Watery/Sunset/Gloomy/Cloudy");
        }
        
        public override void Help(Player p) {
            p.SendMessage("%T/env [target] [variable] [value]");
            p.SendMessage("%H  Valid targets: player or p, level or l");
            p.SendMessage("%H  Valid variables: fog, cloud, sky, sun, shadow, weather");
            p.SendMessage("%H     level only variables: level, horizon, border, preset");
            p.SendMessage("%HUsing 'normal' as a value will reset the variable");
        }
    }
}
