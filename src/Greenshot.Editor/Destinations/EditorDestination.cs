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
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Forms;
using log4net;

namespace Greenshot.Editor.Destinations
{
    /// <summary>
    /// Description of EditorDestination.
    /// </summary>
    public class EditorDestination : AbstractDestination
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(EditorDestination));
        private static readonly EditorConfiguration editorConfiguration = IniConfig.GetIniSection<EditorConfiguration>();
        public const string DESIGNATION = "Editor";
        private readonly IImageEditor editor;
        private static readonly Image greenshotIcon = GreenshotResources.GetGreenshotIcon().ToBitmap();

        public EditorDestination()
        {
            // Do not remove, is needed for the framework
        }

        public EditorDestination(IImageEditor editor)
        {
            this.editor = editor;
        }

        public override string Designation => DESIGNATION;

        public override string Description
        {
            get
            {
                if (editor == null)
                {
                    return Language.GetString(LangKey.settings_destination_editor);
                }

                return Language.GetString(LangKey.settings_destination_editor) + " - " + editor.CaptureDetails.Title?.Substring(0, Math.Min(20, editor.CaptureDetails.Title.Length));
            }
        }

        public override int Priority => 1;

        public override bool IsDynamic => true;

        public override Image DisplayIcon => greenshotIcon;

        public override IEnumerable<IDestination> DynamicDestinations()
        {
            foreach (IImageEditor someEditor in ImageEditorForm.Editors)
            {
                yield return new EditorDestination(someEditor);
            }
        }

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            ExportInformation exportInformation = new ExportInformation(Designation, Description);
            // Make sure we collect the garbage before opening the screenshot
            GC.Collect();
            GC.WaitForPendingFinalizers();

            bool modified = surface.Modified;
            if (editor == null)
            {
                if (editorConfiguration.ReuseEditor)
                {
                    // ha-pro: a new capture never opens a second editor window.
                    // Editors with unsaved changes are saved silently (FileNoDialog overwrites the
                    // file the capture was already written to), the first editor gets the new
                    // capture, every other editor is closed. Only an editor whose auto-save failed
                    // is left untouched, so no work is ever lost.
                    // Snapshot: closing an editor removes it from ImageEditorForm.Editors.
                    foreach (IImageEditor openedEditor in new List<IImageEditor>(ImageEditorForm.Editors))
                    {
                        if (openedEditor.Surface.Modified && !HaProAutoSave(openedEditor))
                        {
                            continue;
                        }

                        if (!exportInformation.ExportMade)
                        {
                            openedEditor.Surface = surface;
                            exportInformation.ExportMade = true;
                        }
                        else if (openedEditor is System.Windows.Forms.Form editorWindow)
                        {
                            editorWindow.Close();
                        }
                    }
                }

                if (!exportInformation.ExportMade)
                {
                    try
                    {
                        ImageEditorForm editorForm = new ImageEditorForm(surface, !surface.Modified); // Output made??

                        if (!string.IsNullOrEmpty(captureDetails.Filename))
                        {
                            editorForm.SetImagePath(captureDetails.Filename);
                        }

                        editorForm.Show();
                        editorForm.Activate();
                        LOG.Debug("Finished opening Editor");
                        exportInformation.ExportMade = true;
                    }
                    catch (Exception e)
                    {
                        LOG.Error(e);
                        exportInformation.ErrorMessage = e.Message;
                    }
                }
            }
            else
            {
                try
                {
                    using (Image image = surface.GetImageForExport())
                    {
                        editor.Surface.AddImageContainer(image, 10, 10);
                    }

                    exportInformation.ExportMade = true;
                }
                catch (Exception e)
                {
                    LOG.Error(e);
                    exportInformation.ErrorMessage = e.Message;
                }
            }

            ProcessExport(exportInformation, surface);
            // Workaround for the modified flag when using the editor.
            surface.Modified = modified;
            return exportInformation;
        }

        /// <summary>
        /// ha-pro: save the (modified) content of an open editor without any dialog.
        /// </summary>
        /// <returns>true when the save succeeded and the editor is no longer modified</returns>
        private static bool HaProAutoSave(IImageEditor openedEditor)
        {
            try
            {
                var saved = DestinationHelper.ExportCapture(false, WellKnownDestinations.FileNoDialog, openedEditor.Surface, openedEditor.CaptureDetails);
                if (saved != null && saved.ExportMade)
                {
                    openedEditor.Surface.Modified = false;
                    LOG.InfoFormat("ha-pro: auto-saved editor content to {0}", saved.Filepath);
                    return true;
                }

                LOG.WarnFormat("ha-pro: auto-save of editor content failed: {0}", saved?.ErrorMessage);
            }
            catch (Exception e)
            {
                LOG.Error("ha-pro: auto-save of editor content failed", e);
            }

            return false;
        }
    }
}