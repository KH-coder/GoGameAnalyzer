using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SkiaSharp;

public class SgfAnalyzer
{
    private class Stone
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsBlack { get; set; }

        public Stone(int x, int y, bool isBlack)
        {
            X = x;
            Y = y;
            IsBlack = isBlack;
        }
    }

    private class BoardRegion
    {
        public int StartX { get; set; }  // 起始列
        public int EndX { get; set; }    // 結束列
        public int StartY { get; set; }  // 起始行
        public int EndY { get; set; }    // 結束行
        public string Name { get; set; }  // 區域名稱

        public BoardRegion(int startX, int endX, int startY, int endY, string name)
        {
            StartX = startX;
            EndX = endX;
            StartY = startY;
            EndY = endY;
            Name = name;
        }
    }

    private const int BOARD_SIZE = 19;
    private const int CELL_SIZE = 30;
    private const int MARGIN = 40;
    private const int STONE_SIZE = 24;
    private readonly List<Stone> stones = new List<Stone>();

    public void ParseSgf(string sgfContent)
    {
        stones.Clear();
        
        // 提取AB和AW部分
        Match blackSection = Regex.Match(sgfContent, @"AB\[(.*?)\](?=\w+\[|$)");
        Match whiteSection = Regex.Match(sgfContent, @"AW\[(.*?)\](?=\w+\[|$)");

        if (blackSection.Success)
        {
            string blackStones = blackSection.Groups[1].Value;
            MatchCollection blackMatches = Regex.Matches(blackStones, @"([a-s][a-s])(?:\]|$)");
            foreach (Match pos in blackMatches)
            {
                string position = pos.Groups[1].Value;
                int x = position[0] - 'a';
                int y = position[1] - 'a';
                stones.Add(new Stone(x, y, true));
            }
        }

        if (whiteSection.Success)
        {
            string whiteStones = whiteSection.Groups[1].Value;
            MatchCollection whiteMatches = Regex.Matches(whiteStones, @"([a-s][a-s])(?:\]|$)");
            foreach (Match pos in whiteMatches)
            {
                string position = pos.Groups[1].Value;
                int x = position[0] - 'a';
                int y = position[1] - 'a';
                stones.Add(new Stone(x, y, false));
            }
        }
    }
    public void GenerateRegionImages()
    {
        // 定義四個區域
        var regions = new List<BoardRegion>
        {
            new BoardRegion(9, 18, 0, 9, "region1"),   // 垂直(ja-jj) 水平(jj-js)
            new BoardRegion(9, 18, 9, 18, "region2"),  // 垂直(js-jj) 水平(jj-js)
            new BoardRegion(0, 9, 0, 9, "region3"),    // 垂直(ja-jj) 水平(jj-aa)
            new BoardRegion(0, 9, 9, 18, "region4"),   // 垂直(js-jj) 水平(jj-aa)
            // 如果需要第四個區域，可以在這裡添加
        };

        foreach (var region in regions)
        {
            GenerateRegionImage(region);
        }
    }
    private void GenerateRegionImage(BoardRegion region)
    {
        int regionWidth = (region.EndX - region.StartX + 1) * CELL_SIZE + 2 * MARGIN;
        int regionHeight = (region.EndY - region.StartY + 1) * CELL_SIZE + 2 * MARGIN;

        using (var surface = SKSurface.Create(new SKImageInfo(regionWidth, regionHeight)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(new SKColor(220, 179, 92));
            
            // 使用 SKPaint 替换所有的 Pen 和 Brush
            using (var borderPaint = new SKPaint
            {
                Color = SKColors.Black,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            })
            {
                // 繪製網格線和邊框
                canvas.DrawRect(new SKRect(MARGIN, MARGIN, (region.EndX - region.StartX) * CELL_SIZE + MARGIN, (region.EndY - region.StartY) * CELL_SIZE + MARGIN), borderPaint);

                using (var gridPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    StrokeWidth = 1,
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true
                })
                {
                    // 垂直線
                    for (int i = 1; i < (region.EndX - region.StartX); i++)
                    {
                        int linePos = MARGIN + i * CELL_SIZE;
                        canvas.DrawLine(linePos, MARGIN, linePos, MARGIN + (region.EndY - region.StartY) * CELL_SIZE, gridPaint);
                    }

                    // 水平線
                    for (int i = 1; i < (region.EndY - region.StartY); i++)
                    {
                        int linePos = MARGIN + i * CELL_SIZE;
                        canvas.DrawLine(MARGIN, linePos, MARGIN + (region.EndX - region.StartX) * CELL_SIZE, linePos, gridPaint);
                    }
                }

                // 繪製星位點（如果在區域內）
                using (var starPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    int[] starPoints = { 3, 9, 15 };
                    foreach (int x in starPoints)
                    {
                        if (x >= region.StartX && x <= region.EndX)
                        {
                            foreach (int y in starPoints)
                            {
                                if (y >= region.StartY && y <= region.EndY)
                                {
                                    int px = MARGIN + (x - region.StartX) * CELL_SIZE;
                                    int py = MARGIN + (y - region.StartY) * CELL_SIZE;
                                    canvas.DrawCircle(px, py, 3, starPaint);
                                }
                            }
                        }
                    }
                }

                // 繪製座標標籤
                using (var labelPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 10,
                    IsAntialias = true
                })
                using (var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    IsAntialias = true
                })
                {
                    for (int i = 0; i <= region.EndX - region.StartX; i++)
                    {
                        string label = ((char)('A' + region.StartX + i)).ToString();
                        canvas.DrawText(label, new SKPoint(MARGIN + i * CELL_SIZE, 10), textPaint);
                    }

                    for (int i = 0; i <= region.EndY - region.StartY; i++)
                    {
                        string label = ((char)('A' + region.StartY + i)).ToString();
                        canvas.DrawText(label, new SKPoint(10, MARGIN + i * CELL_SIZE), textPaint);
                    }
                }

                // 繪製區域內的棋子
                foreach (var stone in stones)
                {
                    if (stone.X >= region.StartX && stone.X <= region.EndX &&
                        stone.Y >= region.StartY && stone.Y <= region.EndY)
                    {
                        int centerX = MARGIN + (stone.X - region.StartX) * CELL_SIZE;
                        int centerY = MARGIN + (stone.Y - region.StartY) * CELL_SIZE;

                        if (stone.IsBlack)
                        {
                            using (var stonePaint = new SKPaint
                            {
                                Color = SKColors.Black,
                                Style = SKPaintStyle.Fill,
                                IsAntialias = true
                            })
                            {
                                canvas.DrawCircle(centerX, centerY, STONE_SIZE / 2, stonePaint);
                            }
                        }
                        else
                        {
                            using (var stonePaint = new SKPaint
                            {
                                Color = SKColors.White,
                                Style = SKPaintStyle.Fill,
                                IsAntialias = true
                            })
                            using (var stoneOutlinePaint = new SKPaint
                            {
                                Color = SKColors.Black,
                                StrokeWidth = 1,
                                Style = SKPaintStyle.Stroke,
                                IsAntialias = true
                            })
                            {
                                canvas.DrawCircle(centerX, centerY, STONE_SIZE / 2, stonePaint);
                                canvas.DrawCircle(centerX, centerY, STONE_SIZE / 2, stoneOutlinePaint);
                            }
                        }
                    }
                }
            }

            // 保存图片
            using (var image = surface.Snapshot())
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite($"goboard_{region.Name}.png"))
            {
                data.SaveTo(stream);
            }
        }
    }
    // public void GenerateImage(string outputPath)
    // {
    //     int imageSize = BOARD_SIZE * CELL_SIZE + 2 * MARGIN;
    //     using (var bitmap = new Bitmap(imageSize, imageSize))
    //     using (var graphics = Graphics.FromImage(bitmap))
    //     {
    //         graphics.Clear(Color.FromArgb(220, 179, 92));
    //         graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

    //         // 繪製邊界和網格線
    //         using (var pen = new Pen(Color.Black, 2)) // 邊界線較粗
    //         {
    //             // 外邊框
    //             graphics.DrawRectangle(pen, 
    //                 MARGIN, MARGIN, 
    //                 (BOARD_SIZE - 1) * CELL_SIZE, 
    //                 (BOARD_SIZE - 1) * CELL_SIZE);
    //         }

    //         using (var pen = new Pen(Color.Black, 1)) // 內部網格線較細
    //         {
    //             // 垂直線
    //             for (int i = 1; i < BOARD_SIZE - 1; i++)
    //             {
    //                 int linePos = MARGIN + i * CELL_SIZE;
    //                 graphics.DrawLine(pen, 
    //                     linePos, MARGIN,
    //                     linePos, MARGIN + (BOARD_SIZE - 1) * CELL_SIZE);
    //             }

    //             // 水平線
    //             for (int i = 1; i < BOARD_SIZE - 1; i++)
    //             {
    //                 int linePos = MARGIN + i * CELL_SIZE;
    //                 graphics.DrawLine(pen,
    //                     MARGIN, linePos,
    //                     MARGIN + (BOARD_SIZE - 1) * CELL_SIZE, linePos);
    //             }
    //         }

    //         // 繪製星位點
    //         using (var brush = new SolidBrush(Color.Black))
    //         {
    //             int[] starPoints = { 3, 9, 15 };
    //             foreach (int x in starPoints)
    //             {
    //                 foreach (int y in starPoints)
    //                 {
    //                     int px = MARGIN + x * CELL_SIZE;
    //                     int py = MARGIN + y * CELL_SIZE;
    //                     graphics.FillEllipse(brush, px - 3, py - 3, 6, 6);
    //                 }
    //             }
    //         }

    //         // 繪製座標標籤
    //         using (var font = new Font("Arial", 10))
    //         using (var brush = new SolidBrush(Color.Black))
    //         {
    //             for (int i = 0; i < BOARD_SIZE; i++)
    //             {
    //                 string label = ((char)('A' + i)).ToString();
    //                 // 頂部座標
    //                 graphics.DrawString(label, font, brush,
    //                     MARGIN + i * CELL_SIZE, 10,
    //                     new StringFormat { Alignment = StringAlignment.Center });
    //                 // 左側座標
    //                 graphics.DrawString(label, font, brush,
    //                     10, MARGIN + i * CELL_SIZE,
    //                     new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
    //             }
    //         }

    //         // 繪製棋子
    //         foreach (var stone in stones)
    //         {
    //             int centerX = MARGIN + stone.X * CELL_SIZE;
    //             int centerY = MARGIN + stone.Y * CELL_SIZE;

    //             if (stone.IsBlack)
    //             {
    //                 using (var brush = new SolidBrush(Color.Black))
    //                 {
    //                     graphics.FillEllipse(brush, 
    //                         centerX - STONE_SIZE/2, 
    //                         centerY - STONE_SIZE/2, 
    //                         STONE_SIZE, STONE_SIZE);
    //                 }
    //             }
    //             else
    //             {
    //                 using (var brush = new SolidBrush(Color.White))
    //                 using (var pen = new Pen(Color.Black, 1))
    //                 {
    //                     graphics.FillEllipse(brush, 
    //                         centerX - STONE_SIZE/2, 
    //                         centerY - STONE_SIZE/2, 
    //                         STONE_SIZE, STONE_SIZE);
    //                     graphics.DrawEllipse(pen, 
    //                         centerX - STONE_SIZE/2, 
    //                         centerY - STONE_SIZE/2, 
    //                         STONE_SIZE, STONE_SIZE);
    //                 }
    //             }
    //         }

    //         bitmap.Save(outputPath, ImageFormat.Png);
    //     }
    // }
}

