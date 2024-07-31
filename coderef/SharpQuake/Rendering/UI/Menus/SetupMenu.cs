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
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.WAD;
using SharpQuake.Sys;
using System;
using System.Runtime.InteropServices;

namespace SharpQuake.Rendering.UI
{
    public class SetupMenu : BaseMenu
    {
        private const Int32 NUM_SETUP_CMDS = 5;

        private readonly Int32[] CursorTable = new Int32[]
        {
            40, 56, 80, 104, 140
        }; // setupCursor_table

        private String _HostName; // setup_hostname[16]
        private String _MyName; // setup_myname[16]
        private Int32 _OldTop; // setup_oldtop
        private Int32 _OldBottom; // setup_oldbottom
        private Int32 _Top; // setup_top
        private Int32 _Bottom; // setup_bottom
        private Boolean hasPlayPixels;

        private readonly client _client;
        private readonly Network _network;
        private readonly snd _sound;
        private readonly CommandFactory _commands;
        private readonly ClientVariableFactory _cvars;
        private readonly PictureFactory _pictures;
        private readonly Drawer _drawer;

        public SetupMenu( IKeyboardInput keyboard, MenuFactory menus, client client, 
            Network network, snd sound, CommandFactory commands, ClientVariableFactory cvars, 
            PictureFactory pictures, Drawer drawer ) : base( "menu_setup", keyboard, menus )
        {
            _client = client;
            _network = network;
            _sound = sound;
            _commands = commands;
            _cvars = cvars;
            _pictures = pictures;
            _drawer = drawer;
        }

        /// <summary>
        /// M_Menu_Setup_f
        /// </summary>
        public override void Show( )
        {
            _MyName = _client.Name;
            _HostName = _network.HostName;
            _Top = _OldTop = ( ( Int32 ) _client.Color ) >> 4;
            _Bottom = _OldBottom = ( ( Int32 ) _client.Color ) & 15;

            base.Show( );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    _menus.Show( "menu_multiplayer" );
                    break;

                case KeysDef.K_UPARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    Cursor--;
                    if ( Cursor < 0 )
                        Cursor = NUM_SETUP_CMDS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    Cursor++;
                    if ( Cursor >= NUM_SETUP_CMDS )
                        Cursor = 0;
                    break;

                case KeysDef.K_LEFTARROW:
                    if ( Cursor < 2 )
                        return;
                    _sound.LocalSound( "misc/menu3.wav" );
                    if ( Cursor == 2 )
                        _Top = _Top - 1;
                    if ( Cursor == 3 )
                        _Bottom = _Bottom - 1;
                    break;

                case KeysDef.K_RIGHTARROW:
                    if ( Cursor < 2 )
                        return;
                    forward:
                    _sound.LocalSound( "misc/menu3.wav" );
                    if ( Cursor == 2 )
                        _Top = _Top + 1;
                    if ( Cursor == 3 )
                        _Bottom = _Bottom + 1;
                    break;

                case KeysDef.K_ENTER:
                    if ( Cursor == 0 || Cursor == 1 )
                        return;

                    if ( Cursor == 2 || Cursor == 3 )
                        goto forward;

                    // Cursor == 4 (OK)
                    if ( _MyName != _client.Name )
                        _commands.Buffer.Append( String.Format( "name \"{0}\"\n", _MyName ) );
                    if ( _network.HostName != _HostName )
                        _cvars.Set( "hostname", _HostName );
                    if ( _Top != _OldTop || _Bottom != _OldBottom )
                        _commands.Buffer.Append( String.Format( "color {0} {1}\n", _Top, _Bottom ) );
                    _menus.EnterSound = true;
                    _menus.Show( "menu_multiplayer" );
                    break;

                case KeysDef.K_BACKSPACE:
                    if ( Cursor == 0 )
                    {
                        if ( !String.IsNullOrEmpty( _HostName ) )
                            _HostName = _HostName.Substring( 0, _HostName.Length - 1 );// setup_hostname[strlen(setup_hostname) - 1] = 0;
                    }

                    if ( Cursor == 1 )
                    {
                        if ( !String.IsNullOrEmpty( _MyName ) )
                            _MyName = _MyName.Substring( 0, _MyName.Length - 1 );
                    }
                    break;

                default:
                    if ( key < 32 || key > 127 )
                        break;
                    if ( Cursor == 0 )
                    {
                        var l = _HostName.Length;
                        if ( l < 15 )
                        {
                            _HostName = _HostName + ( Char ) key;
                        }
                    }
                    if ( Cursor == 1 )
                    {
                        var l = _MyName.Length;
                        if ( l < 15 )
                        {
                            _MyName = _MyName + ( Char ) key;
                        }
                    }
                    break;
            }

