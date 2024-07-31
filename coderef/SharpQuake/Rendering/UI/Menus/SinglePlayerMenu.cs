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

using System;
using SharpQuake.Desktop;
using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;
using SharpQuake.Framework.Factories.IO;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Networking.Client;
using SharpQuake.Networking.Server;
using SharpQuake.Rendering.UI.Menus;
using SharpQuake.Sys;

namespace SharpQuake.Rendering.UI
{
    public class SinglePlayerMenu : BaseMenu
    {
        private const Int32 SINGLEPLAYER_ITEMS = 3;

        private Boolean ShowSaveOption
        {
            get
            {
                if ( !_serverState.Data.active )
                    return false;

                if ( _clientState.Data.intermission != 0 )
                    return false;

                if ( _serverState.StaticData.maxclients != 1 )
                    return false;

                return true;
            }
        }

        private readonly ClientState _clientState;
        private readonly ServerState _serverState;
        private readonly snd _sound;
        private readonly CommandFactory _commands;
        private readonly Scr _screen;
        private readonly PictureFactory _pictures;

        public SinglePlayerMenu( IKeyboardInput keyboard, MenuFactory menus, ServerState serverState, ClientState clientState,
            snd sound, CommandFactory commands, Scr screen, PictureFactory pictures ) : base( "menu_singleplayer", keyboard, menus )
        {
            _serverState = serverState;
            _clientState = clientState;
            _sound = sound;
            _commands = commands;
            _screen = screen;
            _pictures = pictures;
        }

        /// <summary>
        /// M_SinglePlayer_Key
        /// </summary>
        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    _menus.Show( "menu_main" );
                    break;

                case KeysDef.K_DOWNARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    if ( ++Cursor >= SINGLEPLAYER_ITEMS )
                        Cursor = 0;
                    break;

                case KeysDef.K_UPARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    if ( --Cursor < 0 )
                        Cursor = SINGLEPLAYER_ITEMS - 1;
                    break;

                case KeysDef.K_ENTER:
                    _menus.EnterSound = true;

                    switch ( Cursor )
                    {
                        case 0:

                            if ( _serverState.Data.active )
                            {
                                if ( !_screen.ModalMessage( "Are you sure you want to\nstart a new game?\n" ) )
                                    break;
                            }

                            _keyboard.Destination = KeyDestination.key_game;

                            if ( _serverState.Data.active )
                                _commands.Buffer.Append( "disconnect\n" );

                            _commands.Buffer.Append( "maxplayers 1\n" );
                            _commands.Buffer.Append( "map start\n" );
                            break;

                        case 1:
                            _menus.Show( "menu_load" );
                            break;

                        case 2:
                            _menus.Show( "menu_save" );
                            break;
                    }
                    break;
            }
        }

        private void DrawPlaque( MenuAdorner adorner )
        {
            var scale = _menus.UIScale;

            //_menus.DrawTransPic( 16 * scale, 4 * scale, _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" ), scale: scale );
            var logoPic = _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" );
            _menus.DrawTransPic( adorner.LeftPoint - ( logoPic.Width * _menus.UIScale ), adorner.MidPointY - ( ( logoPic.Height * scale ) / 2 ), logoPic, scale: scale );

            var p = _pictures.Cache( "gfx/ttl_sgl.lmp", "GL_NEAREST" );
            _menus.DrawPic( adorner.MidPointX - ( ( p.Width * scale ) / 2 ), 4 * scale, p, scale: scale );
        }

        static String[] OPTIONS = new[]
        {
            "New Game",
            "Load",
            "Save"
        };

        /// <summary>
        /// M_SinglePlayer_Draw
        /// </summary>
        public override void Draw( )
        {
            var scale = _menus.UIScale;

            var adorner = _menus.BuildAdorner( OPTIONS.Length - ( ShowSaveOption ? 0 : 1 ), 0, 0, width: 152, isBigFont: true );

            DrawPlaque( adorner );

            if ( Cvars.NewUI.Get<Boolean>( ) )
            {
                for ( var i = 0; i < OPTIONS.Length - ( ShowSaveOption ? 0 : 1 ); i++ )
                {
                    var text = OPTIONS[i];

                    if ( Cursor == i )
                        adorner.PrintWhite( text, TextAlignment.Centre );
                    else
                        adorner.Print( text, TextAlignment.Centre );
                }
            }
            else
            {
                var options = _pictures.Cache( "gfx/sp_menu.lmp", "GL_NEAREST" );
                var optionsY = adorner.MidPointY - ( ( options.Height * scale ) / 2 );
                var optionsX = adorner.MidPointX - ( ( options.Width * scale ) / 2 );

                _menus.DrawTransPic( optionsX, optionsY, options, scale: scale );

                var f = ( Int32 ) ( Time._Time * 10 ) % 6;
                //_menus.DrawTransPic( 54 * scale, ( 32 + Cursor * 20 ) * scale, _pictures.Cache( String.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" ), scale: scale );

                var cursorPic = _pictures.Cache( String.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" );

                _menus.DrawTransPic( adorner.LeftPoint + ( ( cursorPic.Width * scale ) / 2 ), optionsY + ( 20 * Cursor * scale ), cursorPic, scale: scale );
            }

            //_menus.DrawTransPic( 16, 4, _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            //var p = _pictures.Cache( "gfx/ttl_sgl.lmp", "GL_NEAREST" );
            //_menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );
            //_menus.DrawTransPic( 72, 32, _pictures.Cache( "gfx/sp_menu.lmp", "GL_NEAREST" ) );

            //var f = ( Int32 ) ( Time._Time * 10 ) % 6;

            //_menus.DrawTransPic( 54, 32 + Cursor * 20, _pictures.Cache( String.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" ) );
        }
    }
}
