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
using SharpQuake.Rendering.UI.Menus;
using SharpQuake.Sys;
using System;

namespace SharpQuake.Rendering.UI
{
    public class KeysMenu : BaseMenu
    {
        private static readonly String[][] _BindNames = new String[][]
        {
            new String[] {"+attack",        "attack"},
            new String[] {"impulse 10",     "change weapon"},
            new String[] {"+jump",          "jump / swim up"},
            new String[] {"+forward",       "walk forward"},
            new String[] {"+back",          "backpedal"},
            new String[] {"+left",          "turn left"},
            new String[] {"+right",         "turn right"},
            new String[] {"+speed",         "run"},
            new String[] {"+moveleft",      "step left"},
            new String[] {"+moveright",     "step right"},
            new String[] {"+strafe",        "sidestep"},
            new String[] {"+lookup",        "look up"},
            new String[] {"+lookdown",      "look down"},
            new String[] {"centerview",     "center view"},
            new String[] {"+mlook",         "mouse look"},
            new String[] {"+klook",         "keyboard look"},
            new String[] {"+moveup",        "swim up"},
            new String[] {"+movedown",      "swim down"}
        };

        //const inte	NUMCOMMANDS	(sizeof(bindnames)/sizeof(bindnames[0]))

        private Boolean _BindGrab; // bind_grab

        private readonly snd _sound;
        private readonly CommandFactory _commands;
        private readonly PictureFactory _pictures;

        public KeysMenu( IKeyboardInput keyboard, MenuFactory menus, snd sound,
            CommandFactory commands, PictureFactory pictures ) : base( "menu_keys", keyboard, menus )
        {
            _commands = commands;
            _pictures = pictures;
            _sound = sound;
        }

        public override void Show( )
        {
            base.Show( );
        }

        public override void KeyEvent( Int32 key )
        {
            if ( _BindGrab )
            {
                // defining a key
                _sound.LocalSound( "misc/menu1.wav" );
                if ( key == KeysDef.K_ESCAPE )
                {
                    _BindGrab = false;
                }
                else if ( key != '`' )
                {
                    var cmd = String.Format( "bind \"{0}\" \"{1}\"\n", _keyboard.KeynumToString( key ), _BindNames[Cursor][0] );
                    _commands.Buffer.Insert( cmd );
                }

                _BindGrab = false;
                return;
            }

            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    _menus.Show( "menu_options" );
                    break;

                case KeysDef.K_LEFTARROW:
                case KeysDef.K_UPARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    Cursor--;
                    if ( Cursor < 0 )
                        Cursor = _BindNames.Length - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_RIGHTARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    Cursor++;
                    if ( Cursor >= _BindNames.Length )
                        Cursor = 0;
                    break;

                case KeysDef.K_ENTER:		// go into bind mode
                    var keys = new Int32[2];
                    FindKeysForCommand( _BindNames[Cursor][0], keys );
                    _sound.LocalSound( "misc/menu2.wav" );
                    if ( keys[1] != -1 )
                        UnbindCommand( _BindNames[Cursor][0] );
                    _BindGrab = true;
                    break;

                case KeysDef.K_BACKSPACE:		// delete bindings
                case KeysDef.K_DEL:				// delete bindings
                    _sound.LocalSound( "misc/menu2.wav" );
                    UnbindCommand( _BindNames[Cursor][0] );
                    break;
            }
        }

        private void DrawPlaque( MenuAdorner adorner )
        {
            var scale = _menus.UIScale;

            var p = _pictures.Cache( "gfx/ttl_cstm.lmp", "GL_NEAREST" );
            _menus.DrawPic( adorner.MidPointX - ( ( p.Width * scale ) / 2 ), 4 * scale, p, scale: scale );
        }

        public override void Draw( )
        {
            var adorner = _menus.BuildAdorner( _BindNames.Length, 0, 24 );

            //var p = _pictures.Cache( "gfx/ttl_cstm.lmp", "GL_NEAREST" );
            //_menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );
            DrawPlaque( adorner );
            //if ( _BindGrab )
            //    _menus.Print( 12, 32, "Press a key or button for this action" );
            //else
            //    _menus.Print( 18, 32, "Enter to change, backspace to clear" );
            if ( _BindGrab )
            {
                var w = _menus.Measure( "Press a key or button for this action" );
                _menus.Print( adorner.MidPointX - ( w / 2 ), ( Int32 ) ( adorner.LineY( 0 ) - ( adorner.LineHeight * 2 ) ), "Press a key or button for this action" );
            }
            else
            {
                var w = _menus.Measure( "Enter to change, backspace to clear" );
                _menus.Print( adorner.MidPointX - ( w / 2 ), ( Int32 ) ( adorner.LineY( 0 ) - ( adorner.LineHeight * 2 ) ), "Enter to change, backspace to clear" );
            }

            // search for known bindings
            var keys = new Int32[2];

            for ( var i = 0; i < _BindNames.Length; i++ )
            {
                var y = 48 + 8 * i;

                //_menus.Print( 16, y, _BindNames[i][1] );

                FindKeysForCommand( _BindNames[i][0], keys );

                var val = "";

                if ( keys[0] == -1 )
                {
                    val = "???";
                    //_menus.Print( 140, y, "???" );
                }
                else
                {
                    var name = _keyboard.KeynumToString( keys[0] );
                    //_menus.Print( 140, y, name );
                    val = name;

                    var x = name.Length * 8;

                    if ( keys[1] != -1 )
                    {
                        val += "^9 or ^0";
                        val += _keyboard.KeynumToString( keys[1] );

                        //_menus.Print( 140 + x + 8, y, "or" );
                        //_menus.Print( 140 + x + 32, y, _keyboard.KeynumToString( keys[1] ) );
                    }
                }

                adorner.PrintValue( _BindNames[i][1], val );
            }

            //if ( _BindGrab )
            //    _menus.DrawCharacter( 130, 48 + Cursor * 8, '=' );
            //else
            //    _menus.DrawCharacter( 130, 48 + Cursor * 8, 12 + ( ( Int32 ) ( Time.Absolute * 4 ) & 1 ) );
            if ( _BindGrab )
                _menus.DrawCharacter( adorner.MidPointX, adorner.LineY( Cursor + 1 ), '=' );
            else
                _menus.DrawCharacter( adorner.MidPointX, adorner.LineY( Cursor + 1 ), 12 + ( ( Int32 ) ( Time.Absolute * 4 ) & 1 ) );
        }

        /// <summary>
        /// M_FindKeysForCommand
        /// </summary>
        private void FindKeysForCommand( String command, Int32[] twokeys )
        {
            twokeys[0] = twokeys[1] = -1;
            var len = command.Length;
            var count = 0;

            for ( var j = 0; j < 256; j++ )
            {
                var b = _keyboard.Bindings[j];
                if ( String.IsNullOrEmpty( b ) )
                    continue;

                if ( String.Compare( b, 0, command, 0, len ) == 0 )
                {
                    twokeys[count] = j;
                    count++;
                    if ( count == 2 )
                        break;
                }
            }
        }

        /// <summary>
        /// M_UnbindCommand
        /// </summary>
        private void UnbindCommand( String command )
        {
            var len = command.Length;

            for ( var j = 0; j < 256; j++ )
            {
                var b = _keyboard.Bindings[j];
                if ( String.IsNullOrEmpty( b ) )
                    continue;

                if ( String.Compare( b, 0, command, 0, len ) == 0 )
                    _keyboard.SetBinding( j, String.Empty );
            }
        }
    }

}
