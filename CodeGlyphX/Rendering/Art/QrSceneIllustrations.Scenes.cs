using CodeGlyphX.Rendering;
using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Art;

internal static partial class QrSceneIllustrations {
    // A greenhouse arch on a warm afternoon: the code hangs in the arch window, monstera and palm fronds
    // reach in from opposite corners at different scales, and hibiscus blooms sit on the frame.
    private static void Garden(SceneContext s) {
        var g = s.G; var c = s.C; var f = s.Frame; var paper = s.Paper; var r = s.Random;
        s.Backdrop();
        g.Rect(ScenePaint.Linear(0, 0, 0, 1, (0, Mix(paper, c[2], 0.42)), (0.6, Mix(paper, c[0], 0.22)), (1, Mix(paper, c[0], 0.38))), 0, 0, 1, 1);
        g.Circle(Solid(Mix(paper, c[2], 0.45)), 0.8, 0.17, 0.21);
        g.Circle(Solid(Mix(paper, c[2], 0.9)), 0.8, 0.17, 0.16);
        g.Polygon(Solid(Mix(paper, c[1], 0.32)), new ScenePath(0, 0.86).Cubic(0.25, 0.76, 0.55, 0.9, 1, 0.8).Line(1, 1).Line(0, 1).ToArray());
        g.Polygon(Solid(Mix(paper, c[1], 0.55)), new ScenePath(0, 0.94).Cubic(0.3, 0.87, 0.7, 0.98, 1, 0.9).Line(1, 1).Line(0, 1).ToArray());

        // The arch: a sage frame band around a paper window. Its radius keeps every code corner inside.
        s.Plate();
        var side = 0.04; var head = 0.09; var foot = Math.Max(f.Bottom + 0.16, 0.82);
        var radius = ArchRadius(side, head, f.Size);
        g.Fill(Solid(c[3]), QrSceneGeometry.RoundRectangle(f.X - side - 0.022, f.Y - head - 0.022, f.Size + 2 * side + 0.044, foot - f.Y + head + 0.044, radius + 0.022, 0.03));
        g.Fill(Solid(Mix(paper, c[1], 0.3)), QrSceneGeometry.RoundRectangle(f.X - side - 0.012, f.Y - head - 0.012, f.Size + 2 * side + 0.024, foot - f.Y + head + 0.024, radius + 0.012, 0.022));
        g.Fill(Solid(paper), QrSceneGeometry.RoundRectangle(f.X - side, f.Y - head, f.Size + 2 * side, foot - f.Y + head, radius, 0.016));

        s.Motifs();
        var deep = c[3]; var leaf = c[1]; var light = Mix(c[1], paper, 0.35);
        // Lower left: a tall monstera and a second, smaller one turned the other way.
        Monstera(g, -0.03, 1.02, 0.46, -1.42 + r.Range(-0.05, 0.05), leaf, light);
        Monstera(g, -0.04, 0.78, 0.3, -0.62 + r.Range(-0.06, 0.06), deep, Mix(deep, paper, 0.35));
        // Upper right: a palm frond sweeping down across the corner.
        Frond(g, 1.03, -0.02, 0.52, -0.12, 1.95, deep, Mix(deep, c[1], 0.45));
        // Lower right: banana leaves rising from the ground.
        g.Fill(Solid(Mix(c[1], deep, 0.4)), Leaf(0.98, 1.03, 0.36, 0.1, -1.95, 0.04));
        g.Fill(Solid(leaf), Leaf(1.05, 1.0, 0.3, 0.09, -2.25, -0.03));
        g.Stroke(Solid(light), 0.004, 0.98, 1.03, 0.98 + 0.33 * Math.Cos(-1.95), 1.03 + 0.33 * Math.Sin(-1.95));
        // A monstera reaching in from the right crosses the arch frame; its tip stops short of the code.
        Monstera(g, 1.05, f.Y + f.Size * 0.48, Math.Max(0.12, 1.05 - (f.Right + 0.025)), Math.PI + r.Range(-0.08, 0.08), Mix(c[1], deep, 0.25), light);
        Hibiscus(g, f.Right + side + 0.05, f.Y + f.Size * 0.86, 0.06, r.Range(0, 1.2), c[0], c[2], deep);
        Hibiscus(g, f.X - side - 0.035, f.Y + 0.02, 0.048, r.Range(-1, 0.2), Mix(c[0], c[2], 0.35), c[2], deep);
        Hibiscus(g, 0.16, 0.94, 0.036, r.Range(0, 1.2), c[0], c[2], deep);
        // A trailing vine from the top-left corner along the arch.
        Vine(g, -0.01, 0.02, f.X - side + 0.04, f.Y - head - 0.01, Mix(c[1], deep, 0.3), c[1]);
    }

    private static void Vine(QrSceneGeometry g, double x0, double y0, double x1, double y1, Rgba32 stem, Rgba32 leaf) {
        var path = new ScenePath(x0, y0).Cubic(x0 + 0.12, y0 + 0.02, x1 - 0.12, y1 - 0.1, x1, y1, 16).ToArray();
        g.Stroke(ScenePaint.Solid(stem), 0.005, path);
        for (var i = 4; i < path.Length / 2 - 1; i += 3) {
            var px = path[2 * i]; var py = path[2 * i + 1];
            var tangent = Math.Atan2(path[2 * i + 3] - py, path[2 * i + 2] - px);
            g.Fill(ScenePaint.Solid(i % 2 == 0 ? leaf : stem), Leaf(px, py, 0.05, 0.022, tangent + (i % 2 == 0 ? 0.9 : -0.9)));
        }
    }

    private static double ArchRadius(double side, double head, double size) {
        // Largest radius whose rounded corner still contains the code corner (side and head margins).
        var best = Math.Min(side, head);
        for (var r = best; r <= size / 2 + side; r += 0.002) {
            var dx = r - side; var dy = r - head;
            if (dx > 0 && dy > 0 && Math.Sqrt(dx * dx + dy * dy) > r - 0.004) break;
            best = r;
        }
        return best;
    }

