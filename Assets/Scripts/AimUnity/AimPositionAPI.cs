// AimPosition V3.1.2 — Unity P/Invoke layer
// Port of AimFunc.cs from AimPosDll_3.1.2, adapted for Unity (IL2CPP safe).
//
// Setup:
//   1. Copy AimPosition3.1.2.dll into Assets/Plugins/x86_64/
//   2. Place .aimtool files in Assets/StreamingAssets/AimTools/
//   3. Add AimPositionManager to a GameObject in your first scene.
//
// Coordinate convention note:
//   AimPosition uses a right-handed metric system (mm).
//   Unity uses left-handed meters.  Conversion applied in TrackedTool:
//     position  = (x, y, -z) / 1000
//     quaternion = (-qx, -qy, qz, qw)  where Qoxyz = [qx, qy, qz, qw]

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

using System;
using System.Runtime.InteropServices;

namespace AimPosition
{
    public static class AimPositionAPI
    {
        private const string DLL = "AimPosition3.1.2";

        public const int PtMaxNUM  = 200;
        public const int TOOLIDMAX = 50;

        // ── Enumerations ─────────────────────────────────────────────────────

        public enum AIMPOS_TYPE
        {
            eAP_Basic    = 0,
            eAP_Standard = 1,
            eAP_Industry = 2,
            eAP_Lite     = 3,
            eAP_Ultimate = 4,
            eOP_M31      = 5,
            eOP_M321     = 6,
            eOP_M322     = 7,
            eOP_M62      = 8,
            eOP_M631     = 9,
            eOP_M632     = 10,
            eOP_I61      = 11,
            eOP_I621     = 12,
            eOP_I622     = 13,
            eOP_I81      = 14,
            eOP_I821     = 15,
            eOP_I822     = 16,
            eOP_I520     = 17,
            eOP_I520A    = 18,
            eOP_M631H    = 19,
            eOP_M33      = 20,
            eAP_NONE     = 100,
        }

        public enum E_Interface
        {
            I_USB      = 0,
            I_ETHERNET = 1,
            I_WIFI     = 2,
        }

        // Explicit values kept in sync with the native DLL header.
        // The original C# wrapper accidentally commented out AIMOOE_WRITE_FAULT,
        // shifting subsequent values by one — corrected here.
        public enum E_ReturnValue
        {
            AIMOOE_ERROR          = -1,
            AIMOOE_OK             = 0,
            AIMOOE_CONNECT_ERROR  = 1,
            AIMOOE_NOT_CONNECT    = 2,
            AIMOOE_READ_FAULT     = 3,
            AIMOOE_WRITE_FAULT    = 4,
            AIMOOE_NOT_REFLASH    = 5,
            AIMOOE_INITIAL_FAIL   = 6,
            AIMOOE_HANDLE_IS_NULL = 7,
        }

        public enum E_DataType
        {
            DT_NONE                  = 0,
            DT_INFO                  = 1,
            DT_MARKER_INFO_WITH_WIFI = 2,
            DT_STATUS_INFO           = 3,
            DT_IMGDUAL               = 4,
            DT_IMGCOLOR              = 5,
            DT_INFO_IMGDUAL          = 6,
            DT_INFO_IMGCOLOR         = 7,
            DT_INFO_IMGDUAL_IMGCOLOR = 8,
        }

        public enum E_SystemCommand
        {
            SC_COLLISION_DISABLE     = 0,
            SC_COLLISION_ENABLE      = 1,
            SC_COLLISION_INFO_CLEAR  = 2,
            SC_COLLISION_CONTROL     = 3,
            SC_IRLED_ON              = 4,
            SC_IRLED_OFF             = 5,
            SC_IRLED_MODE_ON         = 6,
            SC_IRLED_MODE_OFF        = 7,
            SC_LASER_ON              = 8,
            SC_LASER_OFF             = 9,
            SC_LASER_MODE_ON         = 10,
            SC_LASER_MODE_OFF        = 11,
            SC_LCD_PAGE_SUBPIXEL     = 12,
            SC_LCD_PAGE_COLOR        = 13,
            SC_LCD_ON                = 14,
            SC_LCD_OFF               = 15,
            SC_LCD_MODE_ON           = 16,
            SC_LCD_MODE_OFF          = 17,
            SC_AF_CONTINUOUSLY_ON    = 18,
            SC_AF_CONTINUOUSLY_OFF   = 19,
            SC_AF_SINGLE             = 20,
            SC_AF_FIX_INFINITY       = 21,
            SC_AF_EXP_AUTO_ON        = 22,
            SC_AF_EXP_AUTO_OFF       = 23,
            SC_AF_RESTART            = 24,
            SC_DUALCAM_AUTO_EXP_ON   = 25,
            SC_DUALCAM_AUTO_EXP_OFF  = 26,
            SC_DUALCAM_POWER_ON      = 27,
            SC_DUALCAM_POWER_OFF     = 28,
            SC_DUALCAM_SYNC_TRIG     = 29,
            SC_LOGO_MODE_ON          = 30,
            SC_LOGO_MODE_OFF         = 31,
            SC_FPGA_STATUS           = 32,
            SC_NONE                  = 33,
        }

