using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HostingEmap
{
    /// <summary>
    /// FieldName 클래스
    /// const 변수로 Field 별 ID를 지정한다.
    /// </summary>
    public class FieldNameRequest
    {
        /// <summary>
        /// MES - MES 는 UNIA 호출값의 변수명이 제대로 작명되지 않았으며 그냥 I1~I10 이런식으로 순서대로 호출됨.
        /// </summary>
        public const string MES_I1 = "I1";
        public const string MES_I2 = "I2";
        public const string MES_I3 = "I3";
        public const string MES_I4 = "I4";
        public const string MES_I5 = "I5";
        public const string MES_I6 = "I6";
        public const string MES_I7 = "I7";
        public const string MES_I8 = "I8";
        public const string MES_I9 = "I9";
        public const string MES_I10 = "I10";
        public const string MES_I11 = "I11";
        public const string MES_I12 = "I12";

        public const string MES_ICHAR = "ICHAR";
        public const string MES_IROW_CNT = "IROW_CNT";
        public const string MES_ICOL_CNT = "ICOL_CNT";

        /// <summary>
        /// V_SAWON_ID - 길이 6의 문자 필드 (20130627)
        /// </summary>
        public const string V_SAWON_ID = "V_SAWON_ID";


        /// <summary>
        /// V_SAWON_PW -문자필드, 길이 7 (20130627)
        /// </summary>
        public const string V_SAWON_PW = "V_SAWON_PW";


        /// <summary>
        /// SAWON_ID - 길이 6의 문자 필드 (20130705 - CES)
        /// </summary>
        public const string SAWON_ID = "SAWON_ID";


        /// <summary>
        /// SAWON_PW -문자필드, 길이 7 (20130705 - CES)
        /// </summary>
        public const string SAWON_PW = "SAWON_PW";


        /// <summary>
        /// IUSERID - 로그인 사번 (20130625)
        /// </summary>
        public const string IUSERID = "IUSERID";

        /// <summary>
        /// IUSER - CES로그인 사번 (20140102)
        /// </summary>
        public const string IUSER = "IUSER";


        /// <summary>
        /// IUSERPASSWORD - 로그인 암호 (20130625)
        /// </summary>
        public const string IUSERPASSWORD = "IUSERPASSWORD";

        /// <summary>
        /// Tag Hexa Header - 19 Digits
        /// </summary>
        public const string V_ITEMTAG_HEADER = "V_ITEMTAG_HEADER";

        /// <summary>
        /// Number of Limit rows for Download tag data.
        /// </summary>
        public const string V_MAX_ITEMTAG_CNT = "V_MAX_ITEMTAG_CNT";

        /// <summary>
        /// 제품코드
        /// </summary>
        public const string V_MATNR = "V_MATNR";

        /// <summary>
        /// 제품명
        /// </summary>
        public const string V_MAKTX = "V_MAKTX";

        /// <summary>
        /// 마스터 업데이트 날짜
        /// </summary>
        public const string V_UDATE = "V_UDATE";


        /// <summary>
        /// 조회 시작 날짜
        /// </summary>
        public const string V_SDATE = "V_SDATE";


        /// <summary>
        /// 조회 끝 날짜
        /// </summary>
        public const string V_EDATE = "V_EDATE";

        /// <summary>
        /// 제조번호
        /// </summary>
        public const string V_LOT_NO = "V_LOT_NO";


        /// <summary>
        /// 작업상태 (0~5)
        /// </summary>
        public const string V_WORK_FG = "V_WORK_FG";


        /// <summary>
        /// V_WERKS -_- 뭔지 아직 모르겠음 (20130212)
        /// </summary>
        public const string V_WERKS = "V_WERKS";

        /// <summary>
        /// V_LIFNR -_- 뭔지 아직 모르겠음 (20130212)
        /// </summary>
        public const string V_LIFNR = "V_LIFNR";

        /// <summary>
        /// V_LICHA -_- 공급자 배치번호
        /// </summary>
        public const string V_LICHA = "V_LICHA";

        /// <summary>
        /// V_CHARG - 배치정보 (20130212)
        /// </summary>
        public const string V_CHARG = "V_CHARG";

        /// <summary>
        /// V_EMP_NO - 사원번호 (20130212)
        /// </summary>
        public const string V_EMP_NO = "V_EMP_NO";

        ///// <summary>
        ///// EMP_KOR_NM - 사원이름
        ///// </summary>
        //public const string EMP_KOR_NM = "EMP_KOR_NM";

        /// <summary>
        /// ENAME - 사원이름
        /// </summary>
        public const string USER_NM = "USER_NM";

        /// <summary>
        /// V_EMP_NO - CES id (20140412)
        /// </summary>
        public const string V_CES_ID = "V_CES_ID";

        /// <summary>
        /// GROUP_CODE - 권한확인 (20141012)
        /// </summary>
        public const string ADM_GRP_CL_CD = "ADM_GRP_CL_CD";

        /// <summary>
        /// V_IPGO_DATE - 태그 입고 날짜 (20130212)
        /// </summary>
        public const string V_IPGO_DATE = "V_IPGO_DATE";

        /// <summary>
        /// ITEM_TAG - 전송할 rfid 태그 리스트, hexa 형태 (20130212)
        /// </summary>
        public const string ITEM_TAG = "ITEM_TAG";

        /// <summary>
        /// T_JAE_ITEM_TAG - 전송할 rfid 태그 리스트, hexa 형태 (20130212)
        /// </summary>
        public const string T_JAE_ITEM_TAG = "T_JAE_ITEM_TAG";

        /// <summary>
        /// ECODE - 프로시저 호출 결과코드 (20130212)
        /// </summary>
        public const string ECODE = "ECODE";

        /// <summary>
        /// MESSAGE - 프로시저 호출 결과 메세지 (20130212)
        /// </summary>
        public const string MESSAGE = "MESSAGE";

    }

    public class TagStatusStr
    {
        /// <summary>
        /// 4미확인제품
        /// </summary>
        public static string TAG_OTHER_PRODUCT = "4미확인제품";

        /// <summary>
        /// 3부적합태그
        /// </summary>
        public static string TAGINVALID = "3부적합태그";

        /// <summary>
        /// 9기입고
        /// </summary>
        public static string TAGNORMAL = "9기입고";

        /// <summary>
        /// 2중복검수
        /// </summary>
        public static string TAG_ALREADY_IPGO = "2중복검수";

        /// <summary>
        /// 5기존처리됨
        /// </summary>
        public static string TAG_ALREADY_IPGO_OTHERBATCH = "5기존처리됨";

        /// <summary>
        /// 8미입고
        /// </summary>
        public static string TAGNOTREAD = "8미입고";
    }

    public class FieldNameReplyEX
    {
        public const string PALT_IDS = "PALT_IDS";
        public const string PALT_ID = "PALT_ID";
        public const string BOX_TAG = "BOX_TAG";
        public const string BOX_TAGS = "BOX_TAGS";
        public const string USER_ID = "USER_ID";
        public const string USER_NM = "USER_NM";
        public const string USER_IDS = "USER_IDS";
        public const string PROD_ID = "PROD_ID";
        public const string PROD_IDS = "PROD_IDS";
        public const string PROD_NM = "PROD_NM";
        public const string BAT_NUM = "BAT_NUM";
        public const string PROD_QTY = "PROD_QTY";
        public const string BOX_ST_CD = "BOX_ST_CD";
        public const string BOX_ST_NM = "BOX_ST_NM";
        public const string TAG_CL_CD = "TAG_CL_CD";
        public const string TAG_CL_NM = "TAG_CL_NM";
        public const string O_ECODE = "O_ECODE";
        public const string O_MESSAGE = "O_MESSAGE";
        /// <summary>
        /// 작업 상태값 (0-작업예정,1-작업시작,2-일시중지,4-작업완료)
        /// </summary>
        public const string PROD_ORD_ST_CD = "PROD_ORD_ST_CD";
        public const string PROD_ORD_ST_NM = "PROD_ORD_ST_NM";

    }

    public class FieldNameReply
    {
        /// <summary>
        /// ces사번
        /// </summary>
        public const string USER_ID = "USER_ID";

        /// <summary>
        /// 제품코드
        /// </summary>
        public const string MATNR = "MATNR";

        /// <summary>n
        /// 제품코드 - 축약
        /// </summary>
        public const string MATNRS = "MATNRS";

        /// <summary>
        /// 제품명
        /// </summary>
        public const string MAKTX = "MAKTX";

        /// <summary>
        /// 마스터 업데이트 날짜
        /// </summary>
        public const string UDATE = "UDATE";


        /// <summary>
        /// 조회 시작 날짜
        /// </summary>
        public const string SDATE = "SDATE";


        /// <summary>
        /// 조회 끝 날짜
        /// </summary>
        public const string EDATE = "EDATE";

        /// <summary>
        /// 제조번호 - 상품
        /// </summary>
        public const string JEJO_NO = "JEJO_NO";

        /// <summary>
        /// 제조번호 - 포장
        /// </summary>
        public const string LOT_NO = "LOT_NO";


        /// <summary>
        /// 작업상태 (0~5)
        /// </summary>
        public const string WORK_FG = "WORK_FG";

        /// <summary>
        /// WERKS -_- 뭔지 아직 모르겠음 (20130212)
        /// </summary>
        public const string WERKS = "WERKS";

        /// <summary>
        /// LIFNR -_- 뭔지 아직 모르겠음 (20130212)
        /// </summary>
        public const string LIFNR = "LIFNR";

        /// <summary>
        /// LICHA -_- 공급자 배치번호
        /// </summary>
        public const string LICHA = "LICHA";

        /// <summary>
        /// CHARG - 배치정보 (20130212)
        /// </summary>
        public const string CHARG = "CHARG";

        /// <summary>
        /// ENCODING_CHARG - 수탁배치정보 (20130812)
        /// </summary>
        public const string ENCODING_CHARG = "ENCODING_CHARG";

        /// <summary>
        /// IPGO_DATE - 태그 입고 날짜 (20130212)
        /// </summary>
        public const string IPGO_DATE = "IPGO_DATE";

        /// <summary>
        /// _ROWID - 결과 테이블의 임시 순번 (20130212)
        /// </summary>
        public const string _ROWID = "_ROWID";

        /// <summary>
        /// RFID_GUBUN - RFID 구분 (커스텀 or 표준EPC?) - (20130212)
        /// </summary>
        public const string RFID_GUBUN = "RFID_GUBUN";

        /// <summary>
        /// LASTSEEN - 태그가 인식된 시간 (YYMMDDHHMMSS)
        /// </summary>
        public const string LASTSEEN = "LASTSEEN";

        /// <summary>
        /// 표준바코드
        /// </summary>
        public const string EAN11 = "EAN11";

        /// <summary>
        /// 태그 처리 상태
        /// </summary>
        public const string TAGSTATUS = "TAGSTATUS";


        /// <summary>
        /// 태그값 (HEXAID)
        /// </summary>
        public const string HEXAID = "HEXAID";

        /// <summary>
        /// 시리얼번호 (SERIAL)
        /// </summary>
        public const string SERIAL = "SERIAL";

        /// <summary>
        /// 외부보고용 시리얼번호 (고유번호)
        /// </summary>
        public const string SERIALEX = "SERIALEX";

        /// <summary>
        /// 진행수량
        /// </summary>
        public const string IPGOCOUNT = "IPGOCOUNT";

        /// <summary>
        /// 지시수량 (포장)
        /// </summary>
        public const string PACK_PLAN_QTY = "PACK_PLAN_QTY";

        /// <summary>
        /// 지시수량 (상품)
        /// </summary>
        public const string MENGE = "MENGE";
    }


    public class clsUniAKeyType
    {
        public static string RESULT = "RESULT";
        public static string RC = "RC";
    }

    // UniA Data Type
    public class clsDataType
    {
        public static int StringType = 1;
        public static int Int32Type = 2;
        public static int DateTiem = 3;
    }

    // 작업 구분
    public class clsWorkGubun
    {
        public static int PQC = 1;
        public static int PPR = 2;

    }

    public enum UNIAReplyIndex : int
    {
        ECODE = 0x0000,
        MESSAGE = 0x0001,
        DATATABLE = 0x0002,
    }


    public class UNIAReplyCode
    {
        /// <summary>
        /// UNIA 응답코드 - 000 : 정상
        /// </summary>
        public const string R000 = "000";
        /// <summary>
        /// UNIA 응답코드 - 111 : 저장 혹은 조회 실패 (재전송)
        /// </summary>
        public const string R111 = "111";
        /// <summary>
        /// UNIA 응답코드 - 222 : 저장 혹은 조회 실패 (데이터 버림)
        /// </summary>
        public const string R222 = "222";
        /// <summary>
        /// UNIA 응답코드 - 333 : 결과값이 조회되지 않았습니다
        /// </summary>
        public const string R333 = "333";
        /// <summary>
        /// UNIA 응답코드 - 444 : NULL 값이 조회되었습니다
        /// </summary>
        public const string R444 = "444";

    }
    //public class FieldMaster
    //{
    //    /// <summary>
    //    /// 제품코드
    //    /// </summary>
    //    public const string JEJO_NO = "JEJO_NO";

    //    /// <summary>
    //    /// 표준바코드
    //    /// </summary>
    //    public const string EAN11 = "EAN11";

    //    /// <summary>
    //    /// 제품코드
    //    /// </summary>
    //    public const string MATNR = "MATNR";

    //    /// <summary>
    //    /// 제품명
    //    /// </summary>
    //    public const string MAKTX = "MAKTX";

    //}
}