    private static void Monstera(QrSceneGeometry g, double x, double y, double length, double angle, Rgba32 color, Rgba32 rib) {
        var points = new List<double>();
        var cos = Math.Cos(angle); var sin = Math.Sin(angle);
        const int steps = 180;
        for (var i = 0; i <= steps; i++) Edge(i / (double)steps, 1);
        for (var i = steps - 1; i >= 1; i--) Edge(i / (double)steps, -1);
        var holes = new List<double[]>();
        foreach (var t in new[] { 0.34, 0.5, 0.64 }) foreach (var sideSign in new[] { 1.0, -1.0 }) {
            var u = t * length; var v = sideSign * 0.13 * length * Profile(t);
            holes.Add(QrSceneGeometry.Ellipse(x + u * cos - v * sin, y + u * sin + v * cos, 0.035 * length, 0.014 * length, angle + sideSign * 0.5, 24));
        }
        g.FillWithHoles(ScenePaint.Solid(color), points.ToArray(), holes.ToArray());
        g.Stroke(ScenePaint.Solid(rib), 0.006 * length / 0.3, x, y, x + 0.92 * length * cos, y + 0.92 * length * sin);
        void Edge(double t, double side) {
            var half = 0.43 * length * Profile(t);
            // V-shaped notches cut most of the way toward the midrib, as on a mature leaf.
            var notch = 1.0;
            for (var k = 0; k < 5; k++) {
                var centre = 0.22 + k * 0.14 + (side > 0 ? 0 : 0.05);
                notch = Math.Min(notch, 0.5 + 0.5 * Math.Min(1, Math.Abs(t - centre) / 0.03));
            }
            var u = t * length; var v = side * half * notch;
            points.Add(x + u * cos - v * sin); points.Add(y + u * sin + v * cos);
        }
        static double Profile(double t) => Math.Pow(Math.Sin(Math.PI * Math.Min(1, t)), 0.75) * (0.55 + 0.45 * Math.Sin(Math.PI * Math.Min(1, t * 0.85)));
    }

    private static void Frond(QrSceneGeometry g, double x, double y, double length, double bend, double angle, Rgba32 color, Rgba32 stem) {
        var cos = Math.Cos(angle); var sin = Math.Sin(angle);
        var spine = new List<double>();
        for (var i = 0; i <= 24; i++) {
            var t = i / 24.0; var u = t * length; var v = bend * length * t * t;
            spine.Add(x + u * cos - v * sin); spine.Add(y + u * sin + v * cos);
        }
        for (var i = 3; i <= 22; i++) {
            var t = i / 24.0;
            var px = spine[2 * i]; var py = spine[2 * i + 1];
            var tangent = Math.Atan2(spine[2 * i + 1] - spine[2 * i - 1], spine[2 * i] - spine[2 * i - 2]);
            var size = length * (0.26 - 0.18 * t);
            foreach (var sideSign in new[] { 1.0, -1.0 })
                g.Fill(ScenePaint.Solid(i % 2 == 0 ? color : Mix(color, stem, 0.35)), Leaf(px, py, size, size * 0.17, tangent + sideSign * 0.95, sideSign * 0.05));
        }
        g.Stroke(ScenePaint.Solid(stem), 0.007, spine.ToArray());
    }

    private static void Hibiscus(QrSceneGeometry g, double x, double y, double r, double turn, Rgba32 petal, Rgba32 pollen, Rgba32 deep) {
        for (var i = 0; i < 5; i++) {
            var a = turn + i * Math.PI * 2 / 5;
            g.Oval(ScenePaint.Solid(i % 2 == 0 ? petal : Mix(petal, deep, 0.12)), x + Math.Cos(a) * r * 0.52, y + Math.Sin(a) * r * 0.52, r * 0.62, r * 0.48, a);
        }
        g.Circle(ScenePaint.Solid(Mix(petal, deep, 0.55)), x, y, r * 0.24);
        var tipX = x + Math.Cos(turn - 0.9) * r * 0.85; var tipY = y + Math.Sin(turn - 0.9) * r * 0.85;
        g.Stroke(ScenePaint.Solid(Mix(petal, deep, 0.35)), r * 0.07, x, y, tipX, tipY);
        g.Circle(ScenePaint.Solid(pollen), tipX, tipY, r * 0.1);
    }

