using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  WINTRUST NATIVE API  —  wintrust.dll  +  crypt32.dll
    //  Проверка цифровых подписей Authenticode (Microsoft Windows Publisher).
    //  Позволяет ВЫЯВИТЬ ПОДМЕНУ: если kernel32.dll подписан не Microsoft —
    //  значит файл был заменён злоумышленником.
    // ===========================================================================

    public static class WinTrustConstants
    {
        public const int WTD_UI_NONE             = 2;
        public const int WTD_REVOKE_NONE         = 0x00000000;
        public const int WTD_REVOKE_WHOLECHAIN   = 0x00000001;
        public const int WTD_CHOICE_FILE         = 1;
        public const int WTD_STATEACTION_IGNORE  = 0x00000000;
        public const int WTD_SAFER_FLAG          = 0x00000020;
        public const int WTD_REVOCATION_CHECK_NONE = 0x00000010;
        public const int WTD_UICONTEXT_EXECUTE   = 0;

        // Известные HRESULT'ы WinTrust
        public const int S_OK                       = 0;
        public const int TRUST_E_NOSIGNATURE        = unchecked((int)0x800B0100);
        public const int TRUST_E_EXPLICIT_DISTRUST  = unchecked((int)0x800B0111);
        public const int CERT_E_EXPIRED             = unchecked((int)0x800B0101);
        public const int CERT_E_REVOKED             = unchecked((int)0x800B010C);
        public const int CERT_E_UNTRUSTEDROOT       = unchecked((int)0x800B0109);
        public const int CERT_E_PURPOSE             = unchecked((int)0x800B0106);
        public const int TRUST_E_SUBJECT_NOT_TRUSTED= unchecked((int)0x800B0004);
        public const int TRUST_E_BAD_DIGEST         = unchecked((int)0x80096010);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WinTrustFileInfo
    {
        public uint cbStruct;
        public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WinTrustData
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint dwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
    }

    /// <summary>Результат проверки подписи — удобная структура для Engine.</summary>
    public sealed class SignatureResult
    {
        public bool IsValid { get; set; }
        public bool IsSigned { get; set; }
        public int HResult { get; set; }
        public string Publisher { get; set; }
        public string Subject { get; set; }
        public string CertThumbprint { get; set; }
        public DateTime? NotBefore { get; set; }
        public DateTime? NotAfter { get; set; }
        public string Error { get; set; }

        public bool IsMicrosoft
            => Publisher != null &&
               (Publisher.Contains("Microsoft Windows", StringComparison.OrdinalIgnoreCase) ||
                Publisher.Contains("Microsoft Corporation", StringComparison.OrdinalIgnoreCase));
    }

    public static class WinTrustNativeApi
    {
        private const string WINTRUST = "wintrust.dll";
        private const string CRYPT32  = "crypt32.dll";

        public static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 =
            new("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");

        [DllImport(WINTRUST, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern int WinVerifyTrust(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
            IntPtr pWVTData);

        // --- crypt32.dll: извлечение информации о подписи (CMSG) ----------

        public const int PKCS_7_ASN_ENCODING     = 0x00010000;
        public const int X509_ASN_ENCODING       = 0x00000001;
        public const int CERT_QUERY_OBJECT_FILE  = 0x00000001;
        public const int CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED = 0x00000040;
        public const int CERT_QUERY_CONTENT_FLAG_ALL = 0x0000003E | CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED;
        public const int CERT_QUERY_FORMAT_FLAG_ALL = 0x0000000E;

        public const int CERT_NAME_SIMPLE_DISPLAY_TYPE = 4;
        public const int CERT_NAME_ATTR_TYPE = 4;

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_DECODE_PARA
        {
            public uint cbSize;
            public IntPtr pfnAlloc;
            public IntPtr pfnFree;
        }

        [DllImport(CRYPT32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptQueryObject(
            uint dwObjectType,
            string pvObject,
            uint dwExpectedContentTypeFlags,
            uint dwExpectedFormatTypeFlags,
            uint dwFlags,
            out uint pdwMsgAndCertEncodingType,
            out uint pdwContentType,
            out uint pdwFormatType,
            ref IntPtr phCertStore,
            ref IntPtr phMsg,
            ref IntPtr ppvContext);

        [DllImport(CRYPT32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptMsgGetParam(
            IntPtr hCryptMsg,
            uint dwParamType,
            uint dwIndex,
            byte[] pvData,
            ref uint pcbData);

        public const uint CMSG_SIGNER_INFO_PARAM = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct CMSG_SIGNER_INFO
        {
            public uint dwVersion;
            public CRYPT_DATA_BLOB Issuer;
            public CRYPT_DATA_BLOB SerialNumber;
            public CRYPT_ALGORITHM_IDENTIFIER HashAlgorithm;
            public CRYPT_ALGORITHM_IDENTIFIER HashEncryptionAlgorithm;
            public CRYPT_DATA_BLOB EncryptedHash;
            public CRYPT_ATTRIBUTES AuthAttrs;
            public CRYPT_ATTRIBUTES UnauthAttrs;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_DATA_BLOB
        {
            public uint cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_ALGORITHM_IDENTIFIER
        {
            public string pszObjId;
            public CRYPT_OBJID_BLOB Parameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_OBJID_BLOB
        {
            public uint cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_ATTRIBUTES
        {
            public uint cAttr;
            public IntPtr rgAttr;
        }

        [DllImport(CRYPT32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CertFindCertificateInStore(
            IntPtr hCertStore,
            uint dwCertEncodingType,
            uint dwFindFlags,
            uint dwFindType,
            IntPtr pvFindPara,
            IntPtr pPrevCertContext);

        public const uint CERT_FIND_SUBJECT_CERT = 0x00010000;

        [DllImport(CRYPT32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CertGetNameString(
            IntPtr pCertContext,
            uint dwType,
            uint dwFlags,
            IntPtr pvTypePara,
            StringBuilder pszNameString,
            uint cchNameString);

        [DllImport(CRYPT32, SetLastError = true)]
        public static extern bool CertFreeCertificateContext(IntPtr pCertContext);

        [DllImport(CRYPT32, SetLastError = true)]
        public static extern bool CertCloseStore(IntPtr hCertStore, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct CERT_CONTEXT
        {
            public uint dwCertEncodingType;
            public IntPtr pbCertEncoded;
            public uint cbCertEncoded;
            public IntPtr pCertInfo;
            public IntPtr hCertStore;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CERT_INFO
        {
            public uint dwVersion;
            public CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;
            public CERT_NAME_BLOB Issuer;
            public FILETIME NotBefore;
            public FILETIME NotAfter;
            public CERT_NAME_BLOB Subject;
            public CRYPT_PUBKEY_BLOB SubjectPublicKeyInfo;
            public CRYPT_BIT_BLOB IssuerUniqueId;
            public CRYPT_BIT_BLOB SubjectUniqueId;
            public uint cExtension;
            public IntPtr rgExtension;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CERT_NAME_BLOB
        {
            public uint cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_PUBKEY_BLOB
        {
            public CRYPT_ALGORITHM_IDENTIFIER Algorithm;
            public CRYPT_BIT_BLOB PublicKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_BIT_BLOB
        {
            public uint cbData;
            public IntPtr pbData;
            public uint cUnusedBits;
        }
    }
}
