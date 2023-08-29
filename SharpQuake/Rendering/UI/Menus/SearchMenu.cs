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
using SharpQuake.Sys;
using System;

namespace SharpQuake.Rendering.UI
{
    public class SearchMenu : BaseMenu
    {
        private Boolean _SearchComplete;
        private Double _SearchCompleteTime;

        private readonly Network _network;
        private readonly PictureFactory _pictures;

        public SearchMenu( IKeyboardInput keyboard, MenuFactory menus, Network network,
            PictureFactory pictures ) : base( "menu_search", keyboard, menus )
        {
            _network = network;
            _pictures = pictures;
        }

        public override void Show( )
        {
            base.Show( );
            _network.SlistSilent = true;
            _network.SlistLocal = false;
            _SearchComplete = false;
            _network.Slist_f( null );
        }

        public override void KeyEvent( Int32 key )
        {
            // nothing to do
        }

        public override void Draw( )
        {
            var p = _pictures.Cache( "gfx/p_multi.lmp", "GL_NEAREST" );
            _menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );
            var x = ( 320 / 2 ) - ( ( 12 * 8 ) / 2 ) + 4;
            _menus.DrawTextBox( x - 8, 32, 12, 1 );
            _menus.Print( x, 40, "Searching..." );

            if ( _network.SlistInProgress )
            {
                _network.Poll( );
                return;
            }

            if ( !_SearchComplete )
            {
                _SearchComplete = true;
                _SearchCompleteTime = Time.Absolute;
            }

            if ( _network.HostCacheCount > 0 )
            {
                _menus.Show( "menu_server_list" );
                return;
            }

            _menus.PrintWhite( ( 320 / 2 ) - ( ( 22 * 8 ) / 2 ), 64, "No Quake servers found" );
            if ( ( Time.Absolute - _SearchCompleteTime ) < 3.0 )
                return;

            _menus.Show( "menu_lan_config" );
        }
    }
}