    // A billboard over a night skyline: the code is the lit sign, the city stays visible below and beside it.
    private static void City(SceneContext s) {
        var g = s.G; var c = s.C; var f = s.Frame; var paper = s.Paper; var r = s.Random;
        var night = Mix(s.Ink, Rgb(0x0B0E2A), 0.5);
        s.Backdrop();
        var sky = ScenePaint.Linear(0, 0, 0, 0.9, (0, night), (0.55, Mix(night, c[3], 0.45)), (1, Mix(c[3], c[0], 0.55)));
        g.Rect(sky, 0, 0, 1, 1);
        for (var i = 0; i < 70; i++) {
            var x = r.Next(); var y = r.Next() * 0.6;
            g.Circle(Solid(paper, 0.35 + 0.5 * r.Next()), x, y, 0.0012 + 0.0022 * r.Next());
        }
        // A crescent: the moon, then a disc of the same sky gradient over it.
        g.Circle(Solid(Mix(paper, c[2], 0.35)), 0.11, 0.1, 0.05);
        g.Circle(sky, 0.132, 0.086, 0.044);
        // Neon spill around the sign.
        g.Circle(ScenePaint.RadialGradient(f.CenterX, f.CenterY, f.Size * 1.05, 0.6, (0, c[1]), (0.5, Fade(Mix(c[1], c[3], 0.5), 0.45)), (1, Fade(c[3], 0))), f.CenterX, f.CenterY, f.Size * 1.05);
        // Far skyline.
        var far = Mix(night, c[3], 0.72);
        var x0 = -0.02;
        while (x0 < 1.02) {
            var w = 0.05 + 0.06 * r.Next(); var h = 0.17 + 0.2 * r.Next();
            g.Rect(Solid(far), x0, 0.88 - h, w + 0.002, h + 0.12);
            if (r.Next() < 0.3) g.Rect(Solid(far), x0 + w * 0.4, 0.88 - h - 0.04, w * 0.2, 0.05);
            for (var wy = 0.9 - h; wy < 0.84; wy += 0.03) if (r.Next() < 0.25) g.Rect(Solid(c[2], 0.35), x0 + w * (0.2 + 0.5 * r.Next()), wy, 0.007, 0.01);
            x0 += w;
        }

        // The sign: a dark cabinet with a neon tube and a paper face, on two steel legs.
        s.Plate();
        var m = 0.042; var frame = Mix(night, s.Ink, 0.5);
        var legTop = f.Bottom + m; var roof = Math.Min(0.95, Math.Max(legTop + 0.06, 0.8));
        foreach (var lx in new[] { f.X + f.Size * 0.24, f.Right - f.Size * 0.24 }) {
            g.Rect(Solid(frame), lx - 0.011, legTop - 0.01, 0.022, roof - legTop + 0.02);
        }
        g.Stroke(Solid(frame), 0.007, f.X + f.Size * 0.24, legTop + 0.01, f.Right - f.Size * 0.24, roof - 0.01);
        g.Stroke(Solid(frame), 0.007, f.Right - f.Size * 0.24, legTop + 0.01, f.X + f.Size * 0.24, roof - 0.01);
        g.RoundRect(Solid(c[1], 0.28), f.X - m - 0.022, f.Y - m - 0.022, f.Size + 2 * m + 0.044, f.Size + 2 * m + 0.044, 0.05);
        g.RoundRect(Solid(frame), f.X - m - 0.012, f.Y - m - 0.012, f.Size + 2 * m + 0.024, f.Size + 2 * m + 0.024, 0.04);
        g.FillWithHoles(Solid(c[1]), QrSceneGeometry.RoundRectangle(f.X - m - 0.006, f.Y - m - 0.006, f.Size + 2 * m + 0.012, f.Size + 2 * m + 0.012, 0.034, 0.034),
            QrSceneGeometry.RoundRectangle(f.X - m + 0.001, f.Y - m + 0.001, f.Size + 2 * m - 0.002, f.Size + 2 * m - 0.002, 0.027, 0.027));
        g.RoundRect(Solid(paper), f.X - m + 0.006, f.Y - m + 0.006, f.Size + 2 * m - 0.012, f.Size + 2 * m - 0.012, 0.022);
        // Two floodlights on arms reach over the cabinet's top edge and stop in the margin above the code.
        foreach (var lx in new[] { f.X + f.Size * 0.2, f.Right - f.Size * 0.2 }) {
            var tip = f.Y - 0.012;
            g.Stroke(Solid(frame), 0.008, lx, f.Y - m - 0.06, lx, tip - 0.012);
            g.Fill(Solid(Mix(frame, c[1], 0.15)), QrSceneGeometry.RoundRectangle(lx - 0.022, tip - 0.022, 0.044, 0.016, 0.006, 0.006));
            g.Fill(Solid(Mix(paper, c[2], 0.5)), QrSceneGeometry.RoundRectangle(lx - 0.016, tip - 0.008, 0.032, 0.005, 0.002, 0.002));
        }

        s.Motifs();
        // Near skyline with lit windows, kept below the sign.
        var near = Mix(night, s.Ink, 0.35);
        x0 = -0.03; var index = 0;
        while (x0 < 1.03) {
            var w = 0.07 + 0.07 * r.Next();
            var middle = x0 + w / 2;
            var clearTop = middle > f.X - 0.03 && middle < f.Right + 0.03 ? roof - 0.01 : 0.6;
            var top = clearTop + (0.86 - clearTop) * (0.15 + 0.5 * r.Next());
            g.Rect(Solid(index % 3 == 1 ? Mix(near, c[3], 0.18) : near), x0, top, w + 0.002, 1 - top);
            if (index % 4 == 2) {
                g.Rect(Solid(near), x0 + w * 0.45, top - 0.05, 0.006, 0.05);
                g.Circle(Solid(c[0]), x0 + w * 0.45 + 0.003, top - 0.052, 0.005);
            }
            for (var wy = top + 0.022; wy < 0.885; wy += 0.026) for (var wx = x0 + 0.012; wx < x0 + w - 0.016; wx += 0.02) {
                var lit = r.Next();
                if (lit < 0.42) g.Rect(Solid(lit < 0.12 ? c[1] : c[2], 0.9), wx, wy, 0.009, 0.012);
            }
            x0 += w; index++;
        }
        // Street and light trails.
        g.Rect(Solid(Mix(night, s.Ink, 0.7)), 0, 0.885, 1, 0.115);
        g.Stroke(Solid(c[0], 0.3), 0.012, -0.05, 0.9, 1.05, 0.906);
        g.Stroke(Solid(c[0]), 0.0035, -0.05, 0.9, 1.05, 0.906);
        g.Stroke(Solid(c[2], 0.3), 0.012, -0.05, 0.913, 1.05, 0.918);
        g.Stroke(Solid(c[2]), 0.0035, -0.05, 0.913, 1.05, 0.918);
    }

