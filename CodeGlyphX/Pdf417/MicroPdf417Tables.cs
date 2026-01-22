using System;

namespace CodeGlyphX.Pdf417;

internal static class MicroPdf417Tables {
    internal static readonly int[] MicroVariants = {
    1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2,
    2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4,
    4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 11, 14,
    17, 20, 24, 28, 8, 11, 14, 17, 20, 23, 26, 6,
    8, 10, 12, 15, 20, 26, 32, 38, 44, 4, 6, 8,
    10, 12, 15, 20, 26, 32, 38, 44, 7, 7, 7, 8,
    8, 8, 8, 9, 9, 10, 11, 13, 15, 12, 14, 16,
    18, 21, 26, 32, 38, 44, 50, 8, 12, 14, 16, 18,
    21, 26, 32, 38, 44, 50, 0, 0, 0, 7, 7, 7,
    7, 15, 15, 24, 34, 57, 84, 45, 70, 99, 115, 133,
    154, 180, 212, 250, 294, 7, 45, 70, 99, 115, 133, 154,
    180, 212, 250, 294,
    };

    internal static readonly int[] MicroAutosize = {
    4, 6, 7, 8, 8, 10, 10, 12, 12, 13, 14, 16,
    18, 18, 19, 20, 24, 24, 24, 29, 30, 33, 34, 37,
    39, 46, 54, 58, 70, 72, 82, 90, 108, 126, 1, 14,
    2, 7, 24, 3, 15, 25, 4, 8, 16, 5, 17, 26,
    9, 6, 10, 18, 27, 11, 28, 12, 19, 13, 29, 20,
    30, 21, 22, 31, 23, 32, 33, 34,
    };

    internal static readonly int[] MicroCoefficients = {
    76, 925, 537, 597, 784, 691, 437, 237, 308, 436, 284, 646,
    653, 428, 379, 567, 527, 622, 257, 289, 362, 501, 441, 205,
    377, 457, 64, 244, 826, 841, 818, 691, 266, 612, 462, 45,
    565, 708, 825, 213, 15, 68, 327, 602, 904, 597, 864, 757,
    201, 646, 684, 347, 127, 388, 7, 69, 851, 764, 713, 342,
    384, 606, 583, 322, 592, 678, 204, 184, 394, 692, 669, 677,
    154, 187, 241, 286, 274, 354, 478, 915, 691, 833, 105, 215,
    460, 829, 476, 109, 904, 664, 230, 5, 80, 74, 550, 575,
    147, 868, 642, 274, 562, 232, 755, 599, 524, 801, 132, 295,
    116, 442, 428, 295, 42, 176, 65, 279, 577, 315, 624, 37,
    855, 275, 739, 120, 297, 312, 202, 560, 321, 233, 756, 760,
    573, 108, 519, 781, 534, 129, 425, 681, 553, 422, 716, 763,
    693, 624, 610, 310, 691, 347, 165, 193, 259, 568, 443, 284,
    887, 544, 788, 93, 477, 760, 331, 608, 269, 121, 159, 830,
    446, 893, 699, 245, 441, 454, 325, 858, 131, 847, 764, 169,
    361, 575, 922, 525, 176, 586, 640, 321, 536, 742, 677, 742,
    687, 284, 193, 517, 273, 494, 263, 147, 593, 800, 571, 320,
    803, 133, 231, 390, 685, 330, 63, 410, 234, 228, 438, 848,
    133, 703, 529, 721, 788, 322, 280, 159, 738, 586, 388, 684,
    445, 680, 245, 595, 614, 233, 812, 32, 284, 658, 745, 229,
    95, 689, 920, 771, 554, 289, 231, 125, 117, 518, 476, 36,
    659, 848, 678, 64, 764, 840, 157, 915, 470, 876, 109, 25,
    632, 405, 417, 436, 714, 60, 376, 97, 413, 706, 446, 21,
    3, 773, 569, 267, 272, 213, 31, 560, 231, 758, 103, 271,
    572, 436, 339, 730, 82, 285, 923, 797, 576, 875, 156, 706,
    63, 81, 257, 874, 411, 416, 778, 50, 205, 303, 188, 535,
    909, 155, 637, 230, 534, 96, 575, 102, 264, 233, 919, 593,
    865, 26, 579, 623, 766, 146, 10, 739, 246, 127, 71, 244,
    211, 477, 920, 876, 427, 820, 718, 435,
    };

    internal static readonly int[] RapTable = {
    1, 8, 36, 19, 9, 25, 1, 1, 8, 36, 19, 9,
    27, 1, 7, 15, 25, 37, 1, 1, 21, 15, 1, 47,
    1, 7, 15, 25, 37, 1, 1, 21, 15, 1, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,
    7, 15, 25, 37, 17, 9, 29, 31, 25, 19, 1, 7,
    15, 25, 37, 17, 9, 29, 31, 25, 9, 8, 36, 19,
    17, 33, 1, 9, 8, 36, 19, 17, 35, 1, 7, 15,
    25, 37, 33, 17, 37, 47, 49, 43, 1, 7, 15, 25,
    37, 33, 17, 37, 47, 49, 0, 3, 6, 0, 6, 0,
    0, 0, 3, 6, 0, 6, 6, 0, 0, 6, 0, 0,
    0, 0, 6, 6, 0, 3, 0, 0, 6, 0, 0, 0,
    0, 6, 6, 0,
    };

    internal static readonly string[] RapLR = {
    "", "221311", "311311", "312211", "222211", "213211",
    "214111", "223111", "313111", "322111", "412111", "421111",
    "331111", "241111", "232111", "231211", "321211", "411211",
    "411121", "411112", "321112", "312112", "311212", "311221",
    "311131", "311122", "311113", "221113", "221122", "221131",
    "221221", "222121", "312121", "321121", "231121", "231112",
    "222112", "213112", "212212", "212221", "212131", "212122",
    "212113", "211213", "211123", "211132", "211141", "211231",
    "211222", "211312", "211321", "211411", "212311",
    };

