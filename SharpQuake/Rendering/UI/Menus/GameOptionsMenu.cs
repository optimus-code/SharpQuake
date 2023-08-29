/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019-2023
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using SharpQuake.Desktop;
using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;
using SharpQuake.Framework.Factories.IO;
using SharpQuake.Networking.Server;
using SharpQuake.Sys;
using System;

namespace SharpQuake.Rendering.UI
{
    /// <summary>
    /// M_Menu_GameOptions_functions
    /// </summary>
    public class GameOptionsMenu : BaseMenu
    {
        private const Int32 NUM_GAMEOPTIONS = 9;

        private static readonly level_t[] Levels = new level_t[]
        {
            new level_t("start", "Entrance"),	// 0

	        new level_t("e1m1", "Slipgate Complex"),				// 1
	        new level_t("e1m2", "Castle of the Damned"),
            new level_t("e1m3", "The Necropolis"),
            new level_t("e1m4", "The Grisly Grotto"),
            new level_t("e1m5", "Gloom Keep"),
            new level_t("e1m6", "The Door To Chthon"),
            new level_t("e1m7", "The House of Chthon"),
            new level_t("e1m8", "Ziggurat Vertigo"),

            new level_t("e2m1", "The Installation"),				// 9
	        new level_t("e2m2", "Ogre Citadel"),
            new level_t("e2m3", "Crypt of Decay"),
            new level_t("e2m4", "The Ebon Fortress"),
            new level_t("e2m5", "The Wizard's Manse"),
            new level_t("e2m6", "The Dismal Oubliette"),
            new level_t("e2m7", "Underearth"),

            new level_t("e3m1", "Termination Central"),			// 16
	        new level_t("e3m2", "The Vaults of Zin"),
            new level_t("e3m3", "The Tomb of Terror"),
            new level_t("e3m4", "Satan's Dark Delight"),
            new level_t("e3m5", "Wind Tunnels"),
            new level_t("e3m6", "Chambers of Torment"),
            new level_t("e3m7", "The Haunted Halls"),

            new level_t("e4m1", "The Sewage System"),				// 23
	        new level_t("e4m2", "The Tower of Despair"),
            new level_t("e4m3", "The Elder God Shrine"),
            new level_t("e4m4", "The Palace of Hate"),
            new level_t("e4m5", "Hell's Atrium"),
            new level_t("e4m6", "The Pain Maze"),
            new level_t("e4m7", "Azure Agony"),
            new level_t("e4m8", "The Nameless City"),

            new level_t("end", "Shub-Niggurath's Pit"),			// 31

	        new level_t("dm1", "Place of Two Deaths"),				// 32
	        new level_t("dm2", "Claustrophobopolis"),
            new level_t("dm3", "The Abandoned Base"),
            new level_t("dm4", "The Bad Place"),
            new level_t("dm5", "The Cistern"),
            new level_t("dm6", "The Dark Zone")
        };

        //MED 01/06/97 added hipnotic levels
        private static readonly level_t[] HipnoticLevels = new level_t[]
        {
           new level_t("start", "Command HQ"),  // 0

           new level_t("hip1m1", "The Pumping Station"),          // 1
           new level_t("hip1m2", "Storage Facility"),
           new level_t("hip1m3", "The Lost Mine"),
           new level_t("hip1m4", "Research Facility"),
           new level_t("hip1m5", "Military Complex"),

           new level_t("hip2m1", "Ancient Realms"),          // 6
           new level_t("hip2m2", "The Black Cathedral"),
           new level_t("hip2m3", "The Catacombs"),
           new level_t("hip2m4", "The Crypt"),
           new level_t("hip2m5", "Mortum's Keep"),
           new level_t("hip2m6", "The Gremlin's Domain"),

           new level_t("hip3m1", "Tur Torment"),       // 12
           new level_t("hip3m2", "Pandemonium"),
           new level_t("hip3m3", "Limbo"),
           new level_t("hip3m4", "The Gauntlet"),

           new level_t("hipend", "Armagon's Lair"),       // 16

           new level_t("hipdm1", "The Edge of Oblivion")           // 17
        };