    // An open-air stage at sunset: spotlights rise behind an LED screen that carries the code, a speaker
    // stack stands on one side and a light tower on the other, and the crowd fills the foreground.
    private static void Festival(SceneContext s) {
        var g = s.G; var c = s.C; var f = s.Frame; var paper = s.Paper; var r = s.Random;
        var dusk = Mix(s.Ink, c[3], 0.35);
        s.Backdrop();
        g.Rect(ScenePaint.Linear(0, 0, 0, 0.86, (0, dusk), (0.45, Mix(c[3], c[0], 0.45)), (0.8, Mix(c[0], c[2], 0.55)), (1, Mix(c[2], paper, 0.25))), 0, 0, 1, 1);
        // Striped retro sun behind the stage.
        var sun = new List<double[]>();
        for (var i = 0; i < 5; i++) {
            var y = 0.74 + i * 0.03; var gap = 0.004 + i * 0.0025;
            var chord = Math.Sqrt(Math.Max(0, 0.33 * 0.33 - (y + gap - 0.78) * (y + gap - 0.78))) - 0.01;
            if (chord > 0.02) sun.Add(new[] { 0.5 - chord, y, 0.5 + chord, y, 0.5 + chord, y + gap, 0.5 - chord, y + gap });
        }
        g.FillWithHoles(ScenePaint.Linear(0, 0.5, 0, 0.9, (0, c[2]), (1, Mix(c[2], c[0], 0.6))), QrSceneGeometry.Ellipse(0.5, 0.78, 0.33, 0.33, 0), sun.ToArray());
        // Spotlight beams fanning up from the stage floor.
        var beams = new[] { (0.16, -0.45, c[1]), (0.3, -0.12, c[2]), (0.7, 0.14, c[1]), (0.84, 0.42, c[0]) };
        foreach (var (bx, lean, color) in beams) {
            var tx = bx + lean * 0.9;
            g.Polygon(Solid(Mix(color, paper, 0.3), 0.26), bx - 0.008, 0.86, bx + 0.008, 0.86, tx + 0.075, -0.02, tx - 0.075, -0.02);
        }

        // The LED screen: dark bezel, paper face.
        s.Plate();
        var m = 0.03; var bezel = Mix(Mix(s.Ink, c[3], 0.35), paper, 0.18);
        g.RoundRect(Solid(bezel), f.X - m - 0.014, f.Y - m - 0.014, f.Size + 2 * m + 0.028, f.Size + 2 * m + 0.028, 0.03);
        g.RoundRect(Solid(paper), f.X - m, f.Y - m, f.Size + 2 * m, f.Size + 2 * m, 0.018);
        var floor = Math.Max(f.Bottom + m + 0.03, 0.84);
        g.Rect(Solid(bezel), f.CenterX - 0.012, f.Bottom + m, 0.024, floor - f.Bottom - m);

        s.Motifs();
        // Stage deck.
        g.Rect(Solid(Mix(s.Ink, c[3], 0.15)), 0, floor, 1, 0.05);
        g.Rect(Solid(c[2]), 0, floor, 1, 0.006);
        // Speaker stack (left) with sound arcs.
        var left = Math.Max(0.02, f.X - m - 0.16);
        for (var k = 0; k < 3; k++) {
            var top = floor - (k + 1) * 0.105;
            g.RoundRect(Solid(Mix(s.Ink, c[3], 0.1)), left, top, 0.12, 0.1, 0.012);
            g.Circle(Solid(Mix(s.Ink, c[3], 0.45)), left + 0.06, top + 0.05, 0.032);
            g.Circle(Solid(Mix(s.Ink, c[1], 0.35)), left + 0.06, top + 0.05, 0.012);
        }
        for (var k = 0; k < 3; k++) {
            var radius = 0.06 + k * 0.035; var cx = left + 0.06; var cy = floor - 0.36;
            var arc = new List<double>();
            for (var a = -2.7; a <= -1.35; a += 0.08) { arc.Add(cx + radius * Math.Cos(a)); arc.Add(cy + radius * Math.Sin(a)); }
            g.Stroke(Solid(c[1], 0.9 - k * 0.22), 0.007, arc.ToArray());
        }
        // Light tower (right): a truss with three lamps.
        var tower = Math.Min(0.94, f.Right + m + 0.085);
        g.Rect(Solid(Mix(s.Ink, c[3], 0.1)), tower - 0.01, floor - 0.42, 0.006, 0.42);
        g.Rect(Solid(Mix(s.Ink, c[3], 0.1)), tower + 0.024, floor - 0.42, 0.006, 0.42);
        for (var y = floor - 0.42; y < floor - 0.02; y += 0.04) g.Stroke(Solid(Mix(s.Ink, c[3], 0.1)), 0.004, tower - 0.007, y, tower + 0.027, y + 0.04);
        for (var k = 0; k < 3; k++) {
            var ly = floor - 0.4 + k * 0.05;
            g.RoundRect(Solid(Mix(s.Ink, c[3], 0.05)), tower - 0.006, ly, 0.034, 0.03, 0.008);
            g.Circle(Solid(k == 1 ? c[0] : c[2]), tower + 0.011, ly + 0.015, 0.009);
        }
        // Notes drifting up beside the screen.
        Note(g, Math.Min(0.92, f.Right + m + 0.07), f.Y + 0.02, 0.05, -0.15, Mix(paper, c[2], 0.3));
        Note(g, Math.Max(0.06, f.X - m - 0.07), f.Y + 0.06, 0.036, 0.2, Mix(paper, c[1], 0.25));
        // The crowd: a lighter back row and a darker front row with raised hands and phone lights.
        var back = Mix(s.Ink, c[3], 0.55); var front = Mix(s.Ink, c[3], 0.12);
        Crowd(g, r, floor + 0.035, 0.034, back, null, 0.25);
        Crowd(g, r, floor + 0.085, 0.044, front, c[2], 0.35);
        // One spotlight from the stage floor sweeps across the screen's lower-right corner, clear of the code.
        g.Polygon(ScenePaint.Linear(f.Right, floor, f.Right + 0.25, 0, (0, Fade(Mix(c[2], paper, 0.4), 0.55)), (1, Fade(c[2], 0))),
            f.Right - 0.002, floor, f.Right + 0.012, floor, f.Right + 0.33, -0.02, f.Right + 0.2, -0.02);
    }

    private static void Note(QrSceneGeometry g, double x, double y, double size, double tilt, Rgba32 color) {
        var cos = Math.Cos(tilt); var sin = Math.Sin(tilt);
        double X(double u, double v) => x + u * cos - v * sin;
        double Y(double u, double v) => y + u * sin + v * cos;
        g.Oval(ScenePaint.Solid(color), X(0, size), Y(0, size), size * 0.32, size * 0.23, tilt - 0.35);
        g.Stroke(ScenePaint.Solid(color), size * 0.1, X(size * 0.27, size), Y(size * 0.27, size), X(size * 0.27, 0), Y(size * 0.27, 0));
        g.Polygon(ScenePaint.Solid(color), X(size * 0.22, 0), Y(size * 0.22, 0), X(size * 0.62, size * 0.22), Y(size * 0.62, size * 0.22),
            X(size * 0.62, size * 0.42), Y(size * 0.62, size * 0.42), X(size * 0.32, size * 0.2), Y(size * 0.32, size * 0.2));
    }

    private static void Crowd(QrSceneGeometry g, SceneRandom r, double y, double head, Rgba32 color, Rgba32? light, double hands) {
        var shapes = new List<double[]>();
        var x = -0.03;
        while (x < 1.03) {
            var h = head * (0.85 + 0.3 * r.Next()); var lift = (r.Next() - 0.5) * head * 0.6;
            shapes.Add(QrSceneGeometry.Ellipse(x, y + lift, h * 0.5, h * 0.58, 0, 28));
            shapes.Add(QrSceneGeometry.RoundRectangle(x - h * 1.05, y + lift + h * 0.55, h * 2.1, 0.3, h * 0.7, 0));
            if (r.Next() < hands) {
                var side = r.Next() < 0.5 ? -1 : 1;
                // Shoulder, elbow and hand: a bent arm reads as a cheering fan, not a pole.
                var ex = x + side * h * 1.25; var ey = y + lift - h * (0.2 + 0.4 * r.Next());
                var hx = x + side * h * (0.75 + 0.5 * r.Next()); var hy = ey - h * (0.85 + 0.35 * r.Next());
                shapes.AddRange(QrSceneGeometry.StrokeContours(h * 0.36, new[] { x + side * h * 0.75, y + lift + h * 0.75, ex, ey, hx, hy }));
                shapes.Add(QrSceneGeometry.Ellipse(hx, hy - h * 0.12, h * 0.26, h * 0.3, 0, 20));
                if (light is not null && r.Next() < 0.4) g.Circle(ScenePaint.Solid(light.Value, 0.9), hx, hy - h * 0.15, h * 0.16);
            }
            x += h * (1.25 + 0.5 * r.Next());
        }
        g.Fill(ScenePaint.Solid(color), shapes.ToArray());
    }

