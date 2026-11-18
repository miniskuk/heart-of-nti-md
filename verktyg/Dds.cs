// Läser och skriver DDS-bilder (DXT1/DXT3/DXT5/okomprimerad) utan extra program.
// Används av verktyg/md_bygg.ps1 via Add-Type.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class Dds
{
    public static int[] Size(string path)
    {
        using (var fs = File.OpenRead(path))
        {
            var h = new byte[20];
            fs.Read(h, 0, 20);
            if (h[0] == 'D' && h[1] == 'D' && h[2] == 'S')
                return new[] { BitConverter.ToInt32(h, 16), BitConverter.ToInt32(h, 12) }; // bredd, höjd
        }
        // Vissa filer i MD är egentligen JPG/PNG med .dds-namn: ersätt dem med ett vanligt stort porträtt.
        return new[] { 156, 210 };
    }

    public static Bitmap Load(string path)
    {
        if (!path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
        {
            using (var img = Image.FromFile(path)) return new Bitmap(img);
        }
        var d = File.ReadAllBytes(path);
        int h = BitConverter.ToInt32(d, 12), w = BitConverter.ToInt32(d, 16);
        int pfFlags = BitConverter.ToInt32(d, 80);
        string four = System.Text.Encoding.ASCII.GetString(d, 84, 4);
        int bpp = BitConverter.ToInt32(d, 88);
        var px = new int[w * h];
        int off = 128;
        if ((pfFlags & 4) != 0)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            int bs = four == "DXT1" ? 8 : 16;
            var block = new int[16];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++, off += bs)
                {
                    DecodeBlock(d, off, four, block);
                    for (int i = 0; i < 16; i++)
                    {
                        int x = bx * 4 + (i & 3), y = by * 4 + (i >> 2);
                        if (x < w && y < h) px[y * w + x] = block[i];
                    }
                }
        }
        else
        {
            uint rm = BitConverter.ToUInt32(d, 92), gm = BitConverter.ToUInt32(d, 96), bm = BitConverter.ToUInt32(d, 100), am = BitConverter.ToUInt32(d, 104);
            int step = bpp / 8;
            for (int i = 0; i < w * h; i++, off += step)
            {
                uint v = 0;
                for (int k = 0; k < step; k++) v |= (uint)d[off + k] << (8 * k);
                int a = am == 0 ? 255 : (int)Chan(v, am);
                px[i] = (a << 24) | ((int)Chan(v, rm) << 16) | ((int)Chan(v, gm) << 8) | (int)Chan(v, bm);
            }
        }
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(px, 0, bd.Scan0, px.Length);
        bmp.UnlockBits(bd);
        return bmp;
    }

    static uint Chan(uint v, uint mask)
    {
        if (mask == 0) return 0;
        int shift = 0; while (((mask >> shift) & 1) == 0) shift++;
        uint m = mask >> shift; uint c = (v & mask) >> shift;
        return m == 255 ? c : c * 255 / m;
    }

    static int C565(int c) { int r = (c >> 11) & 31, g = (c >> 5) & 63, b = c & 31; return ((r * 255 / 31) << 16) | ((g * 255 / 63) << 8) | (b * 255 / 31); }
    static int Mix(int a, int b, int wa, int wb, int div)
    {
        int r = (((a >> 16) & 255) * wa + ((b >> 16) & 255) * wb) / div;
        int g = (((a >> 8) & 255) * wa + ((b >> 8) & 255) * wb) / div;
        int bl = ((a & 255) * wa + (b & 255) * wb) / div;
        return (r << 16) | (g << 8) | bl;
    }

    static void DecodeBlock(byte[] d, int off, string four, int[] outPx)
    {
        var alpha = new int[16];
        int co = off;
        if (four == "DXT5")
        {
            int a0 = d[off], a1 = d[off + 1];
            var pal = new int[8]; pal[0] = a0; pal[1] = a1;
            if (a0 > a1) for (int i = 1; i < 7; i++) pal[i + 1] = ((7 - i) * a0 + i * a1) / 7;
            else { for (int i = 1; i < 5; i++) pal[i + 1] = ((5 - i) * a0 + i * a1) / 5; pal[6] = 0; pal[7] = 255; }
            ulong bits = 0; for (int k = 0; k < 6; k++) bits |= (ulong)d[off + 2 + k] << (8 * k);
            for (int i = 0; i < 16; i++) alpha[i] = pal[(int)((bits >> (3 * i)) & 7)];
            co = off + 8;
        }
        else if (four == "DXT3")
        {
            for (int i = 0; i < 16; i++) { int n = (d[off + i / 2] >> (4 * (i & 1))) & 15; alpha[i] = n * 17; }
            co = off + 8;
        }
        else for (int i = 0; i < 16; i++) alpha[i] = 255;
        int c0 = d[co] | (d[co + 1] << 8), c1 = d[co + 2] | (d[co + 3] << 8);
        var cp = new int[4]; cp[0] = C565(c0); cp[1] = C565(c1);
        bool four4 = four != "DXT1" || c0 > c1;
        if (four4) { cp[2] = Mix(cp[0], cp[1], 2, 1, 3); cp[3] = Mix(cp[0], cp[1], 1, 2, 3); }
        else { cp[2] = Mix(cp[0], cp[1], 1, 1, 2); cp[3] = 0; }
        uint idx = BitConverter.ToUInt32(d, co + 4);
        for (int i = 0; i < 16; i++)
        {
            int k = (int)((idx >> (2 * i)) & 3);
            int a = (four == "DXT1" && !four4 && k == 3) ? 0 : alpha[i];
            outPx[i] = (a << 24) | cp[k];
        }
    }

    // Skalar så att bilden täcker w x h (som ImageMagick "-resize WxH^ -extent"), centrerat.
    public static Bitmap Cover(Bitmap src, int w, int h)
    {
        double s = Math.Max((double)w / src.Width, (double)h / src.Height);
        double sw = w / s, sh = h / s;
        var r = new RectangleF((float)((src.Width - sw) / 2), (float)((src.Height - sh) / 2), (float)sw, (float)sh);
        var dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(dst))
        using (var ia = new ImageAttributes())
        {
            ia.SetWrapMode(WrapMode.TileFlipXY);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(src, new Rectangle(0, 0, w, h), r.X, r.Y, r.Width, r.Height, GraphicsUnit.Pixel, ia);
        }
        return dst;
    }

    public static Bitmap Crop(Bitmap src, int x, int y, int w, int h)
    {
        var r = Rectangle.Intersect(new Rectangle(x, y, w, h), new Rectangle(0, 0, src.Width, src.Height));
        return src.Clone(r, PixelFormat.Format32bppArgb);
    }

    // Skriver DXT5 utan mipmaps (samma som gamla modden).
    public static void SaveDxt5(Bitmap bmp, string path)
    {
        int w = bmp.Width, h = bmp.Height;
        var px = new int[w * h];
        var bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, px.Length);
        bmp.UnlockBits(bd);
        int bw = (w + 3) / 4, bh = (h + 3) / 4;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var fs = File.Create(path))
        using (var o = new BinaryWriter(fs))
        {
            o.Write(0x20534444); o.Write(124); o.Write(0x1 | 0x2 | 0x4 | 0x1000 | 0x80000);
            o.Write(h); o.Write(w); o.Write(bw * bh * 16); o.Write(0); o.Write(0);
            for (int i = 0; i < 11; i++) o.Write(0);
            o.Write(32); o.Write(4); o.Write(System.Text.Encoding.ASCII.GetBytes("DXT5"));
            for (int i = 0; i < 5; i++) o.Write(0);
            o.Write(0x1000); for (int i = 0; i < 4; i++) o.Write(0);
            var blk = new int[16];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        int x = Math.Min(bx * 4 + (i & 3), w - 1), y = Math.Min(by * 4 + (i >> 2), h - 1);
                        blk[i] = px[y * w + x];
                    }
                    EncodeBlock(blk, o);
                }
        }
    }

    static int To565(int r, int g, int b) { return ((r * 31 + 127) / 255 << 11) | ((g * 63 + 127) / 255 << 5) | ((b * 31 + 127) / 255); }

    static void EncodeBlock(int[] p, BinaryWriter o)
    {
        // Alfa
        int amin = 255, amax = 0;
        foreach (var c in p) { int a = (c >> 24) & 255; amin = Math.Min(amin, a); amax = Math.Max(amax, a); }
        o.Write((byte)amax); o.Write((byte)amin);
        ulong abits = 0;
        var apal = new int[8]; apal[0] = amax; apal[1] = amin;
        for (int i = 1; i < 7; i++) apal[i + 1] = ((7 - i) * amax + i * amin) / 7;
        for (int i = 0; i < 16; i++)
        {
            int a = (p[i] >> 24) & 255, best = 0, bd = 999;
            if (amax != amin) for (int k = 0; k < 8; k++) { int dd = Math.Abs(apal[k] - a); if (dd < bd) { bd = dd; best = k; } }
            abits |= (ulong)best << (3 * i);
        }
        for (int k = 0; k < 6; k++) o.Write((byte)(abits >> (8 * k)));
        // Färg: ändpunkter längs största variationen (enkel principalaxel)
        double mr = 0, mg = 0, mb = 0;
        foreach (var c in p) { mr += (c >> 16) & 255; mg += (c >> 8) & 255; mb += c & 255; }
        mr /= 16; mg /= 16; mb /= 16;
        double xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
        foreach (var c in p)
        {
            double r = ((c >> 16) & 255) - mr, g = ((c >> 8) & 255) - mg, b = (c & 255) - mb;
            xx += r * r; xy += r * g; xz += r * b; yy += g * g; yz += g * b; zz += b * b;
        }
        double vr = 1, vg = 1, vb = 1;
        for (int it = 0; it < 8; it++)
        {
            double nr = xx * vr + xy * vg + xz * vb, ng = xy * vr + yy * vg + yz * vb, nb = xz * vr + yz * vg + zz * vb;
            double len = Math.Sqrt(nr * nr + ng * ng + nb * nb); if (len < 1e-9) break;
            vr = nr / len; vg = ng / len; vb = nb / len;
        }
        double tmin = double.MaxValue, tmax = double.MinValue;
        foreach (var c in p)
        {
            double t = (((c >> 16) & 255) - mr) * vr + (((c >> 8) & 255) - mg) * vg + ((c & 255) - mb) * vb;
            tmin = Math.Min(tmin, t); tmax = Math.Max(tmax, t);
        }
        Func<double, int> cl = v => Math.Max(0, Math.Min(255, (int)Math.Round(v)));
        int c0 = To565(cl(mr + vr * tmax), cl(mg + vg * tmax), cl(mb + vb * tmax));
        int c1 = To565(cl(mr + vr * tmin), cl(mg + vg * tmin), cl(mb + vb * tmin));
        if (c0 < c1) { int t = c0; c0 = c1; c1 = t; }
        var cp = new int[4]; cp[0] = C565(c0); cp[1] = C565(c1);
        cp[2] = Mix(cp[0], cp[1], 2, 1, 3); cp[3] = Mix(cp[0], cp[1], 1, 2, 3);
        uint idx = 0;
        if (c0 != c1)
            for (int i = 0; i < 16; i++)
            {
                int best = 0, bd = int.MaxValue;
                for (int k = 0; k < 4; k++)
                {
                    int dr = ((p[i] >> 16) & 255) - ((cp[k] >> 16) & 255), dg = ((p[i] >> 8) & 255) - ((cp[k] >> 8) & 255), db = (p[i] & 255) - (cp[k] & 255);
                    int dd = dr * dr + dg * dg + db * db; if (dd < bd) { bd = dd; best = k; }
                }
                idx |= (uint)best << (2 * i);
            }
        o.Write((ushort)c0); o.Write((ushort)c1); o.Write(idx);
    }

    // Enkel bild-"fingeravtryck" (8x8 medelvärdeshash) för att se vilka bilder som föreställer samma sak.
    public static ulong Hash(Bitmap b)
    {
        using (var s = Cover(b, 8, 8))
        {
            var v = new int[64]; double m = 0;
            for (int i = 0; i < 64; i++) { var c = s.GetPixel(i & 7, i >> 3); v[i] = c.R + c.G + c.B; m += v[i]; }
            m /= 64; ulong h = 0;
            for (int i = 0; i < 64; i++) if (v[i] > m) h |= 1UL << i;
            return h;
        }
    }
    public static int Diff(ulong a, ulong b) { ulong x = a ^ b; int n = 0; while (x != 0) { x &= x - 1; n++; } return n; }

    // Behåller bara bilderna i den största gruppen av likadana bilder (den person landet egentligen fått).
    public static Bitmap[] Majority(Bitmap[] imgs)
    {
        var hs = new ulong[imgs.Length];
        for (int i = 0; i < imgs.Length; i++) hs[i] = Hash(imgs[i]);
        int best = 0, bestN = -1;
        for (int i = 0; i < imgs.Length; i++)
        {
            int n = 0; for (int j = 0; j < imgs.Length; j++) if (Diff(hs[i], hs[j]) <= 12) n++;
            if (n > bestN) { bestN = n; best = i; }
        }
        var res = new System.Collections.Generic.List<Bitmap>();
        for (int j = 0; j < imgs.Length; j++) if (Diff(hs[best], hs[j]) <= 12) res.Add(imgs[j]);
        return res.ToArray();
    }

    public static void SavePng(Bitmap b, string path) { b.Save(path, ImageFormat.Png); }
}
