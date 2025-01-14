﻿/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
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
using System.Collections.Generic;
using System.Linq;
using MCGalaxy.SQL;

namespace MCGalaxy {

    public sealed partial class Level : IDisposable {
        
        public byte[] blocks;
        public byte[][] CustomBlocks;
        public int ChunksX, ChunksY, ChunksZ;
        
        public byte GetTile(ushort x, ushort y, ushort z) {
            int index = PosToInt(x, y, z);
            if (index < 0 || blocks == null) return Block.Zero;
            return blocks[index];
        }

        public byte GetTile(int b) {
            ushort x = 0, y = 0, z = 0;
            IntToPos(b, out x, out y, out z);
            return GetTile(x, y, z);
        }
        
        public byte GetExtTile(ushort x, ushort y, ushort z) {
            int index = PosToInt(x, y, z);
            if (index < 0 || blocks == null) return Block.Zero;
            
            int cx = x >> 4, cy = y >> 4, cz = z >> 4;
            byte[] chunk = CustomBlocks[(cy * ChunksZ + cz) * ChunksX + cx];
            return chunk == null ? (byte)0 :
                chunk[(y & 0x0F) << 8 | (z & 0x0F) << 4 | (x & 0x0F)];
        }
        
        public byte GetExtTile(int index) {
            ushort x, y, z;
            IntToPos(index, out x, out y, out z);
            
            int cx = x >> 4, cy = y >> 4, cz = z >> 4;
            byte[] chunk = CustomBlocks[(cy * ChunksZ + cz) * ChunksX + cx];
            return chunk == null ? (byte)0 :
                chunk[(y & 0x0F) << 8 | (z & 0x0F) << 4 | (x & 0x0F)];
        }
        
        public void SetTile(int b, byte type) {
            if (blocks == null || b < 0 || b >= blocks.Length) return;
            blocks[b] = type;
        }
        
        public void SetTile(ushort x, ushort y, ushort z, byte type) {
            int b = PosToInt(x, y, z);
            if (blocks == null || b < 0) return;
            blocks[b] = type;
        }
        
        public void SetExtTile(ushort x, ushort y, ushort z, byte extType) {
            int index = PosToInt(x, y, z);
            if (index < 0 || blocks == null) return;
            SetExtTileNoCheck(x, y, z, extType);
        }
        
        public void SetExtTileNoCheck(ushort x, ushort y, ushort z, byte extType) {
            int cx = x >> 4, cy = y >> 4, cz = z >> 4;
            int cIndex = (cy * ChunksZ + cz) * ChunksX + cx;
            byte[] chunk = CustomBlocks[cIndex];
            
            if (chunk == null) {
                chunk = new byte[16 * 16 * 16];
                CustomBlocks[cIndex] = chunk;
            }
            chunk[(y & 0x0F) << 8 | (z & 0x0F) << 4 | (x & 0x0F)] = extType;
        }
        
        public void RevertExtTileNoCheck(ushort x, ushort y, ushort z) {
            int cx = x >> 4, cy = y >> 4, cz = z >> 4;
            int cIndex = (cy * ChunksZ + cz) * ChunksX + cx;        
            byte[] chunk = CustomBlocks[cIndex];
            
            if (chunk == null) return;
            chunk[(y & 0x0F) << 8 | (z & 0x0F) << 4 | (x & 0x0F)] = 0;
        }
        
        public void SetTile(ushort x, ushort y, ushort z, byte type, Player p, byte extType = 0) {
            int b = PosToInt(x, y, z);
            if (blocks == null || b < 0) return;
            
            byte oldType = blocks[b];
            blocks[b] = type;
            byte oldExtType = 0;
            
            if (oldType == Block.custom_block) {
            	oldExtType = GetExtTile(x, y, z);
            	if (type != Block.custom_block)
            		RevertExtTileNoCheck(x, y, z);
            }
            if (type == Block.custom_block)
            	SetExtTileNoCheck(x, y, z, extType);
            if (p == null)
                return;    
            
            Level.BlockPos bP;
            bP.name = p.name;
            bP.TimePerformed = DateTime.Now;
            bP.index = b;
            bP.type = type;
            bP.extType = extType;
            bP.deleted = bP.type == 0;
            blockCache.Add(bP);
            
            Player.UndoPos Pos;
            Pos.x = x; Pos.y = y; Pos.z = z;
            Pos.mapName = this.name;
            Pos.type = oldType; Pos.extType = oldExtType;
            Pos.newtype = type; Pos.newExtType = extType;
            Pos.timePlaced = DateTime.Now;
            p.UndoBuffer.Add(Pos);
        }

