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
using SharpQuake.Sys;
using System;

namespace SharpQuake.Rendering.UI
{
    public class ServerListMenu : BaseMenu
    {
        private Boolean _Sorted;

        private readonly Network _network;
        private readonly snd _sound;
        private readonly PictureFactory _pictures;
        private readonly CommandFactory _commands;

        public ServerListMenu( IKeyboardInput keyboard, MenuFactory menus, Network network,
            snd sound, PictureFactory pictures, CommandFactory commands ) : base( "menu_server_list", keyboard, menus )
        {
            _network = network;
            _sound = sound;
            _pictures = pictures;
            _commands = commands;
        }

        public override void Show( )
        {
            base.Show( );
            Cursor = 0;
            _menus.ReturnOnError = false;
            _menus.ReturnReason = String.Empty;
            _Sorted = false;
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    _menus.Show( "menu_lan_config" );
                    break;

                case KeysDef.K_SPACE:
                    _menus.Show( "menu_search" );
                    break;

                case KeysDef.K_UPARROW:
                case KeysDef.K_LEFTARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    Cursor--;
                    if ( Cursor < 0 )
                        Cursor = _network.HostCacheCount - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_RIGHTARROW:
                    _sound.LocalSound( "misc/menu1.wav" );
                    Cursor++;
                    if ( Cursor >= _network.HostCacheCount )
                        Cursor = 0;
                    break;

                case KeysDef.K_ENTER:
                    _sound.LocalSound( "misc/menu2.wav" );
                    _menus.ReturnMenu = this;
                    _menus.ReturnOnError = true;
                    _Sorted = false;
                    _menus.CurrentMenu.Hide( );
                    _commands.Buffer.Append( String.Format( "connect \"{0}\"\n", _network.HostCache[Cursor].cname ) );
                    break;

                default:
                    break;
            }
        }

        public override void Draw( )
        {
            if ( !_Sorted )
            {
                if ( _network.HostCacheCount > 1 )
                {
                    Comparison<hostcache_t> cmp = delegate ( hostcache_t a, hostcache_t b )
                    {
                        return String.Compare( a.cname, b.cname );
                    };

                    Array.Sort( _network.HostCache, cmp );
                }
                _Sorted = true;
            }

            var p = _pictures.Cache( "gfx/p_multi.lmp", "GL_NEAREST" );
            _menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );

            for ( var n = 0; n < _network.HostCacheCount; n++ )
            {
                var hc = _network.HostCache[n];
                String tmp;
                if ( hc.maxusers > 0 )
                    tmp = String.Format( "{0,-15} {1,-15} {2:D2}/{3:D2}\n", hc.name, hc.map, hc.users, hc.maxusers );
                else
                    tmp = String.Format( "{0,-15} {1,-15}\n", hc.name, hc.map );
                _menus.Print( 16, 32 + 8 * n, tmp );
            }

            _menus.DrawCharacter( 0, 32 + Cursor * 8, 12 + ( ( Int32 ) ( Time.Absolute * 4 ) & 1 ) );

            if ( !String.IsNullOrEmpty( _menus.ReturnReason ) )
                _menus.PrintWhite( 16, 148, _menus.ReturnReason );
        }
    }
}
