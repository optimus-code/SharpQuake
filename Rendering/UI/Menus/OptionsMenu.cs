﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class OptionsMenu : MenuBase
    {
        private const Int32 OPTIONS_ITEMS = 13;

        //private float _BgmVolumeCoeff = 0.1f;

        public override void Show( Host host )
        {
            /*if( sys.IsWindows )  fix cd audio first
             {
                 _BgmVolumeCoeff = 1.0f;
             }*/

            if ( _Cursor > OPTIONS_ITEMS - 1 )
                _Cursor = 0;

            if ( _Cursor == OPTIONS_ITEMS - 1 && MenuBase.VideoMenu == null )
                _Cursor = 0;

            base.Show( host );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.MainMenu.Show( Host );
                    break;

                case KeysDef.K_ENTER:
                    Menu.EnterSound = true;
                    switch ( _Cursor )
                    {
                        case 0:
                            MenuBase.KeysMenu.Show( Host );
                            break;

                        case 1:
                            MenuBase.Hide( );
                            Con.ToggleConsole_f( );
                            break;

                        case 2:
                            Host.CommandBuffer.AddText( "exec default.cfg\n" );
                            break;

                        case 12:
                            MenuBase.VideoMenu.Show( Host );
                            break;

                        default:
                            AdjustSliders( 1 );
                            break;
                    }
                    return;

                case KeysDef.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if ( _Cursor < 0 )
                        _Cursor = OPTIONS_ITEMS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if ( _Cursor >= OPTIONS_ITEMS )
                        _Cursor = 0;
                    break;

                case KeysDef.K_LEFTARROW:
                    AdjustSliders( -1 );
                    break;

                case KeysDef.K_RIGHTARROW:
                    AdjustSliders( 1 );
                    break;
            }

            /*if( _Cursor == 12 && VideoMenu == null )
            {
                if( key == KeysDef.K_UPARROW )
                    _Cursor = 11;
                else
                    _Cursor = 0;
            }*/

            if ( _Cursor == 12 )
            {
                if ( key == KeysDef.K_UPARROW )
                    _Cursor = 11;
                else
                    _Cursor = 0;
            }

            /*#if _WIN32
                        if ((options_cursor == 13) && (modestate != MS_WINDOWED))
                        {
                            if (k == K_UPARROW)
                                options_cursor = 12;
                            else
                                options_cursor = 0;
                        }
            #endif*/
        }

        public override void Draw( )
        {
            Menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            GLPic p = Drawer.CachePic( "gfx/p_option.lmp" );
            Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            Menu.Print( 16, 32, "    Customize controls" );
            Menu.Print( 16, 40, "         Go to console" );
            Menu.Print( 16, 48, "     Reset to defaults" );

            Menu.Print( 16, 56, "           Screen size" );
            var r = ( Scr.ViewSize.Value - 30 ) / ( 120 - 30 );
            Menu.DrawSlider( 220, 56, r );

            Menu.Print( 16, 64, "            Brightness" );
            r = ( 1.0f - view.Gamma ) / 0.5f;
            Menu.DrawSlider( 220, 64, r );

            Menu.Print( 16, 72, "           Mouse Speed" );
            r = ( client.Sensitivity - 1 ) / 10;
            Menu.DrawSlider( 220, 72, r );

            Menu.Print( 16, 80, "       CD Music Volume" );
            r = snd.BgmVolume;
            Menu.DrawSlider( 220, 80, r );

            Menu.Print( 16, 88, "          Sound Volume" );
            r = snd.Volume;
            Menu.DrawSlider( 220, 88, r );

            Menu.Print( 16, 96, "            Always Run" );
            Menu.DrawCheckbox( 220, 96, client.ForwardSpeed > 200 );

            Menu.Print( 16, 104, "          Invert Mouse" );
            Menu.DrawCheckbox( 220, 104, client.MPitch < 0 );

            Menu.Print( 16, 112, "            Lookspring" );
            Menu.DrawCheckbox( 220, 112, client.LookSpring );

            Menu.Print( 16, 120, "            Lookstrafe" );
            Menu.DrawCheckbox( 220, 120, client.LookStrafe );

            /*if( VideoMenu != null )
                Menu.Print( 16, 128, "         Video Options" );*/

#if _WIN32
	if (modestate == MS_WINDOWED)
	{
		Menu.Print (16, 136, "             Use Mouse");
		Menu.DrawCheckbox (220, 136, _windowed_mouse.value);
	}
#endif

            // cursor
            Menu.DrawCharacter( 200, 32 + _Cursor * 8, 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );
        }

        /// <summary>
        /// M_AdjustSliders
        /// </summary>
        private void AdjustSliders( Int32 dir )
        {
            snd.LocalSound( "misc/menu3.wav" );
            Single value;

            switch ( _Cursor )
            {
                case 3:	// screen size
                    value = Scr.ViewSize.Value + dir * 10;
                    if ( value < 30 )
                        value = 30;
                    if ( value > 120 )
                        value = 120;
                    CVar.Set( "viewsize", value );
                    break;

                case 4:	// gamma
                    value = view.Gamma - dir * 0.05f;
                    if ( value < 0.5 )
                        value = 0.5f;
                    if ( value > 1 )
                        value = 1;
                    CVar.Set( "gamma", value );
                    break;

                case 5:	// mouse speed
                    value = client.Sensitivity + dir * 0.5f;
                    if ( value < 1 )
                        value = 1;
                    if ( value > 11 )
                        value = 11;
                    CVar.Set( "sensitivity", value );
                    break;

                case 6:	// music volume
                    value = snd.BgmVolume + dir * 0.1f; ///_BgmVolumeCoeff;
                    if ( value < 0 )
                        value = 0;
                    if ( value > 1 )
                        value = 1;
                    CVar.Set( "bgmvolume", value );
                    break;

                case 7:	// sfx volume
                    value = snd.Volume + dir * 0.1f;
                    if ( value < 0 )
                        value = 0;
                    if ( value > 1 )
                        value = 1;
                    CVar.Set( "volume", value );
                    break;

                case 8:	// allways run
                    if ( client.ForwardSpeed > 200 )
                    {
                        CVar.Set( "cl_forwardspeed", 200f );
                        CVar.Set( "cl_backspeed", 200f );
                    }
                    else
                    {
                        CVar.Set( "cl_forwardspeed", 400f );
                        CVar.Set( "cl_backspeed", 400f );
                    }
                    break;

                case 9:	// invert mouse
                    CVar.Set( "m_pitch", -client.MPitch );
                    break;

                case 10:	// lookspring
                    CVar.Set( "lookspring", !client.LookSpring ? 1f : 0f );
                    break;

                case 11:	// lookstrafe
                    CVar.Set( "lookstrafe", !client.LookStrafe ? 1f : 0f );
                    break;

#if _WIN32
	        case 13:	// _windowed_mouse
		        Cvar_SetValue ("_windowed_mouse", !_windowed_mouse.value);
		        break;
#endif
            }
        }
    }
}