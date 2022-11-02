using Common.Data.EPC;
using Common.Utils.Log;
using Common.Utils.Tools;
using DS.Core.Network.TCP.AsyncSockets;
using eMapLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace HostingEmap
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        // Emap 관련 변수
        private eMapServer eMap;
        public string cameraPOS;
        public string eMap_Clicked;
        bool bTestData = false;

        public static string _baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string _exePath = System.Environment.GetCommandLineArgs()[0];
        public static string _appBaseDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

        //범용변수
        static string _sLog = nameof(MainWindow);
        static ConsoleUtil _UTIL = new ConsoleUtil();
        DispatcherTimer _tmMsgPolling = new DispatcherTimer();
        public static string _sDivStringSlash = "/";
        public const string _sDivStrComma = ",";
        //public const string _sEMapXMLPath = "DEFAULT";
        public const string _sEMapXMLPath = @"C:\eMap3D\emap.xml";
        public const string _sDivStrEMart = "`";
        public static DataTable _dtMasterInfo = new DataTable();
        public static Dictionary<string, string> _dicCurrentJobMasterRecord = new Dictionary<string, string>();
        public static DataView _dtMasterInfoView = new DataView();
        public static Dictionary<string, string> _dicNodeInfo = new Dictionary<string, string>();

        ///TCP 메세지 수신, 발신.
        /// 비동기 소켓 클라이언트
        public static AsyncSocketClient _pAsyncSocketClient = null;
        public static bool _bICSRConnect = false;
        /// 비동기 소켓 서버
        public static AsyncSocketServer _pAsyncSocketServer = null;
        public static string _sRecvBuffer = string.Empty;
        public static List<string> _lsTCPRecv = new List<string>();
        public static int _nID;
        /// 비동기 소켓 클라이언트 목록
        public static List<AsyncSocketClient> _pAsyncSocketClientList = new List<AsyncSocketClient>();

        public static List<byte> _lsTCPBuffer = new List<byte>();
        public static List<string> _lsLogQueue = new List<string>();
        public static List<string> _lsErrorMsgList = new List<string>();

        List<Dictionary<string, string>> lsDicArray = new List<Dictionary<string, string>>();
        List<string> lsMessage = new List<string>();
        DataTable dtMaster = new DataTable();

        string sCameraPos_Init = "5.842119,1.627855,5.519603,12.00816,216.2473,0";
        string sCameraPos_BigRack = "3.269714,0.5922918,3.858808,359.9898,180.7484,0";
        string sCameraPos_SmallRack = "2.824755,0.3481083,1.556059,357.7602,265.4985,0";
        string sCameraPos_Top = "2.703658,5.653304,1.606037,77.25869,179.4973,0";


        int iListenPort = 65531;

        #region TCP 메세지 송수신 처리

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public static void InitListenTCP(int iListenPort)
        {
            if (_pAsyncSocketClientList == null)
            {
                _pAsyncSocketClientList = new List<AsyncSocketClient>();
            }
            else
            {
                _pAsyncSocketClientList.Clear();
            }

            if (_pAsyncSocketServer != null)
            {
                _pAsyncSocketServer.Stop();
                _pAsyncSocketServer = null;
            }

            _pAsyncSocketServer = new AsyncSocketServer(iListenPort);
            _pAsyncSocketServer.AsyncSocketAccepted += new AsyncSocketAcceptedEventHandler(OnAcceptedListen);
            _pAsyncSocketServer.AsyncSocketError += new AsyncSocketErrorEventHandler(OnErrorListen);
            _nID = 0;
            _pAsyncSocketServer.Listen();
            LogManager.Trace2(_sLog, "{0} - Listen TCP port : {1}", _UTIL.GetMethodName(), _pAsyncSocketServer.Port);
        }

        #region 인수시 처리하기 - OnAccepted(pSender, pAsyncSocketAcceptedEventArgs)

        /// <summary>
        /// 인수시 처리하기
        /// </summary>
        /// <param name="pSender">이벤트 발생자</param>
        /// <param name="pAsyncSocketAcceptedEventArgs">이벤트 인자</param>
        private static void OnAcceptedListen(object pSender, AsyncSocketAcceptedEventArgs pAsyncSocketAcceptedEventArgs)
        {
            AsyncSocketClient pAsyncSocketClient = new AsyncSocketClient(_nID++, pAsyncSocketAcceptedEventArgs.Socket);
            pAsyncSocketClient.Receive();

            pAsyncSocketClient.AsyncSocketConnected += new AsyncSocketConnectedEventHandler(OnConnectedListen);
            pAsyncSocketClient.AsyncSocketClosed += new AsyncSocketClosedEventHandler(OnClosedListen);
            pAsyncSocketClient.AsyncSocketError += new AsyncSocketErrorEventHandler(OnErrorListen);
            pAsyncSocketClient.AsyncSocketSent += new AsyncSocketSentEventHandler(OnSent);
            pAsyncSocketClient.AsyncSocketReceived += new AsyncSocketReceivedEventHandler(OnReceivedListen);

            _pAsyncSocketClientList.Add(pAsyncSocketClient);

            //UpdateMessage(_tbMessage, );
            LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetClassName(pSender), "HOST -> PC : Accepted :" + pAsyncSocketAcceptedEventArgs.ToString());
        }

        #endregion

        #region 연결시 처리하기 - OnConnected(pSender, pAsyncSocketConnectedEventArgs)

        /// <summary>
        /// 연결시 처리하기
        /// </summary>
        /// <param name="pSender">이벤트 발생자</param>
        /// <param name="pAsyncSocketConnectedEventArgs">이벤트 인자</param>
        private static void OnConnectedListen(object pSender, AsyncSocketConnectedEventArgs pAsyncSocketConnectedEventArgs)
        {
            //UpdateMessage(_tbMessage, "HOST -> PC : Connected ID : " + pAsyncSocketConnectedEventArgs.ID.ToString() + "\n");
            LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetClassName(pSender), "HOST -> PC : Connected ID :" + pAsyncSocketConnectedEventArgs.ID.ToString());
        }

        #endregion

        #region 수신시 처리하기 - OnReceived(pSender, pAsyncSocketReceivedEventArgs)

        /// <summary>
        /// 수신시 처리하기
        /// </summary>
        /// <param name="pSender">이벤트 발생자</param>
        /// <param name="pAsyncSocketReceivedEventArgs">이벤트 인자</param>
        private static void OnReceivedListen(object pSender, AsyncSocketReceivedEventArgs pAsyncSocketReceivedEventArgs)
        {
            //UpdateMessage(_tbMessage, );
            LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetClassName(pSender), "HOST -> PC : Connected ID :" + "HOST -> PC : Receive ID : " + pAsyncSocketReceivedEventArgs.ID.ToString() + ", Bytes received : " + pAsyncSocketReceivedEventArgs.ReceiveByteCount.ToString());

            //_lsTCPBuffer.AddRange(pAsyncSocketReceivedEventArgs.ReceiveData);
            for (int i = 0; i < pAsyncSocketReceivedEventArgs.ReceiveData.Length; i++)
            {
                if (pAsyncSocketReceivedEventArgs.ReceiveData[i] == (byte)('\0'))
                {
                    break;
                }
                else
                {
                    _lsTCPBuffer.Add(pAsyncSocketReceivedEventArgs.ReceiveData[i]);
                }
            }

            List<byte> lsTmp = new List<byte>(_lsTCPBuffer.ToArray());
            //_sRecvBuffer += sData;
            byte bySplitter = (byte)(10);
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
                    string sData = Encoding.Default.GetString(byArTmp);
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


        #region 메세지 보내기
        public static void ReplyTcpMsg(string sMsg, int iSocketID)
        {
            string strMessage = sMsg + "\n";

            for (int i = 0; i < _pAsyncSocketClientList.Count; i++)
            {
                if (_pAsyncSocketClientList[i].ID == iSocketID || iSocketID < 0)
                {
                    _pAsyncSocketClientList[i].Send(Encoding.Default.GetBytes(strMessage));
                    LogManager.Trace2(_sLog, "{0} : {1} - Send Data : {2}", _UTIL.GetMethodName(), iSocketID, sMsg);
                    _lsLogQueue.Add("=> " + strMessage);

                    if (iSocketID >= 0)
                    {
                        break;
                    }
                }


            }
        }

        public static void ReplyToAllTcpMsg(string sMsg)
        {
            ReplyTcpMsg(sMsg, -1);
        }


        #endregion

        #region 전송시 처리하기 - OnSent(pSender, pAsyncSocketSentEventArgs)



        /// <summary>
        /// 전송시 처리하기
        /// </summary>
        /// <param name="pSender">이벤트 발생자</param>
        /// <param name="pAsyncSocketSentEventArgs">이벤트 인자</param>
        private static void OnSent(object pSender, AsyncSocketSentEventArgs pAsyncSocketSentEventArgs)
        {
            //UpdateMessage(_tbMessage, ;
            LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetClassName(pSender), "PC -> HOST : Send ID : " + pAsyncSocketSentEventArgs.ID.ToString() + ", Bytes sent : " + pAsyncSocketSentEventArgs.SendByteCount.ToString());

        }

        #endregion

        #region 폐쇄시 처리하기 - OnClosedListen(pSender, pAsyncSocketConnectedEventArgs)

        /// <summary>
        /// 폐쇄시 처리하기
        /// </summary>
        /// <param name="pSender">이벤트 발생자</param>
        /// <param name="pAsyncSocketConnectedEventArgs">이벤트 인자</param>
        private static void OnClosedListen(object pSender, AsyncSocketConnectedEventArgs pAsyncSocketConnectedEventArgs)
        {
            //UpdateMessage(_tbMessage, 
            LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetClassName(pSender), "HOST -> PC : Closed ID : " + pAsyncSocketConnectedEventArgs.ID.ToString());
        }

        #endregion

        #region 에러시 처리하기 - OnError(pSender, pAsyncSocketErrorEventArgs)

        /// <summary>
        /// 에러시 처리하기
        /// </summary>
        /// <param name="pSender">이벤트 발생자</param>
        /// <param name="pAsyncSocketErrorEventArgs">이벤트 인자</param>
        private static void OnErrorListen(object pSender, AsyncSocketErrorEventArgs pAsyncSocketErrorEventArgs)
        {
            //UpdateMessage(_tbMessage, 
            string sMsgTmp = "HOST -> PC : Error ID : " + pAsyncSocketErrorEventArgs.ID.ToString() + "Error Message : " + pAsyncSocketErrorEventArgs.Exception.ToString();
            LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetClassName(pSender), sMsgTmp);
            _lsErrorMsgList.Add(sMsgTmp);

            for (int i = 0; i < _pAsyncSocketClientList.Count; i++)
            {
                if (_pAsyncSocketClientList[i].ID == pAsyncSocketErrorEventArgs.ID)
                {
                    _pAsyncSocketClientList.Remove(_pAsyncSocketClientList[i]);
                    break;
                }
            }
        }

        /// <summary>
        /// EAN11 값을 기준으로 마스터 정보를 구해온다.
        /// </summary>
        /// <param name="sEAN11">바코드정보 GTIN-14</param>
        /// <returns></returns>
        public static List<Dictionary<string, string>> GetMasterInfoBarCD(string sEAN11)
        {
            return GetMasterInfoBarCD(sEAN11, string.Empty, false);
        }

        public static List<Dictionary<string, string>> GetMasterInfoBarCD(string sEAN11, string sRFIDGUBUN, bool bEqualRFIDGUBUN)
        {
            string sEqualRFIDGUBUN = "<>";
            sEAN11 = sEAN11.TrimStart('0');
            if (bEqualRFIDGUBUN)
            {
                sEqualRFIDGUBUN = "=";
            }
            List<Dictionary<string, string>> dicInfoArray = new List<Dictionary<string, string>>();
            Dictionary<string, string> dicInfo = new Dictionary<string, string>();
            string sExpression = string.Format("{0} = '{1}' AND {2} {3} '{4}' ", FieldNameReply.EAN11, sEAN11, FieldNameReply.RFID_GUBUN, sEqualRFIDGUBUN, sRFIDGUBUN);
            try
            {
                //string sTmp = _UTIL.StringMerge(_UTIL.DataTable2Strings(_dtMasterInfo, true, " / "), "\r\n");
                //LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetMethodName(), sTmp);
                dicInfoArray = new List<Dictionary<string, string>>(_UTIL.DataTable2DicArray(_UTIL.SelectFromDataTable(_dtMasterInfo, sExpression, string.Empty)));
            }
            catch (Exception ex)
            {
                LogManager.Trace2(_sLog, "{0}-{1}", _UTIL.GetMethodName(), ex.Message);
            }
            return dicInfoArray;
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
        public static string _sNodeMasterFULLPath = _appBaseDir + "\\MASTER\\Node_Master.ini";
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

                // 마스터 데이터 테이블을 바코드 순서대로 정렬. (바코드로 검색시 검색시간 빨라짐)
                _dtMasterInfo = _UTIL.Strings2DataTable(lsNewMaster.ToArray(), true, _sDivStrEMart);
                _dtMasterInfoView = new DataView(_dtMasterInfo);
                _dtMasterInfoView.Sort = string.Format("{0}, {1}", FieldNameReply.EAN11, FieldNameReply.RFID_GUBUN);
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


        void RackCellClicked(string sClickInfo)
        {
            try
            {
                string[] saClickInfo = sClickInfo.Split(",".ToArray());
                string sRackID = saClickInfo[0].PadLeft(4,'0');
                string sTierID = saClickInfo[1];

                foreach (string sKey in cmb_Location.Items)
                {
                    string sNode = sKey.ToString();

                    if (sNode.StartsWith(sRackID))
                    {
                        cmb_Location.SelectedItem = sKey;
                        if (cmb_Ant.Items.Contains(sTierID))
                        {
                            cmb_Ant.SelectedItem = sTierID;
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Trace0(_sLog, "{0}-{1}", _UTIL.GetMethodName(), ex.ToString());
            }
        }

        void InitEMap(string v)
        {
            try
            {
                dtMaster.Clear();
                bTestData = false;
                eMap.rotateDragnFly(90);
                eMap.SetCameraLock(true);
                eMap.SetCameraPos(sCameraPos_Init);
                eMap.SetShadowMode(false);
                Set_NORMAL_Status_ALL(true);

                LogManager.Trace2(_sLog, "{0} - {1}", "초기화 완료!", _UTIL.GetTimeString(new DateTime(long.Parse(v))));
            }
            catch (Exception ex)
            {
                LogManager.Trace0(_sLog, "{0}-{1}", _UTIL.GetMethodName(), ex.ToString());
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Process[] proc_eMap = Process.GetProcessesByName("eMap");
            if (proc_eMap != null)
            {
                for (int i = 0; i < proc_eMap.Length; i++)
                {
                    proc_eMap[i].Kill();
                }
            }

            //appControl.startPosX = 0;
            //appControl.startPosY = 50;
            appControl.ExeName = @"C:\eMap3D\eMap.exe";
            //appControl.connectString = "127.0.0.1, 9999"; //명령줄인수 서버ip,port 입력 default loopback 9999번 포트
            System.Windows.Application.Current.Exit += new ExitEventHandler((s, e) => { appControl.Dispose(); });

            eMap = new eMapServer();
            var listener = new eMapListener();
            listener.selectedAction += (v =>
            {
                eMap_Clicked = v.Replace(" ","");
                LogManager.Trace2(_sLog, "{0} - {1}", nameof(eMap_Clicked), eMap_Clicked);
                //eMap.setEffectColor(255, 255, 0, 128); //20180912 현재로서는 전체 색깔 변경 밖에 안됨. 셀별로 색상 지정 가능하게 요청.

                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    RackCellClicked(eMap_Clicked);
                }), DispatcherPriority.ApplicationIdle);
            });

            listener.cameraPosAction += (v =>
            {
                this.cameraPOS = v;
                LogManager.Trace2(_sLog, "{0} - {1}", nameof(cameraPOS), cameraPOS);

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
            InitListenTCP(iListenPort);
            _dicNodeInfo = _UTIL.LoadDicFromFile(_sNodeMasterFULLPath, _sDivStrEMart);

            appControl.startPosX = 300;
            appControl.startPosY = 20;

            this._tmMsgPolling.Interval = TimeSpan.FromMilliseconds(300);
            this._tmMsgPolling.Tick += new System.EventHandler(_tmMsgPolling_Tick);
            _tmMsgPolling.Start();
        }

        private void _tmMsgPolling_Tick(object sender, EventArgs e)
        {
            UpdateConnectionStatus();
            lsMessage = new List<string>(_lsTCPRecv.ToArray());
            if (lsMessage.Count == 0)
            {
                return;
            }
            else
            {
                _lsTCPRecv.Clear();
            }

            string sResult = string.Empty;

            Dictionary<string, string> dicUpdate = new Dictionary<string, string>();
            foreach (string sMsg in lsMessage)
            {
                LogManager.Trace0(_sLog, "MSG receiverd : {0}\r\n{1}", _UTIL.GetMethodName(), sMsg);
                string sMsgTmp = _UTIL.GetLastString(sMsg, _sDivStringSlash).Replace("\r\n", string.Empty);
            
                if (sMsg.StartsWith("STATUS"))
                {
                    dicUpdate = _UTIL.Strings2Dic(sMsgTmp.Split(_sDivStrComma.ToArray()), _sDivStrEMart);
                    //System.Windows.Forms.Control ctl = new System.Windows.Forms.Control();
                    var ds = SetTextBoxesWPF(this.gb_Alive, "t", dicUpdate);
                    ///_UTIL.FillTextBoxes(dicUpdate, , false, true, true);

                    if (dicUpdate.ContainsKey("START"))
                    {
                        string sSEQ = dicUpdate["START"];
                        int iChk = -1;
                        bool bValidNode = int.TryParse(sSEQ, out iChk);
                        //string sCurrentCol = _dicNodeInfo[sSEQ];
                        if (sSEQ.Equals("0002"))
                        {
                            sSEQ = "0201";
                        }
                        if (bValidNode)
                        {
                            Set_DF_Status(sSEQ);
                        }
                    }

                    if(dicUpdate.ContainsKey("SEQ"))
                    {
                        string sSEQ = dicUpdate["SEQ"];
                        if (sSEQ.Equals("0001"))
                        {
                            InitEMap(DateTime.Now.Ticks.ToString());
                        }
                    }
                }
                else if (sMsg.StartsWith("ALARM"))
                {
                    //tBox_Message.AppendText(sMsgTmp);
                    string[] saAlarm = sMsg.Split("/".ToCharArray());
                    MessageBox.Show(saAlarm[1], saAlarm[0]);
                }
                else if (sMsg.StartsWith("MSG"))
                {
                    //tBox_Message.AppendText(sMsgTmp);
                    InitEMap(DateTime.Now.Ticks.ToString());
                }
                if (sMsg.StartsWith("TAGLIST"))
                {
                        List<string> lsEPC = new List<string>();
                    
                    //로그 기록
                    lsDicArray.Clear();
                    dtMaster.Clear();
                    //DATATABLE 구성, 조회 버튼 활성화.
                    string[] saDT = sMsgTmp.Split(_sDivStrEMart.ToArray()); //
                    foreach (string sRaw in saDT) //0000000000001234567890CE,4-636537974569600406
                    {
                        //Dictionary<string, string> dicRaw = new Dictionary<string, string>();
                        string EPC = _UTIL.GetIndexString(sRaw, _sDivStrComma, 0);
                        string Location = _UTIL.GetIndexString(sRaw, _sDivStrComma, 1).Replace("0002","0201");
                      

                        string Ant = _UTIL.GetIndexString(Location, "-", 0);
                        string Node = _UTIL.GetIndexString(Location, "-", 1);
                        int iStartNode = int.Parse(_UTIL.GetIndexString(Node, "_", 0));
                        int iEndNode = int.Parse(_UTIL.GetIndexString(Node, "_", 1));

                        if (iStartNode > iEndNode)
                        {
                            Node = string.Format("{0}_{1}", iEndNode.ToString("0000"), iStartNode.ToString("0000"));
                        }
                        EPCTagInfo epcInfo = EPCTagInfo.ParseEPC(EPC);
                        if (epcInfo == null)
                        {
                            continue;
                        }
                        List<Dictionary<string, string>> lsDic = GetMasterInfoBarCD(epcInfo.BARCODE, "01", true);
                        if (lsDic.Count > 0)
                        {
                            Dictionary<string, string> dicRaw = lsDic[0];
                            string sSerial = epcInfo.SERIAL_NUMBER_WHOLE.ToString().PadLeft(4, '0');
                            string sLocation = string.Format("{0}-{1}", Node, Ant);
                            dicRaw.Add(nameof(EPC), EPC);
                            dicRaw.Add(nameof(Ant), Ant);
                            dicRaw.Add(nameof(Node), Node);
                            dicRaw.Add("SERIAL", sSerial);
                            dicRaw.Add("BARCD", epcInfo.BARCODE);
                            dicRaw.Add("FILTER", GetEPCFilter(epcInfo.RFID_TYPE).ToString());
                            //if (epcInfo.SERIAL_NUMBER_WHOLE.ToString().Equals("1112"))
                            //{
                            //    LogManager.Trace0(_sLog, "MSG receiverd : {0}\r\n{1}", _UTIL.GetMethodName(), _UTIL.getXMLFromModel(dicRaw));
                            //}

                            if ( (epcInfo.BARCODE.Equals("1111111100011") && epcInfo.SERIAL_NUMBER_WHOLE < 101) 
                                || (epcInfo.BARCODE.Equals("1111111100011") && epcInfo.SERIAL_NUMBER_WHOLE > 500) )
                            {
                                //continue;
                            }
                            else
                            {
                                string sEPC_Serial = string.Format("{0}\t{1}", sSerial, sLocation);
                                lsDicArray.Add(dicRaw);
                                if(lsEPC.Contains(sEPC_Serial) == false)
                                {
                                    lsEPC.Add(sEPC_Serial);
                                }
                            }
                        }
                        //if (EPC.Equals("3036198218024BE0CEF912A1"))
                        //{
                        //    lsDicArray.Add(dicRaw);
                        //}
                        //else
                        //{
                        //    lsDicArray.Add(dicRaw);
                        //}
                    }
                    if (!bTestData)
                    {
                        string sSaveMath = @"C:\eMap3D\LOG\TAGTEST_" + _UTIL.GetCurrentTime() + "_" + lsDicArray.Count.ToString() + "개.txt";
                        File.WriteAllText(sSaveMath, sMsg);
                        lsEPC.Sort();
                        string sSaveMath2 = @"C:\eMap3D\LOG\TAGTEST_" + _UTIL.GetCurrentTime() + "_" + lsDicArray.Count.ToString() + "개.txt";
                        File.WriteAllLines(sSaveMath2, lsEPC.ToArray());
                        Process.Start(sSaveMath2);
                    }


                    if (lsDicArray.Count > 0)
                    {
                        dtMaster = _UTIL.DicArray2DataTable(lsDicArray.ToArray());
                        SetComboBoxItems(dtMaster);
                        //eMap.rotateDragnFly(90);
                        //eMap.moveDestNode("0001");
                        //MessageBox.Show("작업이 완료되었습니다", "재고조사작업 완료!");
                    }

                    Set_DF_Status("0001");
                }
            }
            sResult = _UTIL.StringMerge(lsMessage.ToArray(), "\r\n");
            //tBox_Message.AppendText(sResult);
            lsMessage.Clear();
        }


        void UpdateConnectionStatus()
        {
            string sConnectionCnt = _pAsyncSocketClientList.Count.ToString();
            if (_pAsyncSocketClientList.Count == 0)
            {
                lbl_StatusConnect.Background = new SolidColorBrush(Colors.Red);
                //lbl_StatusConnect.Content = "접속대기";
            }
            else
            {
                lbl_StatusConnect.Background = new SolidColorBrush(Colors.Green);
                //lbl_StatusConnect.Content = "접속중";
            }
            //lbl_StatusConnect.Content += string.Format("({0})", sConnectionCnt);
        }

        private void Set_DF_Status(string sSEQ)
        {
            eMap.moveDestNode(sSEQ);
            for (int j = 0; j < 4; j++)
            {
                int iRackID = int.Parse(sSEQ);
                int iTier = j + 1;
                //if (bNormal)
                //{
                //    eMap.ObjectEffectTurnOff(iRackID.ToString(), iTier);
                //}
                //else
                //{
                //    eMap.ObjectEffectTurnOn(iRackID.ToString(), iTier);
                //}
            }
            if (sSEQ.StartsWith("03") || sSEQ.StartsWith("3"))
            {
                eMap.rotateDragnFly(0);
            }
            else
            {
                eMap.rotateDragnFly(90);
            }

        }

        void SetComboBoxItems(DataTable dtMasterCurrent)
        {
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

            if (_dicNodeInfo.Count > 0)
            {
                Set_NORMAL_Status_ALL(true);
                //this.Dispatcher.Invoke((ThreadStart)(() => { }), DispatcherPriority.ApplicationIdle);
                //Thread.Sleep(1000);
                List<Dictionary<string, string>> lsDicResult = new List<Dictionary<string, string>>(_UTIL.DataTable2DicArray(dtMasterCurrent));
                foreach (Dictionary<string, string> dicTmp in lsDicResult)
                {
                    string sNode = _UTIL.GetIndexString(dicTmp["Node"], "_", 0);
                    int iNode = int.Parse(sNode);
                    if(iNode < 200)
                    {
                        iNode += 100;
                    }
                    Set_RED_Status(iNode.ToString(),dicTmp["Ant"]);
                }
            }
        }

        void InitCmbBox()
        {
            cmb_nodeTest.Items.Clear();

            Dictionary<int, int> dicRackInfo = new Dictionary<int, int>();
            dicRackInfo.Add(201, 5);
            dicRackInfo.Add(301, 9);

            foreach (int iKey in dicRackInfo.Keys)
            {
                for (int i = 0; i < dicRackInfo[iKey]; i++)
                {
                    cmb_nodeTest.Items.Add((iKey+i) .ToString().PadLeft(4, '0'));
                }
            }

            cmb_nodeTest.Items.Insert(0, "0002");
            cmb_nodeTest.Items.Insert(0, "0001");
        }

        private void Set_NORMAL_Status_ALL(bool bNormal)
        {
            Dictionary<int, int> dicRackInfo = new Dictionary<int, int>();
            dicRackInfo.Add(301, 9);
            dicRackInfo.Add(201, 5);

            foreach (int iKey in dicRackInfo.Keys)
            {
                for (int i = 0; i < dicRackInfo[iKey]; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        int iRackID = iKey + i;
                        int iTier = j + 1;
                        if (bNormal)
                        {
                            eMap.ObjectEffectTurnOff(iRackID.ToString(), iTier);
                        }
                        else
                        {
                            eMap.ObjectEffectTurnOn(iRackID.ToString(), iTier);
                        }
                    }
                }
            }
            //await Task.Delay(100);
        }

        private void Set_RED_Status(string v1, string v2)
        {
            eMap.ObjectEffectTurnOn(v1, int.Parse(v2));
        }

        private void btnTEST_Click(object sender, RoutedEventArgs e)
        {
            string sTagTest = @"C:\eMap3D\TAGSTATUS.txt";
            string saTagList = File.ReadAllText(sTagTest);
            _lsTCPRecv.Add(saTagList);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string sTagTest = @"C:\eMap3D\TAGTEST.txt";
            string saTagList = File.ReadAllText(sTagTest);
            bTestData = true;
            _lsTCPRecv.Add(saTagList);
        }

        private void cmb_Test_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string sNode = cmb_nodeTest.SelectedItem.ToString();
            //eMap.moveDestNode(sNode);
            Set_DF_Status(sNode);
        }

        private void cmb_Location_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string expression = string.Empty;
            string sortOrder = string.Empty;
            System.Windows.Controls.ComboBox ctlSender = (System.Windows.Controls.ComboBox)sender;
            ArrayList alTmp = GetObjByTagWPF(ctlSender.Name, gb_Search);
            if (alTmp.Count > 0)
            {
                System.Windows.Controls.TextBox ctlTmp = (System.Windows.Controls.TextBox)(alTmp[0]);

                if (ctlSender.SelectedItem == null)
                {
                    return;
                }

                ctlTmp.Text = ctlSender.SelectedItem.ToString();
                //ctlTmp.Text = ctlSender.SelectedValue.ToString();
                if (ctlTmp.Text.StartsWith("-"))
                {
                    return;
                }

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

                DataRow[] drTmp = dtMaster.Select(expression, sortOrder);
                DataTable dtTmp = new DataTable();
                dtTmp = dtMaster.Clone();
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
            SetComboBoxItems(dtMaster);
            FillTextBoxesWPF(gb_Search, "txt_", true, string.Empty);
        }

        private void btn_Reload_Click(object sender, RoutedEventArgs e)
        {
            eMap.loadMapData(_sEMapXMLPath);
            //eMap.rotateDragnFly(90);
            //eMap.SetShadowMode(false);
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            eMap.clearMap();
        }

        private void Image_TouchEnter(object sender, MouseButtonEventArgs e)
        {
            bool bState = false;
            if (gb_Btns.Visibility == Visibility.Hidden)
            {
                bState = true;
            }
            gb_Btns.Visibility = bState ? Visibility.Visible : Visibility.Hidden;
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
            Set_NORMAL_Status_ALL(bChk);
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            eMap.SetCameraPos(sCameraPos_Init);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            eMap.SetCameraPos(sCameraPos_BigRack);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            eMap.SetCameraPos(sCameraPos_SmallRack);
        }
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            eMap.SetCameraPos(sCameraPos_Top);
        }

        private void txt_Battery_Copy8_KeyDown(object sender, KeyEventArgs e)
        {
            string sTmp = txt_Battery_Copy8.Text;
            if (sTmp == null)
            {
                return;
            }
            if (e.Key == Key.Enter)
            {
                string sMAKTX = txt_Battery_Copy7.Text;
                List<string> saInputTmp = new List<string>(sTmp.Split(",".ToArray()));
                List<string> saInput = new List<string>();
                List<string> saInputRange = new List<string>(sTmp.Split("~".ToArray()));
                List<string> lsFound = new List<string>();

                if(saInputRange.Count == 2)
                {
                    saInput.Clear();
                    int iStart = int.Parse(saInputRange[0]);
                    int iEnd = int.Parse(saInputRange[1]);

                    int iSmall = iStart > iEnd ? iEnd : iStart;
                    int iBig = iStart < iEnd ? iEnd : iStart;

                    for (int i = iSmall; i <= iBig ; i++)
                    {
                        saInput.Add(i.ToString("0000"));
                    }
                }
                else
                {
                    foreach( string sTagSerial in saInputTmp)
                    {
                        saInput.Add(sTagSerial.PadLeft(4, '0'));
                    }
                }

                if (saInput.Count > 0)
                {
                    //2개의 연속된 번호를 입력하면 해당범위에 있는 숫자를 다 검색해서 박스를 빨갛게 표시해줌
                    Set_NORMAL_Status_ALL(true);
                    List<Dictionary<string, string>> lsDicResult = new List<Dictionary<string, string>>(_UTIL.DataTable2DicArray(dtMaster));
                    foreach (Dictionary<string, string> dicTmp in lsDicResult)
                    {
                        string sSerial = dicTmp["SERIAL"];
                        string sNodeOrg = dicTmp["Node"];
                        string sMAKTXOrg = dicTmp["MAKTX"];
                        string sNode = _UTIL.GetIndexString(sNodeOrg, "_", 0);
                        int iNode = int.Parse(sNode);

                        if (saInput.Contains(sSerial))
                        {
                            bool bChk = false;
                            if (sMAKTXOrg.Equals(sMAKTX) == true)
                            {
                                bChk = true;
                            }
                            else if (sMAKTX.Equals(string.Empty))
                            {
                                bChk = true;
                            }

                            if(bChk)
                            {
                                Set_RED_Status(iNode.ToString(), dicTmp["Ant"]);
                                lsFound.Add(sSerial);
                            }
                        }
                    }

                    if (lsFound.Count != saInput.Count)
                    {
                        foreach(string sFoundNum in lsFound)
                        {
                            saInput.Remove(sFoundNum);
                        }
                        string sLog = _UTIL.StringMerge(saInput.ToArray(), ",");
                        LogManager.Trace0(_sLog, "미인식 갯수 : {0}\r\n{1}", saInput.Count, sLog);
                        MessageBox.Show(sLog, "미인식 갯수 : " + saInput.Count.ToString());
                    }
                    else
                    {

                    }

                }


            }
        }

        private void Image_Init(object sender, MouseButtonEventArgs e)
        {
            InitEMap(DateTime.Now.Ticks.ToString());
        }
    }
}