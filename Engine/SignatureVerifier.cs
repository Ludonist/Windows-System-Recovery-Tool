using System;
using System.Runtime.InteropServices;
using System.Text;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using SystemRestoreTool.Api;
using SystemRestoreTool.Utils;

namespace SystemRestoreTool.Engine
{
    // ===========================================================================
    //  SIGNATURE VERIFIER
    //  Высокоуровневая проверка подписей Authenticode + извлечение издателя.
    //  Позволяет отличить "подписан Microsoft" от "подписан Васей Пупкиным"
    //  и таким образом выявить подмену системного файла.
    // ===========================================================================

    public static class SignatureVerifier
    {
        /// <summary>Проверяет подпись файла и возвращает богатый результат.</summary>
        public static SignatureResult Verify(string filePath)
        {
            var result = new SignatureResult { IsValid = false, IsSigned = false };

            // 1. WinVerifyTrust — основная проверка подписи и цепочки
            var fileInfo = new WinTrustFileInfo
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo)),
                pcwszFilePath = filePath,
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero
            };

            var trustData = new WinTrustData
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WinTrustData)),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = WinTrustConstants.WTD_UI_NONE,
                dwRevocationChecks = WinTrustConstants.WTD_REVOKE_NONE,
                dwUnionChoice = WinTrustConstants.WTD_CHOICE_FILE,
                pFile = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustFileInfo))),
                dwStateAction = WinTrustConstants.WTD_STATEACTION_IGNORE,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = IntPtr.Zero,
                dwProvFlags = WinTrustConstants.WTD_SAFER_FLAG | WinTrustConstants.WTD_REVOCATION_CHECK_NONE,
                dwUIContext = WinTrustConstants.WTD_UICONTEXT_EXECUTE
            };

            IntPtr pTrustData = IntPtr.Zero;
            try
            {
                Marshal.StructureToPtr(fileInfo, trustData.pFile, false);
                pTrustData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustData)));
                Marshal.StructureToPtr(trustData, pTrustData, false);

                int hr = WinTrustNativeApi.WinVerifyTrust(
                    IntPtr.Zero,
                    WinTrustNativeApi.WINTRUST_ACTION_GENERIC_VERIFY_V2,
                    pTrustData);

                result.HResult = hr;

                if (hr == WinTrustConstants.S_OK)
                {
                    result.IsValid = true;
                    result.IsSigned = true;
                }
                else if (hr == WinTrustConstants.TRUST_E_NOSIGNATURE)
                {
                    result.IsSigned = false;
                    result.Error = "Подпись отсутствует";
                }
                else if (hr == WinTrustConstants.TRUST_E_BAD_DIGEST)
                {
                    result.IsSigned = true;
                    result.Error = "Подпись НЕВЕРНА — файл модифицирован!";
                }
                else if (hr == WinTrustConstants.CERT_E_EXPIRED)
                {
                    result.IsSigned = true;
                    result.Error = "Сертификат истёк";
                }
                else if (hr == WinTrustConstants.CERT_E_REVOKED)
                {
                    result.IsSigned = true;
                    result.Error = "Сертификат отозван";
                }
                else if (hr == WinTrustConstants.CERT_E_UNTRUSTEDROOT)
                {
                    result.IsSigned = true;
                    result.Error = "Ненадёжный корневой центр";
                }
                else
                {
                    result.IsSigned = true;
                    result.Error = $"Ошибка проверки: 0x{hr:X8}";
                }

                // 2. Если файл подписан — извлечь издателя через crypt32.dll
                if (result.IsSigned)
                {
                    try { ExtractPublisherInfo(filePath, result); }
                    catch (Exception ex)
                    {
                        Logger.Instance.Warn($"Не удалось извлечь издателя для {filePath}: {ex.Message}");
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(trustData.pFile);
                if (pTrustData != IntPtr.Zero) Marshal.FreeHGlobal(pTrustData);
            }

            return result;
        }

        /// <summary>
        /// Извлекает имя издателя (Publisher) и Subject из подписи через
        /// CryptQueryObject + CertGetNameString. Только Microsoft имеет право
        /// подписывать системные файлы Windows.
        /// </summary>
        private static void ExtractPublisherInfo(string filePath, SignatureResult result)
        {
            uint encoding, contentType, formatType;
            IntPtr hStore = IntPtr.Zero, hMsg = IntPtr.Zero, ctx = IntPtr.Zero;

            bool ok = WinTrustNativeApi.CryptQueryObject(
                WinTrustNativeApi.CERT_QUERY_OBJECT_FILE,
                filePath,
                WinTrustNativeApi.CERT_QUERY_CONTENT_FLAG_ALL,
                WinTrustNativeApi.CERT_QUERY_FORMAT_FLAG_ALL,
                0,
                out encoding,
                out contentType,
                out formatType,
                ref hStore,
                ref hMsg,
                ref ctx);

            if (!ok || hStore == IntPtr.Zero) return;

            try
            {
                // Перечисляем сертификаты в хранилище подписи
                IntPtr pCert = IntPtr.Zero;
                while (true)
                {
                    pCert = WinTrustNativeApi.CertFindCertificateInStore(
                        hStore, encoding, 0, 0, IntPtr.Zero, pCert);
                    if (pCert == IntPtr.Zero) break;

                    // Subject = владелец сертификата (для подписи Windows это "Microsoft Windows")
                    // CERT_NAME_SIMPLE_DISPLAY_TYPE = 4
                    string subject  = GetCertName(pCert, WinTrustNativeApi.CERT_NAME_SIMPLE_DISPLAY_TYPE);
                    // Issuer: CERT_NAME_ISSUER_FLAG (0x1) | CERT_NAME_SIMPLE_DISPLAY_TYPE (4) = 5
                    string issuer   = GetCertName(pCert, 0x1 | WinTrustNativeApi.CERT_NAME_SIMPLE_DISPLAY_TYPE);

                    if (!string.IsNullOrEmpty(subject))
                    {
                        result.Subject = subject;
                        result.Publisher = subject;
                    }

                    // Срок действия
                    var certCtx = Marshal.PtrToStructure<WinTrustNativeApi.CERT_CONTEXT>(pCert);
                    if (certCtx.pCertInfo != IntPtr.Zero)
                    {
                        var info = Marshal.PtrToStructure<WinTrustNativeApi.CERT_INFO>(certCtx.pCertInfo);
                        result.NotBefore = FiletimeToDateTime(info.NotBefore);
                        result.NotAfter  = FiletimeToDateTime(info.NotAfter);
                    }

                    WinTrustNativeApi.CertFreeCertificateContext(pCert);
                    break; // Берём только первый сертификат подписанта
                }
            }
            finally
            {
                if (hStore != IntPtr.Zero) WinTrustNativeApi.CertCloseStore(hStore, 0);
            }
        }

        private static string GetCertName(IntPtr pCertContext, uint type)
        {
            var sb = new StringBuilder(512);
            bool ok = WinTrustNativeApi.CertGetNameString(
                pCertContext, type, 0, IntPtr.Zero, sb, (uint)sb.Capacity);
            return ok ? sb.ToString() : null;
        }

        private static DateTime FiletimeToDateTime(FILETIME ft)
        {
            try
            {
                long hFT = (((long)ft.dwHighDateTime) << 32) + ft.dwLowDateTime;
                return DateTime.FromFileTimeUtc(hFT);
            }
            catch { return DateTime.MinValue; }
        }
    }
}
