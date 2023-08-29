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

using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.Logging;
using SharpQuake.Sys;
using System;
using System.IO;
using System.Text;

namespace SharpQuake.Logging
{
	/// <summary>
	/// Core logging functionality
	/// </summary>
	public class ConsoleLogger : IConsoleLogger, IDisposable
	{
		private const String LOG_FILE_NAME = "qconsole.log";

		/// <summary>
		/// Should console data be dumped to a file?
		/// </summary>
		private Boolean LogToFile
		{
			get;
			set;
		}

		/// <summary>
		/// The internal logging file stream
		/// </summary>
		private FileStream Stream
		{
			get;
			set;
		}

		/// <summary>
		/// Is the console forced open?
		/// </summary>
		/// <remarks>
		/// (Due to errors etc.)
		/// </remarks>
		public Boolean IsForcedUp
		{
			get;
			set;
		}

		/// <summary>
		/// Callback for handling the visual aspect of console printing
		/// </summary>
		public Action<String> OnPrint
        {
			get;
			set;
        }

		private readonly IEngine _engine;

		public ConsoleLogger( IEngine engine )
        {
			_engine = engine;
			Initialise( );
		}

		/// <summary>
		/// Setup the dependencies for logging
		/// </summary>
		private void Initialise()
		{
			LogToFile = CommandLine.CheckParm( "-condebug" ) > 0;

			if ( LogToFile )
			{
				var path = Path.Combine( FileSystem.GameDir, LOG_FILE_NAME );

				if ( File.Exists( path ) )
					File.Delete( path );

				Stream = new FileStream( path, FileMode.Create, FileAccess.Write, FileShare.Read );
			}

			// TODO - Get rid of the console wrapper when logging is abstracted
			ConsoleWrapper.OnPrint += ( txt ) =>
			{
				Print( txt );
			};

			ConsoleWrapper.OnPrint2 += ( fmt, args ) =>
			{
				Print( fmt, args );
			};

			ConsoleWrapper.OnDPrint += ( fmt, args ) =>
			{
				DPrint( fmt, args );
			};
		}

		/// <summary>
		/// Print a message to the console
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public void Print( String format, params Object[] args )
        {
			var msg = args.Length > 0 ? String.Format( format, args ) : format;

			DebugPrint( msg );

			// Execute a visual console handler if we have one, to propagate the message
			OnPrint?.Invoke( msg );
		}

		/// <summary>
		/// Con_DPrintf
		/// </summary>
		/// <remarks>
		/// A Con_Printf that only shows up if the "developer" cvar is set
		/// </remarks>
		/// <param name="fmt"></param>
		/// <param name="args"></param>
		public void DPrint( String fmt, params Object[] args )
		{
			// don't confuse non-developers with techie stuff...
			if ( !_engine.IsDeveloper )
				return;

			var text = fmt;

			// All debug lines are grey
			if ( !text.StartsWith( "^9" ) )
				text = "^9" + text;

            Print( text, args );
		}

		/// <summary>
		/// Con_DebugLog
		/// </summary>
		private void DebugPrint( String message )
		{			
			Console.WriteLine( message ); // Debug stuff

			if ( !LogToFile )
				return;

			// log all messages to file
			Write( message );
		}

		/// <summary>
		/// Write a message to the logging file
		/// </summary>
		/// <param name="message"></param>
		private void Write( String message )
		{
			if ( Stream == null )
				return;

			var tmp = Encoding.UTF8.GetBytes( message );
			Stream.Write( tmp, 0, tmp.Length );
		}

		/// <summary>
		/// Release the logging file
		/// </summary>
		public void Dispose( )
		{
			if ( Stream == null )
				return;

			Stream.Flush( );
			Stream.Dispose( );
			Stream = null;			
		}
    }
}
