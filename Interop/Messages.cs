﻿namespace Ecng.Interop
{
	using System;

	[CLSCompliant(false)]
	public enum Messages : uint
	{
// ReSharper disable InconsistentNaming
		BM_GETCHECK = 0x00F0,
		BM_SETCHECK = 0x00F1,
		BM_GETSTATE = 0x00F2,
		BM_SETSTATE = 0x00F3,
		BM_SETSTYLE = 0x00F4,
		BM_CLICK = 0x00F5,
		BM_GETIMAGE = 0x00F6,
		BM_SETIMAGE = 0x00F7,

		STM_SETICON = 0x0170,
		STM_GETICON = 0x0171,
		STM_SETIMAGE = 0x0172,
		STM_GETIMAGE = 0x0173,
		STM_MSGMAX = 0x0174,

		DM_GETDEFID = (WM.USER + 0),
		DM_SETDEFID = (WM.USER + 1),
		DM_REPOSITION = (WM.USER + 2),

		LB_ADDSTRING = 0x0180,
		LB_INSERTSTRING = 0x0181,
		LB_DELETESTRING = 0x0182,
		LB_SELITEMRANGEEX = 0x0183,
		LB_RESETCONTENT = 0x0184,
		LB_SETSEL = 0x0185,
		LB_SETCURSEL = 0x0186,
		LB_GETSEL = 0x0187,
		LB_GETCURSEL = 0x0188,
		LB_GETTEXT = 0x0189,
		LB_GETTEXTLEN = 0x018A,
		LB_GETCOUNT = 0x018B,
		LB_SELECTSTRING = 0x018C,
		LB_DIR = 0x018D,
		LB_GETTOPINDEX = 0x018E,
		LB_FINDSTRING = 0x018F,
		LB_GETSELCOUNT = 0x0190,
		LB_GETSELITEMS = 0x0191,
		LB_SETTABSTOPS = 0x0192,
		LB_GETHORIZONTALEXTENT = 0x0193,
		LB_SETHORIZONTALEXTENT = 0x0194,
		LB_SETCOLUMNWIDTH = 0x0195,
		LB_ADDFILE = 0x0196,
		LB_SETTOPINDEX = 0x0197,
		LB_GETITEMRECT = 0x0198,
		LB_GETITEMDATA = 0x0199,
		LB_SETITEMDATA = 0x019A,
		LB_SELITEMRANGE = 0x019B,
		LB_SETANCHORINDEX = 0x019C,
		LB_GETANCHORINDEX = 0x019D,
		LB_SETCARETINDEX = 0x019E,
		LB_GETCARETINDEX = 0x019F,
		LB_SETITEMHEIGHT = 0x01A0,
		LB_GETITEMHEIGHT = 0x01A1,
		LB_FINDSTRINGEXACT = 0x01A2,
		LB_SETLOCALE = 0x01A5,
		LB_GETLOCALE = 0x01A6,
		LB_SETCOUNT = 0x01A7,
		LB_INITSTORAGE = 0x01A8,
		LB_ITEMFROMPOINT = 0x01A9,
		LB_MULTIPLEADDSTRING = 0x01B1,
		LB_GETLISTBOXINFO = 0x01B2,
		LB_MSGMAX_501 = 0x01B3,
		LB_MSGMAX_WCE4 = 0x01B1,
		LB_MSGMAX_4 = 0x01B0,
		LB_MSGMAX_PRE4 = 0x01A8,

		CB_GETEDITSEL = 0x0140,
		CB_LIMITTEXT = 0x0141,
		CB_SETEDITSEL = 0x0142,
		CB_ADDSTRING = 0x0143,
		CB_DELETESTRING = 0x0144,
		CB_DIR = 0x0145,
		CB_GETCOUNT = 0x0146,
		CB_GETCURSEL = 0x0147,
		CB_GETLBTEXT = 0x0148,
		CB_GETLBTEXTLEN = 0x0149,
		CB_INSERTSTRING = 0x014A,
		CB_RESETCONTENT = 0x014B,
		CB_FINDSTRING = 0x014C,
		CB_SELECTSTRING = 0x014D,
		CB_SETCURSEL = 0x014E,
		CB_SHOWDROPDOWN = 0x014F,
		CB_GETITEMDATA = 0x0150,
		CB_SETITEMDATA = 0x0151,
		CB_GETDROPPEDCONTROLRECT = 0x0152,
		CB_SETITEMHEIGHT = 0x0153,
		CB_GETITEMHEIGHT = 0x0154,
		CB_SETEXTENDEDUI = 0x0155,
		CB_GETEXTENDEDUI = 0x0156,
		CB_GETDROPPEDSTATE = 0x0157,
		CB_FINDSTRINGEXACT = 0x0158,
		CB_SETLOCALE = 0x0159,
		CB_GETLOCALE = 0x015A,
		CB_GETTOPINDEX = 0x015B,
		CB_SETTOPINDEX = 0x015C,
		CB_GETHORIZONTALEXTENT = 0x015d,
		CB_SETHORIZONTALEXTENT = 0x015e,
		CB_GETDROPPEDWIDTH = 0x015f,
		CB_SETDROPPEDWIDTH = 0x0160,
		CB_INITSTORAGE = 0x0161,
		CB_MULTIPLEADDSTRING = 0x0163,
		CB_GETCOMBOBOXINFO = 0x0164,
		CB_MSGMAX_501 = 0x0165,
		CB_MSGMAX_WCE400 = 0x0163,
		CB_MSGMAX_400 = 0x0162,
		CB_MSGMAX_PRE400 = 0x015B,

		SBM_SETPOS = 0x00E0,
		SBM_GETPOS = 0x00E1,
		SBM_SETRANGE = 0x00E2,
		SBM_SETRANGEREDRAW = 0x00E6,
		SBM_GETRANGE = 0x00E3,
		SBM_ENABLE_ARROWS = 0x00E4,
		SBM_SETSCROLLINFO = 0x00E9,
		SBM_GETSCROLLINFO = 0x00EA,
		SBM_GETSCROLLBARINFO = 0x00EB,

		TV_FIRST = 0x1100,// TreeView messages
		HDM_FIRST = 0x1200,// Header messages
		TCM_FIRST = 0x1300,// Tab control messages
		PGM_FIRST = 0x1400,// Pager control messages

		BCM_FIRST = 0x1600,// Button control messages
		CBM_FIRST = 0x1700,// Combobox control messages
		CCM_FIRST = 0x2000,// Common control shared messages
		CCM_LAST = (CCM_FIRST + 0x200),
		CCM_SETBKCOLOR = (CCM_FIRST + 1),
		CCM_SETCOLORSCHEME = (CCM_FIRST + 2),
		CCM_GETCOLORSCHEME = (CCM_FIRST + 3),
		CCM_GETDROPTARGET = (CCM_FIRST + 4),
		CCM_SETUNICODEFORMAT = (CCM_FIRST + 5),
		CCM_GETUNICODEFORMAT = (CCM_FIRST + 6),
		CCM_SETVERSION = (CCM_FIRST + 0x7),
		CCM_GETVERSION = (CCM_FIRST + 0x8),
		CCM_SETNOTIFYWINDOW = (CCM_FIRST + 0x9),
		CCM_SETWINDOWTHEME = (CCM_FIRST + 0xb),
		CCM_DPISCALE = (CCM_FIRST + 0xc),

		HDM_GETITEMCOUNT = (HDM_FIRST + 0),
		HDM_INSERTITEMA = (HDM_FIRST + 1),
		HDM_INSERTITEMW = (HDM_FIRST + 10),
		HDM_DELETEITEM = (HDM_FIRST + 2),
		HDM_GETITEMA = (HDM_FIRST + 3),
		HDM_GETITEMW = (HDM_FIRST + 11),
		HDM_SETITEMA = (HDM_FIRST + 4),
		HDM_SETITEMW = (HDM_FIRST + 12),
		HDM_LAYOUT = (HDM_FIRST + 5),
		HDM_HITTEST = (HDM_FIRST + 6),
		HDM_GETITEMRECT = (HDM_FIRST + 7),
		HDM_SETIMAGELIST = (HDM_FIRST + 8),
		HDM_GETIMAGELIST = (HDM_FIRST + 9),
		HDM_ORDERTOINDEX = (HDM_FIRST + 15),
		HDM_CREATEDRAGIMAGE = (HDM_FIRST + 16),
		HDM_GETORDERARRAY = (HDM_FIRST + 17),
		HDM_SETORDERARRAY = (HDM_FIRST + 18),
		HDM_SETHOTDIVIDER = (HDM_FIRST + 19),
		HDM_SETBITMAPMARGIN = (HDM_FIRST + 20),
		HDM_GETBITMAPMARGIN = (HDM_FIRST + 21),
		HDM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		HDM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		HDM_SETFILTERCHANGETIMEOUT = (HDM_FIRST + 22),
		HDM_EDITFILTER = (HDM_FIRST + 23),
		HDM_CLEARFILTER = (HDM_FIRST + 24),

		TB_ENABLEBUTTON = (WM.USER + 1),
		TB_CHECKBUTTON = (WM.USER + 2),
		TB_PRESSBUTTON = (WM.USER + 3),
		TB_HIDEBUTTON = (WM.USER + 4),
		TB_INDETERMINATE = (WM.USER + 5),
		TB_MARKBUTTON = (WM.USER + 6),
		TB_ISBUTTONENABLED = (WM.USER + 9),
		TB_ISBUTTONCHECKED = (WM.USER + 10),
		TB_ISBUTTONPRESSED = (WM.USER + 11),
		TB_ISBUTTONHIDDEN = (WM.USER + 12),
		TB_ISBUTTONINDETERMINATE = (WM.USER + 13),
		TB_ISBUTTONHIGHLIGHTED = (WM.USER + 14),
		TB_SETSTATE = (WM.USER + 17),
		TB_GETSTATE = (WM.USER + 18),
		TB_ADDBITMAP = (WM.USER + 19),
		TB_ADDBUTTONSA = (WM.USER + 20),
		TB_INSERTBUTTONA = (WM.USER + 21),
		TB_ADDBUTTONS = (WM.USER + 20),
		TB_INSERTBUTTON = (WM.USER + 21),
		TB_DELETEBUTTON = (WM.USER + 22),
		TB_GETBUTTON = (WM.USER + 23),
		TB_BUTTONCOUNT = (WM.USER + 24),
		TB_COMMANDTOINDEX = (WM.USER + 25),
		TB_SAVERESTOREA = (WM.USER + 26),
		TB_SAVERESTOREW = (WM.USER + 76),
		TB_CUSTOMIZE = (WM.USER + 27),
		TB_ADDSTRINGA = (WM.USER + 28),
		TB_ADDSTRINGW = (WM.USER + 77),
		TB_GETITEMRECT = (WM.USER + 29),
		TB_BUTTONSTRUCTSIZE = (WM.USER + 30),
		TB_SETBUTTONSIZE = (WM.USER + 31),
		TB_SETBITMAPSIZE = (WM.USER + 32),
		TB_AUTOSIZE = (WM.USER + 33),
		TB_GETTOOLTIPS = (WM.USER + 35),
		TB_SETTOOLTIPS = (WM.USER + 36),
		TB_SETPARENT = (WM.USER + 37),
		TB_SETROWS = (WM.USER + 39),
		TB_GETROWS = (WM.USER + 40),
		TB_SETCMDID = (WM.USER + 42),
		TB_CHANGEBITMAP = (WM.USER + 43),
		TB_GETBITMAP = (WM.USER + 44),
		TB_GETBUTTONTEXTA = (WM.USER + 45),
		TB_GETBUTTONTEXTW = (WM.USER + 75),
		TB_REPLACEBITMAP = (WM.USER + 46),
		TB_SETINDENT = (WM.USER + 47),
		TB_SETIMAGELIST = (WM.USER + 48),
		TB_GETIMAGELIST = (WM.USER + 49),
		TB_LOADIMAGES = (WM.USER + 50),
		TB_GETRECT = (WM.USER + 51),
		TB_SETHOTIMAGELIST = (WM.USER + 52),
		TB_GETHOTIMAGELIST = (WM.USER + 53),
		TB_SETDISABLEDIMAGELIST = (WM.USER + 54),
		TB_GETDISABLEDIMAGELIST = (WM.USER + 55),
		TB_SETSTYLE = (WM.USER + 56),
		TB_GETSTYLE = (WM.USER + 57),
		TB_GETBUTTONSIZE = (WM.USER + 58),
		TB_SETBUTTONWIDTH = (WM.USER + 59),
		TB_SETMAXTEXTROWS = (WM.USER + 60),
		TB_GETTEXTROWS = (WM.USER + 61),
		TB_GETOBJECT = (WM.USER + 62),
		TB_GETHOTITEM = (WM.USER + 71),
		TB_SETHOTITEM = (WM.USER + 72),
		TB_SETANCHORHIGHLIGHT = (WM.USER + 73),
		TB_GETANCHORHIGHLIGHT = (WM.USER + 74),
		TB_MAPACCELERATORA = (WM.USER + 78),
		TB_GETINSERTMARK = (WM.USER + 79),
		TB_SETINSERTMARK = (WM.USER + 80),
		TB_INSERTMARKHITTEST = (WM.USER + 81),
		TB_MOVEBUTTON = (WM.USER + 82),
		TB_GETMAXSIZE = (WM.USER + 83),
		TB_SETEXTENDEDSTYLE = (WM.USER + 84),
		TB_GETEXTENDEDSTYLE = (WM.USER + 85),
		TB_GETPADDING = (WM.USER + 86),
		TB_SETPADDING = (WM.USER + 87),
		TB_SETINSERTMARKCOLOR = (WM.USER + 88),
		TB_GETINSERTMARKCOLOR = (WM.USER + 89),
		TB_SETCOLORSCHEME = CCM_SETCOLORSCHEME,
		TB_GETCOLORSCHEME = CCM_GETCOLORSCHEME,
		TB_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		TB_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		TB_MAPACCELERATORW = (WM.USER + 90),
		TB_GETBITMAPFLAGS = (WM.USER + 41),
		TB_GETBUTTONINFOW = (WM.USER + 63),
		TB_SETBUTTONINFOW = (WM.USER + 64),
		TB_GETBUTTONINFOA = (WM.USER + 65),
		TB_SETBUTTONINFOA = (WM.USER + 66),
		TB_INSERTBUTTONW = (WM.USER + 67),
		TB_ADDBUTTONSW = (WM.USER + 68),
		TB_HITTEST = (WM.USER + 69),
		TB_SETDRAWTEXTFLAGS = (WM.USER + 70),
		TB_GETSTRINGW = (WM.USER + 91),
		TB_GETSTRINGA = (WM.USER + 92),
		TB_GETMETRICS = (WM.USER + 101),
		TB_SETMETRICS = (WM.USER + 102),
		TB_SETWINDOWTHEME = CCM_SETWINDOWTHEME,

		RB_INSERTBANDA = (WM.USER + 1),
		RB_DELETEBAND = (WM.USER + 2),
		RB_GETBARINFO = (WM.USER + 3),
		RB_SETBARINFO = (WM.USER + 4),
		RB_GETBANDINFO = (WM.USER + 5),
		RB_SETBANDINFOA = (WM.USER + 6),
		RB_SETPARENT = (WM.USER + 7),
		RB_HITTEST = (WM.USER + 8),
		RB_GETRECT = (WM.USER + 9),
		RB_INSERTBANDW = (WM.USER + 10),
		RB_SETBANDINFOW = (WM.USER + 11),
		RB_GETBANDCOUNT = (WM.USER + 12),
		RB_GETROWCOUNT = (WM.USER + 13),
		RB_GETROWHEIGHT = (WM.USER + 14),
		RB_IDTOINDEX = (WM.USER + 16),
		RB_GETTOOLTIPS = (WM.USER + 17),
		RB_SETTOOLTIPS = (WM.USER + 18),
		RB_SETBKCOLOR = (WM.USER + 19),
		RB_GETBKCOLOR = (WM.USER + 20),
		RB_SETTEXTCOLOR = (WM.USER + 21),
		RB_GETTEXTCOLOR = (WM.USER + 22),
		RB_SIZETORECT = (WM.USER + 23),
		RB_SETCOLORSCHEME = CCM_SETCOLORSCHEME,
		RB_GETCOLORSCHEME = CCM_GETCOLORSCHEME,
		RB_BEGINDRAG = (WM.USER + 24),
		RB_ENDDRAG = (WM.USER + 25),
		RB_DRAGMOVE = (WM.USER + 26),
		RB_GETBARHEIGHT = (WM.USER + 27),
		RB_GETBANDINFOW = (WM.USER + 28),
		RB_GETBANDINFOA = (WM.USER + 29),
		RB_MINIMIZEBAND = (WM.USER + 30),
		RB_MAXIMIZEBAND = (WM.USER + 31),
		RB_GETDROPTARGET = (CCM_GETDROPTARGET),
		RB_GETBANDBORDERS = (WM.USER + 34),
		RB_SHOWBAND = (WM.USER + 35),
		RB_SETPALETTE = (WM.USER + 37),
		RB_GETPALETTE = (WM.USER + 38),
		RB_MOVEBAND = (WM.USER + 39),
		RB_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		RB_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		RB_GETBANDMARGINS = (WM.USER + 40),
		RB_SETWINDOWTHEME = CCM_SETWINDOWTHEME,
		RB_PUSHCHEVRON = (WM.USER + 43),

		TTM_ACTIVATE = (WM.USER + 1),
		TTM_SETDELAYTIME = (WM.USER + 3),
		TTM_ADDTOOLA = (WM.USER + 4),
		TTM_ADDTOOLW = (WM.USER + 50),
		TTM_DELTOOLA = (WM.USER + 5),
		TTM_DELTOOLW = (WM.USER + 51),
		TTM_NEWTOOLRECTA = (WM.USER + 6),
		TTM_NEWTOOLRECTW = (WM.USER + 52),
		TTM_RELAYEVENT = (WM.USER + 7),
		TTM_GETTOOLINFOA = (WM.USER + 8),
		TTM_GETTOOLINFOW = (WM.USER + 53),
		TTM_SETTOOLINFOA = (WM.USER + 9),
		TTM_SETTOOLINFOW = (WM.USER + 54),
		TTM_HITTESTA = (WM.USER + 10),
		TTM_HITTESTW = (WM.USER + 55),
		TTM_GETTEXTA = (WM.USER + 11),
		TTM_GETTEXTW = (WM.USER + 56),
		TTM_UPDATETIPTEXTA = (WM.USER + 12),
		TTM_UPDATETIPTEXTW = (WM.USER + 57),
		TTM_GETTOOLCOUNT = (WM.USER + 13),
		TTM_ENUMTOOLSA = (WM.USER + 14),
		TTM_ENUMTOOLSW = (WM.USER + 58),
		TTM_GETCURRENTTOOLA = (WM.USER + 15),
		TTM_GETCURRENTTOOLW = (WM.USER + 59),
		TTM_WINDOWFROMPOINT = (WM.USER + 16),
		TTM_TRACKACTIVATE = (WM.USER + 17),
		TTM_TRACKPOSITION = (WM.USER + 18),
		TTM_SETTIPBKCOLOR = (WM.USER + 19),
		TTM_SETTIPTEXTCOLOR = (WM.USER + 20),
		TTM_GETDELAYTIME = (WM.USER + 21),
		TTM_GETTIPBKCOLOR = (WM.USER + 22),
		TTM_GETTIPTEXTCOLOR = (WM.USER + 23),
		TTM_SETMAXTIPWIDTH = (WM.USER + 24),
		TTM_GETMAXTIPWIDTH = (WM.USER + 25),
		TTM_SETMARGIN = (WM.USER + 26),
		TTM_GETMARGIN = (WM.USER + 27),
		TTM_POP = (WM.USER + 28),
		TTM_UPDATE = (WM.USER + 29),
		TTM_GETBUBBLESIZE = (WM.USER + 30),
		TTM_ADJUSTRECT = (WM.USER + 31),
		TTM_SETTITLEA = (WM.USER + 32),
		TTM_SETTITLEW = (WM.USER + 33),
		TTM_POPUP = (WM.USER + 34),
		TTM_GETTITLE = (WM.USER + 35),
		TTM_SETWINDOWTHEME = CCM_SETWINDOWTHEME,

		SB_SETTEXTA = (WM.USER + 1),
		SB_SETTEXTW = (WM.USER + 11),
		SB_GETTEXTA = (WM.USER + 2),
		SB_GETTEXTW = (WM.USER + 13),
		SB_GETTEXTLENGTHA = (WM.USER + 3),
		SB_GETTEXTLENGTHW = (WM.USER + 12),
		SB_SETPARTS = (WM.USER + 4),
		SB_GETPARTS = (WM.USER + 6),
		SB_GETBORDERS = (WM.USER + 7),
		SB_SETMINHEIGHT = (WM.USER + 8),
		SB_SIMPLE = (WM.USER + 9),
		SB_GETRECT = (WM.USER + 10),
		SB_ISSIMPLE = (WM.USER + 14),
		SB_SETICON = (WM.USER + 15),
		SB_SETTIPTEXTA = (WM.USER + 16),
		SB_SETTIPTEXTW = (WM.USER + 17),
		SB_GETTIPTEXTA = (WM.USER + 18),
		SB_GETTIPTEXTW = (WM.USER + 19),
		SB_GETICON = (WM.USER + 20),
		SB_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		SB_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		SB_SETBKCOLOR = CCM_SETBKCOLOR,
		SB_SIMPLEID = 0x00ff,

		TBM_GETPOS = (WM.USER),
		TBM_GETRANGEMIN = (WM.USER + 1),
		TBM_GETRANGEMAX = (WM.USER + 2),
		TBM_GETTIC = (WM.USER + 3),
		TBM_SETTIC = (WM.USER + 4),
		TBM_SETPOS = (WM.USER + 5),
		TBM_SETRANGE = (WM.USER + 6),
		TBM_SETRANGEMIN = (WM.USER + 7),
		TBM_SETRANGEMAX = (WM.USER + 8),
		TBM_CLEARTICS = (WM.USER + 9),
		TBM_SETSEL = (WM.USER + 10),
		TBM_SETSELSTART = (WM.USER + 11),
		TBM_SETSELEND = (WM.USER + 12),
		TBM_GETPTICS = (WM.USER + 14),
		TBM_GETTICPOS = (WM.USER + 15),
		TBM_GETNUMTICS = (WM.USER + 16),
		TBM_GETSELSTART = (WM.USER + 17),
		TBM_GETSELEND = (WM.USER + 18),
		TBM_CLEARSEL = (WM.USER + 19),
		TBM_SETTICFREQ = (WM.USER + 20),
		TBM_SETPAGESIZE = (WM.USER + 21),
		TBM_GETPAGESIZE = (WM.USER + 22),
		TBM_SETLINESIZE = (WM.USER + 23),
		TBM_GETLINESIZE = (WM.USER + 24),
		TBM_GETTHUMBRECT = (WM.USER + 25),
		TBM_GETCHANNELRECT = (WM.USER + 26),
		TBM_SETTHUMBLENGTH = (WM.USER + 27),
		TBM_GETTHUMBLENGTH = (WM.USER + 28),
		TBM_SETTOOLTIPS = (WM.USER + 29),
		TBM_GETTOOLTIPS = (WM.USER + 30),
		TBM_SETTIPSIDE = (WM.USER + 31),
		TBM_SETBUDDY = (WM.USER + 32),
		TBM_GETBUDDY = (WM.USER + 33),
		TBM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		TBM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,

		DL_BEGINDRAG = (WM.USER + 133),
		DL_DRAGGING = (WM.USER + 134),
		DL_DROPPED = (WM.USER + 135),
		DL_CANCELDRAG = (WM.USER + 136),

		UDM_SETRANGE = (WM.USER + 101),
		UDM_GETRANGE = (WM.USER + 102),
		UDM_SETPOS = (WM.USER + 103),
		UDM_GETPOS = (WM.USER + 104),
		UDM_SETBUDDY = (WM.USER + 105),
		UDM_GETBUDDY = (WM.USER + 106),
		UDM_SETACCEL = (WM.USER + 107),
		UDM_GETACCEL = (WM.USER + 108),
		UDM_SETBASE = (WM.USER + 109),
		UDM_GETBASE = (WM.USER + 110),
		UDM_SETRANGE32 = (WM.USER + 111),
		UDM_GETRANGE32 = (WM.USER + 112),
		UDM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		UDM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		UDM_SETPOS32 = (WM.USER + 113),
		UDM_GETPOS32 = (WM.USER + 114),

		PBM_SETRANGE = (WM.USER + 1),
		PBM_SETPOS = (WM.USER + 2),
		PBM_DELTAPOS = (WM.USER + 3),
		PBM_SETSTEP = (WM.USER + 4),
		PBM_STEPIT = (WM.USER + 5),
		PBM_SETRANGE32 = (WM.USER + 6),
		PBM_GETRANGE = (WM.USER + 7),
		PBM_GETPOS = (WM.USER + 8),
		PBM_SETBARCOLOR = (WM.USER + 9),
		PBM_SETBKCOLOR = CCM_SETBKCOLOR,

		HKM_SETHOTKEY = (WM.USER + 1),
		HKM_GETHOTKEY = (WM.USER + 2),
		HKM_SETRULES = (WM.USER + 3),

		TVM_INSERTITEMA = (TV_FIRST + 0),
		TVM_INSERTITEMW = (TV_FIRST + 50),
		TVM_DELETEITEM = (TV_FIRST + 1),
		TVM_EXPAND = (TV_FIRST + 2),
		TVM_GETITEMRECT = (TV_FIRST + 4),
		TVM_GETCOUNT = (TV_FIRST + 5),
		TVM_GETINDENT = (TV_FIRST + 6),
		TVM_SETINDENT = (TV_FIRST + 7),
		TVM_GETIMAGELIST = (TV_FIRST + 8),
		TVM_SETIMAGELIST = (TV_FIRST + 9),
		TVM_GETNEXTITEM = (TV_FIRST + 10),
		TVM_SELECTITEM = (TV_FIRST + 11),
		TVM_GETITEMA = (TV_FIRST + 12),
		TVM_GETITEMW = (TV_FIRST + 62),
		TVM_SETITEMA = (TV_FIRST + 13),
		TVM_SETITEMW = (TV_FIRST + 63),
		TVM_EDITLABELA = (TV_FIRST + 14),
		TVM_EDITLABELW = (TV_FIRST + 65),
		TVM_GETEDITCONTROL = (TV_FIRST + 15),
		TVM_GETVISIBLECOUNT = (TV_FIRST + 16),
		TVM_HITTEST = (TV_FIRST + 17),
		TVM_CREATEDRAGIMAGE = (TV_FIRST + 18),
		TVM_SORTCHILDREN = (TV_FIRST + 19),
		TVM_ENSUREVISIBLE = (TV_FIRST + 20),
		TVM_SORTCHILDRENCB = (TV_FIRST + 21),
		TVM_ENDEDITLABELNOW = (TV_FIRST + 22),
		TVM_GETISEARCHSTRINGA = (TV_FIRST + 23),
		TVM_GETISEARCHSTRINGW = (TV_FIRST + 64),
		TVM_SETTOOLTIPS = (TV_FIRST + 24),
		TVM_GETTOOLTIPS = (TV_FIRST + 25),
		TVM_SETINSERTMARK = (TV_FIRST + 26),
		TVM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		TVM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		TVM_SETITEMHEIGHT = (TV_FIRST + 27),
		TVM_GETITEMHEIGHT = (TV_FIRST + 28),
		TVM_SETBKCOLOR = (TV_FIRST + 29),
		TVM_SETTEXTCOLOR = (TV_FIRST + 30),
		TVM_GETBKCOLOR = (TV_FIRST + 31),
		TVM_GETTEXTCOLOR = (TV_FIRST + 32),
		TVM_SETSCROLLTIME = (TV_FIRST + 33),
		TVM_GETSCROLLTIME = (TV_FIRST + 34),
		TVM_SETINSERTMARKCOLOR = (TV_FIRST + 37),
		TVM_GETINSERTMARKCOLOR = (TV_FIRST + 38),
		TVM_GETITEMSTATE = (TV_FIRST + 39),
		TVM_SETLINECOLOR = (TV_FIRST + 40),
		TVM_GETLINECOLOR = (TV_FIRST + 41),
		TVM_MAPACCIDTOHTREEITEM = (TV_FIRST + 42),
		TVM_MAPHTREEITEMTOACCID = (TV_FIRST + 43),

		CBEM_INSERTITEMA = (WM.USER + 1),
		CBEM_SETIMAGELIST = (WM.USER + 2),
		CBEM_GETIMAGELIST = (WM.USER + 3),
		CBEM_GETITEMA = (WM.USER + 4),
		CBEM_SETITEMA = (WM.USER + 5),
		CBEM_DELETEITEM = CB_DELETESTRING,
		CBEM_GETCOMBOCONTROL = (WM.USER + 6),
		CBEM_GETEDITCONTROL = (WM.USER + 7),
		CBEM_SETEXTENDEDSTYLE = (WM.USER + 14),
		CBEM_GETEXTENDEDSTYLE = (WM.USER + 9),
		CBEM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		CBEM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		CBEM_SETEXSTYLE = (WM.USER + 8),
		CBEM_GETEXSTYLE = (WM.USER + 9),
		CBEM_HASEDITCHANGED = (WM.USER + 10),
		CBEM_INSERTITEMW = (WM.USER + 11),
		CBEM_SETITEMW = (WM.USER + 12),
		CBEM_GETITEMW = (WM.USER + 13),

		TCM_GETIMAGELIST = (TCM_FIRST + 2),
		TCM_SETIMAGELIST = (TCM_FIRST + 3),
		TCM_GETITEMCOUNT = (TCM_FIRST + 4),
		TCM_GETITEMA = (TCM_FIRST + 5),
		TCM_GETITEMW = (TCM_FIRST + 60),
		TCM_SETITEMA = (TCM_FIRST + 6),
		TCM_SETITEMW = (TCM_FIRST + 61),
		TCM_INSERTITEMA = (TCM_FIRST + 7),
		TCM_INSERTITEMW = (TCM_FIRST + 62),
		TCM_DELETEITEM = (TCM_FIRST + 8),
		TCM_DELETEALLITEMS = (TCM_FIRST + 9),
		TCM_GETITEMRECT = (TCM_FIRST + 10),
		TCM_GETCURSEL = (TCM_FIRST + 11),
		TCM_SETCURSEL = (TCM_FIRST + 12),
		TCM_HITTEST = (TCM_FIRST + 13),
		TCM_SETITEMEXTRA = (TCM_FIRST + 14),
		TCM_ADJUSTRECT = (TCM_FIRST + 40),
		TCM_SETITEMSIZE = (TCM_FIRST + 41),
		TCM_REMOVEIMAGE = (TCM_FIRST + 42),
		TCM_SETPADDING = (TCM_FIRST + 43),
		TCM_GETROWCOUNT = (TCM_FIRST + 44),
		TCM_GETTOOLTIPS = (TCM_FIRST + 45),
		TCM_SETTOOLTIPS = (TCM_FIRST + 46),
		TCM_GETCURFOCUS = (TCM_FIRST + 47),
		TCM_SETCURFOCUS = (TCM_FIRST + 48),
		TCM_SETMINTABWIDTH = (TCM_FIRST + 49),
		TCM_DESELECTALL = (TCM_FIRST + 50),
		TCM_HIGHLIGHTITEM = (TCM_FIRST + 51),
		TCM_SETEXTENDEDSTYLE = (TCM_FIRST + 52),
		TCM_GETEXTENDEDSTYLE = (TCM_FIRST + 53),
		TCM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		TCM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,

		ACM_OPENA = (WM.USER + 100),
		ACM_OPENW = (WM.USER + 103),
		ACM_PLAY = (WM.USER + 101),
		ACM_STOP = (WM.USER + 102),

		MCM_FIRST = 0x1000,
		MCM_GETCURSEL = (MCM_FIRST + 1),
		MCM_SETCURSEL = (MCM_FIRST + 2),
		MCM_GETMAXSELCOUNT = (MCM_FIRST + 3),
		MCM_SETMAXSELCOUNT = (MCM_FIRST + 4),
		MCM_GETSELRANGE = (MCM_FIRST + 5),
		MCM_SETSELRANGE = (MCM_FIRST + 6),
		MCM_GETMONTHRANGE = (MCM_FIRST + 7),
		MCM_SETDAYSTATE = (MCM_FIRST + 8),
		MCM_GETMINREQRECT = (MCM_FIRST + 9),
		MCM_SETCOLOR = (MCM_FIRST + 10),
		MCM_GETCOLOR = (MCM_FIRST + 11),
		MCM_SETTODAY = (MCM_FIRST + 12),
		MCM_GETTODAY = (MCM_FIRST + 13),
		MCM_HITTEST = (MCM_FIRST + 14),
		MCM_SETFIRSTDAYOFWEEK = (MCM_FIRST + 15),
		MCM_GETFIRSTDAYOFWEEK = (MCM_FIRST + 16),
		MCM_GETRANGE = (MCM_FIRST + 17),
		MCM_SETRANGE = (MCM_FIRST + 18),
		MCM_GETMONTHDELTA = (MCM_FIRST + 19),
		MCM_SETMONTHDELTA = (MCM_FIRST + 20),
		MCM_GETMAXTODAYWIDTH = (MCM_FIRST + 21),
		MCM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		MCM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,

		DTM_FIRST = 0x1000,
		DTM_GETSYSTEMTIME = (DTM_FIRST + 1),
		DTM_SETSYSTEMTIME = (DTM_FIRST + 2),
		DTM_GETRANGE = (DTM_FIRST + 3),
		DTM_SETRANGE = (DTM_FIRST + 4),
		DTM_SETFORMATA = (DTM_FIRST + 5),
		DTM_SETFORMATW = (DTM_FIRST + 50),
		DTM_SETMCCOLOR = (DTM_FIRST + 6),
		DTM_GETMCCOLOR = (DTM_FIRST + 7),
		DTM_GETMONTHCAL = (DTM_FIRST + 8),
		DTM_SETMCFONT = (DTM_FIRST + 9),
		DTM_GETMCFONT = (DTM_FIRST + 10),

		PGM_SETCHILD = (PGM_FIRST + 1),
		PGM_RECALCSIZE = (PGM_FIRST + 2),
		PGM_FORWARDMOUSE = (PGM_FIRST + 3),
		PGM_SETBKCOLOR = (PGM_FIRST + 4),
		PGM_GETBKCOLOR = (PGM_FIRST + 5),
		PGM_SETBORDER = (PGM_FIRST + 6),
		PGM_GETBORDER = (PGM_FIRST + 7),
		PGM_SETPOS = (PGM_FIRST + 8),
		PGM_GETPOS = (PGM_FIRST + 9),
		PGM_SETBUTTONSIZE = (PGM_FIRST + 10),
		PGM_GETBUTTONSIZE = (PGM_FIRST + 11),
		PGM_GETBUTTONSTATE = (PGM_FIRST + 12),
		PGM_GETDROPTARGET = CCM_GETDROPTARGET,

		BCM_GETIDEALSIZE = (BCM_FIRST + 0x0001),
		BCM_SETIMAGELIST = (BCM_FIRST + 0x0002),
		BCM_GETIMAGELIST = (BCM_FIRST + 0x0003),
		BCM_SETTEXTMARGIN = (BCM_FIRST + 0x0004),
		BCM_GETTEXTMARGIN = (BCM_FIRST + 0x0005),

		CB_SETMINVISIBLE = (CBM_FIRST + 1),
		CB_GETMINVISIBLE = (CBM_FIRST + 2),

		LM_HITTEST = (WM.USER + 0x300),
		LM_GETIDEALHEIGHT = (WM.USER + 0x301),
		LM_SETITEM = (WM.USER + 0x302),
		LM_GETITEM = (WM.USER + 0x303)
// ReSharper restore InconsistentNaming
	}
}