        //PGM 01/07/97 added rogue levels
        //PGM 03/02/97 added dmatch level
        private static readonly level_t[] RogueLevels = new level_t[]
        {
            new level_t("start", "Split Decision"),
            new level_t("r1m1", "Deviant's Domain"),
            new level_t("r1m2", "Dread Portal"),
            new level_t("r1m3", "Judgement Call"),
            new level_t("r1m4", "Cave of Death"),
            new level_t("r1m5", "Towers of Wrath"),
            new level_t("r1m6", "Temple of Pain"),
            new level_t("r1m7", "Tomb of the Overlord"),
            new level_t("r2m1", "Tempus Fugit"),
            new level_t("r2m2", "Elemental Fury I"),
            new level_t("r2m3", "Elemental Fury II"),
            new level_t("r2m4", "Curse of Osiris"),
            new level_t("r2m5", "Wizard's Keep"),
            new level_t("r2m6", "Blood Sacrifice"),
            new level_t("r2m7", "Last Bastion"),
            new level_t("r2m8", "Source of Evil"),
            new level_t("ctf1", "Division of Change")
        };

        private static readonly episode_t[] Episodes = new episode_t[]
        {
            new episode_t("Welcome to Quake", 0, 1),
            new episode_t("Doomed Dimension", 1, 8),
            new episode_t("Realm of Black Magic", 9, 7),
            new episode_t("Netherworld", 16, 7),
            new episode_t("The Elder World", 23, 8),
            new episode_t("Final Level", 31, 1),
            new episode_t("Deathmatch Arena", 32, 6)
        };

        //MED 01/06/97  added hipnotic episodes
        private static readonly episode_t[] HipnoticEpisodes = new episode_t[]
        {
           new episode_t("Scourge of Armagon", 0, 1),
           new episode_t("Fortress of the Dead", 1, 5),
           new episode_t("Dominion of Darkness", 6, 6),
           new episode_t("The Rift", 12, 4),
           new episode_t("Final Level", 16, 1),
           new episode_t("Deathmatch Arena", 17, 1)
        };

        //PGM 01/07/97 added rogue episodes
        //PGM 03/02/97 added dmatch episode
        private static readonly episode_t[] RogueEpisodes = new episode_t[]
        {
            new episode_t("Introduction", 0, 1),
            new episode_t("Hell's Fortress", 1, 7),
            new episode_t("Corridors of Time", 8, 8),
            new episode_t("Deathmatch Arena", 16, 1)
        };

        private static readonly Int32[] CursorTable = new Int32[]
        {
            40, 56, 64, 72, 80, 88, 96, 112, 120
        };

        private Int32 _StartEpisode;

        private Int32 _StartLevel;

        private Int32 _MaxPlayers;

        private Boolean _ServerInfoMessage;

        private Double _ServerInfoMessageTime;

        private readonly ServerState _serverState;
        private readonly snd _sound;
        private readonly ClientVariableFactory _cvars;
        private readonly CommandFactory _commands;
        private readonly PictureFactory _pictures;
        private readonly Scr _screen;

        public GameOptionsMenu( IKeyboardInput keyboard, MenuFactory menus, snd sound, ClientVariableFactory cvars,
            CommandFactory commands, PictureFactory pictures, Scr screen, ServerState serverState ) : base( "menu_game_options", keyboard, menus )
        {
            _sound = sound;
            _cvars = cvars;
            _commands = commands;
            _pictures = pictures;
            _screen = screen;
            _serverState = serverState;
        }

