using Common;
using Common.Data.EPC;
using Common.Data.Objects;
using Common.Utils.Log;
using Common.Utils.Tools;
using eMapLibrary;
using Server.Common.MessageItemClass;
using Server.Common.Messages;
using Server.Formatter.CommonMessages;
using Server.ReceiveChannel.TCP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;

namespace HostingEmap
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("kernel32.dll")]
        extern public static void Beep(int freq, int dur);

        // Emap 관련 변수
        private eMapServer eMap;
        public string cameraPOS;
        public string eMap_Clicked;
        bool bTestData = false;
        public static TcpListenReceiveChannel _nChannel_Listen = new TcpListenReceiveChannel();
        string _sXMLPath = @"C:\eMap3D\emap.xml";
        const string _sPathDF = @"C:\TT_DRAGONFLY\PATHDATA\TEST.txt";
        const string _sPathDFTmp = @"C:\TT_DRAGONFLY\PathTemp.txt";
        public static string _baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string _exePath = System.Environment.GetCommandLineArgs()[0];
        public static string _appBaseDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);


        //범용변수
        static string _sLog = nameof(MainWindow);
        static ConsoleUtil _UTIL = new ConsoleUtil();
        DispatcherTimer _tmMsgPolling = new DispatcherTimer();
        public static string _sDivStringSlash = "/";
        public const string _sDivStrComma = ",";
        public const string _sDivStrColon = ":";
        List<string> _lsCamNodes = new List<string>(); //현재맵에서 카메라가 있는 노드만 기록한 배열, 3D 차트에서 해당 노드만 색깔과 텍스트를 컨트롤 하게 된다.
        public static string _sEMapXMLDir = @"C:\eMap3D\";
        public static string _sEMapBarImgDir = @"C:\eMap3D\BARIMG\";
        public static string _sEMapXMLFile = "emap.xml";
        public static string _sEMapXMLFullPath = _sEMapXMLDir + _sEMapXMLFile;
        public const string _sDivStrEMart = "`";
        public const string _sDivStrTab = "\t";
        public const char _chDivBar = ':';
        /// 노드 - 바코드1,바코드2,바코드3 형식으로 저장. RAW 데이터는 노드 중심으로 짜여져있음.
        Dictionary<string, string> dicInfo = new Dictionary<string, string>();

        /// 바코드 - 노드1,노드2,노드3 형식으로 저장.
        Dictionary<string, string> dicBarPrimaryInfo = new Dictionary<string, string>();

        public static Dictionary<string, string> _dicCurrentJobMasterRecord = new Dictionary<string, string>();
        public static DataView _dtMasterInfoView = new DataView();
        public static Dictionary<string, string> _dicConfig = new Dictionary<string, string>();
        public static DataTable _dtPath = new DataTable();
        List<string> _lsNewMaster = new List<string>();

        ///TCP 메세지 수신, 발신.
        /// 비동기 소켓 클라이언트
        //public static AsyncSocketClient _pAsyncSocketClient = null;
        public static bool _bICSRConnect = false;
        /// 비동기 소켓 서버
        //public static AsyncSocketServer _pAsyncSocketServer = null;
        public static string _sRecvBuffer = string.Empty;
        public static List<string> _lsTCPRecv = new List<string>();
        public static int _nID;
        /// 비동기 소켓 클라이언트 목록
        //public static List<AsyncSocketClient> _pAsyncSocketClientList = new List<AsyncSocketClient>();

        public static List<byte> _lsTCPBuffer = new List<byte>();
        public static List<string> _lsLogQueue = new List<string>();
        public static List<string> _lsErrorMsgList = new List<string>();

        List<Dictionary<string, string>> _lsDicArray = new List<Dictionary<string, string>>();
        List<string> lsMessage = new List<string>();
        DataTable _dtMaster = new DataTable();

        //2019 02 전시회 배치 (선반 5줄)
        //string sCameraPos_Init = "5.990322,1.927756,6.095122,8.006007,99.24512,0";
        //string sCameraPos_BigRack = "12.36747,0.7561774,3.962943,0.7500323,280.4957,0";
        //string sCameraPos_SmallRack = "10.98446,0.7561774,4.500902,359,187.9957,-6.671122E-09";
        //string sCameraPos_Top = "12.3494,5.094865,3.968554,65.24475,271.996,0";
        bool bInit = false;
        //2018 09 전시회 배치
        //string sCameraPos_Init = "5.842119,1.627855,5.519603,12.00816,216.2473,0";
        //string sCameraPos_BigRack = "3.269714,0.5922918,3.858808,359.9898,180.7484,0";
        //string sCameraPos_SmallRack = "2.824755,0.3481083,1.556059,357.7602,265.4985,0";
        //string sCameraPos_Top = "2.703658,5.653304,1.606037,77.25869,179.4973,0";

        //RF Blaster 지원
        int iListenPort = 65531;

        #region TCP 메세지 송수신 처리

        /// <summary>
        /// 데이터 초기화
        /// </summary>

        private static string _sRCNameListen = "EdgePlus"; // Receive Channel 이름, app.config에서 설정정보를 가져올때 쓰임.
        public static void Start()
        {
            try
            {
                Stop();

                // Part 1 : Write 채널
                if (_nChannel_Listen != null)
                {
                    _nChannel_Listen.Stop();
                    _nChannel_Listen = null;
                }

                // 1. Receive Hanlder 설정.
                MessageHandler[] handlers = new MessageHandler[2];
                MessageHandler rcvHandler, cmdHander;
                // Data받는 Handler
                rcvHandler = new MessageHandler();
                rcvHandler.Event += new MsgEvent(OnArrive);
                handlers[0] = rcvHandler;

                // Admin 상태 관리 Hadler
                cmdHander = new MessageHandler();
                cmdHander.Event += new MsgEvent(OnAdmin);
                handlers[1] = cmdHander;

                // 2. RC Parameter설정
                NameValueCollection param = ConfigurationManager.AppSettings;
                if (param == null)
                {
                    LogManager.Trace0(_sLog, "!!! Cannot find config to " + _sRCNameListen + ".");
                    return;
                }

                // 3. RC생성및 실행
                _nChannel_Listen = new TcpListenReceiveChannel();
                _nChannel_Listen.Initialize(param, handlers);
                _nChannel_Listen.Start();
                //_EdpAdmin.SuspendRC("*", "start");
            }
            catch (Exception ex)
            {
                LogManager.Trace0(_sLog, "Exception - Start() : {0}", ex.Message);
            }
        }

        public static void Stop()
        {
            try
            {
                _nChannel_Listen.Stop();
            }
            catch (Exception ex)
            {
                _nChannel_Listen = null;
                LogManager.Trace0(_sLog, "Exception - Stop() : {0}", ex.Message);
            }
        }

        public static void OnAdmin(object sender, MsgEventArgs e)
        {
            try
            {
                LogManager.Trace1(_sLog, "OnAdmin : status data from {0}", e.interfaceName);

                BasicIMMessageFormatter formatter = new BasicIMMessageFormatter();

                Stream streamMsg = (Stream)e.msg;
                IMMessage dmMsg = formatter.Deserialize(streamMsg);
                streamMsg.Seek(0, 0);

                if (dmMsg == null)
                {
                    LogManager.Trace0(_sLog, "OnAdmin : Unknown message");
                    return;
                }

                SystemCommandData cmdData = (SystemCommandData)dmMsg.GetDataObject(typeof(SystemCommandData));

                if (cmdData.Command.ToUpper() == "SETDEVICESTATE")
                {
                    Hashtable ht = (Hashtable)cmdData.Parameters;

                    string status = (string)ht["status"];
                    string devName = (string)ht["name"];

                    LogManager.Trace0(_sLog, "Device Name : {0}, Status : {1}", devName, status);
                    _bICSRConnect = status.Equals("Online");
                    //lbl_StatusConnect.Background = new SolidColorBrush(Colors.Green);
                }

            }
            catch (Exception err)
            {
                LogManager.Trace0(_sLog, "OnAdmin() Exception : {0}", err.Message);
                return;
            }
        }
        private static void OnArrive(object sender, MsgEventArgs e)
        {
            object[] args = new object[1];
            args[0] = e;
            Stream streamMsg = (Stream)e.msg;

            byte[] byData = new byte[streamMsg.Length];
            streamMsg.Read(byData, 0, byData.Length);
            streamMsg.Seek(0, 0);

            OnReceivedListen(byData);
            //string sData = Encoding.UTF8.GetString(byData).ToUpper().Replace("\r", string.Empty);
            //if (sData.IndexOf("R:") >= 0)
            //{
            //    //UpdateTagList("PDA", sData.Substring(sData.IndexOf("R:")), 10);
            //}
            //else if (sData.IndexOf("B:") >= 0)
            //{
            //    //UpdateTagList("PDA", sData.Substring(sData.IndexOf("B:")), 10);
            //}
            //else
            //{
            //    return;
            //}

        }

        #region 수신시 처리하기 - OnReceived(pSender, pAsyncSocketReceivedEventArgs)

        /// <summary>
        /// 수신시 처리하기
        /// </summary>
        /// <param name="pSender">이벤트 발생자</param>
        /// <param name="pAsyncSocketReceivedEventArgs">이벤트 인자</param>
        private static void OnReceivedListen(byte[] byData)
        {
            //UpdateMessage(_tbMessage, );
            //LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetClassName(pSender), "HOST -> PC : Connected ID :" + "HOST -> PC : Receive ID : " + pAsyncSocketReceivedEventArgs.ID.ToString() + ", Bytes received : " + pAsyncSocketReceivedEventArgs.ReceiveByteCount.ToString());

            //_lsTCPBuffer.AddRange(pAsyncSocketReceivedEventArgs.ReceiveData);
            //for (int i = 0; i < pAsyncSocketReceivedEventArgs.ReceiveData.Length; i++)
            //{
            //    if (pAsyncSocketReceivedEventArgs.ReceiveData[i] == (byte)('\0'))
            //    {
            //        break;
            //    }
            //    else
            //    {
            //        _lsTCPBuffer.Add(pAsyncSocketReceivedEventArgs.ReceiveData[i]);
            //    }
            //}
            _lsTCPBuffer.AddRange(byData);
            List<byte> lsTmp = new List<byte>(_lsTCPBuffer.ToArray());
            //_sRecvBuffer += sData;
            byte bySplitter = (byte)(13);
            List<byte[]> lsParsed = _UTIL.SplitByteArray(lsTmp.ToArray(), bySplitter);
            _lsTCPBuffer.Clear();

            foreach (byte[] byArTmp in lsParsed)
            {
                if (byArTmp == null)
                {
                    continue;
                }

                if (byArTmp[byArTmp.Length - 1] == bySplitter)
                {
                    string sReply = string.Empty;
                    //List<byte> lsCurrent = new List<byte>(byArTmp);
                    List<byte> lsCurrent = new List<byte>();

                    ///가독아스키문자만 남기고 다 걸러냄.
                    foreach (byte byTmp in byArTmp)
                    {
                        if (byTmp >= 32 && byTmp < 126)
                        {
                            lsCurrent.Add(byTmp);

                        }
                    }
                    string sData = Encoding.UTF8.GetString(byArTmp);
                    _lsTCPRecv.Add(sData);
                }
                else
                {
                    _lsTCPBuffer = new List<byte>(byArTmp);
                }
                //if (_sRecvBuffer.IndexOf("\n") > 0)
                //{
                //    sData = _sRecvBuffer;
                //    _lsTCPRecv.Add(sData);
                //    //_tbMessage.AppendText(_sRecvBuffer);
                //    //UpdateMessage(_tbMessage, _sRecvBuffer);
                //    _sRecvBuffer = string.Empty;
                //    LogManager.Trace2(_sLog, "{0} - Receive Data : {1}", _UTIL.GetMethodName(), sData);
                //}
                //else
                //{
                //    return;
                //}

                //if (_lsTCPRecv.Count != 0)
                //{
                //    List<string> lsRecv = new List<string>(_lsTCPRecv.ToArray());
                //    _lsTCPRecv.Clear();

                //    foreach (string sTmp in lsRecv)
                //    {
                //        string sReplyMsg = "ERROR";
                //        if (FormHMI.bPLCAlive)
                //        {
                //            sReplyMsg = "OK";
                //        }

                //        if (sTmp.ToUpper().StartsWith("PLC"))
                //        {
                //            ReplyTcpMsg(sReplyMsg, pAsyncSocketReceivedEventArgs.ID);
                //            _pAsyncSocketClientList[pAsyncSocketReceivedEventArgs.ID].Close();
                //        }
                //    }
                //}
            }
        }
        #endregion




        #endregion

        #region EPC 공통함수
        public static string GetEPCFilter(RFIDType strFilter)
        {
            if (strFilter == RFIDType.TYPE_1_SGTIN)
                return "01";
            if (strFilter == RFIDType.TYPE_2_CUSTOM)
                return "02";
            if (strFilter == RFIDType.TYPE_3_SGTIN)
                return "03";
            if (strFilter == RFIDType.TYPE_4_SGTIN)
                return "04";
            if (strFilter == RFIDType.TYPE_5_SGTIN)
                return "05";
            if (strFilter == RFIDType.TYPE_11_SSCC)
                return "11";

            return "99";
        }

        #endregion

        #region RFID 바코드 마스터 핸들링
        public static string _sProductMasterFULLPath = _appBaseDir + "\\MASTER\\ProductMaster.txt";
        //public static string _sNodeMasterFULLPath = _appBaseDir + "\\MASTER\\Node_Master.ini";
        public static bool ReloadMasterTable()
        {
            bool bFinalResult = true;
            try
            {
                /// 마스터 데이터 로드
                string[] saMaster = _UTIL.LoadTextFromFile(_sProductMasterFULLPath);
                if (saMaster.Length == 0)
                {
                    return false;
                }

                /// RFID_GUBUN 필드가 없는 마스터 레코드는 사용하지 않으므로 걸러낸다.
                string[] saMasterField = saMaster[0].Split(_sDivStrEMart.ToArray());

                int iIdxRFIDGUBUN = -1;

                /// RFID GUBUN 이 몇번째 컬럼에 위치하는지 뽑아냄.
                for (int i = 0; i < saMasterField.Length; i++)
                {
                    if (saMasterField[i].Equals(FieldNameReply.RFID_GUBUN))
                    {
                        iIdxRFIDGUBUN = i;
                        break;
                    }
                }

                List<string> lsNewMaster = new List<string>();
                for (int i = 0; i < saMaster.Length; i++)
                {
                    string[] saMasterFieldTmp = saMaster[i].Split(_sDivStrEMart.ToArray());
                    if (saMasterFieldTmp[iIdxRFIDGUBUN].Replace(" ", string.Empty).Equals(string.Empty) == false)
                    {
                        /// RFID_GUBUN 값이 존재하는 레코드만 읽어들인다.
                        lsNewMaster.Add(saMaster[i]);
                    }
                }
                /// RFID_GUBUN 필드 마스터 필터링 루틴 끝
                bFinalResult = true;
            }
            catch (Exception ex)
            {
                LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetMethodName(), ex.ToString());
                bFinalResult = false;
            }
            return bFinalResult;
        }

        #endregion

        #region 유틸리티 함수 (추후 별도 프로젝트로 분리 WPFUTIL 뭐 이런식)
        public static ArrayList SetTextBoxesWPF(DependencyObject depObj, string sTag, Dictionary<string, string> dicSource)
        {
            ArrayList al = new ArrayList();
            // Confirm obj is valid. 
            if (depObj == null) return al;
            int iCntCtrls = VisualTreeHelper.GetChildrenCount(depObj);
            for (int i = 0; i < iCntCtrls; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (((FrameworkElement)child).Name.ToString().StartsWith(sTag))
                {
                    al.Add(child);
                    string sCtrlTag = ((FrameworkElement)child).Tag.ToString();
                    if (dicSource.ContainsKey(sCtrlTag))
                    {
                        ((System.Windows.Controls.TextBox)child).Text = dicSource[sCtrlTag];
                    }
                    else if (dicSource.Count == 0)
                    {
                        ((System.Windows.Controls.TextBox)child).Text = string.Empty;
                    }
                }
            }
            return al;
        }

        /// <summary>
        /// 지정된 컨트롤에서 특정 스트링이 포함된 이름을 갖는 
        /// 컨트롤개체들을 반환합니다.
        /// 유사이름 판단 기준은 IndexOf 입니다.
        /// </summary>
        /// <param name="TagName">검색할인스턴스Name</param>
        /// <param name="controlName">컨트롤들이 포함되어있는그룹이름</param>
        /// <returns>ArrayList 형태로 리턴</returns>
        public ArrayList GetObjByNameWPF(string TagName, DependencyObject controlName, bool bRecursive)
        {
            ArrayList ObjList = new ArrayList();
            if (controlName != null)
            {
                int iCntCtrls = VisualTreeHelper.GetChildrenCount(controlName);
                for (int i = 0; i < iCntCtrls; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(controlName, i);
                    string ObjName = ((FrameworkElement)child).Name;
                    if (ObjName == null)
                    {
                        continue;
                    }

                    if (ObjName.ToUpper().IndexOf(TagName.ToUpper()) > -1)
                    {
                        ObjList.Add(child);
                    }

                    if (bRecursive)
                    {
                        ObjList.AddRange(GetObjByNameWPF(TagName, child, bRecursive));
                    }
                }
            }
            return ObjList;
        }
        /// <summary>
        /// 지정된 컨트롤에서 특정 스트링이 포함된 이름을 갖는 
        /// 컨트롤개체들을 반환합니다.
        /// 유사이름 판단 기준은 IndexOf 입니다.
        /// </summary>
        /// <param name="TagName">검색할인스턴스Name</param>
        /// <param name="controlName">컨트롤들이 포함되어있는그룹이름</param>
        /// <returns>ArrayList 형태로 리턴</returns>
        public ArrayList GetObjByNameWPF(string TagName, DependencyObject controlName)
        {
            return GetObjByNameWPF(TagName, controlName, false);
        }

        /// <summary>
        /// 지정된 컨트롤에서 특정 스트링과 유사한 태그를 갖는 
        /// 컨트롤개체들을 반환합니다.
        /// 유사이름 판단 기준은 IndexOf 입니다.
        /// </summary>
        /// <param name="TagName">검색할태그이름</param>
        /// <param name="controlName">컨트롤들이 포함되어있는그룹이름</param>
        /// <returns>ArrayList 형태로 리턴</returns>
        public ArrayList GetObjByTagWPF(string TagName, DependencyObject controlName, bool bExactMatch)
        {
            ArrayList ObjList = new ArrayList();
            int iCntCtrls = VisualTreeHelper.GetChildrenCount(controlName);
            for (int i = 0; i < iCntCtrls; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(controlName, i);
                object ObjName = ((FrameworkElement)child).Tag;
                if (ObjName == null)
                {
                    continue;
                }
                string objCurrent = ObjName.ToString().ToUpper();
                bool bAdd = bExactMatch ? objCurrent.Equals(TagName.ToUpper()) : objCurrent.IndexOf(TagName.ToUpper()) > -1;
                if (bAdd)
                {
                    ObjList.Add(child);
                }

            }
            return ObjList;
        }

        public ArrayList GetObjByTagWPF(string TagName, DependencyObject controlName)
        {
            return GetObjByTagWPF(TagName, controlName, false);
        }

        /// <summary>
        /// 지정한 그룹박스에 있는 컨트롤의 문자열 혹은 태그값을 검색하여 조건에 맞는 컨트롤들의 Text 를 sFillString 으로 바꾼다
        /// </summary>
        /// <param name="gb"></param>
        /// <param name="sNameTag"></param>
        /// <param name="bUseControlName"></param>
        /// <param name="sFillString"></param>
        public void FillTextBoxesWPF(DependencyObject gb, string sNameTag, bool bUseControlName, string sFillString)
        {
            ArrayList al = new ArrayList();

            if (bUseControlName)
            {
                al = GetObjByNameWPF(sNameTag, gb);
            }
            else
            {
                al = GetObjByTagWPF(sNameTag, gb);
            }

            foreach (System.Windows.Controls.TextBox ctlTmp in al)
            {
                if (ctlTmp.Name.ToUpper().IndexOf(sNameTag.ToUpper()) >= 0)
                {
                    ctlTmp.Text = sFillString;
                }
            }
        }

        public void FillTextBoxes(IDictionary ht, DependencyObject gb, bool bUseControlName, bool bUseLabelText)
        {
            FillTextBoxesWPF(ht, gb, bUseControlName, bUseLabelText, false);
        }

        /// <summary>
        /// 지정한 그룹박스에 있는 컨트롤을 태그값으로 검색하여
        /// 조건이 맞으면 해쉬테이블의 값을 채워준다. - 수정필요
        /// </summary>
        /// <param name="ht">채우고자하는 데이터가 있는 해쉬테이블</param>
        /// <param name="gb">채우고자하는 컨트롤들이 있는 그룹박스</param>
        public void FillTextBoxesWPF(IDictionary ht, DependencyObject gb, bool bUseControlName, bool bUseLabelText, bool bExactMatch)
        {
            foreach (string tmp in ht.Keys)
            {
                ArrayList al = new ArrayList();

                if (bUseControlName)
                {
                    al = GetObjByNameWPF(tmp, gb); //현재 이름기반 검색은 exactmatch 미구현. 시간나면 구현하자-_- 20180817
                }
                else
                {

                }
                {
                    al = GetObjByTagWPF(tmp, gb, bExactMatch);
                }

                if (al.Count != 0)
                {
                    foreach (System.Windows.Controls.Control findcontrol in al)
                    {
                        try
                        {
                            bool bFilled = false;
                            string sCurVal = ht[tmp].ToString();
                            try
                            {
                                for (int i = 0; i < ((System.Windows.Controls.ComboBox)findcontrol).Items.Count; i++)
                                {
                                    if (((System.Windows.Controls.ComboBox)findcontrol).Items[i].ToString().IndexOf(sCurVal) > -1)
                                    {
                                        ((System.Windows.Controls.ComboBox)findcontrol).SelectedIndex = i;
                                        bFilled = true;
                                    }
                                }
                            }
                            catch (Exception ex2)
                            {
                                LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetMethodName(), ex2.ToString());
                            }

                            try
                            {
                                if (!bFilled)
                                {
                                    ((System.Windows.Controls.TextBox)findcontrol).Text = sCurVal;
                                    bFilled = true;
                                }
                            }
                            catch (Exception ex3)
                            {
                                LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetMethodName(), ex3.ToString());
                            }

                            //try
                            //{
                            //    if (!bFilled && bUseLabelText)
                            //    {
                            //        //((System.Windows.Controls.Control)findcontrol) = sCurVal;
                            //        //bFilled = true;
                            //    }
                            //}
                            //catch (Exception ex3)
                            //{
                            //}

                        }
                        catch (Exception ex)
                        {
                            LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetMethodName(), ex.ToString());
                        }
                    }
                }
            }
        }

        #endregion



        string[] GetBarImgFiles(string sFilter)
        {
            List<string> lsReturn = new List<string>();
            if (Directory.Exists(_sEMapBarImgDir))
            {
                List<string> lsTmp = new List<string>(Directory.GetFiles(_sEMapBarImgDir, "*.jp*"));
                foreach (string sFileTmp in lsTmp)
                {
                    string sFileName = _UTIL.GetLastString(_UTIL.GetFileNameFromFullFileName(sFileTmp), "\\");
                    if (sFileName.IndexOf(sFilter) >= 0)
                    {
                        lsReturn.Add(sFileTmp);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(_sEMapBarImgDir);
            }
            return lsReturn.ToArray();
        }

        void RackCellClicked(string sClickInfo)
        {
            try
            {
                string[] saClickInfo = sClickInfo.Split(",".ToArray());
                //string sRackID = saClickInfo[0].PadLeft(4,'0');
                string sRackID = saClickInfo[0];
                string sTierID = saClickInfo[1];
                string sTargetNode = $"{sTierID}_{sRackID}";
                int iTierCurrent = int.Parse(sTierID);

                //eMap.setEffectColor("#FFFF007F");
                //eMap.ObjectEffectTurnOn(sRackID, iTier);
                string sLocation = txt_ClickEvent.Text = $"{sTierID}_{sRackID}";
              
                if (sRackID.IndexOf("Floor") < 0 && sRackID.ToUpper().IndexOf("WALL") <= 0)
                {
                    //foreach (string sNodeTmp in _lsCamNodes)
                    //{
                    //    if (sNodeTmp.Equals(sTargetNode))
                    //    {
                    //        string[] saNodeCnt = dicInfo[sNodeTmp].Split(_sDivStrColon.ToCharArray());
                    //        eMap.ObjectEffectTurnOn(sRackID, iTierCurrent);
                    //        eMap.changeCaptionContent(sRackID, iTierCurrent, saNodeCnt.Length.ToString());
                    //        eMap.changeTierEffect(sRackID, iTierCurrent, "#FF000064");
                    //    }
                    //    else
                    //    {
                    //        eMap.ObjectEffectTurnOff(sRackID, iTierCurrent);
                    //        eMap.changeCaptionContent(sRackID, iTierCurrent, string.Empty);
                    //    }
                    //}
                    Set_State(false);
                    Set_State(sTargetNode, true, dicInfo[sTargetNode].Split(_sDivStrColon.ToCharArray()).Length.ToString());
                    //eMap.setEffectColor("#FFFF007F");
                    //eMap.ObjectEffectTurnOn(sRackID, iTier);
                    //eMap.changeTierEffect(sRackID, iTier, "#FF000064");
                    //eMap.changeCaptionContent(sRackID, iTier, (cmb_SN.Items.Count - 1).ToString());
                    //foreach (string sKey in cmb_Location.Items)
                    //{
                    //    string sNode = sKey.ToString();
                    //    if (sNode.EndsWith(sRackID))
                    //    {
                    //        cmb_Location.SelectedItem = sKey;
                    //        if (cmb_Ant.Items.Contains(sTierID))
                    //        {
                    //            cmb_Ant.SelectedItem = sTierID;
                    //            eMap.ObjectEffectTurnOn(sRackID, iTier);
                    //            eMap.changeTierEffect(sRackID, iTier, "#FF000064");
                    //            string sCount = (cmb_SN.Items.Count - 1).ToString();
                    //            eMap.changeCaptionContent(saClickInfo[0], int.Parse(sTierID), sCount);
                    //        }
                    //        break;
                    //    }
                    //}
                    
                    string[] saBarImg = GetBarImgFiles(sLocation);
                    if (saBarImg.Length > 0)
                    {
                        FrmImgViewer fv = new FrmImgViewer(saBarImg);
                        fv.ShowDialog();
                    }
                    else
                    {
                        txt_ClickEvent.Text += " No Images.";
                    }
                }
                

                //else
                //{
                //    //string[] files = Directory.GetFiles(@"C:\PythonLec\Images\1회차_가득_조명ON\");
                //    //FrmImgViewer fiv = new FrmImgViewer(files);
                //    //fiv.ShowDialog();
                //}
            }
            catch (Exception ex)
            {
                LogManager.Trace0(_sLog, "{ 0}-{1}", _UTIL.GetMethodName(), ex.ToString());
            }
        }

        void InitEMap(string v)
        {
            try
            {
                _lsNewMaster = new List<string>(File.ReadAllLines(_sPathDFTmp));
                _dtPath = _UTIL.Strings2DataTable(_lsNewMaster.ToArray(), true, "\t");
                _dtMaster.Clear();

                bTestData = false;
                //eMap.rotateDragnFly(90);
                eMap.SetCameraLock(true);
                eMap.SetCameraPos(_dicConfig[ConfigField.V1]);
                eMap.SetShadowMode(false);
                //Set_NORMAL_Status_ALL(true);
                bInit = true;
                eMap.loadMapData(_sEMapXMLFullPath);
                LogManager.Trace2(_sLog, "{0} - {1}", "초기화 완료!", _UTIL.GetTimeString(new DateTime(long.Parse(v))));
            }
            catch (Exception ex)
            {
                LogManager.Trace0(_sLog, "{0}-{1}", _UTIL.GetMethodName(), ex.ToString());
            }
        }


        string _sImgPath = string.Empty;
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _dicConfig = _UTIL.Hash2Dic(_UTIL.LoadOptionFromFile(sOptionFilePath));
                string sLogo = _dicConfig[ConfigField.LOGO];
                
                _sImgPath = $"{Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName}\\IMAGES\\{sLogo}";

                //Path.GetFullPath(Application.ExecutablePath);
                ImageSource isLogo = new BitmapImage(new Uri(_sImgPath));
                pic_Title.Source = isLogo;
                Process[] proc_eMap = Process.GetProcessesByName("eMap3D");
                if (proc_eMap != null)
                {
                    for (int i = 0; i < proc_eMap.Length; i++)
                    {
                        proc_eMap[i].Kill();
                    }
                }

                Process prcCurrent = Process.GetCurrentProcess();
                Process[] prcCurrentAll = Process.GetProcesses();
                foreach (Process prcTmp in prcCurrentAll)
                {
                    if (prcTmp.ProcessName.Equals(prcCurrent.ProcessName))
                    {
                        if (prcTmp.Id.Equals(prcCurrent.Id) == false)
                        {
                            prcTmp.Kill();
                        }
                    }
                }
                //_UTIL.SaveOptionFromFile(Dic2Hash(dicTmp), sOptionFilePath, "`");

                //appControl.startPosX = 0;
                //appControl.startPosY = 50;
                appControl.ExeName = @"C:\eMap3D\eMap3D.exe";

                //appControl.ExeName = @"C:\eMap3D\eMap.exe";
                //appControl.connectString = "127.0.0.1, 9999"; //명령줄인수 서버ip,port 입력 default loopback 9999번 포트
                System.Windows.Application.Current.Exit += new ExitEventHandler((s, e) => { appControl.Dispose(); });

                eMap = new eMapServer();
                var listener = new eMapListener();
                listener.selectedAction += (v =>
                {
                    eMap_Clicked = v.Replace(" ", "");
                    LogManager.Trace2(_sLog, "{0} - {1}", nameof(eMap_Clicked), eMap_Clicked);
                //eMap.setEffectColor(255, 255, 0, 128); //20180912 현재로서는 전체 색깔 변경 밖에 안됨. 셀별로 색상 지정 가능하게 요청.

                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        RackCellClicked(eMap_Clicked);
                    }), DispatcherPriority.ApplicationIdle);
                //MessageBox.Show(eMap_Clicked);
            });

                listener.cameraPosAction += (v =>
                {
                    this.cameraPOS = v;
                    LogManager.Trace2(_sLog, "{0} - {1}", nameof(cameraPOS), cameraPOS);
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        txt_CameraPos.Text = v;
                    }), DispatcherPriority.ApplicationIdle);

                });
                listener.InitFinishedAction += (v =>
                {
                    InitEMap(v);
                });
                eMap.addeMapListener(listener);
                InitCmbBox();
                int port = 9999;
                string ip = "127.0.0.1";

                eMap.init(ip, port);

                //this.Unloaded += new RoutedEventHandler((s, e) => { appControl.Dispose(); });
                this.DataContext = this;

                ReloadMasterTable();
                Start();
                
                appControl.startPosX = 250;
                appControl.startPosY = 0;

                this._tmMsgPolling.Interval = TimeSpan.FromMilliseconds(300);
                this._tmMsgPolling.Tick += new System.EventHandler(_tmMsgPolling_Tick);
                _tmMsgPolling.Start();
                MapInfoInit();
                
                //while (bInit == false)
                //{
                //    Thread.Sleep(100);
                //}
                //btn_Reload_Click(null, null);
            }
            catch (Exception ex)
            {
                LogManager.Trace0(_sLog, "{0}\r\n{1}", _UTIL.GetMethodName(), ex.ToString());
                System.Windows.MessageBox.Show(ex.ToString());
            }

        }

        bool _bEMAPSPG = true;
        Hashtable Dic2Hash(IDictionary id)
        {
            Hashtable ht = new Hashtable();
            foreach (string sKey in id.Keys)
            {
                ht[sKey] = id[sKey];
            }
            return ht;
        }

        string sMetaFilePath = @"C:\eMap3D\appBin\optionMeta.ini";
        string sOptionFilePath = @"C:\eMap3D\appBin\optionConfig.ini";
        string _sPreviousMsg = string.Empty;
        private void _tmMsgPolling_Tick(object sender, EventArgs e)
        {
            UpdateConnectionStatus();
            lsMessage = new List<string>(_lsTCPRecv.ToArray());

            string sResult = string.Empty;
            string saTagList = string.Empty;

            //string sWebPath = @"Z:\WWW\test.txt";
            //if (File.Exists(sWebPath))
            //{
            //    saTagList = File.ReadAllText(sWebPath);
            //    if (saTagList.Equals(_sPreviousMsg) == false)
            //    {
            //        string[] saData = saTagList.Split(" ".ToCharArray());
            //        string sTmp = string.Format("B`{0},V`{1},AUTOSTART`{2},HOUR`{3},MIN`{4}", saData[0], saData[1], saData[2], saData[3], saData[4]);
            //        Dictionary<string,string> dicTmp = _UTIL.Strings2Dic(sTmp.Split(",".ToCharArray()), "`");

            //        _UTIL.SaveOptionFromFile(Dic2Hash(dicTmp), sOptionFilePath, "`");

            //        string sMsgStatus = string.Format("STATUS/{0}", sTmp);
            //        _sPreviousMsg = saTagList;
            //        lsMessage.Add(sMsgStatus);
            //    }
            //}

            if (lsMessage.Count == 0)
            {
                return;
            }
            else
            {
                _lsTCPRecv.Clear();
            }

            Dictionary<string, string> dicUpdate = new Dictionary<string, string>();
            foreach (string sMsg in lsMessage)
            {
                LogManager.Trace0(_sLog, "MSG receiverd : {0}\r\n{1}", _UTIL.GetMethodName(), sMsg);
                string sMsgTmp = _UTIL.GetLastString(sMsg, _sDivStringSlash).Replace("\r\n", string.Empty);


                if (sMsg.StartsWith("STATUS"))
                {
                    dicUpdate = _UTIL.Strings2Dic(sMsgTmp.Split(_sDivStrComma.ToArray()), _sDivStrEMart);

                    if (dicUpdate.ContainsKey("B"))
                    {
                        if (dicUpdate["B"].StartsWith("1"))
                        {
                            dicUpdate["B"] = "ON";
                        }
                        else
                        {
                            dicUpdate["B"] = "OFF";
                        }
                    }
                    if (dicUpdate.ContainsKey("AUTOSTART"))
                    {
                        if (dicUpdate["AUTOSTART"].StartsWith("1"))
                        {
                            dicUpdate["AUTOSTART"] = "ON";
                        }
                        else
                        {
                            dicUpdate["AUTOSTART"] = "OFF";
                        }
                    }

                    //System.Windows.Forms.Control ctl = new System.Windows.Forms.Control();
                    var ds = SetTextBoxesWPF(this.gb_Alive, "t", dicUpdate);
                    ///_UTIL.FillTextBoxes(dicUpdate, , false, true, true);

                    if (dicUpdate.ContainsKey("LASTSEEN"))
                    {
                        string sSEQ = dicUpdate["LASTSEEN"];
                        int iChk = -1;
                        bool bValidNode = int.TryParse(sSEQ, out iChk);
                        //string sCurrentCol = _dicNodeInfo[sSEQ];
                        //if (sSEQ.Equals("0002"))
                        //{
                        //    sSEQ = "0201";
                        //}

                        if (bValidNode)
                        {
                            Set_DF_Status("9" + sSEQ);
                        }
                    }

                    //if(dicUpdate.ContainsKey("SEQ"))
                    //{
                    //    string sSEQ = dicUpdate["SEQ"];
                    //    if (sSEQ.Equals("1000"))
                    //    {
                    //        InitEMap(DateTime.Now.Ticks.ToString());
                    //    }
                    //}
                }
                else if (sMsg.StartsWith("EMAP"))
                {
                    try
                    {
                        continue;
                        _bEMAPSPG = sMsg.StartsWith("EMAPSPG");

                        string[] saAlarm = sMsg.Split("/".ToCharArray());
                        string[] saPathData = _UTIL.SplitString(saAlarm[1], ",");
                        File.WriteAllLines(_sPathDFTmp, saPathData);
                        //Common.eMap.XMLEdit(_sXMLPath, _sPathDFTmp);
                        XMLEdit(_sXMLPath, _sPathDFTmp);

                        //sMsgTmp = sMsgTmp.Replace("\r", string.Empty);
                        ////tBox_Message.AppendText(sMsgTmp);
                        //string[] saAlarm = sMsg.Split("/".ToCharArray());
                        ////MessageBox.Show(saAlarm[1], saAlarm[0]);
                        //string sSourceFileFullPath = _sEMapXMLDir + sMsgTmp + _sEMapXMLFile;
                        //File.Copy(sSourceFileFullPath, _sEMapXMLFullPath, true);
                    }
                    catch (Exception ex)
                    {
                        LogManager.Trace0(_sLog, "{0}\r\n{1}", _UTIL.GetMethodName(), ex.ToString());
                    }
                    eMap.clearMap();
                    eMap.loadMapData(_sEMapXMLFullPath);
                    ReloadEmapAngle();
                }
                else if (sMsg.StartsWith("ALARM"))
                {
                    //tBox_Message.AppendText(sMsgTmp);
                    string[] saAlarm = sMsg.Split("/".ToCharArray());
                    System.Windows.MessageBox.Show(saAlarm[1], saAlarm[0]);
                }
                else if (sMsg.StartsWith("MSG"))
                {
                    //tBox_Message.AppendText(sMsgTmp);
                    //InitEMap(DateTime.Now.Ticks.ToString());
                }
                else if (sMsg.StartsWith("KK"))
                {

                    string[] saKiosk = sMsg.Split("/".ToCharArray());
                    string sTagID = _UTIL.GetASCIIFromString(saKiosk[1]);
                    string sExpression = string.Format("{0} = '{1}'", "EPC", sTagID);
                    string sDesc = "_ROWID ASC";
                    if (_dtMaster.Rows.Count == 0)
                    {
                        Button_Click(null, new RoutedEventArgs());
                        _lsTCPRecv.Add(sMsg);
                        return;
                    }
                    DataTable dtTmp1 = _UTIL.SelectFromDataTable(_dtMaster, sExpression, string.Empty);

                    //위치정보까지 다 들어있음
                    string[] saMsg = new string[1] { "KK" + _sDivStrEMart };
                    if (dtTmp1.Rows.Count > 0)
                    {
                        saMsg[0] += "제품을 인식하였습니다.";
                        Dictionary<string, string> dicCurrentTagInfo = _UTIL.DataTable2DicArray(dtTmp1)[0];
                        string sNode = dicCurrentTagInfo["Node"];
                        string sAnt = dicCurrentTagInfo["Ant"];

                        RackCellClicked($"{sNode},{sAnt}");
                        if (_dicXMLMap.ContainsKey(sNode))
                        {
                            string sValue = _dicXMLMap[sNode];
                            string[] saValue = sValue.Split(",".ToCharArray());
                            float iPosX = float.Parse(saValue[0]) / 100;
                            float iPosZ = float.Parse(saValue[1]);
                            float iAngle = float.Parse(saValue[2]);
                            int iDistance = 400;
                            if (iAngle >= 180)
                            {
                                iPosZ = iPosZ + iDistance;
                            }
                            else
                            {
                                iPosZ = iDistance - iPosZ;
                            }
                            iPosZ = iPosZ / 100;
                            string sViewStr = $"{iPosX:F3},0.93,{iPosZ:F3},0,{iAngle:F3},0.000";
                            eMap.SetCameraPos(sViewStr);
                        }
                        else
                        {
                            eMap.SetCameraPos(_dicConfig[ConfigField.V1]);
                        }

                    }
                    else
                    {
                        saMsg[0] += "해당 제품을 찾을 수 없습니다.";
                    }
                    File.WriteAllLines($"{_sEMapXMLDir}ipc.ini", saMsg);
                }
                else if (sMsg.StartsWith("BARLIST"))
                {
                    List<string> lsEPC = new List<string>();
                    
                    //로그 기록
                    //_lsDicArray.Clear();
                    //_dtMaster.Clear();
                    //DATATABLE 구성, 조회 버튼 활성화.
                    string[] saDT = sMsgTmp.Split(_sDivStrEMart.ToArray()); //
                    dicInfo.Clear();
                    dicBarPrimaryInfo.Clear();
                    foreach (string sRaw in saDT) //880643512345,4_0010
                    {
                        if (sRaw.Equals(string.Empty))
                        {
                            continue;
                        }
                        string sBarRAW = _UTIL.GetIndexString(sRaw, _sDivStrComma, 0);
                        string[] sBarArray = sBarRAW.Split(_chDivBar);
                        string Location = _UTIL.GetIndexString(sRaw, _sDivStrComma, 1);
                        dicInfo[Location] = sBarRAW;

                        foreach (string sBarCD in sBarArray)
                        {
                            if (dicBarPrimaryInfo.ContainsKey(sBarCD))
                            {
                                dicBarPrimaryInfo[sBarCD] += $"{_sDivStrComma}{Location}";
                            }
                            else
                            {
                                dicBarPrimaryInfo.Add(sBarCD, Location);
                            }
                        }
                    }
                    string sResult2 = _UTIL.KeyValue2String(dicInfo, ":");
                    LogManager.Trace0(_sLog, "{0}\r\n{1}\r\n{2}", _UTIL.GetMethodName(), _UTIL.KeyValue2String(dicInfo, ":"), _UTIL.KeyValue2String(dicBarPrimaryInfo, ":"));

                    if (dicBarPrimaryInfo.Count > 0)
                    {
                        Set_State();
                        //_dtMaster = _UTIL.DicArray2DataTable(_lsDicArray.ToArray());
                        //SetComboBoxItems(_dtMaster);
                    }
                }
            }
            sResult = _UTIL.StringMerge(lsMessage.ToArray(), "\r\n");
            //tBox_Message.AppendText(sResult);
            lsMessage.Clear();
        }


        static Point GetDFPointer(int iCurrentAngle, int iDFMarginSpace, Point dfCenter)
        {
            double iCurrentX_PT, iCurrentZ_PT = 0;
            if (iCurrentAngle == 0) // 0도의 정의 - Right 으로 주행했을때 X축 값이 증가하는 상태
            {
                iCurrentX_PT = dfCenter.X;
                iCurrentZ_PT = dfCenter.Y + iDFMarginSpace;
            }
            else if (iCurrentAngle == 90) // 90도의 정의 - Right 으로 주행했을때 Z축 값이 감소하는 상태.
            {
                iCurrentX_PT = dfCenter.X + iDFMarginSpace;
                iCurrentZ_PT = dfCenter.Y;
            }
            else if (iCurrentAngle == 180)// 180도의 정의 - Right 으로 주행했을때 X축 값이 증가하는 상태.
            {
                iCurrentX_PT = dfCenter.X;
                iCurrentZ_PT = dfCenter.Y - iDFMarginSpace;
            }
            else// 270도의 정의 - Right 으로 주행했을때 Z축 값이 증가하는 상태.
            {
                iCurrentX_PT = dfCenter.X - iDFMarginSpace;
                iCurrentZ_PT = dfCenter.Y;
            }
            return new Point((int)iCurrentX_PT, (int)iCurrentZ_PT);
        }

        static Point getMidpoint(Point x1, Point x2) //두 점사이의 중점을 구하는 메서드
        {
            Point result = new Point((x1.X + x2.X) / 2, (x1.Y + x2.Y) / 2);
            return result;
        }
        static Point GetNextPoint(int iCurrentAngle, bool bRightDirection, int iNodeLength, Point ptOld)
        {
            double iCurrentX = ptOld.X;
            double iCurrentZ = ptOld.Y;

            //안테나 포인터 좌표 계산 - 안테나 포인터의 궤적이 선반 위치가 된다.
            //이 부분을 잘 반영해야 함. DF의 두께와 너비를 반영.
            if (iCurrentAngle == 0) // 0도의 정의 - Right 으로 주행했을때 X축 값이 증가하는 상태
            {
                if (bRightDirection)
                {
                    iCurrentX += iNodeLength;
                }
                else
                {
                    iCurrentX -= iNodeLength;
                }
            }
            else if (iCurrentAngle == 90) // 90도의 정의 - Right 으로 주행했을때 Z축 값이 감소하는 상태.
            {
                if (bRightDirection)
                {
                    iCurrentZ -= iNodeLength;
                }
                else
                {
                    iCurrentZ += iNodeLength;
                }
            }
            else if (iCurrentAngle == 180)// 180도의 정의 - Right 으로 주행했을때 X축 값이 감소하는 상태.
            {
                if (bRightDirection)
                {
                    iCurrentX -= iNodeLength;
                }
                else
                {
                    iCurrentX += iNodeLength;
                }
            }
            else// 270도의 정의 - Right 으로 주행했을때 Z축 값이 증가하는 상태.
            {
                if (bRightDirection)
                {
                    iCurrentZ += iNodeLength;
                }
                else
                {
                    iCurrentZ -= iNodeLength;
                }
            }
            return new Point((int)iCurrentX, (int)iCurrentZ);
        }

        int _iObjType = 2;
        Dictionary<string, int> _dicAngle = new Dictionary<string, int>();
        Dictionary<string, int> _dicStart = new Dictionary<string, int>(); //Start 의 중복 여부 체크한다.
        string GetValueFromDic(IDictionary kp, string sKey)
        {
            if(kp.Contains(sKey))
            {
                return kp[sKey].ToString();
            }
            return string.Empty;
        }
        /// <summary>
        /// 스파이더고 맵을 그리기 위한 함수
        /// 2021-11-23 최병진
        /// 1. 같은 START ID를 읽어들임.
        /// 2. 시작위치는 이전위치에서 MOVE 코드로 LENGTH 만큼 이동한 곳, Angle 반영할 것.
        /// 3. 높이는 D 커맨드 가진 노드의 합계
        /// 4. 셀 수는 D 커맨드 가진 노드의 수.
        /// 5. 너비는 UI 바인딩
        /// </summary>
        /// <param name="sEmapPath"></param>
        /// <param name="lsObjSmall"></param>
        /// <param name="lsLinkListSmall"></param>
        /// <param name="lsNodeListSmall"></param>
        /// <param name="lsCaptionListSmall"></param>
        /// <param name="iMaterialType"></param>
        /// <param name="iCellWidth"></param>
        /// <returns></returns>
        private bool GenerateEMapExSPG2(string sEmapPath, out List<dataObjectList> lsObjSmall, out List<dataLinkList> lsLinkListSmall, out List<dataNodeList> lsNodeListSmall, out List<Caption> lsCaptionListSmall, int iMaterialType, int iCellWidth, int iCellDepth, int iObjType)
        {
            bool bReturn = true;
            try
            {
                Dictionary<string, dataObjectList> dicEmap = new Dictionary<string, dataObjectList>();

                dataNodeList dnOrigin = new dataNodeList();
                
                //int iMaterialType = 8;
                //int iBlockWidth = 53;

                int iDFMarginSpace = iCellWidth;
                //int iCellDepth = 15;
                //int iCellHeight = 53;

                bool bParseResult = false;
                //bParseResult = int.TryParse(txt_DFMargin.Text, out iDFMarginSpace);
                //if (!bParseResult)
                //{
                //    iDFMarginSpace = 45;
                //    txt_DFMargin.Text = iDFMarginSpace.ToString();
                //}
                //bParseResult = int.TryParse(txt_CellDepth.Text, out iCellDepth);
                //if (!bParseResult)
                //{
                //    iCellDepth = 30;
                //    txt_CellDepth.Text = iCellDepth.ToString();
                //}

                lsObjSmall = new List<dataObjectList>();
                lsNodeListSmall = new List<dataNodeList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();

                int iCurrentAngle = 180; //시작시 뒤집혀져있음
                Point ptDF_Center_Old = new Point(0, 0); //시작 지점.
                dnOrigin.posX = ptDF_Center_Old.X;  //데이터노드 - 시작지점
                dnOrigin.posZ = ptDF_Center_Old.Y;
                dnOrigin.posY = 0;
                dnOrigin.id = "99999";
                dnOrigin.type = "0";
                Point ptDF_AntPoint = new Point();
                lsNodeListSmall.Add(dnOrigin);
                string[] saEmapInfo = File.ReadAllLines(sEmapPath);
                List<Dictionary<string, string>> lsDicEmap = new List<Dictionary<string, string>>(_UTIL.Strings2DicArray(saEmapInfo, true, "\t"));

                List<Dictionary<string, string>> lsDicNew = new List<Dictionary<string, string>>();
                Dictionary<string, string> dicReturn = new Dictionary<string, string>();
                int iCntNode = 0;
                _dicStart.Clear();
                string sStartID = string.Empty;
                /// 경로정보를 한줄씩 읽는다.
                /// 필요한 정보 : D 노드의 갯수, D노드의 LENGTH 누적 합.
                /// L이나 R 이 있는 경우 Angle과 LENGTH 를 뽑아야 됨.
                int iHeightH = 0; /// 가로 이동길이 합
                int iHeightV = 0; /// 세로 높이
                int i_DNodeCnt = 0; // D 노드 갯수 (몇단인지 결정)
                int iAngle = 0; // 노드배열 중 가장 큰 Angle 값

                foreach (Dictionary<string, string> dicCurrentTmp in lsDicEmap)
                {
                    string sSTART = dicCurrentTmp[MasterField.START];
                    string sMOVE_ = dicCurrentTmp[MasterField.MOVE_];
                    string sTIER = dicCurrentTmp[MasterField.TIER];
                    string sANGLE = dicCurrentTmp[MasterField.ANGLE];
                    string sLENGTH = dicCurrentTmp[MasterField.LENGTH];
                    if (sStartID.Equals(sSTART) || sStartID.Equals(string.Empty)) ///StartID 가 같으면 추가
                    {
                    }
                    else
                    {
                        Dictionary<string, string> dicTmp = new Dictionary<string, string>();
                        dicTmp.Add(MasterField.START, sStartID);
                        dicTmp.Add(MasterField.ANGLE, iAngle.ToString());
                        dicTmp.Add(MasterField.HEIGHT, iHeightV.ToString());
                        dicTmp.Add(MasterField.TIER, i_DNodeCnt.ToString());
                        dicTmp.Add(MasterField.LENGTH, iHeightH.ToString());
                        lsDicNew.Add(dicTmp);
                        iHeightH = iHeightV = i_DNodeCnt = iAngle = 0;
                    }
                    sStartID = sSTART; //노드배열 ID 업데이트
                    if (sMOVE_.Equals("L") || sMOVE_.Equals("R")) //가로길이 업데이트
                    {
                        int iDir = 1;
                        if (sMOVE_.Equals("L"))
                        {
                            iDir = -1;
                        }
                        iHeightH += (iDir * int.Parse(sLENGTH));
                        if (iAngle > int.Parse(sANGLE))
                        {
                            iAngle = int.Parse(sANGLE);
                        }
                    }
                    else if (sMOVE_.Equals("D"))
                    {
                        i_DNodeCnt++;
                        iHeightV += int.Parse(sLENGTH);
                    }
                }
                Dictionary<string, string> dicRecord = new Dictionary<string, string>();
                dicRecord.Add(MasterField.START, sStartID);
                dicRecord.Add(MasterField.ANGLE, iAngle.ToString());
                dicRecord.Add(MasterField.HEIGHT, iHeightV.ToString());
                dicRecord.Add(MasterField.TIER, i_DNodeCnt.ToString());
                dicRecord.Add(MasterField.LENGTH, iHeightH.ToString());
                lsDicNew.Add(dicRecord);

                foreach (Dictionary<string, string> dicCurrentTmp in lsDicNew)
                {
                    int iNodeLength = int.Parse(dicCurrentTmp[MasterField.LENGTH]);
                    string sStart = dicCurrentTmp[MasterField.START];
                    string sTier = dicCurrentTmp[MasterField.TIER];
                    string sANGLE = dicCurrentTmp[MasterField.ANGLE];
                    string sCellHeight = dicCurrentTmp[MasterField.HEIGHT];
                    int iCurrentAngleTmp = int.Parse(dicCurrentTmp[MasterField.ANGLE]);
                    //안테나 포인터 좌표 계산 - 안테나 포인터의 궤적이 선반 위치가 된다.
                    //이 부분을 잘 반영해야 함. DF의 두께와 너비를 반영.
                    iCurrentAngle += iCurrentAngleTmp;
                    iCurrentAngle = iCurrentAngle % 360;
                    bool bRightDirection = iNodeLength > 0 ? true : false;
                    Point ptDF_Center_New = GetNextPoint(iCurrentAngle, bRightDirection, iNodeLength, ptDF_Center_Old); //경로 이동 후 포인트
                    Point ptDF_Center_Mid = getMidpoint(ptDF_Center_Old, ptDF_Center_New);  //이동전과 이동후의 중점포인트.
                    ptDF_AntPoint = GetDFPointer(iCurrentAngle, iDFMarginSpace, ptDF_Center_Mid); //중점 포인트의 DF안테나 위치 (선반의 센터가 됨)

                    dataNodeList dnCurrentPos = new dataNodeList();
                    dnCurrentPos.posX = ptDF_Center_New.X;
                    dnCurrentPos.posZ = ptDF_Center_New.Y;
                    //dnCurrentPos.id = string.Format("{0}{1}", "9", sLast);
                    dnCurrentPos.id = string.Format("{0}{1}", "9", sStart);
                    dnCurrentPos.type = "0";

                    dataLinkList dlCurrent = new dataLinkList();
                    dlCurrent.VertexCount = 0;
                    dlCurrent.eNode = dnCurrentPos.id;
                    dlCurrent.sNode = lsNodeListSmall[lsNodeListSmall.Count - 1].id;
                    //dlCurrent.id = string.Format("{0}{1}", "8", sLast);
                    dlCurrent.id = string.Format("{0}{1}", "8", sStart);

                    if (iCntNode < 100000)
                    {
                        lsNodeListSmall.Add(dnCurrentPos);
                        lsLinkListSmall.Add(dlCurrent);
                    }

                    //이거 이후 iCurrentZ,X 값 조절 필요.
                    //RFID가 없으면 dObj 를 생성하지 않는다.다만 Angel 에 따라 거리만 늘려줌.
                    if (sTier.Equals("0") == false)
                    {
                        iCntNode++;
                        dataObjectList dObj = new dataObjectList();
                        //dObj.id = sLast;
                        dObj.id = sStart;
                        dObj.type = iObjType.ToString();
                        dObj.materialType = iMaterialType.ToString();
                        ushort usTier = 0;
                        bool bTierValue = ushort.TryParse(sTier, out usTier);
                        dObj.tier = usTier;
                        int iTierCnt = 4 < dObj.tier ? 4 : dObj.tier;
                        dObj.height = int.Parse(sCellHeight);
                        for (int j = 0; j <= iTierCnt; j++)
                        {
                            Caption cp = new Caption();
                            cp.Tier = j;
                            cp.FontSize = 1.5;
                            cp.FontColor = "#000000";
                            cp.TargetID = dObj.id;
                            cp.Content = string.Empty;
                            //cp.Content = string.Format("{0}-{1}", cp.TargetID, cp.Tier);
                            lsCaptionListSmall.Add(cp);
                        }

                        //TODO : MOVE_ 를 파싱해서 Angel 구하는 루틴
                        /*
                        if (sCellHeight.Equals(string.Empty))
                        {
                            dObj.height = iCellHeight * dObj.tier;
                        }
                        else
                        {
                            dObj.height = int.Parse(sCellHeight) * dObj.tier;
                        }
                        */
                        dObj.width = iNodeLength;
                        dObj.angle = iCurrentAngle;
                        dObj.depth = iCellDepth;
                        dObj.posY = 0;
                        dObj.posX = ptDF_AntPoint.X;
                        dObj.posZ = ptDF_AntPoint.Y;

                        //if (sLast.Equals("0300"))
                        //{
                        //    dObj.posY = 0;
                        //}
                        lsObjSmall.Add(dObj);
                    }
                    ptDF_Center_Old = new Point(ptDF_Center_New.X, ptDF_Center_New.Y);
                } //End of Foreach
                //lsNodeListSmall.Clear();
                //lsLinkListSmall.Clear();
            }
            catch (Exception ex)
            {
                lsObjSmall = new List<dataObjectList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();
                lsNodeListSmall = new List<dataNodeList>();
                LogManager.Trace2(_sLog, "{0} - {1}", _UTIL.GetMethodName(), ex.ToString());
                bReturn = false;
            }
            return bReturn;
        }
        /// <summary>
        /// 스파이더고 맵을 그리기 위한 함수
        /// 2021-02-01 최병진
        /// 드래곤 플라이와 차이점 : HEIGHT 에 셀의 높이를 받아 TIER 만큼 쌓아서 선반객체를 완성시킨다.
        /// </summary>
        /// <param name="sEmapPath"></param>
        /// <param name="lsObjSmall"></param>
        /// <param name="lsLinkListSmall"></param>
        /// <param name="lsNodeListSmall"></param>
        /// <param name="lsCaptionListSmall"></param>
        /// <param name="iMaterialType"></param>
        /// <returns></returns>
        private bool GenerateEMapExSPG(string sEmapPath, out List<dataObjectList> lsObjSmall, out List<dataLinkList> lsLinkListSmall, out List<dataNodeList> lsNodeListSmall, out List<Caption> lsCaptionListSmall, int iMaterialType)
        {
            bool bReturn = true;
            try
            {
                Dictionary<string, dataObjectList> dicEmap = new Dictionary<string, dataObjectList>();

                dataNodeList dnOrigin = new dataNodeList();
                int iObjType = _iObjType;
                //int iMaterialType = 8;
                //int iBlockWidth = 53;

                int iDFMarginSpace = 45;
                int iCellDepth = 15;
                int iCellHeight = 53;

                bool bParseResult = false;
                bParseResult = int.TryParse(txt_DFMargin.Text, out iDFMarginSpace);
                if (!bParseResult)
                {
                    iDFMarginSpace = 45;
                    txt_DFMargin.Text = iDFMarginSpace.ToString();
                }
                bParseResult = int.TryParse(txt_CellDepth.Text, out iCellDepth);
                if (!bParseResult)
                {
                    iCellDepth = 30;
                    txt_CellDepth.Text = iCellDepth.ToString();
                }

                lsObjSmall = new List<dataObjectList>();
                lsNodeListSmall = new List<dataNodeList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();

                int iCurrentAngle = 180; //시작시 뒤집혀져있음
                Point ptDF_Center_Old = new Point(0, 0); //시작 지점.
                dnOrigin.posX = ptDF_Center_Old.X;  //데이터노드 - 시작지점
                dnOrigin.posZ = ptDF_Center_Old.Y;
                dnOrigin.posY = 0;
                dnOrigin.id = "99999";
                dnOrigin.type = "0";
                Point ptDF_AntPoint = new Point();
                lsNodeListSmall.Add(dnOrigin);

                bool bResult = true;
                List<Dictionary<string, string>> lsDicEmap = new List<Dictionary<string, string>>(_UTIL.Strings2DicArray(File.ReadAllLines(sEmapPath), true, "\t"));

                int iCntNode = 0;
                _dicStart.Clear();
                foreach (Dictionary<string, string> dicCurrentTmp in lsDicEmap)
                {
                    int iNodeLength = -1;
                    //string sLast = dicCurrentTmp[MasterField.LASTSEEN];
                    string sStart = dicCurrentTmp[MasterField.START];
                    string sTYPE = dicCurrentTmp[MasterField.TYPE];
                    string sMOVE_ = dicCurrentTmp[MasterField.MOVE_];
                    string sTier = dicCurrentTmp[MasterField.TIER];
                    string sANGLE = dicCurrentTmp[MasterField.ANGLE];
                    string sCellHeight = dicCurrentTmp[MasterField.HEIGHT];
                    double dbLength = -1;
                    bool bValidLength = double.TryParse(dicCurrentTmp[MasterField.LENGTH], out dbLength);
                    iNodeLength = (int)Math.Round(dbLength);
                    int iCurrentAngleTmp = -1;
                    bool bValidAngle = int.TryParse(dicCurrentTmp[MasterField.ANGLE], out iCurrentAngleTmp);
                    bool bRightDirection = sMOVE_.IndexOf("R") >= 0;

                    //안테나 포인터 좌표 계산 - 안테나 포인터의 궤적이 선반 위치가 된다.
                    //이 부분을 잘 반영해야 함. DF의 두께와 너비를 반영.
                    if (bValidAngle)
                    {
                        iCurrentAngle += iCurrentAngleTmp;
                    }
                    else
                    {
                        if (sMOVE_.StartsWith("CC"))
                        {
                            iCurrentAngle += 270;
                        }
                        else if (sMOVE_.StartsWith("CW"))
                        {
                            iCurrentAngle += 90;
                        }
                    }
                    iCurrentAngle = iCurrentAngle % 360;

                    Point ptDF_Center_New = GetNextPoint(iCurrentAngle, bRightDirection, iNodeLength, ptDF_Center_Old); //경로 이동 후 포인트
                    Point ptDF_Center_Mid = getMidpoint(ptDF_Center_Old, ptDF_Center_New);  //이동전과 이동후의 중점포인트.
                    ptDF_AntPoint = GetDFPointer(iCurrentAngle, iDFMarginSpace, ptDF_Center_Mid); //중점 포인트의 DF안테나 위치 (선반의 센터가 됨)

                    dataNodeList dnCurrentPos = new dataNodeList();
                    dnCurrentPos.posX = ptDF_Center_New.X;
                    dnCurrentPos.posZ = ptDF_Center_New.Y;
                    //dnCurrentPos.id = string.Format("{0}{1}", "9", sLast);
                    dnCurrentPos.id = string.Format("{0}{1}", "9", sStart);
                    dnCurrentPos.type = "0";

                    dataLinkList dlCurrent = new dataLinkList();
                    dlCurrent.VertexCount = 0;
                    dlCurrent.eNode = dnCurrentPos.id;
                    dlCurrent.sNode = lsNodeListSmall[lsNodeListSmall.Count - 1].id;
                    //dlCurrent.id = string.Format("{0}{1}", "8", sLast);
                    dlCurrent.id = string.Format("{0}{1}", "8", sStart);

                    if (iCntNode < 100000)
                    {
                        lsNodeListSmall.Add(dnCurrentPos);
                        lsLinkListSmall.Add(dlCurrent);
                    }

                    //이거 이후 iCurrentZ,X 값 조절 필요.
                    //RFID가 없으면 dObj 를 생성하지 않는다.다만 Angel 에 따라 거리만 늘려줌.
                    if (sTier.Equals("0") == false)
                    {
                        iCntNode++;
                        dataObjectList dObj = new dataObjectList();
                        //dObj.id = sLast;
                        dObj.id = sStart;
                        dObj.type = iObjType.ToString();
                        dObj.materialType = iMaterialType.ToString();
                        ushort usTier = 0;
                        bool bTierValue = ushort.TryParse(sTier, out usTier);
                        dObj.tier = usTier;


                        int iTierCnt = 4 < dObj.tier ? 4 : dObj.tier;
                        for (int j = 0; j <= iTierCnt; j++)
                        {
                            Caption cp = new Caption();
                            cp.Tier = j;
                            cp.FontSize = 1.5;
                            cp.FontColor = "#000000";
                            cp.TargetID = dObj.id;
                            cp.Content = string.Empty;
                            //cp.Content = string.Format("{0}-{1}", cp.TargetID, cp.Tier);
                            lsCaptionListSmall.Add(cp);
                        }

                        //TODO : MOVE_ 를 파싱해서 Angel 구하는 루틴

                        if (sCellHeight.Equals(string.Empty))
                        {
                            dObj.height = iCellHeight * dObj.tier;
                        }
                        else
                        {
                            dObj.height = int.Parse(sCellHeight) * dObj.tier;
                        }
                        dObj.width = iNodeLength;

                        dObj.angle = iCurrentAngle;
                        dObj.depth = iCellDepth;
                        dObj.posY = 0;
                        dObj.posX = ptDF_AntPoint.X;
                        dObj.posZ = ptDF_AntPoint.Y;

                        //if (sLast.Equals("0300"))
                        //{
                        //    dObj.posY = 0;
                        //}
                        lsObjSmall.Add(dObj);
                    }
                    ptDF_Center_Old = new Point(ptDF_Center_New.X, ptDF_Center_New.Y);
                } //End of Foreach
                //lsNodeListSmall.Clear();
                //lsLinkListSmall.Clear();
            }
            catch (Exception ex)
            {
                lsObjSmall = new List<dataObjectList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();
                lsNodeListSmall = new List<dataNodeList>();
                LogManager.Trace2(_sLog, "{0} - {1}", _UTIL.GetMethodName(), ex.ToString());
                bReturn = false;
            }
            return bReturn;
        }

        private bool GenerateEMapEx(string sEmapPath, out List<dataObjectList> lsObjSmall, out List<dataLinkList> lsLinkListSmall, out List<dataNodeList> lsNodeListSmall, out List<Caption> lsCaptionListSmall, int iMaterialType)
        {
            bool bReturn = true;
            try
            {
                Dictionary<string, dataObjectList> dicEmap = new Dictionary<string, dataObjectList>();

                dataNodeList dnOrigin = new dataNodeList();
                int iObjType = _iObjType;
                //int iMaterialType = 8;
                //int iBlockWidth = 53;

                int iDFMarginSpace = 45;
                int iCellDepth = 15;
                int iCellHeight = 53;

                bool bParseResult = false;
                bParseResult = int.TryParse(txt_DFMargin.Text, out iDFMarginSpace);
                if (!bParseResult)
                {
                    iDFMarginSpace = 45;
                    txt_DFMargin.Text = iDFMarginSpace.ToString();
                }
                bParseResult = int.TryParse(txt_CellDepth.Text, out iCellDepth);
                if (!bParseResult)
                {
                    iCellDepth = 30;
                    txt_CellDepth.Text = iCellDepth.ToString();
                }

                lsObjSmall = new List<dataObjectList>();
                lsNodeListSmall = new List<dataNodeList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();

                int iCurrentAngle = 180; //시작시 뒤집혀져있음
                Point ptDF_Center_Old = new Point(0, 0); //시작 지점.
                dnOrigin.posX = ptDF_Center_Old.X;  //데이터노드 - 시작지점
                dnOrigin.posZ = ptDF_Center_Old.Y;
                dnOrigin.posY = 0;
                dnOrigin.id = "99999";
                dnOrigin.type = "0";
                Point ptDF_AntPoint = new Point();
                lsNodeListSmall.Add(dnOrigin);

                bool bResult = true;
                List<Dictionary<string, string>> lsDicEmap = new List<Dictionary<string, string>>(_UTIL.Strings2DicArray(File.ReadAllLines(sEmapPath), true, "\t"));

                int iCntNode = 0;

                foreach (Dictionary<string, string> dicCurrentTmp in lsDicEmap)
                {
                    int iNodeLength = -1;
                    string sLast = dicCurrentTmp[MasterField.LASTSEEN];
                    string sTYPE = dicCurrentTmp[MasterField.TYPE];
                    string sMOVE_ = dicCurrentTmp[MasterField.MOVE_];
                    string sTier = dicCurrentTmp[MasterField.TIER];
                    string sANGLE = dicCurrentTmp[MasterField.ANGLE];
                    double dbLength = -1;
                    bool bValidLength = double.TryParse(dicCurrentTmp[MasterField.LENGTH], out dbLength);
                    iNodeLength = (int)Math.Round(dbLength);
                    int iCurrentAngleTmp = -1;
                    bool bValidAngle = int.TryParse(dicCurrentTmp[MasterField.ANGLE], out iCurrentAngleTmp);
                    bool bRightDirection = sMOVE_.IndexOf("R") >= 0;

                    //안테나 포인터 좌표 계산 - 안테나 포인터의 궤적이 선반 위치가 된다.
                    //이 부분을 잘 반영해야 함. DF의 두께와 너비를 반영.
                    if (bValidAngle)
                    {
                        iCurrentAngle += iCurrentAngleTmp;
                    }
                    else
                    {
                        if (sMOVE_.StartsWith("CC"))
                        {
                            iCurrentAngle += 270;
                        }
                        else if (sMOVE_.StartsWith("CW"))
                        {
                            iCurrentAngle += 90;
                        }
                    }
                    iCurrentAngle = iCurrentAngle % 360;

                    Point ptDF_Center_New = GetNextPoint(iCurrentAngle, bRightDirection, iNodeLength, ptDF_Center_Old); //경로 이동 후 포인트
                    Point ptDF_Center_Mid = getMidpoint(ptDF_Center_Old, ptDF_Center_New);  //이동전과 이동후의 중점포인트.
                    ptDF_AntPoint = GetDFPointer(iCurrentAngle, iDFMarginSpace, ptDF_Center_Mid); //중점 포인트의 DF안테나 위치 (선반의 센터가 됨)

                    dataNodeList dnCurrentPos = new dataNodeList();
                    dnCurrentPos.posX = ptDF_Center_New.X;
                    dnCurrentPos.posZ = ptDF_Center_New.Y;
                    dnCurrentPos.id = string.Format("{0}{1}", "9", sLast);
                    dnCurrentPos.type = "0";

                    dataLinkList dlCurrent = new dataLinkList();
                    dlCurrent.VertexCount = 0;
                    dlCurrent.eNode = dnCurrentPos.id;
                    dlCurrent.sNode = lsNodeListSmall[lsNodeListSmall.Count - 1].id;
                    dlCurrent.id = string.Format("{0}{1}", "8", sLast);

                    if (iCntNode < 100000)
                    {
                        lsNodeListSmall.Add(dnCurrentPos);
                        lsLinkListSmall.Add(dlCurrent);
                    }

                    //이거 이후 iCurrentZ,X 값 조절 필요.
                    //RFID가 없으면 dObj 를 생성하지 않는다.다만 Angel 에 따라 거리만 늘려줌.
                    if (sTier.Equals("0") == false)
                    {
                        iCntNode++;
                        dataObjectList dObj = new dataObjectList();
                        //dObj.id = int.Parse(dicCurrentTmp[MasterField.START]).ToString();
                        dObj.id = sLast;
                        dObj.type = iObjType.ToString();
                        dObj.materialType = iMaterialType.ToString();
                        ushort usTier = 0;
                        bool bTierValue = ushort.TryParse(sTier, out usTier);
                        dObj.tier = usTier;


                        int iTierCnt = 4 < dObj.tier ? 4 : dObj.tier;
                        for (int j = 0; j <= iTierCnt; j++)
                        {
                            Caption cp = new Caption();
                            cp.Tier = j;
                            cp.FontSize = 1.5;
                            cp.FontColor = "#000000";
                            cp.TargetID = dObj.id;
                            cp.Content = string.Empty;
                            //cp.Content = string.Format("{0}-{1}", cp.TargetID, cp.Tier);
                            lsCaptionListSmall.Add(cp);
                        }

                        //TODO : MOVE_ 를 파싱해서 Angel 구하는 루틴

                        dObj.width = iNodeLength;
                        dObj.height = iCellHeight * dObj.tier;
                        dObj.angle = iCurrentAngle;
                        dObj.depth = iCellDepth;
                        dObj.posY = 0;
                        dObj.posX = ptDF_AntPoint.X;
                        dObj.posZ = ptDF_AntPoint.Y;

                        //if (sLast.Equals("0300"))
                        //{
                        //    dObj.posY = 0;
                        //}
                        lsObjSmall.Add(dObj);
                    }
                    ptDF_Center_Old = new Point(ptDF_Center_New.X, ptDF_Center_New.Y);
                } //End of Foreach
                //lsNodeListSmall.Clear();
                //lsLinkListSmall.Clear();
            }
            catch (Exception ex)
            {
                lsObjSmall = new List<dataObjectList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();
                lsNodeListSmall = new List<dataNodeList>();
                LogManager.Trace2(_sLog, "{0} - {1}", _UTIL.GetMethodName(), ex.ToString());
                bReturn = false;
            }
            return bReturn;
        }

        private bool GenerateEMapSPG(string sEmapPath, out List<dataObjectList> lsObjSmall, out List<dataLinkList> lsLinkListSmall, out List<dataNodeList> lsNodeListSmall, out List<Caption> lsCaptionListSmall, int iMaterialType)
        {
            bool bReturn = true;
            try
            {
                Dictionary<string, dataObjectList> dicEmap = new Dictionary<string, dataObjectList>();

                dataNodeList dnOrigin = new dataNodeList();
                int iObjType = _iObjType;
                //int iMaterialType = 8;
                //int iBlockWidth = 53;

                int iDFMarginSpace = 45;
                int iCellDepth = 15;
                int iCellHeight = 53;

                bool bParseResult = false;
                bParseResult = int.TryParse(txt_DFMargin.Text, out iDFMarginSpace);
                if (!bParseResult)
                {
                    iDFMarginSpace = 45;
                    txt_DFMargin.Text = iDFMarginSpace.ToString();
                }
                bParseResult = int.TryParse(txt_CellDepth.Text, out iCellDepth);
                if (!bParseResult)
                {
                    iCellDepth = 30;
                    txt_CellDepth.Text = iCellDepth.ToString();
                }

                lsObjSmall = new List<dataObjectList>();
                lsNodeListSmall = new List<dataNodeList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();

                int iCurrentAngle = 180; //시작시 뒤집혀져있음
                int iStartPosX = 0, iStartPosY = 0;
                bParseResult = int.TryParse(txt_StartPosX.Text, out iStartPosX);
                bParseResult = int.TryParse(txt_StartPosY.Text, out iStartPosY);
                Point ptDF_Center_Old = new Point(iStartPosX, iStartPosY); //시작 지점.

                dnOrigin.posX = ptDF_Center_Old.X;  //데이터노드 - 시작지점
                dnOrigin.posZ = ptDF_Center_Old.Y;
                dnOrigin.posY = 0;
                dnOrigin.id = "99999";
                dnOrigin.type = "0";
                Point ptDF_Center_Mid = new Point();
                Point ptDF_AntPoint = new Point();
                lsNodeListSmall.Add(dnOrigin);

                bool bResult = true;
                List<Dictionary<string, string>> lsDicEmap = new List<Dictionary<string, string>>(_UTIL.Strings2DicArray(File.ReadAllLines(sEmapPath), true, "\t"));

                int iCntNode = 0;
                Point ptDF_Center_New = new Point();
                foreach (Dictionary<string, string> dicCurrentTmp in lsDicEmap)
                {
                    int iNodeLength = -1;
                    string sLast = dicCurrentTmp[MasterField.LASTSEEN];
                    string sTYPE = dicCurrentTmp[MasterField.TYPE];
                    string sMOVE_ = dicCurrentTmp[MasterField.MOVE_];
                    string sTier = dicCurrentTmp[MasterField.TIER];
                    string sANGLE = dicCurrentTmp[MasterField.ANGLE];
                    double dbLength = -1;
                    bool bValidLength = double.TryParse(dicCurrentTmp[MasterField.LENGTH], out dbLength);
                    //iNodeLength = (int)Math.Round(dbLength);
                    iNodeLength = (int)Math.Round(dbLength) / 10;
                    int iCurrentAngleTmp = -1;
                    bool bValidAngle = int.TryParse(dicCurrentTmp[MasterField.ANGLE], out iCurrentAngleTmp);
                    bool bL_R_Direction = sMOVE_.IndexOf("R") >= 0 || sMOVE_.IndexOf("L") >= 0;
                    bool bRightDirection = sMOVE_.IndexOf("R") >= 0;
                    if (dbLength > 3000)
                    {
                        continue;
                    }
                    //안테나 포인터 좌표 계산 - 안테나 포인터의 궤적이 선반 위치가 된다.
                    //이 부분을 잘 반영해야 함. DF의 두께와 너비를 반영.
                    if (bValidAngle)
                    {
                        iCurrentAngle += iCurrentAngleTmp;
                    }
                    else
                    {
                        if (sMOVE_.StartsWith("CC"))
                        {
                            iCurrentAngle += 270;
                        }
                        else if (sMOVE_.StartsWith("CW"))
                        {
                            iCurrentAngle += 90;
                        }
                    }
                    iCurrentAngle = iCurrentAngle % 360;

                    if (bL_R_Direction)
                    {
                        ptDF_Center_New = GetNextPoint(iCurrentAngle, bRightDirection, iNodeLength, ptDF_Center_Old); //경로 이동 후 포인트
                        ptDF_Center_Mid = getMidpoint(ptDF_Center_Old, ptDF_Center_New);  //이동전과 이동후의 중점포인트.
                        //ptDF_AntPoint = GetDFPointer(iCurrentAngle, iDFMarginSpace, ptDF_Center_Mid); //중점 포인트의 DF안테나 위치 (선반의 센터가 됨)
                        ptDF_AntPoint = GetDFPointer(iCurrentAngle, iDFMarginSpace, ptDF_Center_New); // 센터를 잡는 방식으로 변경

                        dataNodeList dnCurrentPos = new dataNodeList();
                        dnCurrentPos.posX = ptDF_Center_New.X;
                        dnCurrentPos.posZ = ptDF_Center_New.Y;
                        dnCurrentPos.id = string.Format("{0}{1}", "9", sLast);
                        dnCurrentPos.type = "0";

                        dataLinkList dlCurrent = new dataLinkList();
                        dlCurrent.VertexCount = 0;
                        dlCurrent.eNode = dnCurrentPos.id;
                        dlCurrent.sNode = lsNodeListSmall[lsNodeListSmall.Count - 1].id;
                        dlCurrent.id = string.Format("{0}{1}", "8", sLast);

                        if (iCntNode < 100000)
                        {
                            lsNodeListSmall.Add(dnCurrentPos);
                            lsLinkListSmall.Add(dlCurrent);
                        }
                    }
                    ptDF_Center_Old = new Point(ptDF_Center_New.X, ptDF_Center_New.Y);
                    //이거 이후 iCurrentZ,X 값 조절 필요.
                    //RFID가 없으면 dObj 를 생성하지 않는다.다만 Angel 에 따라 거리만 늘려줌.
                    //if (sTier.Equals("0") == false)
                    if (sMOVE_.Equals("D"))
                    {
                        iCntNode++;
                        dataObjectList dObj = new dataObjectList();
                        //dObj.id = int.Parse(dicCurrentTmp[MasterField.START]).ToString();
                        dObj.id = sLast;
                        dObj.type = iObjType.ToString();
                        dObj.materialType = iMaterialType.ToString();
                        ushort usTier = 0;
                        bool bTierValue = ushort.TryParse(sTier, out usTier);
                        dObj.tier = usTier;


                        int iTierCnt = 4 < dObj.tier ? 4 : dObj.tier;
                        for (int j = 0; j <= iTierCnt; j++)
                        {
                            Caption cp = new Caption();
                            cp.Tier = j;
                            cp.FontSize = 1.5;
                            cp.FontColor = "#000000";
                            cp.TargetID = dObj.id;
                            cp.Content = string.Empty;
                            //cp.Content = string.Format("{0}-{1}", cp.TargetID, cp.Tier);
                            lsCaptionListSmall.Add(cp);
                        }

                        //TODO : MOVE_ 를 파싱해서 Angel 구하는 루틴

                        //dObj.width = iNodeLength;
                        dObj.width = 50;
                        //dObj.height = iCellHeight * dObj.tier;
                        dObj.height = dObj.width * dObj.tier;
                        dObj.angle = iCurrentAngle;
                        dObj.depth = iCellDepth;
                        dObj.posY = 0;
                        dObj.posX = ptDF_AntPoint.X;
                        dObj.posZ = ptDF_AntPoint.Y;


                        if (sLast.Equals("0440"))
                        {
                            dObj.posY = 0;
                        }
                        lsObjSmall.Add(dObj);
                    }

                } //End of Foreach
                //lsNodeListSmall.Clear();
                //lsLinkListSmall.Clear();
            }
            catch (Exception ex)
            {
                lsObjSmall = new List<dataObjectList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();
                lsNodeListSmall = new List<dataNodeList>();
                LogManager.Trace2(_sLog, "{0} - {1}", _UTIL.GetMethodName(), ex.ToString());
                bReturn = false;
            }
            return bReturn;
        }

        private bool GenerateEMapSPGBackup(string sEmapPath, out List<dataObjectList> lsObjSmall, out List<dataLinkList> lsLinkListSmall, out List<dataNodeList> lsNodeListSmall, out List<Caption> lsCaptionListSmall, int iMaterialType)
        {
            bool bReturn = true;
            try
            {
                Dictionary<string, dataObjectList> dicEmap = new Dictionary<string, dataObjectList>();

                dataNodeList dnOrigin = new dataNodeList();
                int iObjType = _iObjType;
                //int iMaterialType = 8;
                //int iBlockWidth = 53;

                int iDFMarginSpace = 45;
                int iCellDepth = 15;
                int iCellHeight = 53;

                bool bParseResult = false;
                bParseResult = int.TryParse(txt_DFMargin.Text, out iDFMarginSpace);
                if (!bParseResult)
                {
                    iDFMarginSpace = 45;
                    txt_DFMargin.Text = iDFMarginSpace.ToString();
                }
                bParseResult = int.TryParse(txt_CellDepth.Text, out iCellDepth);
                if (!bParseResult)
                {
                    iCellDepth = 30;
                    txt_CellDepth.Text = iCellDepth.ToString();
                }

                lsObjSmall = new List<dataObjectList>();
                lsNodeListSmall = new List<dataNodeList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();

                int iCurrentAngle = 180; //시작시 뒤집혀져있음

                Point ptDF_Center_Old = new Point(1000, 200); //시작 지점.
                dnOrigin.posX = ptDF_Center_Old.X;  //데이터노드 - 시작지점
                dnOrigin.posZ = ptDF_Center_Old.Y;
                dnOrigin.posY = 0;
                dnOrigin.id = "99999";
                dnOrigin.type = "0";
                Point ptDF_AntPoint = new Point();
                lsNodeListSmall.Add(dnOrigin);

                bool bResult = true;
                List<Dictionary<string, string>> lsDicEmap = new List<Dictionary<string, string>>(_UTIL.Strings2DicArray(File.ReadAllLines(sEmapPath), true, "\t"));
                SortedDictionary<string, Dictionary<string, string>> lsDicInfo = new SortedDictionary<string, Dictionary<string, string>>();

                int iCntNode = 0;

                foreach (Dictionary<string, string> dicCurrentTmp in lsDicEmap)
                {
                    int iNodeLength = -1;
                    string sStart = dicCurrentTmp[MasterField.START];
                    string sLast = dicCurrentTmp[MasterField.LASTSEEN];
                    string sTYPE = dicCurrentTmp[MasterField.TYPE];
                    string sMOVE_ = dicCurrentTmp[MasterField.MOVE_];
                    string sTier = dicCurrentTmp[MasterField.TIER];
                    string sANGLE = dicCurrentTmp[MasterField.ANGLE];
                    double dbLength = -1;
                    bool bValidLength = double.TryParse(dicCurrentTmp[MasterField.LENGTH], out dbLength);
                    iNodeLength = (int)Math.Round(dbLength);
                    int iCurrentAngleTmp = -1;
                    bool bValidAngle = int.TryParse(dicCurrentTmp[MasterField.ANGLE], out iCurrentAngleTmp);
                    bool bRightDirection = sMOVE_.IndexOf("L") >= 0 || sMOVE_.IndexOf("R") >= 0;
                    //Dictionary<string, string> dicCurrentNode;
                    if (lsDicInfo.ContainsKey(sStart))
                    {
                        lsDicInfo[sStart][MasterField.LASTSEEN] = sStart;
                        if (sMOVE_.IndexOf("D") >= 0)
                        {
                            string sRepInfo = lsDicInfo[sStart][MasterField.TIER];
                            if (sRepInfo.IndexOf(sTier) < 0)
                            {
                                lsDicInfo[sStart][MasterField.REP] += sTier + _sDivStrComma;
                            }
                        }
                        if (sMOVE_.IndexOf("T") >= 0)
                        {
                            //lsDicInfo[sStart][;
                        }
                    }
                    else
                    {
                        //dicCurrentTmp[MasterField.TIER] = sTier;
                        //dicCurrentTmp[MasterField.REP] = string.Empty;
                        dicCurrentTmp[MasterField.TIER] = dicCurrentTmp[MasterField.REP] = sTier;
                        lsDicInfo.Add(sStart, dicCurrentTmp);
                    }
                }

                foreach (string sKey in lsDicInfo.Keys)
                {
                    Dictionary<string, string> dicCurrentTmp = lsDicInfo[sKey];

                    int iNodeLength = -1;
                    string sLast = dicCurrentTmp[MasterField.LASTSEEN];
                    string sTYPE = dicCurrentTmp[MasterField.TYPE];
                    string sMOVE_ = dicCurrentTmp[MasterField.MOVE_];
                    int iRep = dicCurrentTmp[MasterField.REP].Split(_sDivStrComma.ToCharArray()).Length - 1;
                    string sTier = iRep.ToString();
                    string sANGLE = dicCurrentTmp[MasterField.ANGLE];
                    double dbLength = -1;
                    bool bValidLength = double.TryParse(dicCurrentTmp[MasterField.LENGTH], out dbLength);
                    iNodeLength = (int)Math.Round(dbLength);
                    //if (sLast.Equals("0005"))
                    //{
                    //    //iNodeLength = 4; //임시
                    //}
                    //else
                    {
                        iNodeLength = iCellHeight;//임의로 추가 (현재는 셀 간격 고정됨)
                    }
                    int iCurrentAngleTmp = -1;
                    bool bValidAngle = int.TryParse(dicCurrentTmp[MasterField.ANGLE], out iCurrentAngleTmp);
                    bool bRightDirection = sMOVE_.IndexOf("R") >= 0;

                    //안테나 포인터 좌표 계산 - 안테나 포인터의 궤적이 선반 위치가 된다.
                    //이 부분을 잘 반영해야 함. DF의 두께와 너비를 반영.
                    if (bValidAngle)
                    {
                        iCurrentAngle += iCurrentAngleTmp;
                    }
                    //else
                    //{
                    //    if (sMOVE_.StartsWith("CC"))
                    //    {
                    //        iCurrentAngle += 270;
                    //    }
                    //    else if (sMOVE_.StartsWith("CW"))
                    //    {
                    //        iCurrentAngle += 90;
                    //    }
                    //}
                    iCurrentAngle = iCurrentAngle % 360;

                    Point ptDF_Center_New = GetNextPoint(iCurrentAngle, bRightDirection, iNodeLength, ptDF_Center_Old); //경로 이동 후 포인트
                    Point ptDF_Center_Mid = getMidpoint(ptDF_Center_Old, ptDF_Center_New);  //이동전과 이동후의 중점포인트.
                    ptDF_AntPoint = GetDFPointer(iCurrentAngle, iDFMarginSpace, ptDF_Center_Mid); //중점 포인트의 DF안테나 위치 (선반의 센터가 됨)

                    dataNodeList dnCurrentPos = new dataNodeList();
                    dnCurrentPos.posX = ptDF_Center_New.X;
                    dnCurrentPos.posZ = ptDF_Center_New.Y;
                    dnCurrentPos.id = string.Format("{0}{1}", "9", sLast);
                    dnCurrentPos.type = "0";

                    dataLinkList dlCurrent = new dataLinkList();
                    dlCurrent.VertexCount = 0;
                    dlCurrent.eNode = dnCurrentPos.id;
                    dlCurrent.sNode = lsNodeListSmall[lsNodeListSmall.Count - 1].id;
                    dlCurrent.id = string.Format("{0}{1}", "8", sLast);

                    if (iCntNode < 100000)
                    {
                        lsNodeListSmall.Add(dnCurrentPos);
                        lsLinkListSmall.Add(dlCurrent);
                    }

                    //이거 이후 iCurrentZ,X 값 조절 필요.
                    //RFID가 없으면 dObj 를 생성하지 않는다.다만 Angel 에 따라 거리만 늘려줌.
                    if (sTier.Equals("0") == false)
                    {
                        iCntNode++;
                        dataObjectList dObj = new dataObjectList();
                        //dObj.id = int.Parse(dicCurrentTmp[MasterField.START]).ToString();
                        dObj.id = sLast;
                        dObj.type = iObjType.ToString();
                        dObj.materialType = iMaterialType.ToString();
                        ushort usTier = 0;
                        bool bTierValue = ushort.TryParse(sTier, out usTier);
                        dObj.tier = usTier;
                        int iTierCnt = dObj.tier = 4; //임시
                        //int iTierCnt = 4 < dObj.tier ? 4 : dObj.tier;
                        //for (int j = 0; j <= iTierCnt; j++)
                        for (int j = iTierCnt; j != 0; j--)
                        {
                            Caption cp = new Caption();
                            cp.Tier = j;
                            cp.FontSize = 1.5;
                            cp.FontColor = "#000000";
                            cp.TargetID = dObj.id;
                            cp.Content = string.Empty;
                            //cp.Content = string.Format("{0}-{1}", cp.TargetID, cp.Tier);
                            lsCaptionListSmall.Add(cp);
                        }

                        //TODO : MOVE_ 를 파싱해서 Angel 구하는 루틴

                        dObj.width = iNodeLength;
                        dObj.height = iCellHeight * dObj.tier;
                        //dObj.width = iNodeLength;
                        //dObj.height = iNodeLength * dObj.tier;
                        dObj.angle = iCurrentAngle;
                        dObj.depth = iCellDepth;
                        dObj.posY = 0;
                        dObj.posX = ptDF_AntPoint.X;
                        dObj.posZ = ptDF_AntPoint.Y;
                        //if (sLast.Equals("0300"))
                        //{
                        //    dObj.posY = 0;
                        //}
                        lsObjSmall.Add(dObj);
                    }
                    ptDF_Center_Old = new Point(ptDF_Center_New.X, ptDF_Center_New.Y);
                } //End of Foreach
            }
            catch (Exception ex)
            {
                lsObjSmall = new List<dataObjectList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();
                lsNodeListSmall = new List<dataNodeList>();
                LogManager.Trace2(_sLog, "{0} - {1}", _UTIL.GetMethodName(), ex.ToString());
                bReturn = false;
            }
            return bReturn;
        }

        void XMLEdit(string sXMLPath, string sPATHData)
        {
            eMap emapTmp = (eMap)XMLObject.XMLReadFile(sXMLPath, typeof(eMap));
            emapTmp = new eMap();
            DragonFlyConfig dfc = new DragonFlyConfig();
            dfc.StartNodeID = "999999";
            emapTmp.DragonFly = dfc;

            //TODO : 일단 ObjectList 부터
            EnviromentConfig ev = emapTmp.Enviroment; //너비
            ev.Width = 1200;
            ev.Depth = 700;
            //ev.Width = 50000;
            //ev.Depth = 50000;
            ev.materialType = 4;
            ev.Wall = 0;
            //ev.LinkWidth = float.Parse(txt_NodeWidth.Text);
            ev.LinkWidth = 0;
            ev.MaxTier = 5;
            emapTmp.Enviroment = ev;

            List<dataObjectList> lsObjSmall = new List<dataObjectList>();
            List<dataNodeList> lsNodeListSmall = new List<dataNodeList>();
            List<dataLinkList> lsLinkListSmall = new List<dataLinkList>();
            List<Caption> lsCaptionListSmall = new List<Caption>();

            bool bOK = false;
            if (_bEMAPSPG)
            {
                bOK = GenerateEMapExSPG2(sPATHData, out lsObjSmall, out lsLinkListSmall, out lsNodeListSmall, out lsCaptionListSmall, int.Parse(_sMaterial), 40, 20, _iObjType);
                //bOK = GenerateEMapExSPG(sPATHData, out lsObjSmall, out lsLinkListSmall, out lsNodeListSmall, out lsCaptionListSmall, int.Parse(_sMaterial));
                //bOK = GenerateEMapSPG(sPATHData, out lsObjSmall, out lsLinkListSmall, out lsNodeListSmall, out lsCaptionListSmall, int.Parse(_sMaterial));
                //bOK = GenerateEMapSPGBackup(sPATHData, out lsObjSmall, out lsLinkListSmall, out lsNodeListSmall, out lsCaptionListSmall, int.Parse(_sMaterial));
            }
            else
            {
                bOK = GenerateEMapEx(sPATHData, out lsObjSmall, out lsLinkListSmall, out lsNodeListSmall, out lsCaptionListSmall, int.Parse(_sMaterial));
            }

            emapTmp.ObjectList = lsObjSmall.ToArray();
            emapTmp.LinkList = lsLinkListSmall.ToArray();
            emapTmp.NodeList = lsNodeListSmall.ToArray();
            emapTmp.CaptionList = lsCaptionListSmall.ToArray();

            XMLObject.XMLWriteFile(sXMLPath, emapTmp);
            //FormClose(this, true);
            //string sMsg = _UTIL.getXMLFromModel(emapTmp);
            //LogManager.Trace0(_sLog, "{0}-{1}",sMsg);
        }

   

        void UpdateConnectionStatus()
        {
            //string sConnectionCnt = _pAsyncSocketClientList.Count.ToString();
            if (_bICSRConnect)
            {
                lbl_StatusConnect.Background = new SolidColorBrush(Colors.Green);
                //lbl_StatusConnect.Content = "접속중";
            }
            else
            {
                lbl_StatusConnect.Background = new SolidColorBrush(Colors.Red);
                //lbl_StatusConnect.Content = "접속대기";
            }
            //lbl_StatusConnect.Content += string.Format("({0})", sConnectionCnt);
        }

        private void Set_DF_Status(string sSEQ)
        {
            //int iSEQ = int.Parse(sSEQ);
            //if (iSEQ >= 90060)
            //{
            //    eMap.rotateDragnFly(90);
            //}
            //else
            //{
            //    eMap.rotateDragnFly(0);
            //}

            eMap.moveDestNode(sSEQ);
            if (_dicAngle.ContainsKey(sSEQ))
            {
                int iRotate = (0 + _dicAngle[sSEQ]) % 360;
                eMap.rotateDragnFly(iRotate);
            }
            //for (int j = 0; j < 4; j++)
            //{
            //    int iRackID = int.Parse(sSEQ);
            //    int iTier = j + 1;
            //    //if (bNormal)
            //    //{
            //    //    eMap.ObjectEffectTurnOff(iRackID.ToString(), iTier);
            //    //}
            //    //else
            //    //{
            //    //    eMap.ObjectEffectTurnOn(iRackID.ToString(), iTier);
            //    //}
            //}
            //if (sSEQ.StartsWith("03") || sSEQ.StartsWith("3"))
            //{
            //    eMap.rotateDragnFly(0);
            //}
            //else
            //{
            //    eMap.rotateDragnFly(90);
            //}

        }

        static List<string> _lsSerialNumbers = new List<string>();
        void SetComboBoxItems(DataTable dtMasterCurrent)
        {
            List<string> lsTmpTest = new List<string>(_UTIL.DataTable2Strings(dtMasterCurrent, true, _sDivStrEMart));
            File.WriteAllLines($"{_sEMapXMLDir}test.ini", lsTmpTest.ToArray());

            foreach (DataColumn dc in dtMasterCurrent.Columns)
            {
                DataTable dtTmp = dtMasterCurrent.DefaultView.ToTable(true, dc.ColumnName);
                List<string> lsTmp = new List<string>(_UTIL.DataTable2Strings(dtTmp, false, _sDivStrEMart));
                lsTmp.Sort();
                lsTmp.Insert(0, "------");

                //ArrayList alTmp = _UTIL.GetObjByTag(dc.ColumnName, gb_Search, true);
                ArrayList alTmp = GetObjByTagWPF(dc.ColumnName, gb_Search, true);
                if (alTmp.Count > 0)
                {
                    System.Windows.Controls.ComboBox ctlTmp = (System.Windows.Controls.ComboBox)(alTmp[0]);
                    ctlTmp.Items.Clear();
                    lsTmp.ToList().ForEach(item => ctlTmp.Items.Add(item));
                }
            }
            int iItemCnt = dtMasterCurrent.Rows.Count;
            lbl_ProductCnt.Content = "위치조회 : " + iItemCnt.ToString();

            //if (_dicNodeInfo.Count > 0)
            //{
            //    _dicColumnCount.Clear();
            //    Set_NORMAL_Status_ALL(true);
            //    List<Dictionary<string, string>> dicMasterTmp = new List<Dictionary<string, string>>(_UTIL.DataTable2DicArray(_dtPath));
            //    foreach (Dictionary<string, string> iKey in dicMasterTmp)
            //    {
            //        string sNode = iKey[MasterField.LASTSEEN];
            //        if (sNode.Equals(string.Empty))
            //            continue;
            //        string sTier = iKey[MasterField.TIER];
            //        int iTierCurrent = int.Parse(sTier);
            //        for (int j = 0; j < iTierCurrent + 1; j++)
            //        {
            //            Set_RED_Status(sNode, (j + 1).ToString(), dtMasterCurrent);
            //        }
            //    }
            //    //List<Dictionary<string, string>> lsDicResult = new List<Dictionary<string, string>>(_UTIL.DataTable2DicArray(dtMasterCurrent));
            //    //foreach (Dictionary<string, string> dicTmp in lsDicResult)
            //    //{
            //    //    string sNode = _UTIL.GetIndexString(dicTmp["Node"], "_", 0);
            //    //    //int iNode = int.Parse(sNode);
            //    //    //if(iNode < 200)
            //    //    //{
            //    //    //    iNode += 100;
            //    //    //}
            //    //    Set_RED_Status(sNode.ToString(),dicTmp["Ant"], dtMasterCurrent);
            //    //}
            //}
            _lsSerialNumbers.Clear();
            foreach (object objTmp in cmb_SN.Items)
            {
                int iSerialTmp = -1;
                bool bParse = int.TryParse(objTmp.ToString(), out iSerialTmp);
                if (bParse)
                {
                    _lsSerialNumbers.Add(iSerialTmp.ToString());
                }
            }


        }

        void InitCmbBox()
        {
            cmb_MapSize.Items.Add("0-Min");
            cmb_MapSize.Items.Add("1-Middle");
            cmb_MapSize.Items.Add("2-Large");
        }


        private void Set_State(bool bState)
        {
            foreach (string sNodeTmp in _lsCamNodes)
            {
                string sTIER = _UTIL.GetIndexString(sNodeTmp, "_", 0);
                int iTier = int.Parse(sTIER);
                string sNode = _UTIL.GetIndexString(sNodeTmp, "_", 1);
                if (!bState)
                {
                    eMap.ObjectEffectTurnOff(sNode, iTier);
                    eMap.changeCaptionContent(sNode, iTier, string.Empty);
                    //eMap.changeCaptionContent(sNode, iTier, "Off");
                }
                else
                {
                    eMap.ObjectEffectTurnOn(sNode, iTier);
                    eMap.changeCaptionContent(sNode, iTier, "On");
                    eMap.changeTierEffect(sNode, iTier, "#FF000064");
                }
            }
        }

        private void Set_State(string sNodeTmp, bool bState, string sText)
        {
            string sTIER = _UTIL.GetIndexString(sNodeTmp, "_", 0);
            int iTier = int.Parse(sTIER);
            string sNode = _UTIL.GetIndexString(sNodeTmp, "_", 1);
            if (!bState)
            {
                eMap.ObjectEffectTurnOff(sNode, iTier);
            }
            else
            {
                eMap.ObjectEffectTurnOn(sNode, iTier);
                eMap.changeTierEffect(sNode, iTier, "#FF000064");
            }
            eMap.changeCaptionContent(sNode, iTier, sText);
        }

        void AddItemstoControl(System.Windows.Controls.ComboBox ctl, object[] objArray, bool bClear)
        {
            if (bClear)
            {
                ctl.Items.Clear();
            }
            foreach(object obj in objArray)
            {
                ctl.Items.Add(obj);
            }
        }

        void Set_ComboBox_Barcode()
        {
            Dictionary<string, string> dicTmp = new Dictionary<string, string>();
            if (dicBarPrimaryInfo.Count == 0)
            {
                Set_State(false);
            }
            else
            {
                foreach (string sNodeTmp in _lsCamNodes)
                {
                    string sTIER = _UTIL.GetIndexString(sNodeTmp, "_", 0);
                    int iTier = int.Parse(sTIER);
                    string sNode = _UTIL.GetIndexString(sNodeTmp, "_", 1);
                    if (dicInfo.ContainsKey(sNodeTmp))
                    {
                        string[] saNodeCnt = dicInfo[sNodeTmp].Split(_sDivStrColon.ToCharArray());
                        eMap.ObjectEffectTurnOn(sNode, iTier);
                        eMap.changeCaptionContent(sNode, iTier, saNodeCnt.Length.ToString());
                        eMap.changeTierEffect(sNode, iTier, "#FF000064");
                    }
                    else
                    {
                        eMap.ObjectEffectTurnOff(sNode, iTier);
                        eMap.changeCaptionContent(sNode, iTier, "0");
                    }
                }
            }
        }

        private void Set_State()
        {
            if (dicInfo.Count == 0)
            {
                Set_State(false);
            }
            else
            {
                foreach (string sNodeTmp in _lsCamNodes)
                {
                    string sTIER = _UTIL.GetIndexString(sNodeTmp, "_", 0);
                    int iTier = int.Parse(sTIER);
                    string sNode = _UTIL.GetIndexString(sNodeTmp, "_", 1);
                    if (dicInfo.ContainsKey(sNodeTmp))
                    {
                        string[] saNodeCnt = dicInfo[sNodeTmp].Split(_sDivStrColon.ToCharArray());
                        eMap.changeTierEffect(sNode, iTier, "#FF000064");
                        eMap.ObjectEffectTurnOn(sNode, iTier);
                        eMap.changeCaptionContent(sNode, iTier, saNodeCnt.Length.ToString());
                    }
                    else
                    {
                        eMap.ObjectEffectTurnOff(sNode, iTier);
                        eMap.changeCaptionContent(sNode, iTier, "0");
                    }
                }
                List<string> lsKeys = new List<string>(dicBarPrimaryInfo.Keys);
                List<string> lsKey2 = new List<string>(dicInfo.Keys);
                AddItemstoControl(cmb_MAKTX, lsKeys.ToArray(), true);
                AddItemstoControl(cmb_Location, lsKey2.ToArray(), true);
                lbl_ProductCnt.Content = "Barcodes : " + lsKeys.Count.ToString();
            }
        }

        

        //private void Set_NORMAL_Status_ALL(bool bNormal)
        //{
        //    // 각 경계를 정확히 얻어오는 방법 확인
        //    for (int i = 0; i < 4; i++)
        //    {
        //        for (int j = 0; j < 4; j++)
        //        {
        //            //string sNode = (i + 1).ToString().PadLeft(3, '0') + "0";
        //            string sNode = (i + 1).ToString().PadLeft(4, '0');
        //            if (bNormal)
        //            {
        //                eMap.ObjectEffectTurnOff(sNode, j+1);
        //                eMap.changeCaptionContent(sNode, j + 1, string.Empty);
        //            }
        //            else
        //            {
        //                eMap.ObjectEffectTurnOn(sNode,j+1);
        //                eMap.changeTierEffect(sNode, j + 1, "#FF000064");
        //            }
        //        }
        //    }
        //}

        //Dictionary<string, int> _dicColumnCount = new Dictionary<string, int>();
        //private void Set_RED_Status(string v1, string v2, DataTable dtMasterCurrent)
        //{
        //    int iNode = int.Parse(v1);
        //    //string sNode = iNode.ToString().PadLeft(4, '0') + "_" + (iNode + 1).ToString().PadLeft(4, '0');
        //    string sNode = iNode.ToString().PadLeft(4, '0');
        //    int iV2 = int.Parse(v2);

        //    if (dtMasterCurrent != null)
        //    {
        //        if (dtMasterCurrent.Columns.Count == 0)
        //        {
        //            return;
        //        }
        //        string sExp = string.Format("{0} = '{1}' AND {2} = '{3}'", "Node", v1, "Ant", v2);
        //        DataRow[] drResult = dtMasterCurrent.Select(sExp);
        //        if (drResult.Length > 0)
        //        {
        //            eMap.ObjectEffectTurnOn(v1, iV2);
        //            eMap.changeTierEffect(v1, iV2, "#FF000064");
        //            eMap.changeCaptionContent(v1, iV2, (drResult.Length).ToString());

        //            if (_dicColumnCount.ContainsKey(sNode))
        //            {
        //                _dicColumnCount[sNode] += drResult.Length;
        //            }
        //            else
        //            {
        //                _dicColumnCount[sNode] = drResult.Length;
        //            }
        //            eMap.changeCaptionContent(v1, 0, _dicColumnCount[sNode].ToString());
        //        }
        //        else //해당셀에 제품이 하나도 없는 경우
        //        {

        //        }
        //    }
        //}

        private void btnTEST_Click(object sender, RoutedEventArgs e)
        {
            //eMap.changeCaptionContent("201", 2, "#FFFFFF64");
            ///eMap.changeTierEffect("201", 2, "#FF00FF64");

            string sTagTest = @"C:\eMap3D\TAGSTATUS.txt";
            string saTagList = File.ReadAllText(sTagTest);
            _lsTCPRecv.Add(saTagList);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string sTagTest = @"C:\eMap3D\BARTEST.txt";
            //string sWebPath = @"Z:\WWW\SPIDERGO.txt";
            string saTagList = string.Empty;
            //if (File.Exists(sWebPath))
            //{
            //saTagList = File.ReadAllText(sWebPath);
            //}
            //else
            //{
                saTagList = File.ReadAllText(sTagTest);
            //}
            //string[] files = Directory.GetFiles(@"C:\eMap3D\appBin\TAGSEND\");
            //foreach(string sFileName in files)
            //{
            //    saTagList = File.ReadAllText(sFileName);
                _lsTCPRecv.Add(saTagList);
            //}
            

        }
        private void MapReload(object sender, RoutedEventArgs e)
        {
            XMLEdit(_sXMLPath, _sPathDFTmp);
            btn_Reload_Click(sender, e);
        }

        static string _sMaterial = "8";
        private void cmb_Test_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string sNode = cmb_nodeTest.SelectedItem.ToString();
            ////eMap.moveDestNode(sNode);
            //Set_DF_Status("9"+sNode);
         
        }

        private void cmb_Location_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string expression = string.Empty;
            string sortOrder = string.Empty;
            System.Windows.Controls.ComboBox ctlSender = (System.Windows.Controls.ComboBox)sender;
            string sNameTag = ctlSender.Name;
            if (ctlSender.SelectedItem == null)
            {
                return;
            }
            Set_State(false);
            string sItem = ctlSender.SelectedItem.ToString();
            bool bLocation = dicInfo.ContainsKey(sItem);
            string sRawData = string.Empty;
            if (bLocation) //Location 에서 온 이벤트
            {
                sRawData = dicInfo[sItem];
                string[] saBarArray = sRawData.Split(_sDivStrColon.ToCharArray());
                AddItemstoControl(cmb_MAKTX, saBarArray, true);
                Set_State(sItem, true, saBarArray.Length.ToString());
            }
            else//Barcode 에서 온 이벤트
            {
                sRawData = dicBarPrimaryInfo[sItem];
                string[] saBarArray = sRawData.Split(_sDivStrComma.ToCharArray());
                AddItemstoControl(cmb_Location, saBarArray, true);
                foreach(string sLocationName in saBarArray)
                {
                    //string[] saBars = dicInfo[sLocationName].Split(_sDivStrColon.ToCharArray());
                    //Set_State(sLocationName, true, saBars.Length.ToString());
                    Set_State(sLocationName, true, "1");
                }
            }
            return;



            ArrayList alTmp = GetObjByTagWPF(ctlSender.Name, gb_Search);
            if (alTmp.Count > 0)
            {
                System.Windows.Controls.TextBox ctlTmp = (System.Windows.Controls.TextBox)(alTmp[0]);

               

                ctlTmp.Text = ctlSender.SelectedItem.ToString();
                //ctlTmp.Text = ctlSender.SelectedValue.ToString();
             

                ArrayList alCmb = GetObjByNameWPF("cmb", gb_Search);
                foreach (System.Windows.Controls.ComboBox cmbTmp in alCmb)
                {
                    ArrayList alTmp2 = GetObjByTagWPF(cmbTmp.Name, gb_Search, true);
                    if (alTmp2.Count > 0)
                    {
                        System.Windows.Controls.TextBox ctlCurrent = (System.Windows.Controls.TextBox)alTmp2[0];
                        if (ctlCurrent.Text.StartsWith("-") || ctlCurrent.Text == "")
                        {
                            continue;
                        }
                        expression += string.Format("{0} = '{1}' AND ", cmbTmp.Tag.ToString(), ctlCurrent.Text);
                    }
                }
                expression = expression.Substring(0, expression.Length - 5);
                sortOrder = ctlSender.Tag.ToString() + " ASC";

                DataRow[] drTmp = _dtMaster.Select(expression, sortOrder);
                DataTable dtTmp = new DataTable();
                dtTmp = _dtMaster.Clone();
                foreach (DataRow dr in drTmp)
                {
                    dtTmp.ImportRow(dr);
                }
                dtTmp.AcceptChanges();
                SetComboBoxItems(dtTmp);

                string sCnt = _UTIL.GetLastString(lbl_ProductCnt.Content.ToString(), " ");
                if (sCnt.Equals("1"))
                {
                    foreach (System.Windows.Controls.ComboBox cmbTmp in alCmb)
                    {
                        cmbTmp.SelectionChanged -= new SelectionChangedEventHandler(cmb_Location_SelectionChanged);
                        cmbTmp.SelectedIndex = cmbTmp.Items.Count - 1;
                        cmbTmp.SelectionChanged += new SelectionChangedEventHandler(cmb_Location_SelectionChanged);
                    }
                }
                else
                {
                    cmb_SN.IsDropDownOpen = true;
                }
            }
        }

        private void btn_Init_Click(object sender, RoutedEventArgs e)
        {
            //SetComboBoxItems(_dtMaster);
            Set_State();
            FillTextBoxesWPF(gb_Search, "txt_", true, string.Empty);
            //Set_DF_Status("1000");
        }

        Dictionary<string, string> _dicXMLMap = new Dictionary<string, string>();
        void ReloadEmapAngle()
        {
            try
            {
                ///XML 파일로부터 각 선반의 위치값 (X,Z,Angle) 쌍을 뽑아낸다.
                _dicXMLMap.Clear(); //<선반ID , (X,Z,Angle)> 의 KeyValue Pair
                XmlDocument xdoc = new XmlDocument();

                xdoc.Load(_sEMapXMLFullPath);
                XmlNodeList nodes = xdoc.SelectNodes("/eMap/ObjectList/dataObjectList/*");
                Dictionary<string, string> d = new Dictionary<string, string>();
                foreach (XmlNode n in nodes)
                {
                    string sKey = n.Name;
                    string sValue = n.InnerText;
                    if (d.ContainsKey(sKey)) // 한바퀴 돌았음. 세이브해야 됨.
                    {
                        string id = d["id"];
                        string posX = d["posX"];
                        string posZ = d["posZ"];
                        string angle = d["angle"];
                        _dicXMLMap[id] = $"{posX},{posZ},{angle}";
                        d.Clear();
                    }
                    else
                    {
                        d.Add(sKey, sValue);
                    }

                }

                List<Dictionary<string, string>> lsDicEmap = new List<Dictionary<string, string>>(_UTIL.Strings2DicArray(File.ReadAllLines(_sPathDFTmp), true, "\t"));
                _dicAngle.Clear();
                cmb_nodeTest.Items.Clear();
                foreach (Dictionary<string, string> dicCurrentTmp in lsDicEmap)
                {
                    int iAngle = -1;
                    string sLast = dicCurrentTmp[MasterField.LASTSEEN];
                    string sANGLE = dicCurrentTmp[MasterField.ANGLE];
                    bool bAngleEnable = int.TryParse(sANGLE, out iAngle);
                    if (bAngleEnable && iAngle > 0)
                    {
                        _dicAngle.Add("9" + sLast, iAngle);
                    }
                }

                cmb_nodeTest.Items.Clear();
                cmb_Material.Items.Clear();

                for (int i = 0; i < 30; i++)
                {
                    int j = (i + 1) * 10;
                    cmb_nodeTest.Items.Add(j.ToString());
                }
                for (int i = 0; i < 10; i++)
                {
                    int j = (i + 1) * 1;
                    cmb_Material.Items.Add(j.ToString());
                }
            }
            catch (Exception ex)
            {
                LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetMethodName(), ex.Message);
            }
        }
        
        private void btn_Reload_Click(object sender, RoutedEventArgs e)
        {
            ReloadEmapAngle();
            eMap.loadMapData(_sEMapXMLFullPath);
            
            //eMap.rotateDragnFly(90);
            //eMap.SetShadowMode(false);
        }

        void MapInfoInit()
        {
            string[] saMap = File.ReadAllLines(_sPathDFTmp);
            List<Dictionary<string, string>> lsMap = new List<Dictionary<string, string>>(_UTIL.Strings2DicArray(saMap, true, _sDivStrTab));
            _lsCamNodes.Clear();
            foreach (Dictionary<string, string> dicCurrent in lsMap)
            {
                if (dicCurrent.ContainsKey(MasterField.DESC))
                {
                    string sDesc = dicCurrent[MasterField.DESC];
                    string sTIER = dicCurrent[MasterField.TIER];
                    string sSTART = dicCurrent[MasterField.START];

                    if (sDesc.StartsWith("CAM"))
                    {
                        string sNode = $"{sTIER}_{sSTART}";
                        if (_lsCamNodes.Contains(sNode) == false)
                        {
                            _lsCamNodes.Add(sNode);
                        }
                    }
                }
            }
            LogManager.Trace2(_sLog, $"Map Node info : {_UTIL.StringMerge(_lsCamNodes.ToArray(), _sDivStrComma)}");
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            eMap.clearMap();
        }

        private void Image_TouchEnter(object sender, MouseButtonEventArgs e)
        {
            string sProcessName = "dragonfly";
            if (_UTIL.IsProcessEnable(sProcessName))
            {
                bool bResult = _UTIL.ProcessFront(sProcessName);
                return;
            }
            int iMinSize = 200;
            bool bState = false;
            //if (gb_Alive_Copy.Visibility == Visibility.Hidden)
            if (appControl.Height > iMinSize)
            {
                bState = true;
            }
            //appControl.Visibility = gb_Alive_Copy.Visibility = bState ? Visibility.Visible : Visibility.Hidden;
            if (bState)
            {
                appControl.Height = appControl.Width = iMinSize;
            }
            else
            {
                _dicConfig = _UTIL.Hash2Dic(_UTIL.LoadOptionFromFile(sOptionFilePath));
                appControl.Width = int.Parse(_dicConfig[ConfigField.WIDTH]);
                //                appControl.Width = 946;
                //appControl.Height = 778;
                appControl.Height = int.Parse(_dicConfig[ConfigField.HEIGHT]);

            }
        }

        public class eMapListener : IeMapEventListener
        {
            public Action<string> selectedAction;
            public Action<string> cameraPosAction;
            public Action<string> InitFinishedAction;

            public void onObjectSelected(string msg, int tier)
            {
                selectedAction.Invoke(msg + "," + tier);
            }

            public void onCameraPOSSaved(string pos)
            {
                cameraPosAction.Invoke(pos);
            }

            public void onMapInitialized()
            {
                InitFinishedAction(DateTime.Now.Ticks.ToString());
            }
        }

        private void Check_Rotate_Status(object sender, RoutedEventArgs e)
        {
            bool bChk = (bool)chk_Rotate.IsChecked;
            eMap.rotateDragnFly(bChk ? 0 : 90);
        }

        private void CheckBox_Status(object sender, RoutedEventArgs e)
        {
            bool bChk = (bool)chk_RackStatus.IsChecked;
            //Set_NORMAL_Status_ALL(bChk);
            Set_State(bChk);
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            bool bChk = (bool)chk_Lock.IsChecked;
            eMap.SetCameraLock(bChk);
        }

        private void chk_Shadow_Checked(object sender, RoutedEventArgs e)
        {
            bool bChk = (bool)chk_Shadow.IsChecked;
            eMap.SetShadowMode(bChk);
        }
        private void chk_SPG_Checked(object sender, RoutedEventArgs e)
        {
            _bEMAPSPG = (bool)chk_SPG.IsChecked;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            eMap.SetCameraPos(_dicConfig[ConfigField.V1]);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            eMap.SetCameraPos(_dicConfig[ConfigField.V2]);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            eMap.SetCameraPos(_dicConfig[ConfigField.V3]);
        }
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            eMap.SetCameraPos(_dicConfig[ConfigField.V4]);
        }

        private void txt_Battery_Copy8_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            return;
        }

        private void Image_Init(object sender, MouseButtonEventArgs e)
        {
            InitEMap(DateTime.Now.Ticks.ToString());
        }

        private void AppControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Btn_Effect_Click(object sender, RoutedEventArgs e)
        {
            //eMap.changeTierEffect("0010", 1, "#FF000000");
            //eMap.ObjectEffectTurnOn("0010", 1);
            //string sRackID = "0010";
            //int iTier = 1;
            //eMap.SetShadowMode(false);
            eMap.moveDestNode("99999");
            //eMap.setEffectColor("#FFFF007F");
            //eMap.ObjectEffectTurnOn(sRackID, iTier);
            //eMap.changeTierEffect(sRackID, iTier, "#FF000064");
        }

        private void Btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Btn_DisConnect_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Btn_Config_Click(object sender, RoutedEventArgs e)
        {
            string sTitle = "SpiderGO";
            string sExeFile = @"C:\eMap3D\appBin\CommonConfig.exe";

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.Arguments = string.Format("{0} {1} {2}", sMetaFilePath, sOptionFilePath, sTitle);
            psi.FileName = sExeFile;
            psi.WorkingDirectory = _UTIL.GetFolderInfo(sExeFile);

            Process.Start(psi);

        }

        string CheckONOFF(string sON_OFF)
        {
            if (sON_OFF.Equals("ON"))
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }

        private void Btn_Config_Apply_Click(object sender, RoutedEventArgs e)
        {
            FrmViewConfig fr = new FrmViewConfig();
            DialogResult dr = fr.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                string sResult = _UTIL.GetLastString(FrmViewConfig._sSelectedView, "_");

                _dicConfig[sResult] = this.cameraPOS;
                _UTIL.SaveOptionFromFile(Dic2Hash(_dicConfig), sOptionFilePath, "`");
                System.Windows.MessageBox.Show(this, string.Format("현재 뷰가 {0}으로 설정되었습니다.", sResult));
            }
            //string sWebPath = @"Z:\WWW\test.txt";
            //Hashtable htConfig = _UTIL.LoadOptionFromFile(sOptionFilePath);
            //string sTmp = string.Format("{0} {1} {2} {3} {4}", txt_Battery.Text, CheckONOFF(txt_Battery_Copy1.Text)
            //    , CheckONOFF(htConfig["AUTOSTART"].ToString()), htConfig["HOUR"], htConfig["MIN"]);
            //File.WriteAllText(sWebPath, sTmp);
            //MessageBox.Show(this, "환경설정이 적용되었습니다!");
            //bool bResult = _UTIL.ProcessFront("dragonfly");
            //MessageBox.Show(bResult.ToString());
        }

        private void Cmb_Material_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmb_Material.SelectedItem == null)
            {
                return;
            }
            _sMaterial = cmb_Material.SelectedItem.ToString();
            XMLEdit(_sXMLPath, _sPathDFTmp);
        }
        private void Cmb_MapSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmb_MapSize.SelectedItem == null)
            {
                return;
            }
            string sMap = cmb_MapSize.SelectedItem.ToString();
            int iMinSize = 200;

            if (sMap.StartsWith("0"))
            {
                appControl.Height = appControl.Width = iMinSize;
            }
            else if (sMap.StartsWith("1"))
            {
                appControl.Width = 946;
                appControl.Height = 778;
            }
            else
            {
                appControl.Width = 1580;
                appControl.Height = 1030;
            }
        }

        private void Txt_Battery_Copy7_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Txt_CameraPos_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                eMap.SetCameraPos(txt_CameraPos.Text);
            }
        }

        private void chk_CellToggle(object sender, RoutedEventArgs e)
        {
            bool bChk = (bool)chk_CellState.IsChecked;
            Set_State(bChk);
        }
    }
}