    internal static readonly string[] RapC = {
    "", "112231", "121231", "122131", "131131", "131221",
    "132121", "141121", "141211", "142111", "133111", "132211",
    "131311", "122311", "123211", "124111", "115111", "114211",
    "114121", "123121", "123112", "122212", "122221", "121321",
    "121411", "112411", "113311", "113221", "113212", "113122",
    "122122", "131122", "131113", "122113", "113113", "112213",
    "112222", "112312", "112321", "111421", "111331", "111322",
    "111232", "111223", "111133", "111124", "111214", "112114",
    "121114", "121123", "121132", "112132", "112141",
    };

    private static readonly char[] BrSet = {
    'A', 'B', 'C', 'D', 'E', 'F', 'a', 'b', 'c', 'd', 'e', 'f',
    'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r',
    's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '*', '+', '-',
    };

    private static readonly string[] PdfTtf = {
    "00000", "00001", "00010", "00011", "00100", "00101", "00110", "00111",
    "01000", "01001", "01010", "01011", "01100", "01101", "01110", "01111",
    "10000", "10001", "10010", "10011", "10100", "10101", "10110", "10111",
    "11000", "11001", "11010", "11011", "11100", "11101", "11110", "11111",
    "01", "1111111101010100", "11111101000101001",
    };
    internal static readonly string[] PdfTtfMap = BuildPdfTtfMap();


