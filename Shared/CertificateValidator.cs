using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Omni.Shared
{
    public static class CertificateValidator
    {
        [DllImport("Omni.Cryptography", EntryPoint = "ValidateCertificate")]
        private static extern unsafe bool NativeValidateCertificate(byte* pfxPathPtr, byte* passwordPtr, byte* hostnamePtr, byte* outBuffer, int outBufferLen);

        public static unsafe bool ValidateCertificate(string pfxPath, string password, string hostname, out string error)
        {
            try
            {
                int _len = 100;
                fixed (byte* pfxPathPtr = GetBytes(pfxPath))
                fixed (byte* pfxpasswordPtr = GetBytes(password))
                fixed (byte* pfxhostnamePtr = GetBytes(hostname))
                fixed (byte* errorBufferPtr = new byte[_len])
                {
                    bool isValidCertificate = NativeValidateCertificate(pfxPathPtr, pfxpasswordPtr, pfxhostnamePtr, errorBufferPtr, _len);
                    error = Marshal.PtrToStringUTF8((IntPtr)errorBufferPtr);
                    return isValidCertificate;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static byte[] GetBytes(string msg)
        {
            return Encoding.UTF8.GetBytes(msg + '\0'); // add the null character
        }
    }
}