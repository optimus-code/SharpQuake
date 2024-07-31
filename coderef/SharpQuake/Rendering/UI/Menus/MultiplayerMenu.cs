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
using SharpQuake.Rendering.UI.Menus;
using SharpQuake.Sys;

namespace SharpQuake.Rendering.UI
{
    public class MultiplayerMenu : BaseMenu
    {
        private const Int32 MULTIPLAYER_ITEMS = 3;

        private readonly Network _network;
        private readonly snd _sound;
        private readonly PictureFactory _pictures;

        public MultiplayerMenu( IKeyboardInput keyboard, MenuFactory menus, Network network,
            snd sound, PictureFactory pictures ) : base( "menu_multiplayer", keyboard, menus )
        {
            _network = network;
            _sound = sound;
            _pictures = pictures;
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    _menus.Show( "menu_main" );
                    break;

                case KeysDef.K_DOWNARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    if ( ++Cursor >= MULTIPLAYER_ITEMS )
                        Cursor = 0;
                    break;

                case KeysDef.K_UPARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    if ( --Cursor < 0 )
                        Cursor = MULTIPLAYER_ITEMS - 1;
                    break;

                case KeysDef.K_ENTER:
                    _menus.EnterSound = true;
                    switch ( Cursor )
                    {
                        case 0:
                            if ( _network.TcpIpAvailable )
                                _menus.Show( "menu_lan_config" );
                            break;

                        case 1:
                            if ( _network.TcpIpAvailable )
                                _menus.Show( "menu_lan_config" );
                            break;

                        case 2:
                            _menus.Show( "menu_setup" );
                            break;
                    }
                    break;
            }
        }

        static String[] OPTIONS = new[]
        {
            "Join a Game",
            "New Game",
            "Setup"
        };

        private void DrawPlaque( MenuAdorner adorner )
        {
            var scale = _menus.UIScale;

            //_menus.DrawTransPic( 16 * scale, 4 * scale, _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" ), scale: scale );
            var logoPic = _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" );
            _menus.DrawTransPic( adorner.LeftPoint - ( logoPic.Width * _menus.UIScale ), adorner.MidPointY - ( ( logoPic.Height * scale ) / 2 ), logoPic, scale: scale );

            var p = _pictures.Cache( "gfx/p_multi.lmp", "GL_NEAREST" );
            _menus.DrawPic( adorner.MidPointX - ( ( p.Width * scale ) / 2 ), 4 * scale, p, scale: scale );
        }

        public override void Draw( )
        {
            var scale = _menus.UIScale;
            var adorner = _menus.BuildAdorner( OPTIONS.Length + ( _network.TcpIpAvailable ? 0 : 1 ), 0, 0, width: 152, isBigFont: true );

            DrawPlaque( adorner );

            var newUI = Cvars.NewUI.Get<Boolean>( );

            if ( newUI )
            {
                for ( var i = 0; i < OPTIONS.Length; i++ )
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
                var options = _pictures.Cache( "gfx/mp_menu.lmp", "GL_NEAREST" );
                var optionsY = adorner.MidPointY - ( ( options.Height * scale ) / 2 );
                var optionsX = adorner.MidPointX - ( ( options.Width * scale ) / 2 );

                _menus.DrawTransPic( optionsX, optionsY, options, scale: scale );

                var f = ( Int32 ) ( Time._Time * 10 ) % 6;
                //_menus.DrawTransPic( 54 * scale, ( 32 + Cursor * 20 ) * scale, _pictures.Cache( String.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" ), scale: scale );

                var cursorPic = _pictures.Cache( String.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" );

                _menus.DrawTransPic( adorner.LeftPoint + ( ( cursorPic.Width * scale ) / 2 ), optionsY + ( 20 * Cursor * scale ), cursorPic, scale: scale );
            }

            //_menus.DrawTransPic( 16, 4, _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            //var p = _pictures.Cache( "gfx/p_multi.lmp", "GL_NEAREST" );
            //_menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );
            //_menus.DrawTransPic( 72, 32, _pictures.Cache( "gfx/mp_menu.lmp", "GL_NEAREST" ) );

           // Single f = ( Int32 ) ( Time._Time * 10 ) % 6;

            //_menus.DrawTransPic( 54, 32 + Cursor * 20, _pictures.Cache( String.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" ) );

            if ( _network.TcpIpAvailable )
                return;

            adorner.PrintWhite( "No Communications Available" );
            //_menus.PrintWhite( ( 320 / 2 ) - ( ( 27 * 8 ) / 2 ), 148, "No Communications Available" );
        }
    }
}