    internal static readonly string[] CodewordPatterns = {
    "urA", "xfs", "ypy", "unk", "xdw", "yoz", "pDA", "uls",
    "pBk", "eBA", "pAs", "eAk", "prA", "uvs", "xhy", "pnk",
    "utw", "xgz", "fDA", "pls", "fBk", "frA", "pvs", "uxy",
    "fnk", "ptw", "uwz", "fls", "psy", "fvs", "pxy", "ftw",
    "pwz", "fxy", "yrx", "ufk", "xFw", "ymz", "onA", "uds",
    "xEy", "olk", "ucw", "dBA", "oks", "uci", "dAk", "okg",
    "dAc", "ovk", "uhw", "xaz", "dnA", "ots", "ugy", "dlk",
    "osw", "ugj", "dks", "osi", "dvk", "oxw", "uiz", "dts",
    "owy", "dsw", "owj", "dxw", "oyz", "dwy", "dwj", "ofA",
    "uFs", "xCy", "odk", "uEw", "xCj", "clA", "ocs", "uEi",
    "ckk", "ocg", "ckc", "ckE", "cvA", "ohs", "uay", "ctk",
    "ogw", "uaj", "css", "ogi", "csg", "csa", "cxs", "oiy",
    "cww", "oij", "cwi", "cyy", "oFk", "uCw", "xBj", "cdA",
    "oEs", "uCi", "cck", "oEg", "uCb", "ccc", "oEa", "ccE",
    "oED", "chk", "oaw", "uDj", "cgs", "oai", "cgg", "oab",
    "cga", "cgD", "obj", "cib", "cFA", "oCs", "uBi", "cEk",
    "oCg", "uBb", "cEc", "oCa", "cEE", "oCD", "cEC", "cas",
    "cag", "caa", "cCk", "uAr", "oBa", "oBD", "cCB", "tfk",
    "wpw", "yez", "mnA", "tds", "woy", "mlk", "tcw", "woj",
    "FBA", "mks", "FAk", "mvk", "thw", "wqz", "FnA", "mts",
    "tgy", "Flk", "msw", "Fks", "Fkg", "Fvk", "mxw", "tiz",
    "Fts", "mwy", "Fsw", "Fsi", "Fxw", "myz", "Fwy", "Fyz",
    "vfA", "xps", "yuy", "vdk", "xow", "yuj", "qlA", "vcs",
    "xoi", "qkk", "vcg", "xob", "qkc", "vca", "mfA", "tFs",
    "wmy", "qvA", "mdk", "tEw", "wmj", "qtk", "vgw", "xqj",
    "hlA", "Ekk", "mcg", "tEb", "hkk", "qsg", "hkc", "EvA",
    "mhs", "tay", "hvA", "Etk", "mgw", "taj", "htk", "qww",
    "vij", "hss", "Esg", "hsg", "Exs", "miy", "hxs", "Eww",
    "mij", "hww", "qyj", "hwi", "Eyy", "hyy", "Eyj", "hyj",
    "vFk", "xmw", "ytj", "qdA", "vEs", "xmi", "qck", "vEg",
    "xmb", "qcc", "vEa", "qcE", "qcC", "mFk", "tCw", "wlj",
    "qhk", "mEs", "tCi", "gtA", "Eck", "vai", "tCb", "gsk",
    "Ecc", "mEa", "gsc", "qga", "mED", "EcC", "Ehk", "maw",
    "tDj", "gxk", "Egs", "mai", "gws", "qii", "mab", "gwg",
    "Ega", "EgD", "Eiw", "mbj", "gyw", "Eii", "gyi", "Eib",
    "gyb", "gzj", "qFA", "vCs", "xli", "qEk", "vCg", "xlb",
    "qEc", "vCa", "qEE", "vCD", "qEC", "qEB", "EFA", "mCs",
    "tBi", "ghA", "EEk", "mCg", "tBb", "ggk", "qag", "vDb",
    "ggc", "EEE", "mCD", "ggE", "qaD", "ggC", "Eas", "mDi",
    "gis", "Eag", "mDb", "gig", "qbb", "gia", "EaD", "giD",
    "gji", "gjb", "qCk", "vBg", "xkr", "qCc", "vBa", "qCE",
    "vBD", "qCC", "qCB", "ECk", "mBg", "tAr", "gak", "ECc",
    "mBa", "gac", "qDa", "mBD", "gaE", "ECC", "gaC", "ECB",
    "EDg", "gbg", "gba", "gbD", "vAq", "vAn", "qBB", "mAq",
    "EBE", "gDE", "gDC", "gDB", "lfA", "sps", "wey", "ldk",
    "sow", "ClA", "lcs", "soi", "Ckk", "lcg", "Ckc", "CkE",
    "CvA", "lhs", "sqy", "Ctk", "lgw", "sqj", "Css", "lgi",
    "Csg", "Csa", "Cxs", "liy", "Cww", "lij", "Cwi", "Cyy",
    "Cyj", "tpk", "wuw", "yhj", "ndA", "tos", "wui", "nck",
    "tog", "wub", "ncc", "toa", "ncE", "toD", "lFk", "smw",
    "wdj", "nhk", "lEs", "smi", "atA", "Cck", "tqi", "smb",
    "ask", "ngg", "lEa", "asc", "CcE", "asE", "Chk", "law",
    "snj", "axk", "Cgs", "trj", "aws", "nii", "lab", "awg",
    "Cga", "awa", "Ciw", "lbj", "ayw", "Cii", "ayi", "Cib",
    "Cjj", "azj", "vpA", "xus", "yxi", "vok", "xug", "yxb",
    "voc", "xua", "voE", "xuD", "voC", "nFA", "tms", "wti",
    "rhA", "nEk", "xvi", "wtb", "rgk", "vqg", "xvb", "rgc",
    "nEE", "tmD", "rgE", "vqD", "nEB", "CFA", "lCs", "sli",
    "ahA", "CEk", "lCg", "slb", "ixA", "agk", "nag", "tnb",
    "iwk", "rig", "vrb", "lCD", "iwc", "agE", "naD", "iwE",
    "CEB", "Cas", "lDi", "ais", "Cag", "lDb", "iys", "aig",
    "nbb", "iyg", "rjb", "CaD", "aiD", "Cbi", "aji", "Cbb",
    "izi", "ajb", "vmk", "xtg", "ywr", "vmc", "xta", "vmE",
    "xtD", "vmC", "vmB", "nCk", "tlg", "wsr", "rak", "nCc",
    "xtr", "rac", "vna", "tlD", "raE", "nCC", "raC", "nCB",
    "raB", "CCk", "lBg", "skr", "aak", "CCc", "lBa", "iik",
    "aac", "nDa", "lBD", "iic", "rba", "CCC", "iiE", "aaC",
    "CCB", "aaB", "CDg", "lBr", "abg", "CDa", "ijg", "aba",
    "CDD", "ija", "abD", "CDr", "ijr", "vlc", "xsq", "vlE",
    "xsn", "vlC", "vlB", "nBc", "tkq", "rDc", "nBE", "tkn",
    "rDE", "vln", "rDC", "nBB", "rDB", "CBc", "lAq", "aDc",
    "CBE", "lAn", "ibc", "aDE", "nBn", "ibE", "rDn", "CBB",
    "ibC", "aDB", "ibB", "aDq", "ibq", "ibn", "xsf", "vkl",
    "tkf", "nAm", "nAl", "CAo", "aBo", "iDo", "CAl", "aBl",
    "kpk", "BdA", "kos", "Bck", "kog", "seb", "Bcc", "koa",
    "BcE", "koD", "Bhk", "kqw", "sfj", "Bgs", "kqi", "Bgg",
    "kqb", "Bga", "BgD", "Biw", "krj", "Bii", "Bib", "Bjj",
    "lpA", "sus", "whi", "lok", "sug", "loc", "sua", "loE",
    "suD", "loC", "BFA", "kms", "sdi", "DhA", "BEk", "svi",
    "sdb", "Dgk", "lqg", "svb", "Dgc", "BEE", "kmD", "DgE",
    "lqD", "BEB", "Bas", "kni", "Dis", "Bag", "knb", "Dig",
    "lrb", "Dia", "BaD", "Bbi", "Dji", "Bbb", "Djb", "tuk",
    "wxg", "yir", "tuc", "wxa", "tuE", "wxD", "tuC", "tuB",
    "lmk", "stg", "nqk", "lmc", "sta", "nqc", "tva", "stD",
    "nqE", "lmC", "nqC", "lmB", "nqB", "BCk", "klg", "Dak",
    "BCc", "str", "bik", "Dac", "lna", "klD", "bic", "nra",
    "BCC", "biE", "DaC", "BCB", "DaB", "BDg", "klr", "Dbg",
    "BDa", "bjg", "Dba", "BDD", "bja", "DbD", "BDr", "Dbr",
    "bjr", "xxc", "yyq", "xxE", "yyn", "xxC", "xxB", "ttc",
    "wwq", "vvc", "xxq", "wwn", "vvE", "xxn", "vvC", "ttB",
    "vvB", "llc", "ssq", "nnc", "llE", "ssn", "rrc", "nnE",
    "ttn", "rrE", "vvn", "llB", "rrC", "nnB", "rrB", "BBc",
    "kkq", "DDc", "BBE", "kkn", "bbc", "DDE", "lln", "jjc",
    "bbE", "nnn", "BBB", "jjE", "rrn", "DDB", "jjC", "BBq",
    "DDq", "BBn", "bbq", "DDn", "jjq", "bbn", "jjn", "xwo",
    "yyf", "xwm", "xwl", "tso", "wwf", "vto", "xwv", "vtm",
    "tsl", "vtl", "lko", "ssf", "nlo", "lkm", "rno", "nlm",
    "lkl", "rnm", "nll", "rnl", "BAo", "kkf", "DBo", "lkv",
    "bDo", "DBm", "BAl", "jbo", "bDm", "DBl", "jbm", "bDl",
    "jbl", "DBv", "jbv", "xwd", "vsu", "vst", "nku", "rlu",
    "rlt", "DAu", "bBu", "jDu", "jDt", "ApA", "Aok", "keg",
    "Aoc", "AoE", "AoC", "Aqs", "Aqg", "Aqa", "AqD", "Ari",
    "Arb", "kuk", "kuc", "sha", "kuE", "shD", "kuC", "kuB",
    "Amk", "kdg", "Bqk", "kvg", "kda", "Bqc", "kva", "BqE",
    "kvD", "BqC", "AmB", "BqB", "Ang", "kdr", "Brg", "kvr",
    "Bra", "AnD", "BrD", "Anr", "Brr", "sxc", "sxE", "sxC",
    "sxB", "ktc", "lvc", "sxq", "sgn", "lvE", "sxn", "lvC",
    "ktB", "lvB", "Alc", "Bnc", "AlE", "kcn", "Drc", "BnE",
    "AlC", "DrE", "BnC", "AlB", "DrC", "BnB", "Alq", "Bnq",
    "Aln", "Drq", "Bnn", "Drn", "wyo", "wym", "wyl", "swo",
    "txo", "wyv", "txm", "swl", "txl", "kso", "sgf", "lto",
    "swv", "nvo", "ltm", "ksl", "nvm", "ltl", "nvl", "Ako",
    "kcf", "Blo", "ksv", "Dno", "Blm", "Akl", "bro", "Dnm",
    "Bll", "brm", "Dnl", "Akv", "Blv", "Dnv", "brv", "yze",
    "yzd", "wye", "xyu", "wyd", "xyt", "swe", "twu", "swd",
    "vxu", "twt", "vxt", "kse", "lsu", "ksd", "ntu", "lst",
    "rvu", "ypk", "zew", "xdA", "yos", "zei", "xck", "yog",
    "zeb", "xcc", "yoa", "xcE", "yoD", "xcC", "xhk", "yqw",
    "zfj", "utA", "xgs", "yqi", "usk", "xgg", "yqb", "usc",
    "xga", "usE", "xgD", "usC", "uxk", "xiw", "yrj", "ptA",
    "uws", "xii", "psk", "uwg", "xib", "psc", "uwa", "psE",
    "uwD", "psC", "pxk", "uyw", "xjj", "ftA", "pws", "uyi",
    "fsk", "pwg", "uyb", "fsc", "pwa", "fsE", "pwD", "fxk",
    "pyw", "uzj", "fws", "pyi", "fwg", "pyb", "fwa", "fyw",
    "pzj", "fyi", "fyb", "xFA", "yms", "zdi", "xEk", "ymg",
    "zdb", "xEc", "yma", "xEE", "ymD", "xEC", "xEB", "uhA",
    "xas", "yni", "ugk", "xag", "ynb", "ugc", "xaa", "ugE",
    "xaD", "ugC", "ugB", "oxA", "uis", "xbi", "owk", "uig",
    "xbb", "owc", "uia", "owE", "uiD", "owC", "owB", "dxA",
    "oys", "uji", "dwk", "oyg", "ujb", "dwc", "oya", "dwE",
    "oyD", "dwC", "dys", "ozi", "dyg", "ozb", "dya", "dyD",
    "dzi", "dzb", "xCk", "ylg", "zcr", "xCc", "yla", "xCE",
    "ylD", "xCC", "xCB", "uak", "xDg", "ylr", "uac", "xDa",
    "uaE", "xDD", "uaC", "uaB", "oik", "ubg", "xDr", "oic",
    "uba", "oiE", "ubD", "oiC", "oiB", "cyk", "ojg", "ubr",
    "cyc", "oja", "cyE", "ojD", "cyC", "cyB", "czg", "ojr",
    "cza", "czD", "czr", "xBc", "ykq", "xBE", "ykn", "xBC",
    "xBB", "uDc", "xBq", "uDE", "xBn", "uDC", "uDB", "obc",
    "uDq", "obE", "uDn", "obC", "obB", "cjc", "obq", "cjE",
    "obn", "cjC", "cjB", "cjq", "cjn", "xAo", "ykf", "xAm",
    "xAl", "uBo", "xAv", "uBm", "uBl", "oDo", "uBv", "oDm",
    "oDl", "cbo", "oDv", "cbm", "cbl", "xAe", "xAd", "uAu",
    "uAt", "oBu", "oBt", "wpA", "yes", "zFi", "wok", "yeg",
    "zFb", "woc", "yea", "woE", "yeD", "woC", "woB", "thA",
    "wqs", "yfi", "tgk", "wqg", "yfb", "tgc", "wqa", "tgE",
    "wqD", "tgC", "tgB", "mxA", "tis", "wri", "mwk", "tig",
    "wrb", "mwc", "tia", "mwE", "tiD", "mwC", "mwB", "FxA",
    "mys", "tji", "Fwk", "myg", "tjb", "Fwc", "mya", "FwE",
    "myD", "FwC", "Fys", "mzi", "Fyg", "mzb", "Fya", "FyD",
    "Fzi", "Fzb", "yuk", "zhg", "hjs", "yuc", "zha", "hbw",
    "yuE", "zhD", "hDy", "yuC", "yuB", "wmk", "ydg", "zEr",
    "xqk", "wmc", "zhr", "xqc", "yva", "ydD", "xqE", "wmC",
    "xqC", "wmB", "xqB", "tak", "wng", "ydr", "vik", "tac",
    "wna", "vic", "xra", "wnD", "viE", "taC", "viC", "taB",
    "viB", "mik", "tbg", "wnr", "qyk", "mic", "tba", "qyc",
    "vja", "tbD", "qyE", "miC", "qyC", "miB", "qyB", "Eyk",
    "mjg", "tbr", "hyk", "Eyc", "mja", "hyc", "qza", "mjD",
    "hyE", "EyC", "hyC", "EyB", "Ezg", "mjr", "hzg", "Eza",
    "hza", "EzD", "hzD", "Ezr", "ytc", "zgq", "grw", "ytE",
    "zgn", "gny", "ytC", "glz", "ytB", "wlc", "ycq", "xnc",
    "wlE", "ycn", "xnE", "ytn", "xnC", "wlB", "xnB", "tDc",
    "wlq", "vbc", "tDE", "wln", "vbE", "xnn", "vbC", "tDB",
    "vbB", "mbc", "tDq", "qjc", "mbE", "tDn", "qjE", "vbn",
    "qjC", "mbB", "qjB", "Ejc", "mbq", "gzc", "EjE", "mbn",
    "gzE", "qjn", "gzC", "EjB", "gzB", "Ejq", "gzq", "Ejn",
    "gzn", "yso", "zgf", "gfy", "ysm", "gdz", "ysl", "wko",
    "ycf", "xlo", "ysv", "xlm", "wkl", "xll", "tBo", "wkv",
    "vDo", "tBm", "vDm", "tBl", "vDl", "mDo", "tBv", "qbo",
    "vDv", "qbm", "mDl", "qbl", "Ebo", "mDv", "gjo", "Ebm",
    "gjm", "Ebl", "gjl", "Ebv", "gjv", "yse", "gFz", "ysd",
    "wke", "xku", "wkd", "xkt", "tAu", "vBu", "tAt", "vBt",
    "mBu", "qDu", "mBt", "qDt", "EDu", "gbu", "EDt", "gbt",
    "ysF", "wkF", "xkh", "tAh", "vAx", "mAx", "qBx", "wek",
    "yFg", "zCr", "wec", "yFa", "weE", "yFD", "weC", "weB",
    "sqk", "wfg", "yFr", "sqc", "wfa", "sqE", "wfD", "sqC",
    "sqB", "lik", "srg", "wfr", "lic", "sra", "liE", "srD",
    "liC", "liB", "Cyk", "ljg", "srr", "Cyc", "lja", "CyE",
    "ljD", "CyC", "CyB", "Czg", "ljr", "Cza", "CzD", "Czr",
    "yhc", "zaq", "arw", "yhE", "zan", "any", "yhC", "alz",
    "yhB", "wdc", "yEq", "wvc", "wdE", "yEn", "wvE", "yhn",
    "wvC", "wdB", "wvB", "snc", "wdq", "trc", "snE", "wdn",
    "trE", "wvn", "trC", "snB", "trB", "lbc", "snq", "njc",
    "lbE", "snn", "njE", "trn", "njC", "lbB", "njB", "Cjc",
    "lbq", "azc", "CjE", "lbn", "azE", "njn", "azC", "CjB",
    "azB", "Cjq", "azq", "Cjn", "azn", "zio", "irs", "rfy",
    "zim", "inw", "rdz", "zil", "ily", "ikz", "ygo", "zaf",
    "afy", "yxo", "ziv", "ivy", "adz", "yxm", "ygl", "itz",
    "yxl", "wco", "yEf", "wto", "wcm", "xvo", "yxv", "wcl",
    "xvm", "wtl", "xvl", "slo", "wcv", "tno", "slm", "vro",
    "tnm", "sll", "vrm", "tnl", "vrl", "lDo", "slv", "nbo",
    "lDm", "rjo", "nbm", "lDl", "rjm", "nbl", "rjl", "Cbo",
    "lDv", "ajo", "Cbm", "izo", "ajm", "Cbl", "izm", "ajl",
    "izl", "Cbv", "ajv", "zie", "ifw", "rFz", "zid", "idy",
    "icz", "yge", "aFz", "ywu", "ygd", "ihz", "ywt", "wce",
    "wsu", "wcd", "xtu", "wst", "xtt", "sku", "tlu", "skt",
    "vnu", "tlt", "vnt", "lBu", "nDu", "lBt", "rbu", "nDt",
    "rbt", "CDu", "abu", "CDt", "iju", "abt", "ijt", "ziF",
    "iFy", "iEz", "ygF", "ywh", "wcF", "wsh", "xsx", "skh",
    "tkx", "vlx", "lAx", "nBx", "rDx", "CBx", "aDx", "ibx",
    "iCz", "wFc", "yCq", "wFE", "yCn", "wFC", "wFB", "sfc",
    "wFq", "sfE", "wFn", "sfC", "sfB", "krc", "sfq", "krE",
    "sfn", "krC", "krB", "Bjc", "krq", "BjE", "krn", "BjC",
    "BjB", "Bjq", "Bjn", "yao", "zDf", "Dfy", "yam", "Ddz",
    "yal", "wEo", "yCf", "who", "wEm", "whm", "wEl", "whl",
    "sdo", "wEv", "svo", "sdm", "svm", "sdl", "svl", "kno",
    "sdv", "lro", "knm", "lrm", "knl", "lrl", "Bbo", "knv",
    "Djo", "Bbm", "Djm", "Bbl", "Djl", "Bbv", "Djv", "zbe",
    "bfw", "npz", "zbd", "bdy", "bcz", "yae", "DFz", "yiu",
    "yad", "bhz", "yit", "wEe", "wgu", "wEd", "wxu", "wgt",
    "wxt", "scu", "stu", "sct", "tvu", "stt", "tvt", "klu",
    "lnu", "klt", "nru", "lnt", "nrt", "BDu", "Dbu", "BDt",
    "bju", "Dbt", "bjt", "jfs", "rpy", "jdw", "roz", "jcy",
    "jcj", "zbF", "bFy", "zjh", "jhy", "bEz", "jgz", "yaF",
    "yih", "yyx", "wEF", "wgh", "wwx", "xxx", "sch", "ssx",
    "ttx", "vvx", "kkx", "llx", "nnx", "rrx", "BBx", "DDx",
    "bbx", "jFw", "rmz", "jEy", "jEj", "bCz", "jaz", "jCy",
    "jCj", "jBj", "wCo", "wCm", "wCl", "sFo", "wCv", "sFm",
    "sFl", "kfo", "sFv", "kfm", "kfl", "Aro", "kfv", "Arm",
    "Arl", "Arv", "yDe", "Bpz", "yDd", "wCe", "wau", "wCd",
    "wat", "sEu", "shu", "sEt", "sht", "kdu", "kvu", "kdt",
    "kvt", "Anu", "Bru", "Ant", "Brt", "zDp", "Dpy", "Doz",
    "yDF", "ybh", "wCF", "wah", "wix", "sEh", "sgx", "sxx",
    "kcx", "ktx", "lvx", "Alx", "Bnx", "Drx", "bpw", "nuz",
    "boy", "boj", "Dmz", "bqz", "jps", "ruy", "jow", "ruj",
    "joi", "job", "bmy", "jqy", "bmj", "jqj", "jmw", "rtj",
    "jmi", "jmb", "blj", "jnj", "jli", "jlb", "jkr", "sCu",
    "sCt", "kFu", "kFt", "Afu", "Aft", "wDh", "sCh", "sax",
    "kEx", "khx", "Adx", "Avx", "Buz", "Duy", "Duj", "buw",
    "nxj", "bui", "bub", "Dtj", "bvj", "jus", "rxi", "jug",
    "rxb", "jua", "juD", "bti", "jvi", "btb", "jvb", "jtg",
    "rwr", "jta", "jtD", "bsr", "jtr", "jsq", "jsn", "Bxj",
    "Dxi", "Dxb", "bxg", "nyr", "bxa", "bxD", "Dwr", "bxr",
    "bwq", "bwn", "pjk", "urw", "ejA", "pbs", "uny", "ebk",
    "pDw", "ulz", "eDs", "pBy", "eBw", "zfc", "fjk", "prw",
    "zfE", "fbs", "pny", "zfC", "fDw", "plz", "zfB", "fBy",
    "yrc", "zfq", "frw", "yrE", "zfn", "fny", "yrC", "flz",
    "yrB", "xjc", "yrq", "xjE", "yrn", "xjC", "xjB", "uzc",
    "xjq", "uzE", "xjn", "uzC", "uzB", "pzc", "uzq", "pzE",
    "uzn", "pzC", "djA", "ors", "ufy", "dbk", "onw", "udz",
    "dDs", "oly", "dBw", "okz", "dAy", "zdo", "drs", "ovy",
    "zdm", "dnw", "otz", "zdl", "dly", "dkz", "yno", "zdv",
    "dvy", "ynm", "dtz", "ynl", "xbo", "ynv", "xbm", "xbl",
    "ujo", "xbv", "ujm", "ujl", "ozo", "ujv", "ozm", "ozl",
    "crk", "ofw", "uFz", "cns", "ody", "clw", "ocz", "cky",
    "ckj", "zcu", "cvw", "ohz", "zct", "cty", "csz", "ylu",
    "cxz", "ylt", "xDu", "xDt", "ubu", "ubt", "oju", "ojt",
    "cfs", "oFy", "cdw", "oEz", "ccy", "ccj", "zch", "chy",
    "cgz", "ykx", "xBx", "uDx", "cFw", "oCz", "cEy", "cEj",
    "caz", "cCy", "cCj", "FjA", "mrs", "tfy", "Fbk", "mnw",
    "tdz", "FDs", "mly", "FBw", "mkz", "FAy", "zFo", "Frs",
    "mvy", "zFm", "Fnw", "mtz", "zFl", "Fly", "Fkz", "yfo",
    "zFv", "Fvy", "yfm", "Ftz", "yfl", "wro", "yfv", "wrm",
    "wrl", "tjo", "wrv", "tjm", "tjl", "mzo", "tjv", "mzm",
    "mzl", "qrk", "vfw", "xpz", "hbA", "qns", "vdy", "hDk",
    "qlw", "vcz", "hBs", "qky", "hAw", "qkj", "hAi", "Erk",
    "mfw", "tFz", "hrk", "Ens", "mdy", "hns", "qty", "mcz",
    "hlw", "Eky", "hky", "Ekj", "hkj", "zEu", "Evw", "mhz",
    "zhu", "zEt", "hvw", "Ety", "zht", "hty", "Esz", "hsz",
    "ydu", "Exz", "yvu", "ydt", "hxz", "yvt", "wnu", "xru",
    "wnt", "xrt", "tbu", "vju", "tbt", "vjt", "mju", "mjt",
    "grA", "qfs", "vFy", "gnk", "qdw", "vEz", "gls", "qcy",
    "gkw", "qcj", "gki", "gkb", "Efs", "mFy", "gvs", "Edw",
    "mEz", "gtw", "qgz", "gsy", "Ecj", "gsj", "zEh", "Ehy",
    "zgx", "gxy", "Egz", "gwz", "ycx", "ytx", "wlx", "xnx",
    "tDx", "vbx", "mbx", "gfk", "qFw", "vCz", "gds", "qEy",
    "gcw", "qEj", "gci", "gcb", "EFw", "mCz", "ghw", "EEy",
    "ggy", "EEj", "ggj", "Eaz", "giz", "gFs", "qCy", "gEw",
    "qCj", "gEi", "gEb", "ECy", "gay", "ECj", "gaj", "gCw",
    "qBj", "gCi", "gCb", "EBj", "gDj", "gBi", "gBb", "Crk",
    "lfw", "spz", "Cns", "ldy", "Clw", "lcz", "Cky", "Ckj",
    "zCu", "Cvw", "lhz", "zCt", "Cty", "Csz", "yFu", "Cxz",
    "yFt", "wfu", "wft", "sru", "srt", "lju", "ljt", "arA",
    "nfs", "tpy", "ank", "ndw", "toz", "als", "ncy", "akw",
    "ncj", "aki", "akb", "Cfs", "lFy", "avs", "Cdw", "lEz",
    "atw", "ngz", "asy", "Ccj", "asj", "zCh", "Chy", "zax",
    "axy", "Cgz", "awz", "yEx", "yhx", "wdx", "wvx", "snx",
    "trx", "lbx", "rfk", "vpw", "xuz", "inA", "rds", "voy",
    "ilk", "rcw", "voj", "iks", "rci", "ikg", "rcb", "ika",
    "afk", "nFw", "tmz", "ivk", "ads", "nEy", "its", "rgy",
    "nEj", "isw", "aci", "isi", "acb", "isb", "CFw", "lCz",
    "ahw", "CEy", "ixw", "agy", "CEj", "iwy", "agj", "iwj",
    "Caz", "aiz", "iyz", "ifA", "rFs", "vmy", "idk", "rEw",
    "vmj", "ics", "rEi", "icg", "rEb", "ica", "icD", "aFs",
    "nCy", "ihs", "aEw", "nCj", "igw", "raj", "igi", "aEb",
    "igb", "CCy", "aay", "CCj", "iiy", "aaj", "iij", "iFk",
    "rCw", "vlj", "iEs", "rCi", "iEg", "rCb", "iEa", "iED",
    "aCw", "nBj", "iaw", "aCi", "iai", "aCb", "iab", "CBj",
    "aDj", "ibj", "iCs", "rBi", "iCg", "rBb", "iCa", "iCD",
    "aBi", "iDi", "aBb", "iDb", "iBg", "rAr", "iBa", "iBD",
    "aAr", "iBr", "iAq", "iAn", "Bfs", "kpy", "Bdw", "koz",
    "Bcy", "Bcj", "Bhy", "Bgz", "yCx", "wFx", "sfx", "krx",
    "Dfk", "lpw", "suz", "Dds", "loy", "Dcw", "loj", "Dci",
    "Dcb", "BFw", "kmz", "Dhw", "BEy", "Dgy", "BEj", "Dgj",
    "Baz", "Diz", "bfA", "nps", "tuy", "bdk", "now", "tuj",
    "bcs", "noi", "bcg", "nob", "bca", "bcD", "DFs", "lmy",
    "bhs", "DEw", "lmj", "bgw", "DEi", "bgi", "DEb", "bgb",
    "BCy", "Day", "BCj", "biy", "Daj", "bij", "rpk", "vuw",
    "xxj", "jdA", "ros", "vui", "jck", "rog", "vub", "jcc",
    "roa", "jcE", "roD", "jcC", "bFk", "nmw", "ttj", "jhk",
    "bEs", "nmi", "jgs", "rqi", "nmb", "jgg", "bEa", "jga",
    "bED", "jgD", "DCw", "llj", "baw", "DCi", "jiw", "bai",
    "DCb", "jii", "bab", "jib", "BBj", "DDj", "bbj", "jjj",
    "jFA", "rms", "vti", "jEk", "rmg", "vtb", "jEc", "rma",
    "jEE", "rmD", "jEC", "jEB", "bCs", "nli", "jas", "bCg",
    "nlb", "jag", "rnb", "jaa", "bCD", "jaD", "DBi", "bDi",
    "DBb", "jbi", "bDb", "jbb", "jCk", "rlg", "vsr", "jCc",
    "rla", "jCE", "rlD", "jCC", "jCB", "bBg", "nkr", "jDg",
    "bBa", "jDa", "bBD", "jDD", "DAr", "bBr", "jDr", "jBc",
    "rkq", "jBE", "rkn", "jBC", "jBB", "bAq", "jBq", "bAn",
    "jBn", "jAo", "rkf", "jAm", "jAl", "bAf", "jAv", "Apw",
    "kez", "Aoy", "Aoj", "Aqz", "Bps", "kuy", "Bow", "kuj",
    "Boi", "Bob", "Amy", "Bqy", "Amj", "Bqj", "Dpk", "luw",
    "sxj", "Dos", "lui", "Dog", "lub", "Doa", "DoD", "Bmw",
    "ktj", "Dqw", "Bmi", "Dqi", "Bmb", "Dqb", "Alj", "Bnj",
    "Drj", "bpA", "nus", "txi", "bok", "nug", "txb", "boc",
    "nua", "boE", "nuD", "boC", "boB", "Dms", "lti", "bqs",
    "Dmg", "ltb", "bqg", "nvb", "bqa", "DmD", "bqD", "Bli",
    "Dni", "Blb", "bri", "Dnb", "brb", "ruk", "vxg", "xyr",
    "ruc", "vxa", "ruE", "vxD", "ruC", "ruB", "bmk", "ntg",
    "twr", "jqk", "bmc", "nta", "jqc", "rva", "ntD", "jqE",
    "bmC", "jqC", "bmB", "jqB", "Dlg", "lsr", "bng", "Dla",
    "jrg", "bna", "DlD", "jra", "bnD", "jrD", "Bkr", "Dlr",
    "bnr", "jrr", "rtc", "vwq", "rtE", "vwn", "rtC", "rtB",
    "blc", "nsq", "jnc", "blE", "nsn", "jnE", "rtn", "jnC",
    "blB", "jnB", "Dkq", "blq", "Dkn", "jnq", "bln", "jnn",
    "rso", "vwf", "rsm", "rsl", "bko", "nsf", "jlo", "bkm",
    "jlm", "bkl", "jll", "Dkf", "bkv", "jlv", "rse", "rsd",
    "bke", "jku", "bkd", "jkt", "Aey", "Aej", "Auw", "khj",
    "Aui", "Aub", "Adj", "Avj", "Bus", "kxi", "Bug", "kxb",
    "Bua", "BuD", "Ati", "Bvi", "Atb", "Bvb", "Duk", "lxg",
    "syr", "Duc", "lxa", "DuE", "lxD", "DuC", "DuB", "Btg",
    "kwr", "Dvg", "lxr", "Dva", "BtD", "DvD", "Asr", "Btr",
    "Dvr", "nxc", "tyq", "nxE", "tyn", "nxC", "nxB", "Dtc",
    "lwq", "bvc", "nxq", "lwn", "bvE", "DtC", "bvC", "DtB",
    "bvB", "Bsq", "Dtq", "Bsn", "bvq", "Dtn", "bvn", "vyo",
    "xzf", "vym", "vyl", "nwo", "tyf", "rxo", "nwm", "rxm",
    "nwl", "rxl", "Dso", "lwf", "bto", "Dsm", "jvo", "btm",
    "Dsl", "jvm", "btl", "jvl", "Bsf", "Dsv", "btv", "jvv",
    "vye", "vyd", "nwe", "rwu", "nwd", "rwt", "Dse", "bsu",
    "Dsd", "jtu", "bst", "jtt", "vyF", "nwF", "rwh", "DsF",
    "bsh", "jsx", "Ahi", "Ahb", "Axg", "kir", "Axa", "AxD",
    "Agr", "Axr", "Bxc", "kyq", "BxE", "kyn", "BxC", "BxB",
    "Awq", "Bxq", "Awn", "Bxn", "lyo", "szf", "lym", "lyl",
    "Bwo", "kyf", "Dxo", "lyv", "Dxm", "Bwl", "Dxl", "Awf",
    "Bwv", "Dxv", "tze", "tzd", "lye", "nyu", "lyd", "nyt",
    "Bwe", "Dwu", "Bwd", "bxu", "Dwt", "bxt", "tzF", "lyF",
    "nyh", "BwF", "Dwh", "bwx", "Aiq", "Ain", "Ayo", "kjf",
    "Aym", "Ayl", "Aif", "Ayv", "kze", "kzd", "Aye", "Byu",
    "Ayd", "Byt", "szp",
    };