    // An air bubble under a sunlit surface: the code floats in the bubble, a school of fish curves around
    // it in one direction, and the reef rises from the sand with coral of different kinds and heights.
    private static void Reef(SceneContext s) {
        var g = s.G; var c = s.C; var f = s.Frame; var paper = s.Paper; var r = s.Random;
        var deep = Mix(c[3], s.Ink, 0.45);
        s.Backdrop();
        g.Rect(ScenePaint.Linear(0, 0, 0, 1, (0, Mix(paper, c[1], 0.32)), (0.4, Mix(c[1], c[3], 0.25)), (0.78, c[3]), (1, deep)), 0, 0, 1, 1);
        // Light shafts from the surface.
        foreach (var (top, lean, width) in new[] { (0.12, 0.18, 0.06), (0.38, 0.12, 0.09), (0.7, 0.2, 0.05), (0.9, 0.16, 0.07) })
            g.Polygon(ScenePaint.Linear(0, 0, 0, 0.85, (0, Fade(paper, 0.2)), (1, Fade(paper, 0))), top, -0.02, top + width, -0.02, top + width + lean + 0.1, 0.85, top + lean - 0.04, 0.85);
        // Surface ripple band.
        g.Polygon(Solid(paper, 0.35), Wave(0, 0.035, 0.012, 9, -0.02));

        // The bubble: a paper disc with a soft rim and a highlight that stays outside the code square.
        s.Plate();
        var radius = f.Size * Math.Sqrt(0.5) + 0.034;
        g.Circle(Solid(paper, 0.35), f.CenterX, f.CenterY, radius + 0.016);
        g.Circle(Solid(paper), f.CenterX, f.CenterY, radius);
        var highlight = new List<double>();
        for (var a = 3.55; a <= 4.25; a += 0.05) { highlight.Add(f.CenterX + (radius - 0.016) * Math.Cos(a)); highlight.Add(f.CenterY + (radius - 0.016) * Math.Sin(a)); }
        g.Stroke(Solid(Mix(paper, c[1], 0.45)), 0.009, highlight.ToArray());

        s.Motifs();
        // Sand and rocks.
        g.Polygon(Solid(Mix(paper, c[2], 0.35)), new ScenePath(0, 0.91).Cubic(0.3, 0.87, 0.6, 0.95, 1, 0.9).Line(1, 1).Line(0, 1).ToArray());
        g.Oval(Solid(Mix(deep, c[3], 0.35)), 0.9, 0.94, 0.08, 0.045, -0.1);
        g.Oval(Solid(Mix(deep, c[3], 0.15)), 0.04, 0.95, 0.07, 0.04);
        // Staghorn coral on the left, kelp and a sea fan on the right; the caption sits on the sand between them.
        Branch(g, 0.08, 0.96, -Math.PI / 2 - 0.08, 0.12, 0.026, 4, c[0], r);
        Branch(g, 0.17, 0.97, -Math.PI / 2 - 0.3, 0.075, 0.018, 3, Mix(c[0], c[2], 0.5), r);
        Fan(g, 0.93, 0.955, 0.13, Mix(c[2], c[0], 0.25));
        foreach (var (kx, height, phase) in new[] { (0.8, 0.15, 0.0), (0.83, 0.1, 1.3) })
            Kelp(g, kx, 0.97, height, phase, Mix(c[1], s.Ink, 0.35));
        // A loose school around the bubble's right side: varied sizes, headings and depth. Smaller, farther fish
        // are paler; one crosses the bubble's rim, well clear of the code.
        var school = new List<(double Size, double X, double Y, double Heading)>();
        for (var i = 0; i < 11; i++) {
            var a = -1.0 + 2.0 * i / 10 + r.Range(-0.08, 0.08);
            var d = radius + 0.035 + r.Range(0, 0.09);
            school.Add((r.Range(0.014, 0.036), f.CenterX + d * Math.Cos(a), f.CenterY + d * Math.Sin(a), a - Math.PI / 2 + r.Range(-0.35, 0.35)));
        }
        school.Add((0.032, f.CenterX + (radius + 0.004) * Math.Cos(0.3), f.CenterY + (radius + 0.004) * Math.Sin(0.3), 0.3 - Math.PI / 2 - 0.2));
        school.Sort((a, b) => a.Size.CompareTo(b.Size));
        var tint = 0;
        foreach (var (size, x, y, heading) in school) {
            var depth = 1 - (size - 0.014) / 0.022;
            var body = (tint++ % 3 == 0 ? c[0] : c[2]);
            Fish(g, x, y, size, heading, Mix(body, Mix(c[1], c[3], 0.4), 0.45 * depth), s.Ink);
        }
        Fish(g, 0.11, 0.7, 0.05, -0.15, c[2], s.Ink, true);
        for (var i = 0; i < 7; i++) {
            var bx = 0.07 + 0.03 * Math.Sin(i * 1.9); var by = 0.6 - i * 0.07;
            g.Ring(Solid(paper, 0.7), bx, by, 0.006 + 0.0025 * (i % 3), 0.0028);
        }
    }
    private static double[] Wave(double x0, double y, double amplitude, double frequency, double phase) {
        var path = new List<double>();
        for (var i = 0; i <= 60; i++) { var x = i / 60.0; path.Add(x); path.Add(y + amplitude * Math.Sin(x * frequency + phase)); }
        path.Add(1); path.Add(-0.01); path.Add(0); path.Add(-0.01);
        return path.ToArray();
    }

    private static void Branch(QrSceneGeometry g, double x, double y, double angle, double length, double width, int depth, Rgba32 color, SceneRandom r) {
        var parts = new List<double[]>();
        Grow(x, y, angle, length, width, depth);
        g.Fill(ScenePaint.Solid(color), parts.ToArray());
        void Grow(double bx, double by, double a, double l, double w, int d) {
            var ex = bx + Math.Cos(a) * l; var ey = by + Math.Sin(a) * l;
            parts.AddRange(QrSceneGeometry.StrokeContours(w, new[] { bx, by, ex, ey }));
            if (d == 0) return;
            Grow(ex, ey, a - 0.42 - 0.1 * r.Next(), l * 0.78, w * 0.8, d - 1);
            Grow(ex, ey, a + 0.38 + 0.1 * r.Next(), l * 0.72, w * 0.8, d - 1);
        }
    }