class Program
{
    static void Main(string[] args)
    {
        string sgfContent = "(;GM[1]SZ[19]CA[UTF-8]ST[0]AP[GOWrite:3.2.0]FF[4]FG[259:]AB[qq][qn][rq][qr][qo][qp][qm][rp][rm][sm]AW[sq][rk][pr][pl][rr][ql][ps][qs][pm][ss][pn][sn][po][ro][pp][pq]MULTIGOGM[1]PM[2])";

        string sgfContent2 = "(;FF[4]CA[UTF-8]ST[0]GM[1]AP[GOWrite:3.2.0]SZ[19]PM[2]AW[rd][rk][pr][ea][rr][co][eo][pm][la][dc][ec][bq][eq][po][lc][be][fq][mc][ce][gq][ro][nc][oc][rc][pq][sq][pl][bb][ql][ps][eb][qs][ss][bp][dp][pn][lb][cd][sn][ar][sb][pp][gr][pd][qd]AB[qr][ac][qm][ma][cc][rm][sm][cq][dq][qo][pc][qc][fs][qq][rq][cb][db][bd][qn][mb][nb][ob][br][rb][er][fr][qp][rp]MULTIGOGM[1]FG[259:])";
         var analyzer = new SgfAnalyzer();
        analyzer.ParseSgf(sgfContent2);
        
        // 生成不同區域的圖片
        analyzer.GenerateRegionImages();
    }
}