            if ( _Top > 13 )
                _Top = 0;
            if ( _Top < 0 )
                _Top = 13;
            if ( _Bottom > 13 )
                _Bottom = 0;
            if ( _Bottom < 0 )
                _Bottom = 13;
        }

        private void DrawPlaque( )
        {
            _menus.DrawTransPic( 16, 4, _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            var p = _pictures.Cache( "gfx/p_multi.lmp", "GL_NEAREST" );
            _menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );
        }

        private void DrawHostName( )
        {
            _menus.Print( 64, 40, "Hostname" );
            _menus.DrawTextBox( 160, 32, 16, 1 );
            _menus.Print( 168, 40, _HostName );
        }

        private void DrawName( )
        {
            _menus.Print( 64, 56, "Your name" );
            _menus.DrawTextBox( 160, 48, 16, 1 );
            _menus.Print( 168, 56, _MyName );
        }

        private void DrawClothesColours( )
        {
            _menus.Print( 64, 80, "Shirt color" );
            _menus.Print( 64, 104, "Pants color" );
        }

        private void DrawAcceptButton( )
        {
            _menus.DrawTextBox( 64, 140 - 8, 14, 1 );
            _menus.Print( 72, 140, "Accept Changes" );
        }

        private void DrawBigBox( )
        {
            var p = _pictures.Cache( "gfx/bigbox.lmp", "GL_NEAREST" );
            _menus.DrawTransPic( 160, 64, p );
        }

        private void DrawPlayer( )
        {
            var p = _pictures.Cache( "gfx/menuplyr.lmp", "GL_NEAREST", true );

            if ( !hasPlayPixels && p != null )
            {
                // HACK HACK HACK --- we need to keep the bytes for
                // the translatable player picture just for the menu
                // configuration dialog

                var headerSize = Marshal.SizeOf( typeof( WadPicHeader ) );
                var data = FileSystem.LoadFile( p.Identifier );
                _drawer._MenuPlayerPixelWidth = p.Texture.Desc.Width;
                _drawer._MenuPlayerPixelHeight = p.Texture.Desc.Height;
                Buffer.BlockCopy( data, headerSize, _drawer._MenuPlayerPixels, 0, p.Texture.Desc.Width * p.Texture.Desc.Height );
                //memcpy (menuplyr_pixels, dat->data, dat->width*dat->height);

                hasPlayPixels = true;
            }

            _menus.BuildTranslationTable( _Top * 16, _Bottom * 16 );
            _menus.DrawTransPicTranslate( 172, 72, p );
        }

        private void DrawText( )
        {
            _menus.DrawCharacter( 56, CursorTable[Cursor], 12 + ( ( Int32 ) ( Time.Absolute * 4 ) & 1 ) );

            if ( Cursor == 0 )
                _menus.DrawCharacter( 168 + 8 * _HostName.Length, CursorTable[Cursor], 10 + ( ( Int32 ) ( Time.Absolute * 4 ) & 1 ) );

            if ( Cursor == 1 )
                _menus.DrawCharacter( 168 + 8 * _MyName.Length, CursorTable[Cursor], 10 + ( ( Int32 ) ( Time.Absolute * 4 ) & 1 ) );
        }

        public override void Draw( )
        {
            DrawPlaque( );
            DrawHostName( );
            DrawName( );
            DrawClothesColours( );
            DrawAcceptButton( );
            DrawBigBox( );
            DrawPlayer( );
            DrawText( );
        }
    }
}
