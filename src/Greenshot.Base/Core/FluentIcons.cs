/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using Greenshot.Base.IniFile;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// ha-pro: renders monochrome icons from the "Segoe Fluent Icons" font (part of Windows 11)
    /// at any pixel size. Used instead of the 16px Fugue bitmaps when the user runs with large
    /// icons (BaseIconSize >= 32), where the bitmaps would be upscaled and blurry.
    /// Falls back silently (returns null) when the font is not installed, so the bitmaps stay.
    /// </summary>
    public static class FluentIcons
    {
        public const string FontName = "Segoe Fluent Icons";

        // Glyphs verified visually on Windows 11 (24.08.2026), see docs in the ha-pro repo.
        public const char Save = '';
        public const char SaveAs = '';
        public const char Copy = '';
        public const char Cut = '';
        public const char Paste = '';
        public const char Clipboard = '';
        public const char Print = '';
        public const char Delete = '';
        public const char Undo = '';
        public const char Redo = '';
        public const char Settings = '';
        public const char Cursor = '';
        public const char Rectangle = '';
        public const char Ellipse = '';
        public const char Line = '';
        public const char Arrow = '';
        public const char Pen = '';
        public const char Text = '';
        public const char SpeechBubble = '';
        public const char Highlight = '';
        public const char Obfuscate = '';
        public const char Pixelize = '';
        public const char Crop = '';
        public const char CropVertical = '';
        public const char CropHorizontal = '';
        public const char Resize = '';
        public const char Rotate = '';
        public const char Effects = '';
        public const char Help = '';
        public const char ZoomIn = '';
        public const char ZoomOut = '';
        public const char ZoomFit = '';
        public const char ZoomActual = '';
        public const char Zoom = '';
        public const char Bold = '';
        public const char Italic = '';
        public const char AlignLeft = '';
        public const char AlignCenter = '';
        public const char AlignRight = '';
        public const char HalfFilledTop = '';
        public const char HalfFilledLeft = '';
        public const char Ok = '';
        public const char Cancel = '';
        public const char Close = '';
        public const char Mail = '';

        public const char Fill = '';

        private static readonly Lazy<bool> Available = new Lazy<bool>(() =>
        {
            try
            {
                using (var font = new Font(FontName, 10f))
                {
                    return string.Equals(font.Name, FontName, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        });

        /// <summary>
        /// Resource names of GreenshotResources that get a Fluent replacement (destinations, tray menu).
        /// </summary>
        private static readonly Dictionary<string, char> ResourceGlyphs = new Dictionary<string, char>
        {
            { "Save.Image", Save },
            { "Clipboard.Image", Clipboard },
            { "Printer.Image", Print },
            { "Email.Image", Mail },
            { "Close.Image", Close },
        };

        private static readonly Dictionary<string, Image> ResourceCache = new Dictionary<string, Image>();

        /// <summary>
        /// True when the font is installed.
        /// </summary>
        public static bool IsAvailable => Available.Value;

        /// <summary>
        /// True when Fluent icons should be used: font installed and the user runs large icons.
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
                if (!IsAvailable)
                {
                    return false;
                }

                var coreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
                return coreConfiguration != null && coreConfiguration.UseLargeIcons;
            }
        }

        /// <summary>
        /// Default icon color, follows the system text color.
        /// </summary>
        public static Color DefaultColor => SystemColors.ControlText;

        /// <summary>
        /// Replacement for a GreenshotResources image, or null when there is none (keep the bitmap).
        /// </summary>
        public static Image ForResource(string resourceName)
        {
            if (resourceName == null || !ResourceGlyphs.TryGetValue(resourceName, out var glyph) || !IsEnabled)
            {
                return null;
            }

            lock (ResourceCache)
            {
                if (!ResourceCache.TryGetValue(resourceName, out var image))
                {
                    var coreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
                    image = Render(glyph, coreConfiguration.IconSize, DefaultColor);
                    // ha-pro: lets the dark editor re-render the glyph in its own color (HaProTheme.ForEditor)
                    image.Tag = glyph;
                    ResourceCache[resourceName] = image;
                }

                return image;
            }
        }

        /// <summary>
        /// Render one glyph centered into a transparent bitmap.
        /// </summary>
        /// <param name="glyph">code point of the Segoe Fluent Icons font</param>
        /// <param name="size">bitmap size in pixels</param>
        /// <param name="color">glyph color</param>
        /// <param name="flipX">mirror horizontally (e.g. rotate counter-clockwise from the clockwise glyph)</param>
        /// <param name="rotateDegrees">rotate clockwise around the center</param>
        public static Bitmap Render(char glyph, Size size, Color color, bool flipX = false, float rotateDegrees = 0f)
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                ApplyTransform(graphics, size, flipX, rotateDegrees);
                DrawGlyph(graphics, glyph, new RectangleF(0, 0, size.Width, size.Height), size.Height * 0.70f, color);
            }

            return bitmap;
        }

        /// <summary>
        /// Render one glyph into the upper part of the bitmap, leaving the bottom rows free
        /// (used for the color buttons, which paint their color bar there).
        /// </summary>
        /// <param name="bottomFree">rows at the bottom to keep empty</param>
        public static Bitmap RenderAboveBar(char glyph, Size size, Color color, int bottomFree)
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                float cellHeight = Math.Max(1, size.Height - bottomFree);
                DrawGlyph(graphics, glyph, new RectangleF(0, 0, size.Width, cellHeight), cellHeight * 0.80f, color);
            }

            return bitmap;
        }

        /// <summary>
        /// Counter icon: circle with the number inside (used for the step-label tool).
        /// </summary>
        public static Bitmap RenderCounter(int number, Size size, Color color)
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                float penWidth = Math.Max(1.5f, size.Height / 16f);
                float inset = penWidth + size.Height * 0.06f;
                var circle = new RectangleF(inset, inset, size.Width - 2 * inset, size.Height - 2 * inset);
                using (var pen = new Pen(color, penWidth))
                {
                    graphics.DrawEllipse(pen, circle);
                }

                string text = number > 20 ? "20+" : number.ToString();
                float fontSize = size.Height * (text.Length >= 3 ? 0.30f : text.Length == 2 ? 0.40f : 0.48f);
                using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(color))
                using (var format = new StringFormat(StringFormat.GenericTypographic)
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    graphics.DrawString(text, font, brush, circle, format);
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Drop-shadow icon: a square with a gray offset copy behind it.
        /// </summary>
        public static Bitmap RenderShadow(Size size, Color color)
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float penWidth = Math.Max(1.5f, size.Height / 16f);
                float offset = size.Height * 0.18f;
                float margin = size.Height * 0.12f;
                var front = new RectangleF(margin, margin, size.Width - 2 * margin - offset, size.Height - 2 * margin - offset);
                var back = new RectangleF(front.X + offset, front.Y + offset, front.Width, front.Height);
                using (var shadowBrush = new SolidBrush(Color.FromArgb(110, color)))
                {
                    graphics.FillRectangle(shadowBrush, back);
                }

                using (var fillBrush = new SolidBrush(SystemColors.Window))
                using (var pen = new Pen(color, penWidth))
                {
                    graphics.FillRectangle(fillBrush, front);
                    graphics.DrawRectangle(pen, front.X, front.Y, front.Width, front.Height);
                }
            }

            return bitmap;
        }

        private static void ApplyTransform(Graphics graphics, Size size, bool flipX, float rotateDegrees)
        {
            if (flipX)
            {
                graphics.TranslateTransform(size.Width, 0);
                graphics.ScaleTransform(-1f, 1f);
            }

            if (Math.Abs(rotateDegrees) > 0.01f)
            {
                graphics.TranslateTransform(size.Width / 2f, size.Height / 2f);
                graphics.RotateTransform(rotateDegrees);
                graphics.TranslateTransform(-size.Width / 2f, -size.Height / 2f);
            }
        }

        private static void DrawGlyph(Graphics graphics, char glyph, RectangleF cell, float fontPixels, Color color)
        {
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var font = new Font(FontName, fontPixels, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                graphics.DrawString(glyph.ToString(), font, brush, cell, format);
            }
        }
    }
}