    internal static readonly int[] RapLRBits = BuildRapBits(RapLR);
    internal static readonly int[] RapCBits = BuildRapBits(RapC);
    internal static readonly int[] CodewordBits = BuildCodewordBits();
    internal static readonly int[][] CodewordLookup = BuildCodewordLookup();

    internal static bool TryGetVariant(int columns, int rows, out int variantIndex) {
        var cols = MicroVariants;
        var rowsTable = MicroVariants;
        for (var i = 0; i < 34; i++) {
            if (cols[i] == columns && rowsTable[i + 34] == rows) { variantIndex = i; return true; }
        }
        variantIndex = -1;
        return false;
    }

    internal static int GetRowWidth(int columns) {
        return columns switch {
            1 => 38,
            2 => 55,
            3 => 82,
            4 => 99,
            _ => throw new ArgumentOutOfRangeException(nameof(columns))
        };
    }

    private static string[] BuildPdfTtfMap() {
        var map = new string[128];
        for (var i = 0; i < BrSet.Length; i++) {
            var ch = BrSet[i];
            if (ch < 0 || ch >= map.Length) continue;
            map[ch] = PdfTtf[i];
        }
        return map;
    }

    private static int[] BuildRapBits(string[] patterns) {
        var bits = new int[patterns.Length];
        for (var i = 1; i < patterns.Length; i++) {
            var pattern = patterns[i];
            var value = 0;
            var flip = true;
            var count = 0;
            for (var j = 0; j < pattern.Length; j++) {
                var run = pattern[j] - '0';
                for (var k = 0; k < run; k++) {
                    value = (value << 1) | (flip ? 1 : 0);
                    count++;
                }
                flip = !flip;
            }
            if (count != 10) throw new InvalidOperationException("Invalid RAP pattern length.");
            bits[i] = value;
        }
        return bits;
    }