        public override void Show( )
        {
            base.Show( );
            
            // TODO - Review the below logic - it's yucky and weird
            if ( _MaxPlayers == 0 )
                _MaxPlayers = _serverState.StaticData.maxclients;

            if ( _MaxPlayers < 2 )
                _MaxPlayers = _serverState.StaticData.maxclientslimit;
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    _menus.Show( "menu_lan_config" );
                    break;

                case KeysDef.K_UPARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    Cursor--;
                    if ( Cursor < 0 )
                        Cursor = NUM_GAMEOPTIONS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    Cursor++;
                    if ( Cursor >= NUM_GAMEOPTIONS )
                        Cursor = 0;
                    break;

                case KeysDef.K_LEFTARROW:
                    if ( Cursor == 0 )
                        break;
                    _sound.LocalSound( "misc/menu3.wav" );
                    Change( -1 );
                    break;

                case KeysDef.K_RIGHTARROW:
                    if ( Cursor == 0 )
                        break;
                    _sound.LocalSound( "misc/menu3.wav" );
                    Change( 1 );
                    break;

                case KeysDef.K_ENTER:
                    _sound.LocalSound( "misc/menu2.wav" );
                    if ( Cursor == 0 )
                    {
                        if ( _serverState.IsActive )
                            _commands.Buffer.Append( "disconnect\n" );
                        _commands.Buffer.Append( "listen 0\n" );	// so host_netport will be re-examined
                        _commands.Buffer.Append( String.Format( "maxplayers {0}\n", _MaxPlayers ) );
                        _screen.BeginLoadingPlaque( );

                        if ( Engine.Common.GameKind == GameKind.Hipnotic )
                            _commands.Buffer.Append( String.Format( "map {0}\n",
                                HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].name ) );
                        else if ( Engine.Common.GameKind == GameKind.Rogue )
                            _commands.Buffer.Append( String.Format( "map {0}\n",
                                RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].name ) );
                        else
                            _commands.Buffer.Append( String.Format( "map {0}\n", Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].name ) );

