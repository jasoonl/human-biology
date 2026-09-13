using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HumanBodyExplorer.Data
{
    /// <summary>
    /// Minimal P/Invoke bindings against the system libsqlite3 shipped with macOS
    /// (used by Editor + Mac Standalone builds). Mobile/WebGL targets need a bundled
    /// native SQLite plugin instead — swap the DllImport target there.
    /// </summary>
    internal static class SqliteNative
    {
        private const string Lib = "sqlite3";

        public const int SQLITE_OK = 0;
        public const int SQLITE_ROW = 100;
        public const int SQLITE_DONE = 101;

        [DllImport(Lib, EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Open(string filename, out IntPtr db);

        [DllImport(Lib, EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Close(IntPtr db);

        [DllImport(Lib, EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Prepare(IntPtr db, byte[] sqlUtf8, int numBytes, out IntPtr stmt, IntPtr tail);

        [DllImport(Lib, EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Step(IntPtr stmt);

        [DllImport(Lib, EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Finalize(IntPtr stmt);

        [DllImport(Lib, EntryPoint = "sqlite3_bind_text", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindText(IntPtr stmt, int index, byte[] valueUtf8, int numBytes, IntPtr destructor);

        [DllImport(Lib, EntryPoint = "sqlite3_bind_double", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindDouble(IntPtr stmt, int index, double value);

        [DllImport(Lib, EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindInt(IntPtr stmt, int index, int value);

        [DllImport(Lib, EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ColumnTextRaw(IntPtr stmt, int col);

        [DllImport(Lib, EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ColumnInt(IntPtr stmt, int col);

        [DllImport(Lib, EntryPoint = "sqlite3_column_double", CallingConvention = CallingConvention.Cdecl)]
        public static extern double ColumnDouble(IntPtr stmt, int col);

        [DllImport(Lib, EntryPoint = "sqlite3_errmsg", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ErrMsgRaw(IntPtr db);

        public static readonly IntPtr SQLITE_TRANSIENT = new IntPtr(-1);

        public static string ColumnText(IntPtr stmt, int col)
        {
            IntPtr ptr = ColumnTextRaw(stmt, col);
            return ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(ptr);
        }

        public static string ErrMsg(IntPtr db)
        {
            IntPtr ptr = ErrMsgRaw(db);
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr);
        }

        public static byte[] ToUtf8(string s) => Encoding.UTF8.GetBytes(s + "\0");
    }
}
