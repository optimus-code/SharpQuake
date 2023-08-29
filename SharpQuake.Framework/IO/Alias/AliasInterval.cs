using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Alias
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasinterval_t
    {
        public Single interval;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasinterval_t ) );

        public static daliasskininterval_t FromBR( BinaryReader br )
        {
            var result = new daliasskininterval_t( );
            result.interval = br.ReadSingle( );
            return result;
        }
    } // daliasinterval_t;
}