        public enum E_AcquireMode
        {
            ContinuousMode    = 0,
            SingleMasterMode  = 1,
            SingleSlaveMode   = 2,
        }

        public enum E_CollisionStatus
        {
            COLLISION_NOT_OCCURRED = 0,
            COLLISION_OCCURRED     = 1,
            COLLISION_NOT_START    = 2,
        }

        public enum E_BackgroundLightStatus
        {
            BG_LIGHT_OK       = 0,
            BG_LIGHT_ABNORMAL = 1,
        }

        public enum E_HardwareStatus
        {
            HW_OK                      = 0,
            HW_LCD_VOLTAGE_TOO_LOW     = 1,
            HW_LCD_VOLTAGE_TOO_HIGH    = 2,
            HW_IR_LEFT_VOLTAGE_TOO_LOW  = 3,
            HW_IR_LEFT_VOLTAGE_TOO_HIGH = 4,
            HW_IR_RIGHT_VOLTAGE_TOO_LOW  = 5,
            HW_IR_RIGHT_VOLTAGE_TOO_HIGH = 6,
            HW_GET_INITIAL_DATA_ERROR    = 7,
        }

        public enum E_MarkWarnType
        {
            eWarn_None     = 0,
            eWarn_Common   = 1,
            eWarn_Critical = 2,
        }

        public enum E_AimToolType
        {
            ePosTool    = 0,
            ePosCalBoard = 1,
        }

        public enum E_RTDirection
        {
            FromToolToOpticalTrackingSystem      = 0,
            FromOpticalTrackingSystemToTool      = 1,
        }

        public enum E_ToolFixRlt
        {
            eToolFixCancle = 0,
            eToolFixRedo   = 1,
            eToolFixSave   = 2,
        }

        // ── Structs ──────────────────────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential)]
        public struct T_Img_Info
        {
            public uint width;
            public uint height;
            public byte channel;   // 1 = 8-bit grayscale, 2 = 16-bit RGB565
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct T_AIMPOS_DATAPARA
        {
            public AIMPOS_TYPE devtype;
            public T_Img_Info  dualimg;
            public T_Img_Info  colorimg;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string hardwareinfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct T_AimPosStatusInfo
        {
            public float  Tcpu;
            public float  Tpcb;
            public byte   LeftCamFps;
            public byte   RightCamFps;
            public byte   ColorCamFps;
            public byte   LCDFps;
            public ushort ExposureTimeLeftCam;
            public ushort ExposureTimeRightCam;
            public E_CollisionStatus CollisionStatus;
            public E_HardwareStatus  HardwareStatus;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct T_MarkerInfo
        {
            public uint ID;
            public int  MarkerNumber;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = PtMaxNUM * 3, ArraySubType = UnmanagedType.R8)]
            public double[] MarkerCoordinate;   // flat [x0,y0,z0, x1,y1,z1, ...]

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = PtMaxNUM, ArraySubType = UnmanagedType.U4)]
            public int[] PhantomMarkerWarning;

