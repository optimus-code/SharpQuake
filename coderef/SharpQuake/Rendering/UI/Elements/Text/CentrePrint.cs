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
using SharpQuake.Framework.IO.Input;
using SharpQuake.Framework.Rendering.UI;
using SharpQuake.Networking.Client;
using SharpQuake.Sys;
using System;

namespace SharpQuake.Rendering.UI.Elements.Text
{
    public class CentrePrint : BaseUIElement, ITextRenderer, IResetableRenderer
    {
        private Int32 _CenterLines; // scr_center_lines
        private Int32 _EraseLines; // scr_erase_lines

        public Single CenterTimeOff;

        //int _EraseCenter; // scr_erase_center
        private Single _CenterTimeStart; // scr_centertime_start	// for slow victory printing

        // scr_centertime_off
        private String _CenterString; // char	scr_centerstring[1024]

        private readonly VideoState _videoState;
        private readonly Drawer _drawer;
        private readonly ClientState _clientState;
        private readonly IKeyboardInput _keyboard;

        public CentrePrint( VideoState videoState, Drawer drawer, ClientState clientState, IKeyboardInput keyboard )
        {
            _videoState = videoState;
            _drawer = drawer;
            _clientState = clientState;
            _keyboard = keyboard;
        }

        // SCR_CenterPrint
        //
        // Called for important messages that should stay in the center of the screen
        // for a few moments
        public void Enqueue( String str )
        {
            _CenterString = str;
            CenterTimeOff = Cvars.CenterTime.Get<Int32>( );
            _CenterTimeStart = ( Single ) _clientState.Data.time;

            // count the number of lines for centering
            _CenterLines = 1;
            foreach ( var c in _CenterString )
            {
                if ( c == '\n' )
                    _CenterLines++;
            }
        }

        // SCR_DrawCenterString
        private void DrawCenterString( )
        {
            Int32 remaining;

            // the finale prints the characters one at a time
            if ( _clientState.Data.intermission > 0 )
                remaining = ( Int32 ) ( Cvars.PrintSpeed.Get<Int32>( ) * ( _clientState.Data.time - _CenterTimeStart ) );
            else
                remaining = 9999;

            //var y = _drawer.CharacterAdvanceHeight() * 6;

            //if ( _CenterLines <= 4 )
            //    y = ( Int32 ) ( _videoState.Data.height * 0.35 );

            var lines = _CenterString.Split( '\n' );
            var y = ( _videoState.Data.height / 2 ) - ( _drawer.CharacterAdvanceHeight( ) * ( lines.Length + 3 ) );
            //var maxLineWidth = 0;

            //for ( var i = 0; i < lines.Length; i++ )
            //{
            //    var line = lines[i].TrimEnd( '\r' );
            //    var lineWidth = _drawer.MeasureString( line );

            //    if ( lineWidth > maxLineWidth )
            //        maxLineWidth = lineWidth;
            //}

            for ( var i = 0; i < lines.Length; i++ )
            {
                var line = lines[i].TrimEnd( '\r' );
                var lineWidth = _drawer.MeasureString( line );

                var startX = ( _videoState.Data.width - lineWidth ) / 2;
                var xAdvance = startX;

                for ( var j = 0; j < line.Length; j++ )
                {
                    _drawer.DrawCharacter( xAdvance, y, line[j] );

                    xAdvance += _drawer.CharacterAdvance() + _drawer.MeasureCharacter( line[j] );

                    if ( remaining-- <= 0 )
                        return;
                }
                y += _drawer.CharacterAdvanceHeight( );
            }
        }

        // SCR_CheckDrawCenterString
        private void CheckDrawCenterString( )
        {
            _videoState.ScreenCopyTop = true;

            if ( _CenterLines > _EraseLines )
                _EraseLines = _CenterLines;

            CenterTimeOff -= ( Single ) Time.Delta;

            if ( CenterTimeOff <= 0 && _clientState.Data.intermission == 0 )
                return;

            if ( _keyboard.Destination != KeyDestination.key_game )
                return;

            DrawCenterString( );
        }

        public override void Draw( )
        {
            base.Draw( );

            if ( !IsVisible || !HasInitialised )
                return;

            CheckDrawCenterString( );
        }

        public void Reset()
        {
            CenterTimeOff = 0;
        }
    }
}
