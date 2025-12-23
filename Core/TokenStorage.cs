using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ClickUpDesktopPowerTools.Core;

public class TokenStorage : ITokenProvider
{
    private const string TargetName = "ClickUpDesktopPowerTools:ClickUpAPIToken";

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, uint type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredFree(IntPtr cred);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref NativeCredential credential, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, uint type, int reservedFlag);

    private const uint CRED_TYPE_GENERIC = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    public string? GetToken()
    {
        try
        {
            if (CredRead(TargetName, CRED_TYPE_GENERIC, 0, out IntPtr credentialPtr))
            {
                var credential = Marshal.PtrToStructure<Credential>(credentialPtr);
                var passwordBytes = new byte[credential.CredentialBlobSize];
                Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, (int)credential.CredentialBlobSize);
                CredFree(credentialPtr);
                return Encoding.Unicode.GetString(passwordBytes).TrimEnd('\0').Trim();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public void StoreToken(string token)
    {
        try
        {
            // Delete existing credential if it exists
            CredDelete(TargetName, CRED_TYPE_GENERIC, 0);

            var bytes = Encoding.Unicode.GetBytes(token);
            var credential = new NativeCredential
            {
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                Comment = IntPtr.Zero,
                TargetAlias = IntPtr.Zero,
                Type = CRED_TYPE_GENERIC,
                Persist = 2, // CRED_PERSIST_LOCAL_MACHINE
                CredentialBlobSize = (uint)bytes.Length,
                CredentialBlob = Marshal.AllocCoTaskMem(bytes.Length),
                TargetName = Marshal.StringToCoTaskMemUni(TargetName),
                UserName = Marshal.StringToCoTaskMemUni(Environment.UserName)
            };

            Marshal.Copy(bytes, 0, credential.CredentialBlob, bytes.Length);
            CredWrite(ref credential, 0);

            Marshal.FreeCoTaskMem(credential.TargetName);
            Marshal.FreeCoTaskMem(credential.UserName);
            Marshal.FreeCoTaskMem(credential.CredentialBlob);
        }
        catch
        {
            // Failed to store token
        }
    }

    public void ClearToken()
    {
        try
        {
            CredDelete(TargetName, CRED_TYPE_GENERIC, 0);
        }
        catch
        {
            // Failed to delete token (may not exist)
        }
    }
}

