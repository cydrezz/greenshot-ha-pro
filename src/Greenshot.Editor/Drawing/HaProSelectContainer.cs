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
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Helpers;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// ha-pro: the rectangle of the "select area" tool.
    /// It is drawn like the crop rectangle, but confirming it does not cut the image:
    /// the pixels inside are copied into a new, movable ImageContainer at the same place,
    /// see <see cref="Surface.ConfirmHaProSelection(bool)"/>. Never exported, never undoable.
    /// </summary>
    [Serializable]
    public class HaProSelectContainer : DrawableContainer
    {
        public HaProSelectContainer(ISurface parent) : base(parent)
        {
            Init();
        }

        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
            Init();
        }

        private void Init()
        {
            CreateDefaultAdorners();
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.FLAGS, FieldFlag.CONFIRMABLE);
        }

        public override void Invalidate()
        {
            _parent?.Invalidate();
        }

        /// <summary>
        /// The whole image, like the crop container, so the border is always repainted completely
        /// </summary>
        public override NativeRect DrawingBounds
        {
            get
            {
                if (_parent?.Image is { } image)
                {
                    return new NativeRect(0, 0, image.Width, image.Height);
                }

                return NativeRect.Empty;
            }
        }

        public override void Draw(Graphics g, RenderMode rm)
        {
            if (_parent == null || rm != RenderMode.EDIT)
            {
                return;
            }

            var selection = new NativeRect(Left, Top, Width, Height).Normalize();
            using Brush tint = new SolidBrush(Color.FromArgb(50, 0, 120, 215));
            g.FillRectangle(tint, selection);
            DrawSelectionBorder(g, new NativeRect(selection.Left - 1, selection.Top - 1, selection.Width + 1, selection.Height + 1));
        }

        public override bool HasContextMenu => false;

        public override bool HandleMouseMove(int x, int y)
        {
            Invalidate();
            _boundsAfterResize = new NativeRectFloat(
                _boundsBeforeResize.Left, _boundsBeforeResize.Top,
                x - _boundsAfterResize.Left, y - _boundsAfterResize.Top);
            _boundsAfterResize = ScaleHelper.Scale(_boundsAfterResize, x, y, GetAngleRoundProcessor());
            ApplyBounds(_boundsAfterResize);
            Invalidate();
            return true;
        }

        /// <summary>
        /// The selection rectangle itself is no undo step, only the copy it creates is one
        /// </summary>
        public override bool IsUndoable => false;

        public override bool IsConfirmable => true;
    }
}