        bool CheckTNTWarsChange(Player p, ushort x, ushort y, ushort z, ref byte type) {
            if (!p.PlayingTntWars) return true;
            if (!(type == Block.tnt || type == Block.bigtnt || type == Block.nuketnt || type == Block.smalltnt))
                return true;
            
            TntWarsGame game = TntWarsGame.GetTntWarsGame(p);
            if (game.InZone(x, y, z, true))
                return false;
            
            if (p.CurrentAmountOfTnt == game.TntPerPlayerAtATime) {
                Player.SendMessage(p, "TNT Wars: Maximum amount of TNT placed"); return false;
            }
            if (p.CurrentAmountOfTnt > game.TntPerPlayerAtATime) {
                Player.SendMessage(p, "TNT Wars: You have passed the maximum amount of TNT that can be placed!"); return false;
            }
            p.TntAtATime();
            type = Block.smalltnt;
            return true;
        }
        
        bool CheckZones(Player p, ushort x, ushort y, ushort z, byte b, ref bool AllowBuild, ref bool inZone, ref string Owners) {
            bool foundDel = false;
            if ((p.group.Permission < LevelPermission.Admin || p.ZoneCheck || p.zoneDel) && !Block.AllowBreak(b))
            {
                List<Zone> toDel = null;
                if (ZoneList.Count == 0)
                    AllowBuild = true;
                else
                {
                    for (int index = 0; index < ZoneList.Count; index++)
                    {
                        Zone zn = ZoneList[index];
                        if (x < zn.smallX || x > zn.bigX || y < zn.smallY || y > zn.bigY || z < zn.smallZ || z > zn.bigZ)
                            continue;
                        inZone = true;
                        if (p.zoneDel) {
                            if (zn.Owner.Length >= 3 && zn.Owner.StartsWith("grp")) {
                                string grpName = zn.Owner.Substring(3);
                                if (p.group.Permission < Group.Find(grpName).Permission)
                                    continue;
                            } else if (zn.Owner != "" && (zn.Owner.ToLower() != p.name.ToLower())) {
                                Group group = Group.findPlayerGroup(zn.Owner.ToLower());
                                if (p.group.Permission < group.Permission)
                                    continue;
                            }
                                
                            Database.executeQuery("DELETE FROM `Zone" + p.level.name + "` WHERE Owner='" +
                                                  zn.Owner + "' AND SmallX='" + zn.smallX + "' AND SMALLY='" +
                                                  zn.smallY + "' AND SMALLZ='" + zn.smallZ + "' AND BIGX='" +
                                                  zn.bigX + "' AND BIGY='" + zn.bigY + "' AND BIGZ='" + zn.bigZ +  "'");
                            if (toDel == null) toDel = new List<Zone>();
                            toDel.Add(zn);

                            Player.SendMessage(p, "Zone deleted for &b" + zn.Owner);
                            foundDel = true;
                        } else {
                            if (zn.Owner.Length >= 3 && zn.Owner.StartsWith("grp")) {
                                string grpName = zn.Owner.Substring(3);
                                if (Group.Find(grpName).Permission <= p.group.Permission && !p.ZoneCheck) {
                                    AllowBuild = true; break;
                                }
                                AllowBuild = false;
                                Owners += ", " + grpName;
                            } else {
                                if (zn.Owner.ToLower() == p.name.ToLower() && !p.ZoneCheck) {
                                    AllowBuild = true; break;
                                }
                                AllowBuild = false;
                                Owners += ", " + zn.Owner;
                            }
                        }
                    }
                }

                if (p.zoneDel) {
                    if (!foundDel) {
                        Player.SendMessage(p, "No zones found to delete.");
                    } else {
                        foreach (Zone Zn in toDel)
                            ZoneList.Remove(Zn);
                    }
                    p.zoneDel = false;
                    return false;
                }

                if (!AllowBuild || p.ZoneCheck) {
                    if (p.ZoneCheck || p.ZoneSpam.AddSeconds(2) <= DateTime.UtcNow) {
                        if (Owners != "")
                            Player.SendMessage(p, "This zone belongs to &b" + Owners.Remove(0, 2) + ".");
                        else
                            Player.SendMessage(p, "This zone belongs to no one.");
                        p.ZoneSpam = DateTime.UtcNow;
                    }
                    if (p.ZoneCheck && !p.staticCommands)
                        p.ZoneCheck = false;
                    return false;
                }
            }
            return true;
        }
        
