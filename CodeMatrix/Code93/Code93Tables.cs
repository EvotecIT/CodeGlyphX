using System.Collections.Generic;

namespace CodeMatrix.Code93;

internal static class Code93Tables {
    public const char Fnc1 = '\u00F1';
    public const char Fnc2 = '\u00F2';
    public const char Fnc3 = '\u00F3';
    public const char Fnc4 = '\u00F4';

    public static readonly IReadOnlyDictionary<char, (int value, uint data)> EncodingTable = new Dictionary<char, (int, uint)> {
        { '0', (0, 276u) },
        { '1', (1, 328u) },
        { '2', (2, 324u) },
        { '3', (3, 322u) },
        { '4', (4, 296u) },
        { '5', (5, 292u) },
        { '6', (6, 290u) },
        { '7', (7, 336u) },
        { '8', (8, 274u) },
        { '9', (9, 266u) },
        { 'A', (10, 424u) },
        { 'B', (11, 420u) },
        { 'C', (12, 418u) },
        { 'D', (13, 404u) },
        { 'E', (14, 402u) },
        { 'F', (15, 394u) },
        { 'G', (16, 360u) },
        { 'H', (17, 356u) },
        { 'I', (18, 354u) },
        { 'J', (19, 308u) },
        { 'K', (20, 282u) },
        { 'L', (21, 344u) },
        { 'M', (22, 332u) },
        { 'N', (23, 326u) },
        { 'O', (24, 300u) },
        { 'P', (25, 278u) },
        { 'Q', (26, 436u) },
        { 'R', (27, 434u) },
        { 'S', (28, 428u) },
        { 'T', (29, 422u) },
        { 'U', (30, 406u) },
        { 'V', (31, 410u) },
        { 'W', (32, 364u) },
        { 'X', (33, 358u) },
        { 'Y', (34, 310u) },
        { 'Z', (35, 314u) },
        { '-', (36, 302u) },
        { '.', (37, 468u) },
        { ' ', (38, 466u) },
        { '$', (39, 458u) },
        { '/', (40, 366u) },
        { '+', (41, 374u) },
        { '%', (42, 430u) },
        { '\u00F1', (43, 294u) },
        { '\u00F2', (44, 474u) },
        { '\u00F3', (45, 470u) },
        { '\u00F4', (46, 306u) },
        { '*', (47, 350u) }
    };

    public static readonly string[] ExtendedTable = new string[128] {
        "\u00F2U", "\u00F1A", "\u00F1B", "\u00F1C", "\u00F1D", "\u00F1E", "\u00F1F", "\u00F1G", "\u00F1H", "\u00F1I",
        "\u00F1J", "\u00F1K", "\u00F1L", "\u00F1M", "\u00F1N", "\u00F1O", "\u00F1P", "\u00F1Q", "\u00F1R", "\u00F1S",
        "\u00F1T", "\u00F1U", "\u00F1V", "\u00F1W", "\u00F1X", "\u00F1Y", "\u00F1Z", "\u00F2A", "\u00F2B", "\u00F2C",
        "\u00F2D", "\u00F2E", " ", "\u00F3A", "\u00F3B", "\u00F3C", "\u00F3D", "\u00F3E", "\u00F3F", "\u00F3G",
        "\u00F3H", "\u00F3I", "\u00F3J", "\u00F3K", "\u00F3L", "-", ".", "\u00F3O", "0", "1",
        "2", "3", "4", "5", "6", "7", "8", "9", "\u00F3Z", "\u00F2F",
        "\u00F2G", "\u00F2H", "\u00F2I", "\u00F2J", "\u00F2V", "A", "B", "C", "D", "E",
        "F", "G", "H", "I", "J", "K", "L", "M", "N", "O",
        "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y",
        "Z", "\u00F2K", "\u00F2L", "\u00F2M", "\u00F2N", "\u00F2O", "\u00F2W", "\u00F4A", "\u00F4B", "\u00F4C",
        "\u00F4D", "\u00F4E", "\u00F4F", "\u00F4G", "\u00F4H", "\u00F4I", "\u00F4J", "\u00F4K", "\u00F4L", "\u00F4M",
        "\u00F4N", "\u00F4O", "\u00F4P", "\u00F4Q", "\u00F4R", "\u00F4S", "\u00F4T", "\u00F4U", "\u00F4V", "\u00F4W",
        "\u00F4X", "\u00F4Y", "\u00F4Z", "\u00F2P", "\u00F2Q", "\u00F2R", "\u00F2S", "\u00F2T"
    };
}
