using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Alias
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasframe_t
    {
        public trivertx_t bboxmin;	// lightnormal isn't used
        public trivertx_t bboxmax;	// lightnormal isn't used
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 16 )]
        public Byte[] name; // char[16]	// frame name from grabbing

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasframe_t ) );

        public static daliasframe_t FromBR( BinaryReader br )
        {
            var frame = new daliasframe_t( );
            frame.bboxmin = trivertx_t.FromBR( br );
            frame.bboxmax = trivertx_t.FromBR( br );
            frame.name = br.ReadBytes( 16 );
            return frame;
        }
    } // daliasframe_t;
}
