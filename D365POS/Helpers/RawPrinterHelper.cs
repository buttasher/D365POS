using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace D365POS.Helpers
{
    public static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true)]
        public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendStringToPrinter(string printerName, string data)
        {
            IntPtr pBytes;
            IntPtr hPrinter;
            int dwCount = data.Length;
            pBytes = Marshal.StringToCoTaskMemAnsi(data);

            DOCINFOA di = new DOCINFOA
            {
                pDocName = "Receipt Print",
                pDataType = "RAW"
            };

            if (!OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                return false;
            }

            StartDocPrinter(hPrinter, 1, di);
            StartPagePrinter(hPrinter);
            bool success = WritePrinter(hPrinter, pBytes, dwCount, out _);
            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
            ClosePrinter(hPrinter);
            Marshal.FreeCoTaskMem(pBytes);

            return success;
        }
        public static bool SendimageToPrinter(string printerName, byte[] bytes)
        {
            if (!OpenPrinter(printerName.Normalize(), out IntPtr hPrinter, IntPtr.Zero))
                return false;

            DOCINFOA di = new DOCINFOA { pDocName = "Receipt Print", pDataType = "RAW" };

            StartDocPrinter(hPrinter, 1, di);
            StartPagePrinter(hPrinter);

            IntPtr unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length);

            bool success = WritePrinter(hPrinter, unmanagedBytes, bytes.Length, out _);

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
            ClosePrinter(hPrinter);
            Marshal.FreeCoTaskMem(unmanagedBytes);

            return success;
        }
    }
}