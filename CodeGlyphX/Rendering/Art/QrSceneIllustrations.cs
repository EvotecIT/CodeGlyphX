using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

internal static class QrSceneIllustrations {
    internal static QrSceneGeometry Build(QrSceneOptions options) {
        var g = new QrSceneGeometry(); var c = options.Colors; var random = new SceneRandom(options.Seed);
        g.Layer(options.Backdrop);
        Backdrop(g, options, random);
        g.Layer(options.Motifs);
        switch (options.Style) {
            case QrSceneStyle.TropicalGarden: Garden(g, c, random); break;
            case QrSceneStyle.ElectricCity: City(g, c, options.Ink, random); break;
            case QrSceneStyle.MusicFestival: Music(g, c, options.Ink); break;
            case QrSceneStyle.OceanReef: Ocean(g, c, options.Ink); break;
            case QrSceneStyle.CosmicOrbit: Cosmos(g, c, options.Ink, random); break;
            default: Arcade(g, c, options.Ink, random); break;
        }
        return g;
    }
    private static void Backdrop(QrSceneGeometry g, QrSceneOptions o, SceneRandom random) {
        var c = o.Colors;
        if (o.Style == QrSceneStyle.ElectricCity || o.Style == QrSceneStyle.CosmicOrbit) {
            g.Rect(o.Ink, 0.025, 0.025, 0.95, 0.95);
            for (var i = 10; i > 0; i--) g.Oval(QrSceneGeometry.Mix(o.Ink, c[3], 0.08 + (10 - i) * 0.04), .65, .35, .045 * i, .055 * i);
            for (var i = 0; i < 80; i++) g.Oval(i % 3 == 0 ? c[2] : o.Paper, .05 + random.Next() * .9, .05 + random.Next() * .9, .001 + random.Next() * .002, .002);
        } else if (o.Style == QrSceneStyle.OceanReef) {
            for (var band = 0; band < 9; band++) {
                var points = new double[86];
                for (var x = 0; x < 41; x++) { points[x * 2] = x / 40.0; points[x * 2 + 1] = .08 + band * .1 + .04 * Math.Sin(x * .3 + band + o.Seed * .01); }
                points[82] = 1; points[83] = 1; points[84] = 0; points[85] = 1;
                g.Polygon(QrSceneGeometry.Mix(o.Paper, c[band % 4], .2 + band * .045), points);
            }
        } else if (o.Style == QrSceneStyle.RetroArcade) {
            for (var y = 0; y < 10; y++) for (var x = 0; x < 10; x++)
                g.Rect(QrSceneGeometry.Mix(o.Paper, c[(x + y) % 4], .12), x * .1, y * .1, .096, .096);
        } else {
            g.Oval(QrSceneGeometry.Mix(o.Paper, c[2], .6), .85, .15, .2, .2);
            g.Polygon(QrSceneGeometry.Mix(o.Paper, c[1], .4), 0, .35, .35, 0, .65, 0, 0, .65);
            g.Polygon(QrSceneGeometry.Mix(o.Paper, c[0], .25), .45, 1, 1, .45, 1, 1);
            for (var i = 0; i < 50; i++) g.Oval(c[i % 4], random.Next(), random.Next(), .003, .003);
        }
    }
    private static void Garden(QrSceneGeometry g, Rgba32[] c, SceneRandom r) {
        for (var side = 0; side < 2; side++) {
            var x = side == 0 ? .07 : .93;
            g.Line(c[3], x, .78, x + (side == 0 ? .05 : -.05), .1, .008);
            for (var i = 0; i < 7; i++) {
                var y = .17 + i * .09; var offset = side == 0 ? .045 : -.045;
                g.Oval(c[(i + side) % 2 + 1], x + offset, y, .078, .025, side == 0 ? -.55 : .55);
                g.Line(c[3], x, y + .02, x + offset * 2, y - .03, .002);
            }
        }
        for (var i = 0; i < 5; i++) {
            var x = .08 + i * .2; var y = i % 2 == 0 ? .8 : .12;
            for (var p = 0; p < 6; p++) { var a = p * Math.PI / 3 + r.Next() * .15; g.Oval(c[0], x + .022 * Math.Cos(a), y + .022 * Math.Sin(a), .018, .018); }
            g.Oval(c[2], x, y, .013, .013);
        }
    }
    private static void City(QrSceneGeometry g, Rgba32[] c, Rgba32 ink, SceneRandom r) {
        for (var i = 0; i < 15; i++) {
            var x = .035 + i * .063; var h = .07 + r.Next() * .16;
            g.Rect(QrSceneGeometry.Mix(ink, c[i % 4], .3), x, .84 - h, .049, h);
            for (var row = 0; row < 5; row++) for (var col = 0; col < 3; col++)
                if (row * .027 < h - .02 && r.Next() > .25) g.Rect(c[(i + row) % 4], x + .005 + col * .013, .825 - row * .027, .006, .012);
        }
        g.Oval(c[2], .18, .115, .07, .07);
        for (var side = 0; side < 2; side++) for (var i = 0; i < 4; i++) {
            var x = side == 0 ? .035 + i * .033 : .965 - i * .033;
            g.Line(c[i % 4], x, .3, x, .58, .004); g.Line(c[i % 4], x, .58, x + (side == 0 ? .035 : -.035), .63, .004);
            g.Oval(c[i % 4], x, .3, .009, .009);
        }
    }
    private static void Music(QrSceneGeometry g, Rgba32[] c, Rgba32 ink) {
        for (var i = 0; i < 8; i++) g.Line(c[i % 4], -.1 + i * .065, .89, .3 + i * .065, .05, .025);
        foreach (var x in new[] { .12, .88 }) {
            g.Oval(ink, x, .2, .095, .095); g.Oval(c[3], x, .2, .069, .069); g.Oval(ink, x, .2, .057, .057);
            g.Oval(c[0], x, .2, .035, .035); g.Oval(c[2], x, .2, .006, .006);
            g.Line(ink, x - .035, .75, x - .035, .57, .009); g.Oval(ink, x - .06, .75, .029, .02, -.3);
            g.Polygon(c[0], x - .035, .57, x + .035, .6, x + .035, .65, x - .035, .62);
        }
        for (var i = 0; i < 16; i++) g.Rect(c[i % 4], .22 + i * .037, .81 - .025 * Math.Sin(i), .022, .065 + .025 * Math.Sin(i));
    }
    private static void Ocean(QrSceneGeometry g, Rgba32[] c, Rgba32 ink) {
        for (var side = 0; side < 2; side++) {
            var x = side == 0 ? .09 : .91;
            for (var i = 0; i < 4; i++) { var y = .17 + i * .17; g.Oval(c[(i + side) % 4], x, y, .049, .024); g.Polygon(c[(i + side) % 4], x + .035, y, x + .073, y - .032, x + .073, y + .032); g.Oval(ink, x - .027, y - .004, .004, .004); }
            for (var i = 0; i < 4; i++) { var xx = x + (i - 2) * .025; g.Line(c[0], xx, .86, xx, .76 - i * .008, .011); g.Line(c[0], xx, .8, xx - .018, .775, .008); }
        }
        for (var i = 0; i < 16; i++) { var x = .08 + i * .056; g.Oval(c[1], x, .1 + .025 * Math.Sin(i), .012, .012); g.Oval(new Rgba32(245, 254, 252), x, .1 + .025 * Math.Sin(i), .008, .008); }
    }
    private static void Cosmos(QrSceneGeometry g, Rgba32[] c, Rgba32 ink, SceneRandom r) {
        foreach (var x in new[] { .12, .88 }) {
            g.Oval(c[1], x, .22, .092, .025, -.35); g.Oval(c[3], x, .22, .053, .053); g.Oval(c[0], x - .012, .2, .024, .015, -.3);
            g.Polygon(c[2], x - .023, .65, x + .023, .65, x, .73);
            g.Polygon(c[0], x - .027, .64, x - .05, .68, x - .04, .6, x, .53, x + .04, .6, x + .05, .68, x + .027, .64);
            g.Oval(c[1], x, .61, .017, .025); g.Oval(ink, x, .61, .011, .017);
        }
        for (var i = 0; i < 24; i++) { var x = r.Next(); var y = i % 2 == 0 ? .11 : .81; g.Line(c[i % 4], x - .006, y, x + .006, y, .003); g.Line(c[i % 4], x, y - .006, x, y + .006, .003); }
    }
    private static void Arcade(QrSceneGeometry g, Rgba32[] c, Rgba32 ink, SceneRandom r) {
        var creature = new[] { "00100100", "00011000", "00111100", "01111110", "11011011", "11111111", "10100101" };
        for (var side = 0; side < 2; side++) for (var k = 0; k < 3; k++) {
            var xx = side == 0 ? .035 : .835; var yy = .1 + k * .26; var color = c[(k + side) % 4];
            for (var y = 0; y < creature.Length; y++) for (var x = 0; x < 8; x++) if (creature[y][x] == '1') g.Rect(color, xx + x * .016, yy + y * .016, .015, .015);
            g.Rect(ink, xx + .033, yy + .05, .014, .014); g.Rect(ink, xx + .081, yy + .05, .014, .014);
        }
        for (var i = 0; i < 30; i++) g.Rect(c[i % 4], .03 + r.Next() * .92, i % 2 == 0 ? .025 : .82, .01, .01);
    }
}