    private static void Fan(QrSceneGeometry g, double x, double y, double radius, Rgba32 color) {
        var blade = new List<double> { x, y };
        for (var a = -Math.PI / 2 - 0.85; a <= -Math.PI / 2 + 0.86; a += 0.05) { blade.Add(x + Math.Cos(a) * radius * 0.98); blade.Add(y + Math.Sin(a) * radius * 0.98); }
        g.Fill(ScenePaint.Solid(color, 0.35), blade.ToArray());
        var parts = new List<double[]>();
        for (var i = 0; i <= 10; i++) {
            var a = -Math.PI / 2 - 0.85 + i * 0.17;
            var mx = x + Math.Cos(a) * radius * 0.5 + 0.01 * Math.Sin(i); var my = y + Math.Sin(a) * radius * 0.5;
            parts.AddRange(QrSceneGeometry.StrokeContours(0.006, new[] { x, y, mx, my, x + Math.Cos(a) * radius, y + Math.Sin(a) * radius }));
        }
        for (var k = 1; k <= 3; k++) {
            var arc = new List<double>();
            for (var a = -Math.PI / 2 - 0.85; a <= -Math.PI / 2 + 0.86; a += 0.1) { arc.Add(x + Math.Cos(a) * radius * k / 3.2); arc.Add(y + Math.Sin(a) * radius * k / 3.2); }
            parts.AddRange(QrSceneGeometry.StrokeContours(0.004, arc.ToArray()));
        }
        g.Fill(ScenePaint.Solid(color), parts.ToArray());
    }

    private static void Kelp(QrSceneGeometry g, double x, double y, double height, double phase, Rgba32 color) {
        var points = new List<double>();
        for (var i = 0; i <= 20; i++) {
            var t = i / 20.0; points.Add(x + 0.018 * Math.Sin(t * 7 + phase) * t); points.Add(y - t * height);
        }
        g.Stroke(ScenePaint.Solid(color), 0.011, points.ToArray());
        for (var i = 4; i <= 18; i += 4) {
            var t = i / 20.0; var px = x + 0.018 * Math.Sin(t * 7 + phase) * t; var py = y - t * height;
            g.Fill(ScenePaint.Solid(color), Leaf(px, py, 0.05 * (1.1 - t), 0.018, -Math.PI / 2 + (i % 8 == 0 ? 0.7 : -0.7)));
        }
    }

    private static void Fish(QrSceneGeometry g, double x, double y, double size, double heading, Rgba32 color, Rgba32 ink, bool stripes = false) {
        var cos = Math.Cos(heading); var sin = Math.Sin(heading);
        double X(double u, double v) => x + u * cos - v * sin;
        double Y(double u, double v) => y + u * sin + v * cos;
        g.Polygon(ScenePaint.Solid(QrSceneGeometry.Mix(color, ink, 0.15)), X(-size * 0.8, 0), Y(-size * 0.8, 0), X(-size * 1.35, size * 0.42), Y(-size * 1.35, size * 0.42),
            X(-size * 1.25, 0), Y(-size * 1.25, 0), X(-size * 1.35, -size * 0.42), Y(-size * 1.35, -size * 0.42));
        g.Oval(ScenePaint.Solid(color), x, y, size, size * 0.52, heading);
        if (stripes) {
            foreach (var u in new[] { -0.35, 0.15 })
                g.Fill(ScenePaint.Solid(QrSceneGeometry.Mix(color, new Rgba32(255, 255, 255), 0.75)), QrSceneGeometry.Ellipse(X(size * u, 0), Y(size * u, 0), size * 0.09, size * 0.46, heading, 24));
        }
        g.Circle(ScenePaint.Solid(ink), X(size * 0.55, -size * 0.12), Y(size * 0.55, -size * 0.12), size * 0.09);
    }

