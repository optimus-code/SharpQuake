
using SharpQuake.Factories.Rendering.UI;
using System;
using System.Collections.Generic;
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
namespace SharpQuake.Rendering.UI.Menus
{
    public class MenuAdorner
    {
        private Int32 Lines
        {
            get;
            set;
        }

        public Int32 TopPoint
        {
            get
            {
                return MidPointY - ( TotalHeight / 2 );
            }
        }

        public Int32 LeftPoint
        {
            get
            {
                return MidPointX - _width;
            }
        }

        public Int32 RightPoint
        {
            get
            {
                return MidPointX + _width;
            }
        }

        public Int32 MidPointX
        {
            get
            {
                return ( ScreenWidth / 2 );
            }
        }

        public Int32 MidPointY
        {
            get
            {
                return ( ScreenHeight / 2 );
            }
        }

        public Int32 LineHeight
        {
            get
            {
                return _drawer.CharacterAdvanceHeight( isBigFont: IsBigFont ) + 6;
            }
        }

        public Int32 CurrentY
        {
            get
            {
                return _y + TopPoint + ( LineHeight * Lines );// + ( LineHeight * Lines ) + _padding;  // _y + _padding + ( LineHeight * Lines );
            }
        }

        public Int32 TotalHeight
        {
            get
            {
                return ( LineHeight * _totalLines ) + ( _padding * 2 );
            }
        }

        private Int32 ScreenWidth
        {
            get
            {
                return _videoState.Data.width;
            }
        }

        private Int32 ScreenHeight
        {
            get
            {
                return _videoState.Data.height;
            }
        }

        public Int32 Padding
        {
            get
            {
                return _padding;
            }
        }

        public Int32 Width
        {
            get
            {
                return _width;
            }
        }

        private Boolean IsBigFont
        {
            get;
            set;
        }

        private HashSet<Int32> BlankLines
        {
            get;
            set;
        } = new HashSet<Int32>( );

        private readonly MenuFactory _menus;
        private readonly Drawer _drawer;
        private readonly VideoState _videoState;
        private readonly Int32 _x;
        private readonly Int32 _y;
        private readonly Int32 _width;
        private readonly Int32 _padding;
        private readonly Int32 _totalLines;

        public MenuAdorner( MenuFactory menus, Drawer drawer, VideoState videoState, Int32 totalLines, Int32 x, Int32 y, Int32 width, Int32 padding, Boolean isBigFont )
        {
            _menus = menus;
            _drawer = drawer;
            _videoState = videoState;
            _totalLines = totalLines;
            _x = x * _menus.UIScale;
            _y = y * _menus.UIScale;
            _padding = padding * _menus.UIScale;
            _width = width * _menus.UIScale;
            IsBigFont = isBigFont;
        }

        public Int32 LineY( Int32 lineNumber )
        {
            var offset = 0;

            if ( BlankLines.Count > 0 )
            {
                foreach ( var blankLine in BlankLines )
                {
                    if ( lineNumber >= blankLine )
                        offset += 1;
                }
            }
            return _y + TopPoint + ( LineHeight * ( lineNumber + offset ) );// _y + _padding + ( LineHeight * lineNumber );
        }

        public void Label( String text, TextAlignment alignment = TextAlignment.Centre )
        {
            var baseX = LeftPoint;
            var x = baseX + _padding;
            var textWidth = _drawer.MeasureString( text, isBigFont: IsBigFont );

            if ( alignment == TextAlignment.Right )
                x = RightPoint - textWidth - ( _padding / 2 );
            else if ( alignment == TextAlignment.Centre )
                x = MidPointX - ( textWidth / 2 );

            _menus.Print( x, CurrentY, text, scale: _menus.UIScale, isBigFont: IsBigFont );

            BlankLines.Add( Lines );
            Lines++;
        }

        public void Print( String text, TextAlignment alignment = TextAlignment.Right )
        {
            var baseX = LeftPoint;
            var x = baseX + _padding;
            var textWidth = _drawer.MeasureString( text, isBigFont: IsBigFont );

            if ( alignment == TextAlignment.Right )
                x = RightPoint - textWidth - ( _padding / 2 );
            else if ( alignment == TextAlignment.Centre )
                x = MidPointX - ( textWidth / 2 );

            _menus.Print( x, CurrentY, text, scale: _menus.UIScale, isBigFont: IsBigFont );

           // _menus.Print( 1280, 0, "A", scale: _menus.UIScale );

            Lines++;
        }

        public void PrintWhite( String text, TextAlignment alignment = TextAlignment.Right )
        {
            var baseX = LeftPoint;
            var x = baseX + _padding;
            var textWidth = _drawer.MeasureString( text, isBigFont: IsBigFont );

            if ( alignment == TextAlignment.Right )
                x = RightPoint - textWidth - ( _padding / 2 );
            else if ( alignment == TextAlignment.Centre )
                x = MidPointX - ( textWidth / 2 );

            _menus.PrintWhite( x, CurrentY, text, scale: _menus.UIScale, isBigFont: IsBigFont );

            Lines++;
        }

        public void Slider( String text, float range, TextAlignment alignment = TextAlignment.Right )
        {
            var baseX = LeftPoint;
            var x = baseX + _padding;
            var textWidth = _drawer.MeasureString( text );

            if ( alignment == TextAlignment.Right )
                x = MidPointX - textWidth - ( _padding / 2 );
            else if ( alignment == TextAlignment.Centre )
                x = MidPointX - ( textWidth / 2 );

            // x += _x;

            var y = CurrentY;

            _menus.Print( x, y, text, scale: _menus.UIScale, isBigFont: IsBigFont );

            _menus.DrawSlider( MidPointX + ( _padding / 2 ), y, _width / 2, range, scale: _menus.UIScale );

            Lines++;
        }

        public void CheckBox( String text, Boolean isChecked, TextAlignment alignment = TextAlignment.Right )
        {
            var baseX = LeftPoint;
            var x = baseX + _padding;
            var textWidth = _drawer.MeasureString( text );

            if ( alignment == TextAlignment.Right )
                x = MidPointX - textWidth - ( _padding / 2 );
            else if ( alignment == TextAlignment.Centre )
                x = MidPointX - ( textWidth / 2 );

            //x += _x;

            var y = CurrentY;

            _menus.Print( x, y, text, scale: _menus.UIScale, isBigFont: IsBigFont );

            _menus.DrawCheckbox( MidPointX, y, isChecked, scale: _menus.UIScale );

            Lines++;
        }

        public void PrintValue( String text, String value, TextAlignment alignment = TextAlignment.Right )
        {
            var baseX = LeftPoint;
            var x = baseX + _padding;
            var textWidth = _drawer.MeasureString( text );

            if ( alignment == TextAlignment.Right )
                x = MidPointX - textWidth - ( _padding / 2 );
            else if ( alignment == TextAlignment.Centre )
                x = MidPointX - ( textWidth / 2 );

            //x += _x;

            var y = CurrentY;

            _menus.Print( x, y, text, scale: _menus.UIScale, isBigFont: IsBigFont );
            _menus.PrintWhite( MidPointX + ( _padding / 2 ), y, value, scale: _menus.UIScale, isBigFont: IsBigFont );

            Lines++;
        }

        public void NewLine()
        {
            BlankLines.Add( Lines );
            Lines++;
        }
    }

    public enum TextAlignment
    {
        Left,
        Centre,
        Right
    }
}