            public int PhantomMarkerGroupNumber;
            public E_BackgroundLightStatus MarkerBGLightStatus;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = PtMaxNUM, ArraySubType = UnmanagedType.U4)]
            public E_MarkWarnType[] MarkWarn;

            public E_MarkWarnType bLeftOutWarnning;
            public E_MarkWarnType bRightOutWarning;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct T_AimToolDataResultSingle
        {
            public E_AimToolType type;

            [MarshalAs(UnmanagedType.U1)]
            public bool validflag;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PtMaxNUM)]
            public string toolname;

            public float MeanError;
            public float Rms;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R4)]
            public float[] rotationvector;    // Euler angles (radians), tool→system

            // Quaternion in order [Qx, Qy, Qz, Qo(=Qw)].
            // Convert to Unity: new Quaternion(-Qx, -Qy, Qz, Qw)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.R4)]
            public float[] Qoxyz;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9, ArraySubType = UnmanagedType.R4)]
            public float[] Rto;               // 3×3 rotation matrix (row-major), tool→system

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R4)]
            public float[] Tto;               // Translation tool→system (mm)

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R4)]
            public float[] OriginCoor;        // Tool origin in system coords (mm)

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R4)]
            public float[] tooltip;           // Tool tip in system coords (mm)

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R4)]
            public float[] toolmid;           // Mid-axis point in system coords (mm)

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R4)]
            public float[] toolCstip;         // Tip in tool coordinate frame (mm)

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R4)]
            public float[] toolCsmid;         // Mid-axis point in tool frame (mm)

            public int toolPtNum;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200, ArraySubType = UnmanagedType.U4)]
            public int[] toolPtId;            // Marker-to-tool-point indices (-1 = unmatched)
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct T_ManufactureInfo
        {
            public ushort Year;
            public ushort Month;
            public byte   Day;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string Version;
            public byte VersionLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct T_AccuracyToolResult
        {
            [MarshalAs(UnmanagedType.U1)]
            public bool validflag;
            public float Dis;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R4)]
            public float[] Angle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct T_ToolFileData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16 * 3, ArraySubType = UnmanagedType.U1)]
            public char[] toolname;
            public char tooType;
            public int   markerNumbers;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200 * 3, ArraySubType = UnmanagedType.R4)]
            public float[] MarkerCoordinate;
            public int constTipNum;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200 * 3, ArraySubType = UnmanagedType.R4)]
            public float[] tipHeadCoordinate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200 * 3, ArraySubType = UnmanagedType.R4)]
            public float[] tipBodyCoordinate;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct t_ToolTipCalProInfo
        {
            [MarshalAs(UnmanagedType.U1)] public bool  isBoardFind;
            [MarshalAs(UnmanagedType.U1)] public bool  isToolFind;
            [MarshalAs(UnmanagedType.U1)] public bool  isValidCalibrate;
            [MarshalAs(UnmanagedType.U1)] public bool  isCalibrateFinished;
            public float CalibrateError;
            public float CalibrateRate;
            public float CalRMSError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct t_ToolMadeProInfo
        {
            [MarshalAs(UnmanagedType.U1)] public bool  unValidMarkerFlag;
            public float madeRate;
            [MarshalAs(UnmanagedType.U1)] public bool  isMadeProFinished;
            public float MadeError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct t_ToolFixProInfo
        {
            [MarshalAs(UnmanagedType.U1)] public bool isToolFind;
            [MarshalAs(UnmanagedType.U1)] public bool validfixflag;
            public int    isValidFixCnt;
            [MarshalAs(UnmanagedType.U1)] public bool isCalibrateFinished;
            public float  MatchError;
            public IntPtr mpMarkMatchStatus;   // unused per SDK docs
            public int    totalmarkcnt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct T_ToolTipPivotInfo
        {
            [MarshalAs(UnmanagedType.I1)] public bool  isToolFind;
            [MarshalAs(UnmanagedType.I1)] public bool  isPivotFinished;
            public float pivotRate;
            public float pivotMeanError;
        }

        // ── API functions ─────────────────────────────────────────────────────

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_API_Initial(out IntPtr aimHandle);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_API_Close(out IntPtr aimHandle);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_ConnectDevice(IntPtr aimHandle, E_Interface interfaceType, out T_AIMPOS_DATAPARA o_pospara);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetEthernetConnectIP(IntPtr aimHandle, byte IP_A, byte IP_B, byte IP_C, byte IP_D);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetEthernetConnectIP(IntPtr aimHandle, out byte IP_A, out byte IP_B, out byte IP_C, out byte IP_D);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetAcquireData(IntPtr aimHandle, E_Interface interfaceType, E_DataType dataType);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetAcquireMode(IntPtr aimHandle, E_Interface interfaceType, E_AcquireMode mode);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetMarkerAndStatusFromHardware(IntPtr aimHandle, E_Interface interfaceType, out T_MarkerInfo markerSt, out T_AimPosStatusInfo statusSt);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetMarkerInfo(IntPtr aimHandle, E_Interface interfaceType, out T_MarkerInfo markerSt);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetStatusInfo(IntPtr aimHandle, E_Interface interfaceType, out T_AimPosStatusInfo statusSt);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetManufactureInfo(IntPtr aimHandle, E_Interface interfaceType, out T_ManufactureInfo manufactureInfo);

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_SetToolInfoFilePath(IntPtr aimHandle, string path);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetCountOfToolInfo(IntPtr aimHandle, out int size);

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_FindSingleToolInfo(IntPtr aimHandle, T_MarkerInfo marker, string toolids, out T_AimToolDataResultSingle dataResult, int minimumMatchPts = 0);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetToolFindOffset(IntPtr aimHandle, float offset = 1.5f);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetToolFindOffset(IntPtr aimHandle, out float offset);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetToolFindPointMatchOptimizeEnable(IntPtr aimHandle, bool en = true);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetToolFindPointMatchOptimizeEnable(IntPtr aimHandle, out bool en);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetToolFindRTDirection(IntPtr aimHandle, E_RTDirection direction);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetToolFindRTDirection(IntPtr aimHandle, out E_RTDirection direction);

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_CheckToolFile(IntPtr aimHandle, string path, out T_ToolFileData toolData);

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_GetSpecificToolFileInfoArray(IntPtr aimHandle, string ptoolid, out int marksize, out float toolsysinfo);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetAllToolFilesBaseInfo(IntPtr aimHandle, out IntPtr ptools);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetSystemCommand(IntPtr aimHandle, E_Interface interfaceType, E_SystemCommand com);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetDualExpTime(IntPtr aimHandle, E_Interface interfaceType, int expTime);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetDualExpTimeByDistance(IntPtr aimHandle, E_Interface interfaceType, int distanceInMM);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetColorExpTime(IntPtr aimHandle, E_Interface interfaceType, int expTimeAF);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetFlashDelay(IntPtr aimHandle, E_Interface interfaceType, int flashOnDelay, int flashOffDelay);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetCollisinoDetectLevel(IntPtr aimHandle, E_Interface interfaceType, byte level);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetLCDShowRawPoint(IntPtr aimHandle, E_Interface interfaceType, bool isRawPointShow = false);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetMarkerParameters(IntPtr aimHandle, E_Interface interfaceType, int minRoundness = 75, int maxArea = 1000, int minBrightness = 80);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SetAimPositionIP(IntPtr aimHandle, E_Interface interfaceType, byte IP_A, byte IP_B, byte IP_C, byte IP_D);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_GetAimPositionIP(IntPtr aimHandle, E_Interface interfaceType, out byte IP_A, out byte IP_B, out byte IP_C, out byte IP_D);

        // ── Tool creation ────────────────────────────────────────────────────

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_InitToolMadeInfo(IntPtr aimHandle, int markcnt, string id);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_ProceedToolMade(IntPtr aimHandle, T_MarkerInfo marker, out t_ToolMadeProInfo mToolMadeinfo);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SaveToolMadeRlt(IntPtr aimHandle, bool en);

        // ── Calibration ──────────────────────────────────────────────────────

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_InitToolTipCalibrationWithToolId(IntPtr aimHandle, string CalTool, string PosTool);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_ProceedToolTipCalibration(IntPtr aimHandle, T_MarkerInfo marker, out t_ToolTipCalProInfo proInfo);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SaveToolTipCalibration(IntPtr aimHandle);

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_InitToolTipPivotWithToolId(IntPtr aimHandle, string toolID, bool clearTipMid = false);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_ProceedToolTipPivot(IntPtr aimHandle, T_MarkerInfo marker, out T_ToolTipPivotInfo pivotInfo);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SaveToolTipPivot(IntPtr aimHandle, int lineidx);

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_InitToolSelfCalibrationWithToolId(IntPtr aimHandle, string tool, out int markcnt);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_ProceedToolSelfCalibration(IntPtr aimHandle, T_MarkerInfo marker, out t_ToolFixProInfo proinfo);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SaveToolSelfCalibration(IntPtr aimHandle, E_ToolFixRlt fixrltcmd);

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_InitToolCoordinateRenewWithToolId(IntPtr aimHandle, string CalTool, string PosTool);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_ProceedToolCoordinateRenew(IntPtr aimHandle, T_MarkerInfo marker, out t_ToolTipCalProInfo info);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_SaveToolCoordinateRenew(IntPtr aimHandle);

        // ── Accuracy check ───────────────────────────────────────────────────

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_InitAccuracyCheckTool(IntPtr aimHandle, string toolids, string toolid1, string toolid2);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_AccuracyCheckTool(IntPtr aimHandle, double[,] markarr, int markercnt, out T_AccuracyToolResult rlt);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_AccuracyCheckToolCalculateError(IntPtr aimHandle, out float meanerro, out float stdev, out float angle);

        // ── Point reconstruction ─────────────────────────────────────────────

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_Calculate3DPoints(IntPtr aimHandle, float[] leftPoint, int leftNum, float[] rightPoint, int rightNum, T_MarkerInfo markerSt);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_InitMappingPointSetsForMarkerSpaceReg(IntPtr aimHandle, float[,] ImgPtArr, int ImgPtSize);

        // ── Robot integration ────────────────────────────────────────────────

        [DllImport(DLL, CharSet = CharSet.Ansi)]
        public static extern E_ReturnValue Aim_SetRobotCalculateRlt(IntPtr aimHandle, double[,] Sys2RobotBaseRTArray, double[,] Tool2RobotEndRTArray, string toolid);

        [DllImport(DLL, CharSet = CharSet.Auto)]
        public static extern E_ReturnValue Aim_CalculateRobotTargetPose(IntPtr aimHandle, double[,] TargetPathArr, float[] rightPoint);
    }
}

#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
