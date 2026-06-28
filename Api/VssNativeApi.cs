using System;
using System.Runtime.InteropServices;

namespace SystemRestoreTool.Api
{
    // ===========================================================================
    //  VSS NATIVE API  —  vssapi.dll (Volume Shadow Copy Service)
    //  Резервное копирование через снимки состояния тома.
    //  Используется для безопасной работы с SYSTEM файлами "вживую".
    //
    //  VSS реализован как COM-интерфейс, поэтому здесь только метаданные
    //  и GUID. Само создание снимка — в VssBackupManager через COM.
    // ===========================================================================

    public static class VssNativeApi
    {
        private const string VSSAPI = "vssapi.dll";

        // VSS GUID'ы
        public static readonly Guid VSS_COORDINATOR_CLASS =
            new("{E579AB5F-1CC4-44b4-BED9-DE0991FF0624}");

        public static readonly Guid CLSID_VSS_DM_VSS_XMLWRITER =
            new("{0C8F0F8E-2D8D-4f3d-9C6D-1C0A47D9C7E9}");

        // VSS_BACKUP_TYPE
        public enum VssBackupType : int
        {
            Undefined = 0,
            Full = 1,
            Incremental = 2,
            Differential = 3,
            Log = 4,
            Copy = 5,
            Other = 6
        }

        // VSS_RESTOREMETHOD_ENUM
        public enum VssRestoreMethod : int
        {
            Undefined = 0,
            RestoreIfNotThere = 1,
            RestoreIfCanReplace = 2,
            StopStartRestore = 3,
            RestoreToAlternate = 4
        }

        [DllImport(VSSAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int CreateVssBackupComponents(out IntPtr ppBackup);

        [DllImport(VSSAPI, SetLastError = true)]
        public static extern int VssFreeSnapshotProperties(IntPtr pSnapshotProperties);
    }
}
