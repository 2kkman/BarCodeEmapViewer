using System;
using System.Collections;
using System.Collections.Specialized;
using Common.Utils.Log;
using System.Diagnostics;
using Server.Common.AdminAdapter;
using Server.Common.MessageItemClass;
using Server.Common.Messages;

namespace Common
{
    /// <summary>
    /// EdgePlusAdminRCManager에 대한 요약 설명입니다.
    /// </summary>
    public class ConfigField
    {
        public const string WIDTH = nameof(WIDTH);
        public const string HEIGHT = nameof(HEIGHT);
        public const string V1 = nameof(V1);
        public const string V2 = nameof(V2);
        public const string V3 = nameof(V3);
        public const string V4 = nameof(V4);
        public const string LOGO = nameof(LOGO);


    }

    public class MasterField
    {
        public const string SEQ = "SEQ";
        public const string REP = "REP";
        public const string DESC = "DESC";
        public const string START = "START";
        public const string STOP = "STOP";
        public const string TYPE = "TYPE";
        public const string RFIDPWR = "RFIDPWR";
        public const string LASTSEEN = "LASTSEEN";
        public const string HEIGHT = "HEIGHT";
        public const string STOPDF = "STOPDF";
        public const string STOPWAVE = "STOPWAVE";
        public const string WAVESCAN = "WAVESCAN";
        public const string SPD_H = "SPD_H";
        public const string MOVE_ = "MOVE_";
        public const string CMD_ARR = "CMD_ARR";
        public const string TIER = "TIER";
        public const string ANGLE = "ANGLE";
        public const string LENGTH = "LENGTH";
    }
}
