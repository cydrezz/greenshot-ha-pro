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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Greenshot.Base.IniFile;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// ha-pro: dark mode for the image editor, painted by hand on .NET Framework 4.7.2
    /// (title bar via DWM, tool strips via an own renderer, plain controls via colors).
    /// Switched by the ini value HaProDarkMode (section Core), default off = upstream look.
    /// Everything here only paints; capture, save and clipboard are not touched.
    /// </summary>
    public static class HaProTheme
    {
        public static readonly Color Background = Color.FromArgb(32, 32, 32);
        public static readonly Color Canvas = Color.FromArgb(56, 56, 56);
        public static readonly Color Input = Color.FromArgb(45, 45, 45);
        public static readonly Color Border = Color.FromArgb(70, 70, 70);
        public static readonly Color Hover = Color.FromArgb(62, 62, 62);
        public static readonly Color Pressed = Color.FromArgb(82, 82, 82);
        public static readonly Color Checked = Color.FromArgb(74, 74, 74);
        public static readonly Color Text = Color.FromArgb(232, 232, 232);
        public static readonly Color TextDisabled = Color.FromArgb(120, 120, 120);

        private static bool _appModeSet;

        /// <summary>
        /// True when the editor should be painted dark.
        /// </summary>
        public static bool IsDark
        {
            get
            {
                try
                {
                    var coreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
                    return coreConfiguration != null && coreConfiguration.HaProDarkMode;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Color for the Fluent glyph icons inside the editor.
        /// </summary>
        public static Color IconColor => IsDark ? Text : SystemColors.ControlText;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        // undocumented, Windows 10 1903+: 0 = default, 1 = allow dark, 2 = force dark
        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        private static extern int SetPreferredAppMode(int mode);

        /// <summary>
        /// Dark title bar for the window (Windows 10 2004+ / Windows 11), silently ignored elsewhere.
        /// </summary>
        public static void ApplyTitleBar(IntPtr hwnd)
        {
            if (!IsDark || hwnd == IntPtr.Zero)
            {
                return;
            }

            try
            {
                int enabled = 1;
                // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE, 19 = the same before Windows 10 2004
                if (DwmSetWindowAttribute(hwnd, 20, ref enabled, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(hwnd, 19, ref enabled, sizeof(int));
                }
            }
            catch
            {
                // no DWM, keep the light title bar
            }
        }

        /// <summary>
        /// Dark native scroll bars for a scrollable control.
        /// </summary>
        public static void ApplyScrollBars(Control control)
        {
            if (!IsDark || control == null)
            {
                return;
            }

            try
            {
                if (!_appModeSet)
                {
                    _appModeSet = true;
                    SetPreferredAppMode(2);
                }

                SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
            }
            catch
            {
                // older Windows, keep the light scroll bars
            }
        }

        /// <summary>
        /// Same glyph bitmap in the editor icon color. Bitmaps that were not rendered by
        /// FluentIcons.ForResource (colored plugin icons) are returned unchanged.
        /// </summary>
        public static Image ForEditor(Image image)
        {
            if (!IsDark || image == null || !(image.Tag is char glyph))
            {
                return image;
            }

            return FluentIcons.Render(glyph, image.Size, IconColor);
        }

        /// <summary>
        /// Color table for the dark tool strips, menus and the status strip.
        /// </summary>
        public class DarkColorTable : ProfessionalColorTable
        {
            public DarkColorTable()
            {
                UseSystemColors = false;
            }

            public override Color ToolStripGradientBegin => Background;
            public override Color ToolStripGradientMiddle => Background;
            public override Color ToolStripGradientEnd => Background;
            public override Color ToolStripBorder => Background;
            public override Color ToolStripDropDownBackground => Background;
            public override Color ToolStripContentPanelGradientBegin => Background;
            public override Color ToolStripContentPanelGradientEnd => Background;
            public override Color ToolStripPanelGradientBegin => Background;
            public override Color ToolStripPanelGradientEnd => Background;
            public override Color MenuStripGradientBegin => Background;
            public override Color MenuStripGradientEnd => Background;
            public override Color StatusStripGradientBegin => Background;
            public override Color StatusStripGradientEnd => Background;
            public override Color OverflowButtonGradientBegin => Background;
            public override Color OverflowButtonGradientMiddle => Background;
            public override Color OverflowButtonGradientEnd => Background;
            public override Color ImageMarginGradientBegin => Background;
            public override Color ImageMarginGradientMiddle => Background;
            public override Color ImageMarginGradientEnd => Background;
            public override Color ImageMarginRevealedGradientBegin => Background;
            public override Color ImageMarginRevealedGradientMiddle => Background;
            public override Color ImageMarginRevealedGradientEnd => Background;
            public override Color MenuBorder => Border;
            public override Color MenuItemBorder => Border;
            public override Color MenuItemSelected => Hover;
            public override Color MenuItemSelectedGradientBegin => Hover;
            public override Color MenuItemSelectedGradientEnd => Hover;
            public override Color MenuItemPressedGradientBegin => Pressed;
            public override Color MenuItemPressedGradientMiddle => Pressed;
            public override Color MenuItemPressedGradientEnd => Pressed;
            public override Color ButtonSelectedBorder => Border;
            public override Color ButtonSelectedHighlight => Hover;
            public override Color ButtonSelectedHighlightBorder => Border;
            public override Color ButtonSelectedGradientBegin => Hover;
            public override Color ButtonSelectedGradientMiddle => Hover;
            public override Color ButtonSelectedGradientEnd => Hover;
            public override Color ButtonPressedBorder => Border;
            public override Color ButtonPressedHighlight => Pressed;
            public override Color ButtonPressedHighlightBorder => Border;
            public override Color ButtonPressedGradientBegin => Pressed;
            public override Color ButtonPressedGradientMiddle => Pressed;
            public override Color ButtonPressedGradientEnd => Pressed;
            public override Color ButtonCheckedHighlight => Checked;
            public override Color ButtonCheckedHighlightBorder => Border;
            public override Color ButtonCheckedGradientBegin => Checked;
            public override Color ButtonCheckedGradientMiddle => Checked;
            public override Color ButtonCheckedGradientEnd => Checked;
            public override Color CheckBackground => Checked;
            public override Color CheckSelectedBackground => Pressed;
            public override Color CheckPressedBackground => Pressed;
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Background;
            public override Color GripDark => Border;
            public override Color GripLight => Background;
        }

        /// <summary>
        /// Renderer for the dark tool strips: light text and arrows, own check mark,
        /// dimmed instead of washed-out disabled icons, no strip border.
        /// </summary>
        public class DarkToolStripRenderer : ToolStripProfessionalRenderer
        {
            public DarkToolStripRenderer() : base(new DarkColorTable())
            {
                RoundedEdges = false;
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // only drop-downs get a border, the strips stay flat like upstream
                if (e.ToolStrip is ToolStripDropDown)
                {
                    using (var pen = new Pen(Border))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                    }
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? Text : TextDisabled;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = e.Item == null || e.Item.Enabled ? Text : TextDisabled;
                base.OnRenderArrow(e);
            }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                var rect = e.ImageRectangle;
                rect.Inflate(2, 2);
                using (var brush = new SolidBrush(Checked))
                using (var pen = new Pen(Border))
                {
                    e.Graphics.FillRectangle(brush, rect);
                    e.Graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
                }

                if (e.Item.Image != null)
                {
                    // the item image itself is painted by OnRenderItemImage
                    return;
                }

                var state = e.Graphics.Save();
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float w = e.ImageRectangle.Width, h = e.ImageRectangle.Height;
                float x = e.ImageRectangle.X, y = e.ImageRectangle.Y;
                using (var pen = new Pen(e.Item.Enabled ? Text : TextDisabled, Math.Max(1.6f, h / 10f)))
                {
                    e.Graphics.DrawLines(pen, new[]
                    {
                        new PointF(x + w * 0.22f, y + h * 0.52f),
                        new PointF(x + w * 0.42f, y + h * 0.72f),
                        new PointF(x + w * 0.78f, y + h * 0.30f)
                    });
                }

                e.Graphics.Restore(state);
            }

            protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
            {
                if (e.Item.Enabled || e.Image == null)
                {
                    base.OnRenderItemImage(e);
                    return;
                }

                // the stock disabled look is a light wash that glows on a dark strip
                using (var attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(new ColorMatrix { Matrix33 = 0.35f });
                    e.Graphics.DrawImage(e.Image, e.ImageRectangle, 0, 0, e.Image.Width, e.Image.Height, GraphicsUnit.Pixel, attributes);
                }
            }
        }
    }
}