    // A pale moon carrying the code, ringed and orbited, in deep space with nebula light and a rocket.
    private static void Cosmos(SceneContext s) {
        var g = s.G; var c = s.C; var f = s.Frame; var paper = s.Paper; var r = s.Random;
        var space = Mix(s.Ink, Rgb(0x080A1E), 0.5);
        s.Backdrop();
        g.Rect(ScenePaint.RadialGradient(f.CenterX, f.CenterY, 0.85, 1, (0, Mix(space, c[3], 0.6)), (0.55, Mix(space, c[3], 0.22)), (1, space)), 0, 0, 1, 1);
        g.Circle(ScenePaint.RadialGradient(0.86, 0.16, 0.3, 0.5, (0, Mix(c[0], c[3], 0.3)), (1, Mix(space, c[3], 0.25))), 0.86, 0.16, 0.3);
        g.Circle(ScenePaint.RadialGradient(0.1, 0.9, 0.28, 0.4, (0, Mix(c[1], c[3], 0.4)), (1, Mix(space, c[3], 0.25))), 0.1, 0.9, 0.28);
        for (var i = 0; i < 130; i++) g.Circle(Solid(i % 7 == 0 ? c[2] : paper, 0.3 + 0.6 * r.Next()), r.Next(), r.Next(), 0.001 + 0.0026 * r.Next() * r.Next());
        foreach (var (sx, sy, size) in new[] { (0.2, 0.08, 0.016), (0.93, 0.36, 0.013), (0.07, 0.48, 0.011), (0.86, 0.06, 0.01) }) Sparkle(g, sx, sy, size, Mix(paper, c[2], 0.25));
        var radius = f.Size * Math.Sqrt(0.5) + 0.034;
        // A large planet's horizon along the bottom anchors the caption.
        g.Circle(Solid(Mix(space, c[3], 0.55)), 0.5, 2.1, 1.18);
        g.Ring(Solid(Mix(c[1], paper, 0.3), 0.7), 0.5, 2.1, 1.18, 0.004);

        // The moon: paper face, faint craters and a shaded limb, all outside the code square.
        s.Plate();
        g.Circle(ScenePaint.RadialGradient(f.CenterX, f.CenterY, radius + 0.07, 1, (0.85, Fade(Mix(c[1], paper, 0.4), 0.55)), (1, Fade(c[1], 0))), f.CenterX, f.CenterY, radius + 0.07);
        g.Circle(Solid(paper), f.CenterX, f.CenterY, radius);
        var half = f.Size / 2;
        for (var k = 0; k < 9; k++) {
            var a = k * Math.PI / 2 * 0.999 + r.Range(-0.45, 0.45);
            var size = r.Range(0.009, 0.02);
            var d = r.Range(half + size + 0.012, radius - size - 0.008);
            var cx = f.CenterX + d * Math.Cos(a); var cy = f.CenterY + d * Math.Sin(a);
            // Keep each crater clear of the code square.
            if (Math.Abs(cx - f.CenterX) < half + size + 0.01 && Math.Abs(cy - f.CenterY) < half + size + 0.01) continue;
            g.Circle(Solid(Mix(paper, c[3], 0.1)), cx, cy, size);
            g.Circle(Solid(Mix(paper, c[3], 0.04)), cx + size * 0.22, cy + size * 0.22, size * 0.72);
        }
        var limb = new List<double>();
        for (var a = -0.35; a <= 2.0; a += 0.04) { limb.Add(f.CenterX + radius * Math.Cos(a)); limb.Add(f.CenterY + radius * Math.Sin(a)); }
        for (var a = 2.0; a >= -0.35; a -= 0.04) {
            var depth = 0.026 * Math.Sin(Math.PI * (a + 0.35) / 2.35);
            limb.Add(f.CenterX + (radius - depth) * Math.Cos(a)); limb.Add(f.CenterY + (radius - depth) * Math.Sin(a));
        }
        g.Polygon(Solid(Mix(paper, c[3], 0.22)), limb.ToArray());

        s.Motifs();
        // One orbit carries every prop: a dotted path that crosses the moon's face above and below the code,
        // two small planets and a rocket climbing over the moon's rim.
        var orbitX = radius + 0.08; var orbitY = radius - 0.01;
        double OrbitX(double a) => f.CenterX + orbitX * Math.Cos(a);
        double OrbitY(double a) => f.CenterY + orbitY * Math.Sin(a);
        for (var a = 0.0; a < Math.PI * 2; a += 0.075) {
            var px = OrbitX(a); var py = OrbitY(a);
            if (Math.Abs(px - f.CenterX) < half + 0.01 && Math.Abs(py - f.CenterY) < half + 0.01) continue;
            g.Circle(Solid(Mix(paper, c[1], 0.3), 0.75), px, py, 0.0034);
        }
        var pink = 3.6; var px1 = OrbitX(pink); var py1 = OrbitY(pink);
        g.Circle(Solid(c[0]), px1, py1, 0.048);
        g.Polygon(Solid(Mix(c[0], c[2], 0.5)), new ScenePath(px1 - 0.045, py1 - 0.008).Quad(px1, py1 - 0.03, px1 + 0.047, py1).Line(px1 + 0.045, py1 + 0.014).Quad(px1, py1 - 0.012, px1 - 0.046, py1 + 0.008).ToArray());
        g.FillWithHoles(Solid(c[2]), QrSceneGeometry.Ellipse(px1, py1, 0.083, 0.02, -0.35), QrSceneGeometry.Ellipse(px1, py1, 0.066, 0.013, -0.35));
        var gold = 2.55; var px2 = OrbitX(gold); var py2 = OrbitY(gold);
        g.Circle(Solid(Mix(c[2], c[0], 0.3)), px2, py2, 0.03);
        g.Circle(Solid(Mix(c[2], paper, 0.5)), px2 - 0.008, py2 - 0.008, 0.009);
        var climb = 1.05;
        Rocket(g, OrbitX(climb), OrbitY(climb), 0.11, Math.Atan2(-orbitY * Math.Cos(climb), orbitX * Math.Sin(climb)), paper, c, s.Ink);
    }
    private static void Sparkle(QrSceneGeometry g, double x, double y, double size, Rgba32 color) {
        g.Polygon(ScenePaint.Solid(color), x, y - size, x + size * 0.18, y - size * 0.18, x + size, y, x + size * 0.18, y + size * 0.18,
            x, y + size, x - size * 0.18, y + size * 0.18, x - size, y, x - size * 0.18, y - size * 0.18);
    }

    private static void Rocket(QrSceneGeometry g, double x, double y, double length, double heading, Rgba32 paper, Rgba32[] c, Rgba32 ink) {
        var cos = Math.Cos(heading); var sin = Math.Sin(heading);
        double[] P(params double[] uv) {
            var points = new double[uv.Length];
            for (var i = 0; i < uv.Length; i += 2) {
                var u = uv[i] * length; var v = uv[i + 1] * length;
                points[i] = x + u * cos - v * sin; points[i + 1] = y + u * sin + v * cos;
            }
            return points;
        }
        g.Fill(ScenePaint.Linear(x, y, x - cos * length * 0.8, y - sin * length * 0.8, (0, c[2]), (1, QrSceneGeometry.Mix(c[0], c[2], 0.2))),
            P(-0.28, -0.09, -0.75, 0, -0.28, 0.09));
        g.Fill(ScenePaint.Solid(c[0]), P(-0.2, -0.13, -0.34, -0.26, -0.34, -0.1, -0.12, -0.08));
        g.Fill(ScenePaint.Solid(c[0]), P(-0.2, 0.13, -0.34, 0.26, -0.34, 0.1, -0.12, 0.08));
        var body = new List<double>();
        for (var i = 0; i <= 24; i++) { var t = i / 24.0; body.Add(-0.3 + 0.8 * t); body.Add(-0.13 * Math.Sqrt(Math.Sin(Math.PI * (0.08 + 0.92 * t)))); }
        for (var i = 24; i >= 0; i--) { var t = i / 24.0; body.Add(-0.3 + 0.8 * t); body.Add(0.13 * Math.Sqrt(Math.Sin(Math.PI * (0.08 + 0.92 * t)))); }
        g.Fill(ScenePaint.Solid(paper), P(body.ToArray()));
        g.Fill(ScenePaint.Solid(QrSceneGeometry.Mix(paper, c[3], 0.25)), P(-0.3, 0.02, 0.4, 0.02, 0.35, 0.09, -0.3, 0.12));
        g.Fill(ScenePaint.Solid(c[0]), P(0.33, -0.1, 0.5, 0, 0.33, 0.1));
        g.Fill(ScenePaint.Solid(ink), QrSceneGeometry.Ellipse(x + 0.12 * length * cos, y + 0.12 * length * sin, 0.062 * length, 0.062 * length, 0, 32));
        g.Fill(ScenePaint.Solid(c[1]), QrSceneGeometry.Ellipse(x + 0.12 * length * cos, y + 0.12 * length * sin, 0.044 * length, 0.044 * length, 0, 32));
    }