        bool CheckRank(Player p, ushort x, ushort y, ushort z, bool AllowBuild, bool inZone) {
            if (p.group.Permission < permissionbuild && (!inZone || !AllowBuild)) {
                if (p.ZoneSpam.AddSeconds(2) <= DateTime.UtcNow) {
                    Player.SendMessage(p, "Must be at least " + PermissionToName(permissionbuild) + " to build here");
                    p.ZoneSpam = DateTime.UtcNow;
                }
                return false;
            }
            
            if (p.group.Permission > perbuildmax && (!inZone || !AllowBuild) &&
                !p.group.CanExecute(Command.all.Find("perbuildmax"))) {
                if (p.ZoneSpam.AddSeconds(2) <= DateTime.UtcNow) {
                    Player.SendMessage(p, "Your rank must be " + perbuildmax + " or lower to build here!");
                    p.ZoneSpam = DateTime.UtcNow;
                }
                return false;
            }
            return true;
        }
        
        public bool CheckAffectPermissions(Player p, ushort x, ushort y, ushort z, byte b, byte type, byte extType = 0) {
            if (!Block.AllowBreak(b) && !Block.canPlace(p, b) && !Block.BuildIn(b)) {
                return false;
            }
            if (!CheckTNTWarsChange(p, x, y, z, ref type))
                return false;
            
            string Owners = "";
            bool AllowBuild = true, inZone = false;
            if (!CheckZones(p, x, y, z, b, ref AllowBuild, ref inZone, ref Owners))
                return false;
            if (Owners == "" && !CheckRank(p, x, y, z, AllowBuild, inZone))
                return false;
            return true;
        }
        
        public void Blockchange(Player p, ushort x, ushort y, ushort z, byte type, byte extType = 0) {
            string errorLocation = "start";
        retry:
            try
            {
                if (x < 0 || y < 0 || z < 0) return;
                if (x >= Width || y >= Height || z >= Length) return;
                byte b = GetTile(x, y, z), extB = 0;
                if (b == Block.custom_block)
                	extB = GetExtTile(x, y, z);

                errorLocation = "Permission checking";
                if (!CheckAffectPermissions(p, x, y, z, b, type, extType)) {
                    p.RevertBlock(x, y, z); return;
                }

                if (b == Block.sponge && physics > 0 && type != Block.sponge)
                    PhysSpongeRemoved(PosToInt(x, y, z));
                if (b == Block.lava_sponge && physics > 0 && type != Block.lava_sponge)
                    PhysSpongeRemoved(PosToInt(x, y, z), true);

                errorLocation = "Undo buffer filling";
                Player.UndoPos Pos;
                Pos.x = x; Pos.y = y; Pos.z = z;
                Pos.mapName = name;
                Pos.type = b; Pos.extType = extB;
                Pos.newtype = type; Pos.newExtType = extType;
                Pos.timePlaced = DateTime.Now;
                p.UndoBuffer.Add(Pos);

                errorLocation = "Setting tile";
                p.loginBlocks++;
                p.overallBlocks++;
                SetTile(x, y, z, type);
                if (b == Block.custom_block && type != Block.custom_block)
                	RevertExtTileNoCheck(x, y, z);
                if (type == Block.custom_block)
                    SetExtTileNoCheck(x, y, z, extType);
                
                errorLocation = "Block sending";
                bool diffBlock = Block.Convert(b) != Block.Convert(type);
                if (!diffBlock && b == Block.custom_block)
                	diffBlock = extType != extB;
                if (diffBlock && !Instant)
                    Player.GlobalBlockchange(this, x, y, z, type, extType);

                errorLocation = "Growing grass";
                if (GetTile(x, (ushort)(y - 1), z) == Block.grass && GrassDestroy && !Block.LightPass(type)) {
                    Blockchange(p, x, (ushort)(y - 1), z, Block.dirt);
                }

                errorLocation = "Adding physics";
                if (p.PlayingTntWars && type == Block.smalltnt) AddCheck(PosToInt(x, y, z), "", false, p);
                if (physics > 0) if (Block.Physics(type)) AddCheck(PosToInt(x, y, z), "", false, p);

                changed = true;
                backedup = false;
            } catch (OutOfMemoryException) {
                Player.SendMessage(p, "Undo buffer too big! Cleared!");
                p.UndoBuffer.Clear();
                goto retry;
            } catch (Exception e) {
                Server.ErrorLog(e);
                Chat.GlobalMessageOps(p.name + " triggered a non-fatal error on " + name);
                Chat.GlobalMessageOps("Error location: " + errorLocation);
                Server.s.Log(p.name + " triggered a non-fatal error on " + name);
                Server.s.Log("Error location: " + errorLocation);
            }
        }
        
