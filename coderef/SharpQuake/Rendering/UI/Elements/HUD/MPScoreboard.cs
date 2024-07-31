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

using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Networking.Client;
using System;

namespace SharpQuake.Rendering.UI.Elements.HUD
{
    public class MPScoreboard : BaseUIElement
    {
        public override Boolean ManualInitialisation
        {
            get
            {
                return true;
            }
        }

        private HudResources _resources;

        private readonly Scr _screen;
        private readonly Vid _video;
        private readonly Drawer _drawer;
        private readonly PictureFactory _pictures;
        private readonly ClientState _clientState;
        private readonly VideoState _videoState;

        public MPScoreboard( Scr screen, Vid video, Drawer drawer, PictureFactory pictures, 
            ClientState clientState, VideoState videoState )
        {
            _screen = screen;
            _video = video;
            _drawer = drawer;
            _pictures = pictures;
            _clientState = clientState;
            _videoState = videoState;
        }

        public override void Initialise( )
        {
            base.Initialise( );

            _resources = _screen.HudResources;

            HasInitialised = true;
        }

        public override void Draw( )
        {
            base.Draw( );

            if ( !IsVisible || !HasInitialised )
                return;

            _videoState.ScreenCopyEverything = true;
            _screen.FullUpdate = 0;

            DeathmatchOverlay( );
        }

        /// <summary>
        /// Sbar_DeathmatchOverlay
        /// </summary>
        private void DeathmatchOverlay( )
        {
            var pic = _pictures.Cache( "gfx/ranking.lmp", "GL_LINEAR" );
            _video.Device.Graphics.DrawPicture( pic, ( 320 - pic.Width ) / 2, 8 );

            // scores
            _resources.SortFrags( );

            // draw the text
            var l = _resources._ScoreBoardLines;

            var x = 80 + ( ( _videoState.Data.width - 320 ) >> 1 );
            var y = 40;
            for ( var i = 0; i < l; i++ )
            {
                var k = _resources._FragSort[i];
                var s = _clientState.Data.scores[k];
                if ( String.IsNullOrEmpty( s.name ) )
                    continue;

                // draw background
                var top = s.colors & 0xf0;
                var bottom = ( s.colors & 15 ) << 4;
                top = _resources.ColorForMap( top );
                bottom = _resources.ColorForMap( bottom );

                _video.Device.Graphics.FillUsingPalette( x, y, 40, 4, top );
                _video.Device.Graphics.FillUsingPalette( x, y + 4, 40, 4, bottom );

                // draw number
                var num = s.frags.ToString( ).PadLeft( 3 );

                _drawer.DrawCharacter( x + 8, y, num[0] );
                _drawer.DrawCharacter( x + 16, y, num[1] );
                _drawer.DrawCharacter( x + 24, y, num[2] );

                if ( k == _clientState.Data.viewentity - 1 )
                    _drawer.DrawCharacter( x - 8, y, 12 );

                // draw name
                _drawer.DrawString( x + 64, y, s.name );

                y += 10;
            }
        }
    }
}