    // An arcade screen on one pixel grid: a square, tile-aligned card holds the code, PRESS START sits below it,
    // a slime peeks over the card's edge and a chase runs along the bottom. Tiles, sprites, card and type share cells.
    private static void Arcade(SceneContext s) {
        var g = s.G; var c = s.C; var f = s.Frame; var paper = s.Paper; var r = s.Random;
        var size = (double)s.Options.Size;
        // 80 cells across, each a whole number of device pixels, centred on the canvas.
        var unit = Math.Max(1, Math.Floor(size / 80)); var origin = Math.Floor((size - 80 * unit) / 2);
        double U(double k) => (origin + k * unit) / size;
        double Cell(double value) => (value * size - origin) / unit;
        var night = Mix(s.Ink, Rgb(0x14123A), 0.5);
        s.Backdrop();
        g.Rect(Solid(night), 0, 0, 1, 1);
        var tiles = new List<double[]>();
        for (var ty = 0; ty < 10; ty++) for (var tx = 0; tx < 10; tx++) if ((tx + ty) % 2 == 0) tiles.Add(Box(U(tx * 8), U(ty * 8), U(tx * 8 + 8), U(ty * 8 + 8)));
        g.Fill(Solid(Mix(night, c[3], 0.22)), tiles.ToArray());
        var stars = new List<double[]>();
        for (var i = 0; i < 40; i++) { var sx = Math.Floor(r.Next() * 79); var sy = Math.Floor(r.Next() * 66); stars.Add(Box(U(sx), U(sy), U(sx + 1), U(sy + 1))); }
        g.Fill(Solid(Mix(paper, c[3], 0.25), 0.7), stars.ToArray());

        // The smallest square of whole tiles that holds the code plus a one-cell border, centred on the code.
        s.Plate();
        double x0 = Cell(f.X), x1 = Cell(f.Right), y0 = Cell(f.Y), y1 = Cell(f.Bottom);
        var span = Math.Ceiling((Math.Max(x1 - x0, y1 - y0) + 2) / 8 - 1e-9) * 8;
        double left = 0, top = 0;
        for (var attempt = 0; attempt < 3; attempt++, span += 8) {
            left = Math.Round(((x0 + x1) / 2 - span / 2) / 8) * 8; top = Math.Round(((y0 + y1) / 2 - span / 2) / 8) * 8;
            if (left + 1 <= x0 + 1e-6 && left + span - 1 >= x1 - 1e-6 && top + 1 <= y0 + 1e-6 && top + span - 1 >= y1 - 1e-6) break;
        }
        var right = left + span; var bottom = top + span;
        g.Fill(Solid(Mix(night, c[3], 0.65)), Stepped(U, left + 1, top + 1, right + 1, bottom + 1, 1));
        g.Fill(Solid(c[1]), Stepped(U, left, top, right, bottom, 1));
        g.Fill(Solid(paper), Stepped(U, left + 1, top + 1, right - 1, bottom - 1, 1));

        s.Motifs();
        Sprite(g, U, 4, 3, Invader, c[1]);
        Sprite(g, U, 67, 3, Saucer, c[2]);
        Sprite(g, U, 5, top + 20, Heart, c[0]);
        // The slime overlaps only the card's border cell, never the paper around the code.
        Sprite(g, U, right - 1, top + 14, Slime, c[0]);
        // The chase: an invader and a slime after the runner, who heads for a trail of coins.
        Sprite(g, U, 6, 70, Invader2, c[3]);
        Sprite(g, U, 22, 69, Slime, Mix(c[0], c[3], 0.35));
        Sprite(g, U, 44, 70, Runner, c[1]);
        foreach (var coin in new[] { 56, 63, 70 }) Sprite(g, U, coin, 72, Coin, c[2]);
        var bricks = new List<double[]>();
        for (var bx = 0; bx < 80; bx += 4) bricks.Add(Box(U(bx), U(78), U(bx + 3), U(79)));
        g.Fill(Solid(Mix(c[3], night, 0.2)), bricks.ToArray());
    }
    private static Rgba32 Fade(Rgba32 color, double alpha) => new(color.R, color.G, color.B, (byte)Math.Round(255 * alpha));

    private static double[] Box(double x0, double y0, double x1, double y1) => new[] { x0, y0, x1, y0, x1, y1, x0, y1 };

    // A rectangle with stepped pixel corners, in cell coordinates.
    private static double[] Stepped(Func<double, double> u, double x0, double y0, double x1, double y1, double step) => new[] {
        u(x0 + step), u(y0), u(x1 - step), u(y0), u(x1 - step), u(y0 + step), u(x1), u(y0 + step),
        u(x1), u(y1 - step), u(x1 - step), u(y1 - step), u(x1 - step), u(y1), u(x0 + step), u(y1),
        u(x0 + step), u(y1 - step), u(x0), u(y1 - step), u(x0), u(y0 + step), u(x0 + step), u(y0 + step)
    };

    private static void Sprite(QrSceneGeometry g, Func<double, double> u, double x, double y, string[] rows, Rgba32 color) {
        var cells = new List<double[]>(); var ink = new List<double[]>();
        for (var row = 0; row < rows.Length; row++) for (var column = 0; column < rows[row].Length; column++) {
            var cell = rows[row][column];
            if (cell == '#') cells.Add(Box(u(x + column), u(y + row), u(x + column + 1), u(y + row + 1)));
            else if (cell == 'o') ink.Add(Box(u(x + column), u(y + row), u(x + column + 1), u(y + row + 1)));
        }
        if (cells.Count > 0) g.Fill(ScenePaint.Solid(color), cells.ToArray());
        if (ink.Count > 0) g.Fill(ScenePaint.Solid(new Rgba32(255, 255, 255)), ink.ToArray());
    }

    private static readonly string[] Invader = { "..#.....#..", "...#...#...", "..#######..", ".##.###.##.", "###########", "#.#######.#", "#.#.....#.#", "...##.##..." };
    private static readonly string[] Invader2 = { "...##...", "..####..", ".######.", "##.##.##", "########", "..#..#..", ".#.##.#.", "#.#..#.#" };
    private static readonly string[] Slime = { "...####...", ".########.", "##########", "#oo####oo#", "#oo####oo#", "##########", "####..####", "##########", "#.##.##.##" };
    private static readonly string[] Heart = { ".##.##.", "#######", "#######", ".#####.", "..###..", "...#..." };
    private static readonly string[] Saucer = { "...####...", ".##o##o##.", "##########", ".#.#..#.#." };
    private static readonly string[] Coin = { ".###.", "##o##", "##o##", "##o##", ".###." };
    private static readonly string[] Runner = { "..###..", "..#o#..", "..###..", ".#####.", "#.###.#", "..#.#..", ".#...#." };
    private static readonly string[] Ship = { "....#....", "...###...", "...#o#...", ".#######.", "#########", "##.###.##", "#...#...#" };
}
