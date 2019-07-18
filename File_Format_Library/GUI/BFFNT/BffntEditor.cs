﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Toolbox.Library.Forms;
using Toolbox.Library;

namespace FirstPlugin.Forms
{
    public class ImagePaenl : STPanel
    {
        public ImagePaenl()
        {
            this.SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.DoubleBuffer,
            true);
        }
    }

    public partial class BffntEditor : STUserControl, IFIleEditor
    {
        public BffntEditor()
        {
            InitializeComponent();
        }

        public List<IFileFormat> GetFileFormats()
        {
            return new List<IFileFormat>() { FileFormat };
        }

        private Image PanelImage { get; set; }

        private FFNT ActiveFile;
        private BFFNT FileFormat;

        public void LoadFontFile(BFFNT fontFile)
        {
            FileFormat = fontFile;
            ActiveFile = fontFile.bffnt;

            fontTypeCB.Bind(typeof(FINF.FontType), ActiveFile.FontSection, "Type");
            fontTypeCB.SelectedItem = ActiveFile.FontSection.Type;

            encodingTypeCB.Bind(typeof(FINF.CharacterCode), ActiveFile.FontSection, "CharEncoding");
            encodingTypeCB.SelectedItem = ActiveFile.FontSection.CharEncoding;

            lineFeedUD.Bind(ActiveFile.FontSection, "LineFeed");
            leftSpacingUD.Bind(ActiveFile.FontSection, "DefaultLeftWidth");
            charWidthUD.Bind(ActiveFile.FontSection, "DefaultCharWidth");
            glyphWidthCB.Bind(ActiveFile.FontSection, "DefaultGlyphWidth");
            ascentUD.Bind(ActiveFile.FontSection, "Ascent");
            fontWidthUD.Bind(ActiveFile.FontSection, "Width");
            fontHeightUD.Bind(ActiveFile.FontSection, "Height");

            ReloadCharacterCodes();
            ReloadTextures();
        }

        private void ReloadCharacterCodes()
        {
            foreach (char entry in ActiveFile.FontSection.CodeMapDictionary.Keys)
                characterCodeCB.Items.Add(entry);

            if (ActiveFile.FontSection.CodeMapDictionary.Count > 0)
                characterCodeCB.SelectedIndex = 0;
        }

        private void ReloadTextures()
        {
            imagesCB.Items.Clear();
            var textureGlyph = ActiveFile.FontSection.TextureGlyph;
            for (int i = 0; i < textureGlyph.SheetCount; i++)
                imagesCB.Items.Add($"Image {i}");

            if (textureGlyph.SheetCount > 0)
                imagesCB.SelectedIndex = 0;
        }

        private void imagePanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (ActiveFile == null) return;

            int ImageIndex = imagesCB.SelectedIndex;

                if (e.Button == MouseButtons.Right && ImageIndex != -1)
                {
                    var image = ActiveFile.FontSection.TextureGlyph.GetImageSheet(ImageIndex);

                imageMenuStrip.Items.Clear();
                imageMenuStrip.Items.Add(new ToolStripMenuItem("Export", null, ExportImageAction, Keys.Control | Keys.E));
                imageMenuStrip.Items.Add(new ToolStripMenuItem("Replace", null, ReplaceImageAction, Keys.Control | Keys.R));
                imageMenuStrip.Items.Add(new ToolStripMenuItem("Copy", null, CopyImageAction, Keys.Control | Keys.C));
                imageMenuStrip.Show(Cursor.Position);
            }
        }

        private void imagesCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ImageIndex = imagesCB.SelectedIndex;
            if (ImageIndex != -1)
                UpdateImagePanel(ImageIndex);
        }

        private void UpdateImagePanel(int ImageIndex)
        {
            var image = ActiveFile.FontSection.TextureGlyph.GetImageSheet(ImageIndex);
            bool IsBntx = ActiveFile.FontSection.TextureGlyph.BinaryTextureFile != null;

            if (IsBntx)
            {
                PanelImage = image.GetBitmap(ImageIndex);
            }
            else
            {
                PanelImage = image.GetBitmap();
            }

            if (PanelImage != null)
            {
                PanelImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
            }

            FillCells();

            imagePanel.Refresh();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Rectangle ee = new Rectangle(10, 10, 30, 30);
            using (Pen pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen, ee);
            }
        }

        
        private void ReplaceImageAction(object sender, EventArgs e)
        {
            int ImageIndex = imagesCB.SelectedIndex;
            if (ImageIndex != -1)
            {
                var image = ActiveFile.FontSection.TextureGlyph.GetImageSheet(ImageIndex);
                bool IsBntx = ActiveFile.FontSection.TextureGlyph.BinaryTextureFile != null;

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = image.ReplaceFilter;
                ofd.Multiselect = false;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    image.Replace(ofd.FileName);
                }

                UpdateImagePanel(ImageIndex);
            }
        }

        private void ExportImageAction(object sender, EventArgs e)
        {
            int ImageIndex = imagesCB.SelectedIndex;
            if (ImageIndex != -1)
            {
                var image = ActiveFile.FontSection.TextureGlyph.GetImageSheet(ImageIndex);
                bool IsBntx = ActiveFile.FontSection.TextureGlyph.BinaryTextureFile != null;

                var args = new STGenericTexture.ImageExportArguments()
                {
                    FlipY = true,
                };

                if (IsBntx)
                    image.ExportArrayImage(ImageIndex, args);
                else
                    image.ExportImage(args);
            }
        }

        public class FontCell
        {
            public Rectangle DrawnRectangle;

            public Color Color { get; set; }

            public FontCell()
            {
                Color = Color.Cyan;
            }

            public bool IsHit(int X, int Y)
            {
                if (DrawnRectangle == null) return false;

                if ((X > DrawnRectangle.X) && (X < DrawnRectangle.X + DrawnRectangle.Width) &&
                    (Y > DrawnRectangle.Y) && (Y < DrawnRectangle.Y + DrawnRectangle.Height))
                    return true;
                else
                    return false;
            }

            public bool IsSelected { get; private set; }

            public void Select()
            {
                Color = Color.Blue;
                IsSelected = true;
            }

            public void Unselect()
            {
                Color = Color.Cyan;
                IsSelected = false;
            }
        }

        private FontCell[] FontCells;

        private void LoadGlyphs()
        {
            var textureGlyph = ActiveFile.FontSection.TextureGlyph;

            for (int c = 0; c < (int)textureGlyph.ColumnCount; c++)
            {
                for (int r = 0; r < (int)textureGlyph.RowCount; r++)
                {
                }
            }
        }

        public GlyphImage[] GlyphImages;

        public class GlyphImage
        {
            public Image Image { get; set; }
        }

        private void FillCells()
        {
            List<GlyphImage> images = new List<GlyphImage>();
            List<FontCell> Cells = new List<FontCell>();

            var textureGlyph = ActiveFile.FontSection.TextureGlyph;
            var fontSection = ActiveFile.FontSection;

            PanelImage = BitmapExtension.Resize(PanelImage, textureGlyph.SheetWidth, textureGlyph.SheetHeight);

            Console.WriteLine($"ColumnCount {textureGlyph.ColumnCount}");
            Console.WriteLine($"RowCount {textureGlyph.RowCount}");

            int y = 0;
            for (int c = 0; c < (int)textureGlyph.RowCount; c++)
            {
                int x = 0;
                for (int r = 0; r < (int)textureGlyph.ColumnCount; r++)
                {
                    var rect = new Rectangle(x, y, textureGlyph.CellWidth, textureGlyph.CellHeight);

                    Cells.Add(new FontCell()
                    {
                        DrawnRectangle = rect,
                    });

                 /*   var glyphImage = new GlyphImage();
                    glyphImage.Image = CopyRegionIntoImage(bitmap, rect);
                    glyphImage.Image.Save($"Glpyh{c} {r}.png");
                    images.Add(glyphImage);*/


                    x += (int)textureGlyph.CellWidth;
                }
                y += (int)textureGlyph.CellHeight;
            }

            GlyphImages = images.ToArray();
            FontCells = Cells.ToArray();
        }

        private static Bitmap CopyRegionIntoImage(Image srcBitmap, Rectangle srcRegion)
        {
            Bitmap destBitmap = new Bitmap(srcRegion.Width, srcRegion.Height);
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, new Rectangle(0,0,destBitmap.Width, destBitmap.Height), srcRegion, GraphicsUnit.Pixel);
            }
            return destBitmap;
        }

        private void imagePanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.DrawImage(PanelImage, 0.0f, 0.0f);

            if (ActiveFile == null)
                return;

            var textureGlyph = ActiveFile.FontSection.TextureGlyph;

             if (FontCells == null)
                return;

            for (int i = 0; i < FontCells.Length; i++) {
                if (FontCells[i].IsSelected)
                {
                    SolidBrush semiTransBrush = new SolidBrush(Color.FromArgb(70, 0, 255, 255));

                    graphics.DrawRectangle(new Pen(FontCells[i].Color, 1), FontCells[i].DrawnRectangle);
                    graphics.FillRectangle(semiTransBrush, FontCells[i].DrawnRectangle);
                }
            }

            graphics.ScaleTransform(textureGlyph.SheetWidth, textureGlyph.SheetHeight);
        }

        private void CopyImageAction(object sender, EventArgs e)
        {
            if (PanelImage != null)
                Clipboard.SetImage(PanelImage);
        }

        bool isMouseDown = false;
        private void imagePanel_MouseDown(object sender, MouseEventArgs e) {
            isMouseDown = true;
        }

        private void imagePanel_MouseUp(object sender, MouseEventArgs e) {
            isMouseDown = false;
        }

        private void imagePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (FontCells != null)
            {
                for (int i = 0; i < FontCells.Length; i++)
                {
                    if (FontCells[i] == null) continue;

                    if (FontCells[i].IsHit(e.X, e.Y))
                        FontCells[i].Select();
                    else 
                        FontCells[i].Unselect();
                }

                imagePanel.Refresh();
            }
        }

        private void imagePanel_MouseLeave(object sender, EventArgs e)
        {
            isMouseDown = false;

            if (FontCells != null)
            {
                for (int i = 0; i < FontCells.Length; i++)
                    FontCells[i].Unselect();

                imagePanel.Refresh();
            }
        }
    }
}