                        return;
                    }

                    Change( 1 );
                    break;
            }
        }

        private void DrawPlaque( )
        {
            var scale = _menus.UIScale;

            _menus.DrawTransPic( 16 * scale, 4 * scale, _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" ), scale: scale );
            var p = _pictures.Cache( "gfx/p_multi.lmp", "GL_NEAREST" );
            _menus.DrawPic( ( ( 320 * scale ) - ( p.Width * scale ) ) / 2, 4 * scale, p, scale: scale );
        }

        private void DrawBeginGame( )
        {
            var scale = _menus.UIScale;
            _menus.DrawTextBox( 152 * scale, 32 * scale, 10 * scale, 1 );
            _menus.Print( 160 * scale, 40 * scale, "begin game" );
        }

        private void DrawMaxPlayers( )
        {
            var scale = _menus.UIScale;
            _menus.Print( 0, 56 * scale, "      Max players" );
            _menus.Print( 160 * scale, 56 * scale, _MaxPlayers.ToString( ) );
        }

        private void DrawGameType( )
        {
            var scale = _menus.UIScale;

            _menus.Print( 0, 64 * scale, "        Game Type" );
            if ( Cvars.Coop.Get<Boolean>( ) )
                _menus.Print( 160 * scale, 64 * scale, "Cooperative" );
            else
                _menus.Print( 160 * scale, 64 * scale, "Deathmatch" );
        }

        private void DrawTeamPlay( )
        {
            var scale = _menus.UIScale;

            _menus.Print( 0, 72 * scale, "        Teamplay" );
            if ( Engine.Common.GameKind == GameKind.Rogue )
            {
                String msg;
                switch ( Cvars.TeamPlay.Get<Int32>( ) )
                {
                    case 1:
                        msg = "No Friendly Fire";
                        break;

                    case 2:
                        msg = "Friendly Fire";
                        break;

                    case 3:
                        msg = "Tag";
                        break;

                    case 4:
                        msg = "Capture the Flag";
                        break;

                    case 5:
                        msg = "One Flag CTF";
                        break;

                    case 6:
                        msg = "Three Team CTF";
                        break;

                    default:
                        msg = "Off";
                        break;
                }
                _menus.Print( 160 * scale, 72 * scale, msg );
            }
            else
            {
                String msg;
                switch ( Cvars.TeamPlay.Get<Int32>( ) )
                {
                    case 1:
                        msg = "No Friendly Fire";
                        break;

                    case 2:
                        msg = "Friendly Fire";
                        break;

                    default:
                        msg = "Off";
                        break;
                }
                _menus.Print( 160 * scale, 72 * scale, msg );
            }
        }

        private void DrawSkill( )
        {
            var scale = _menus.UIScale;

            _menus.Print( 0, 80 * scale, "            Skill" );
            if ( Cvars.Skill.Get<Int32>( ) == 0 )
                _menus.Print( 160 * scale, 80 * scale, "Easy difficulty" );
            else if ( Cvars.Skill.Get<Int32>( ) == 1 )
                _menus.Print( 160 * scale, 80 * scale, "Normal difficulty" );
            else if ( Cvars.Skill.Get<Int32>( ) == 2 )
                _menus.Print( 160 * scale, 80 * scale, "Hard difficulty" );
            else
                _menus.Print( 160 * scale, 80 * scale, "Nightmare difficulty" );
        }

        private void DrawFragLimit( )
        {
            var scale = _menus.UIScale;

            _menus.Print( 0, 88 * scale, "       Frag Limit" );
            if ( Cvars.FragLimit.Get<Int32>( ) == 0 )
                _menus.Print( 160 * scale, 88 * scale, "none" );
            else
                _menus.Print( 160 * scale, 88 * scale, String.Format( "{0} frags", Cvars.FragLimit.Get<Int32>( ) ) );
        }

        private void DrawTimeLimit( )
        {
            var scale = _menus.UIScale;

            _menus.Print( 0, 96 * scale, "       Time Limit" );
            if ( Cvars.TimeLimit.Get<Int32>( ) == 0 )
                _menus.Print( 160 * scale, 96 * scale, "none" );
            else
                _menus.Print( 160 * scale, 96 * scale, String.Format( "{0} minutes", Cvars.TimeLimit.Get<Int32>( ) ) );
        }

        private void DrawEpisode( )
        {
            var scale = _menus.UIScale;

            _menus.Print( 0, 112 * scale, "         Episode" );
            //MED 01/06/97 added hipnotic episodes
            if ( Engine.Common.GameKind == GameKind.Hipnotic )
                _menus.Print( 160 * scale, 112 * scale, HipnoticEpisodes[_StartEpisode].description );
            //PGM 01/07/97 added rogue episodes
            else if ( Engine.Common.GameKind == GameKind.Rogue )
                _menus.Print( 160 * scale, 112 * scale, RogueEpisodes[_StartEpisode].description );
            else
                _menus.Print( 160 * scale, 112 * scale, Episodes[_StartEpisode].description );
        }

        private void DrawLevel( )
        {
            var scale = _menus.UIScale;

            _menus.Print( 0, 120 * scale, "           Level" );
            //MED 01/06/97 added hipnotic episodes
            if ( Engine.Common.GameKind == GameKind.Hipnotic )
            {
                _menus.Print( 160 * scale, 120 * scale, HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].description );
                _menus.Print( 160 * scale, 128 * scale, HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].name );
            }
            //PGM 01/07/97 added rogue episodes
            else if ( Engine.Common.GameKind == GameKind.Rogue )
            {
                _menus.Print( 160 * scale, 120 * scale, RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].description );
                _menus.Print( 160 * scale, 128 * scale, RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].name );
            }
            else
            {
                _menus.Print( 160 * scale, 120 * scale, Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].description );
                _menus.Print( 160 * scale, 128 * scale, Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].name );
            }
        }

        private void DrawServerInfoMessage( )
        {
            var scale = _menus.UIScale;
            if ( _ServerInfoMessage )
            {
                if ( ( Time.Absolute - _ServerInfoMessageTime ) < 5.0 )
                {
                    var x = ( ( 320 * scale ) - ( ( 26 * 8 ) * scale ) ) / 2;
                    _menus.DrawTextBox( x, 138 * scale, 24 * scale, 4 );
                    x += ( 8 * scale );
                    _menus.Print( x, 146 * scale, "  More than 4 players   " );
                    _menus.Print( x, 154 * scale, " requires using command " );
                    _menus.Print( x, 162 * scale, "line parameters; please " );
                    _menus.Print( x, 170 * scale, "   see techinfo.txt.    " );
                }
                else
                {
                    _ServerInfoMessage = false;
                }
            }
        }

        public override void Draw( )
        {
            DrawPlaque( );
            DrawBeginGame( );
            DrawMaxPlayers( );
            DrawGameType( );
            DrawTeamPlay( );
            DrawSkill( );
            DrawFragLimit( );
            DrawTimeLimit( );
            DrawEpisode( );
            DrawLevel( );

            var scale = _menus.UIScale;
            // line cursor
            _menus.DrawCharacter( 144 * scale, CursorTable[Cursor], ( 12 + ( ( Int32 ) ( Time.Absolute * 4 ) & 1 ) ) * scale, scale: scale );

            DrawServerInfoMessage( );
        }

        private class level_t
        {
            public String name;
            public String description;

            public level_t( String name, String desc )
            {
                this.name = name;
                description = desc;
            }
        } //level_t;

        private class episode_t
        {
            public String description;
            public Int32 firstLevel;
            public Int32 levels;

            public episode_t( String desc, Int32 firstLevel, Int32 levels )
            {
                description = desc;
                this.firstLevel = firstLevel;
                this.levels = levels;
            }
        } //episode_t;

        /// <summary>
        /// M_NetStart_Change
        /// </summary>
        private void Change( Int32 dir )
        {
            Int32 count;

            switch ( Cursor )
            {
                case 1:
                    _MaxPlayers += dir;
                    if ( _MaxPlayers > _serverState.StaticData.maxclientslimit )
                    {
                        _MaxPlayers = _serverState.StaticData.maxclientslimit;
                        _ServerInfoMessage = true;
                        _ServerInfoMessageTime = Time.Absolute;
                    }
                    if ( _MaxPlayers < 2 )
                        _MaxPlayers = 2;
                    break;

                case 2:
                    _cvars.Set( "coop", Cvars.Coop.Get<Boolean>( ) );
                    break;

                case 3:
                    if ( Engine.Common.GameKind == GameKind.Rogue )
                        count = 6;
                    else
                        count = 2;

                    var tp = Cvars.TeamPlay.Get<Int32>( ) + dir;

                    if ( tp > count )
                        tp = 0;
                    else if ( tp < 0 )
                        tp = count;

                    _cvars.Set( "teamplay", tp );
                    break;

                case 4:
                    var skill = Cvars.Skill.Get<Int32>( ) + dir;
                    if ( skill > 3 )
                        skill = 0;
                    if ( skill < 0 )
                        skill = 3;
                    _cvars.Set( "skill", skill );
                    break;

                case 5:
                    var fraglimit = Cvars.FragLimit.Get<Int32>( ) + dir * 10;
                    if ( fraglimit > 100 )
                        fraglimit = 0;
                    if ( fraglimit < 0 )
                        fraglimit = 100;
                    _cvars.Set( "fraglimit", fraglimit );
                    break;

                case 6:
                    var timelimit = Cvars.TimeLimit.Get<Int32>( ) + dir * 5;
                    if ( timelimit > 60 )
                        timelimit = 0;
                    if ( timelimit < 0 )
                        timelimit = 60;
                    _cvars.Set( "timelimit", timelimit );
                    break;

                case 7:
                    _StartEpisode += dir;
                    //MED 01/06/97 added hipnotic count
                    if ( Engine.Common.GameKind == GameKind.Hipnotic )
                        count = 6;
                    //PGM 01/07/97 added rogue count
                    //PGM 03/02/97 added 1 for dmatch episode
                    else if ( Engine.Common.GameKind == GameKind.Rogue )
                        count = 4;
                    else if ( Engine.Common.IsRegistered )
                        count = 7;
                    else
                        count = 2;

                    if ( _StartEpisode < 0 )
                        _StartEpisode = count - 1;

                    if ( _StartEpisode >= count )
                        _StartEpisode = 0;

                    _StartLevel = 0;
                    break;

                case 8:
                    _StartLevel += dir;
                    //MED 01/06/97 added hipnotic episodes
                    if ( Engine.Common.GameKind == GameKind.Hipnotic )
                        count = HipnoticEpisodes[_StartEpisode].levels;
                    //PGM 01/06/97 added hipnotic episodes
                    else if ( Engine.Common.GameKind == GameKind.Rogue )
                        count = RogueEpisodes[_StartEpisode].levels;
                    else
                        count = Episodes[_StartEpisode].levels;

                    if ( _StartLevel < 0 )
                        _StartLevel = count - 1;

                    if ( _StartLevel >= count )
                        _StartLevel = 0;
                    break;
            }
        }
    }

}