    private static int[] BuildCodewordBits() {
        var map = new int[128];
        for (var i = 0; i < map.Length; i++) {
            map[i] = -1;
        }
        for (var i = 0; i < BrSet.Length; i++) {
            var bits = PdfTtf[i];
            if (bits.Length != 5) continue;
            map[BrSet[i]] = ParseBits(bits);
        }
        var result = new int[CodewordPatterns.Length];
        for (var i = 0; i < CodewordPatterns.Length; i++) {
            var pattern = CodewordPatterns[i];
            var value = 0;
            for (var j = 0; j < pattern.Length; j++) {
                var mapped = map[pattern[j]];
                if (mapped < 0) throw new InvalidOperationException("Invalid codeword pattern character.");
                value = (value << 5) | mapped;
            }
            result[i] = value;
        }
        return result;
    }

    private static int[][] BuildCodewordLookup() {
        var lookup = new int[3][];
        for (var cluster = 0; cluster < 3; cluster++) {
            var table = new int[1 << 15];
            for (var i = 0; i < table.Length; i++) {
                table[i] = -1;
            }
            var offset = cluster * 929;
            for (var cw = 0; cw < 929; cw++) {
                table[CodewordBits[offset + cw]] = cw;
            }
            lookup[cluster] = table;
        }
        return lookup;
    }

    private static int ParseBits(string bits) {
        var value = 0;
        for (var i = 0; i < bits.Length; i++) {
            value = (value << 1) | (bits[i] == '1' ? 1 : 0);
        }
        return value;
    }
}