        public void Blockchange(int b, byte type, bool overRide = false, string extraInfo = "", byte extType = 0) { //Block change made by physics
            if (b < 0 || b >= blocks.Length || blocks == null) return;
            if (b >= blocks.Length) return;
            byte oldBlock = blocks[b];
            byte oldExtType = GetExtTile(b);
            try
            {
                if (!overRide)
                    if (Block.OPBlocks(oldBlock) || (Block.OPBlocks(type) && extraInfo != "")) return;

                if (b == Block.sponge && physics > 0 && type != Block.sponge)
                    PhysSpongeRemoved(b);

                if (b == Block.lava_sponge && physics > 0 && type != Block.lava_sponge)
                    PhysSpongeRemoved(b, true);

                UndoPos uP;
                uP.location = b;
                uP.newType = type; uP.newExtType = extType;
                uP.oldType = oldBlock; uP.oldExtType = oldExtType;
                uP.timePerformed = DateTime.Now;

                if (currentUndo > Server.physUndo) {
                    currentUndo = 0;
                    UndoBuffer[currentUndo] = uP;
                } else if (UndoBuffer.Count < Server.physUndo) {
                    currentUndo++;
                    UndoBuffer.Add(uP);
                } else {
                    currentUndo++;
                    UndoBuffer[currentUndo] = uP;
                }

                blocks[b] = type;
                if (type == Block.custom_block) {
                	ushort x, y, z;
                	IntToPos(b, out x, out y, out z);
                	SetExtTileNoCheck(x, y, z, extType);
                } else if (oldBlock == Block.custom_block) {
                	ushort x, y, z;
                	IntToPos(b, out x, out y, out z);
                	RevertExtTileNoCheck(x, y, z);
                }
                
                // Save bandwidth sending identical looking blocks, like air/op_air changes.
                bool diffBlock = Block.Convert(oldBlock) != Block.Convert(type);
                if (!diffBlock && oldBlock == Block.custom_block)
                	diffBlock = oldExtType != extType;
                if (diffBlock)    
                    Player.GlobalBlockchange(this, b, type, extType);
                
                if (physics > 0 && ((Block.Physics(type) || extraInfo != "")))
                    AddCheck(b, extraInfo);
            } catch {
                blocks[b] = type;
            }
        }
        
        public void Blockchange(ushort x, ushort y, ushort z, byte type, bool overRide = false, string extraInfo = "", byte extType = 0) {
            Blockchange(PosToInt(x, y, z), type, overRide, extraInfo, extType); //Block change made by physics
        }
        
        public void Blockchange(ushort x, ushort y, ushort z, byte type, byte extType) {
            Blockchange(PosToInt(x, y, z), type, false, "", extType); //Block change made by physics
        }

        public int PosToInt(ushort x, ushort y, ushort z) {
            if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Length)
                return -1;
            return x + (z * Width) + (y * Width * Length);
            //alternate method: (h * widthY + y) * widthX + x;
        }

        public void IntToPos(int pos, out ushort x, out ushort y, out ushort z) {
            y = (ushort)(pos / Width / Length);
            pos -= y * Width * Length;
            z = (ushort)(pos / Width);
            pos -= z * Width;
            x = (ushort)pos;
        }

        public int IntOffset(int pos, int x, int y, int z)  {
            return pos + x + z * Width + y * Width * Length;
        }
    }
}
